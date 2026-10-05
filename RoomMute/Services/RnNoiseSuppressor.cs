using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RoomMute.Core.Audio;
namespace RoomMute.Services;

public sealed class RnNoiseSuppressor : INoiseSuppressor
{
    private readonly State state;
    public RnNoiseSuppressor()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("RNNoise is bundled for Windows.");
        state = new State();
        if (state.IsInvalid) { state.Dispose(); throw new IOException("RNNoise could not allocate a filter state."); }
        if (Native.FrameSize() != 480) { state.Dispose(); throw new NotSupportedException("Unexpected RNNoise frame size."); }
    }
    public float Process(float[] input, float[] output)
    {
        ObjectDisposedException.ThrowIf(state.IsClosed, this);
        if (input.Length != 480 || output.Length != 480) throw new ArgumentException("RNNoise requires exactly 480 samples.");
        return Native.Process(state, output, input);
    }
    public void Dispose() => state.Dispose();
    private sealed class State : SafeHandleZeroOrMinusOneIsInvalid
    {
        public State() : base(true) => SetHandle(Native.Create(IntPtr.Zero));
        protected override bool ReleaseHandle() { Native.Destroy(handle); return true; }
    }
    private static class Native
    {
        [DllImport("rnnoise.dll", EntryPoint = "rnnoise_create", CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.SafeDirectories)]
        public static extern IntPtr Create(IntPtr model);
        [DllImport("rnnoise.dll", EntryPoint = "rnnoise_destroy", CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.SafeDirectories)]
        public static extern void Destroy(IntPtr state);
        [DllImport("rnnoise.dll", EntryPoint = "rnnoise_get_frame_size", CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.SafeDirectories)]
        public static extern int FrameSize();
        [DllImport("rnnoise.dll", EntryPoint = "rnnoise_process_frame", CallingConvention = CallingConvention.Cdecl)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.SafeDirectories)]
        public static extern float Process(State state, [Out] float[] output, [In] float[] input);
    }
}
