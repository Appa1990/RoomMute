using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
namespace RoomMute.Services;

/// <summary>Native hotkey notifications while idle; release polling only while the key is held.</summary>
public sealed class GlobalPushToTalkService : IDisposable
{
    private const int HotkeyId = 0x4D31;
    private const int HotkeyMessage = 0x0312;
    private const uint NoRepeat = 0x4000;
    private readonly HwndSource source;
    private readonly DispatcherTimer releaseTimer;
    private readonly Func<int, bool> isDown;
    private ShortcutGesture gesture;
    private bool registered, held, disposed;
    private readonly byte[] mousePacket = new byte[64];
    public event Action<bool>? HeldChanged;
    public bool IsRegistered => registered;
    public bool IsReleasePolling => releaseTimer.IsEnabled;
    public GlobalPushToTalkService(Func<int, bool>? keyState = null)
    {
        isDown = keyState ?? (key => GetAsyncKeyState(key) < 0);
        var parameters = new HwndSourceParameters("RoomMute push-to-talk")
        {
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE: no visible window is required.
            WindowStyle = 0, Width = 0, Height = 0
        };
        source = new HwndSource(parameters);
        source.AddHook(HandleMessage);
        releaseTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(30)
        };
        releaseTimer.Tick += (_, _) => { if (!gesture.IsHeld(isDown)) Release(); };
    }
    public bool Configure(string key)
    {
        Release();
        Unregister();
        registered = false;
        if (key == "None") return true;
        if (!ShortcutGesture.TryParse(key, out gesture)) throw new ArgumentOutOfRangeException(nameof(key));

        registered = gesture.IsMouse ? RegisterMouse(source.Handle, 0x100)
            : RegisterHotKey(source.Handle, HotkeyId, NoRepeat | gesture.Modifiers, (uint)gesture.VirtualKey);
        return registered;
    }
    private IntPtr HandleMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!disposed && registered && gesture.IsMouse && message == 0x00FF)
        {
            uint size = (uint)mousePacket.Length;
            uint headerSize = (uint)(8 + 2 * IntPtr.Size);
            uint read = GetRawInputData(lParam, 0x10000003, mousePacket, ref size, headerSize);
            if (read != uint.MaxValue && read >= headerSize + 24 && BitConverter.ToUInt32(mousePacket, 0) == 0)
                HandleMouseButtons(BitConverter.ToUInt16(mousePacket, (int)headerSize + 4));
            // Leave WM_INPUT unhandled so WPF/DefWindowProc performs required cleanup.
        }
        if (!disposed && registered && !gesture.IsMouse && message == HotkeyMessage && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            // A queued notification may arrive after release; never latch it as a held key.
            if (!held && gesture.IsHeld(isDown))
            {
                held = true;
                releaseTimer.Start();
                HeldChanged?.Invoke(true);
            }
        }
        return IntPtr.Zero;
    }
    private void HandleMouseButtons(ushort flags)
    {
        if ((flags & 0x20) != 0) { Release(); return; }
        if ((flags & 0x10) != 0 && !held && gesture.IsHeld(isDown))
        {
            held = true;
            releaseTimer.Start();
            HeldChanged?.Invoke(true);
        }
    }
    private void Unregister()
    {
        if (!registered) return;
        if (gesture.IsMouse) RegisterMouse(IntPtr.Zero, 1); // RIDEV_REMOVE requires a null target.
        else UnregisterHotKey(source.Handle, HotkeyId);
        registered = false;
    }
    private static bool RegisterMouse(IntPtr target, uint flags)
    {
        var device = new RawInputDevice { UsagePage = 1, Usage = 2, Flags = flags, Target = target };
        return RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf<RawInputDevice>());
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage, Usage;
        public uint Flags;
        public IntPtr Target;
    }
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices([In] RawInputDevice[] devices, uint count, uint size);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr input, uint command, [Out] byte[] data, ref uint size, uint headerSize);
    private void Release()
    {
        releaseTimer.Stop();
        if (!held) return;
        held = false;
        HeldChanged?.Invoke(false);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Release();
        Unregister();
        source.RemoveHook(HandleMessage);
        source.Dispose();
    }

    internal static void VerifyLifecycle()
    {
        bool down = false, modifierDown = false;
        using var service = new GlobalPushToTalkService(key => key == 0x11 ? modifierDown : down);
        // Test the private notification window, not the user's keyboard or microphone.
        service.gesture = new ShortcutGesture(0x87, 2); // F24 is used only by this short-lived diagnostic.
        service.registered = RegisterHotKey(service.source.Handle, HotkeyId, NoRepeat | 2, 0x87);
        if (!service.registered) throw new InvalidOperationException("Diagnostic hotkey unavailable.");
        var events = new List<bool>();
        service.HeldChanged += events.Add;
        if (service.IsReleasePolling) throw new InvalidOperationException("Idle hotkey must not poll.");
        down = modifierDown = true;
        if (!PostMessage(service.source.Handle, HotkeyMessage, new IntPtr(HotkeyId), IntPtr.Zero))
            throw new InvalidOperationException("Could not post diagnostic notification.");
        PumpUntil(() => events.Count == 1);
        if (!service.IsReleasePolling || !events[0]) throw new InvalidOperationException("Hold not detected.");
        modifierDown = false;
        PumpUntil(() => events.Count == 2);
        if (events[1] || service.IsReleasePolling) throw new InvalidOperationException("Release remained latched.");
        down = modifierDown = true;
        PostMessage(service.source.Handle, HotkeyMessage, new IntPtr(HotkeyId), IntPtr.Zero);
        PumpUntil(() => events.Count == 3);
        down = false;
        PumpUntil(() => events.Count == 4);
        if (!events[2] || events[3] || service.IsReleasePolling) throw new InvalidOperationException("Main-key release failed.");
        if (!service.Configure("Ctrl+Mouse3")) throw new InvalidOperationException("Raw mouse registration failed.");
        down = modifierDown = true;
        service.HandleMouseButtons(0); // Motion and scrolling alone must not activate Mouse3.
        if (events.Count != 4 || service.IsReleasePolling) throw new InvalidOperationException("Mouse motion activated shortcut.");
        service.HandleMouseButtons(0x10);
        if (events.Count != 5 || !events[4]) throw new InvalidOperationException("Mouse press failed.");
        service.HandleMouseButtons(0x20);
        if (events.Count != 6 || events[5] || service.IsReleasePolling) throw new InvalidOperationException("Mouse release failed.");
        service.HandleMouseButtons(0x10);
        modifierDown = false;
        PumpUntil(() => events.Count == 8);
        if (events[7] || service.IsReleasePolling) throw new InvalidOperationException("Mouse modifier release failed.");
        service.Configure("None");
        if (service.IsRegistered || service.IsReleasePolling) throw new InvalidOperationException("Shortcut cleanup failed.");
    }
    private static void PumpUntil(Func<bool> done)
    {
        var frame = new DispatcherFrame();
        var check = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        long until = Environment.TickCount64 + 2000;
        check.Tick += (_, _) => { if (done() || Environment.TickCount64 >= until) frame.Continue = false; };
        check.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { check.Stop(); }
        if (!done()) throw new TimeoutException("Shortcut notification timed out.");
    }
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);



    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}






