namespace RoomMute.Core;

/// <summary>Portable saved gesture using Windows virtual-key codes and native modifier flags.</summary>
public readonly record struct ShortcutGesture(int VirtualKey, uint Modifiers)
{
    public bool Disabled => VirtualKey == 0;
    public bool IsMouse => VirtualKey == 0x04;
    private static readonly Dictionary<int, string> Names = new()
    {
        [0x04]="Mouse3", [0x08]="Backspace", [0x09]="Tab", [0x0D]="Enter", [0x13]="Pause", [0x14]="CapsLock",
        [0x20]="Space", [0x21]="PageUp", [0x22]="PageDown", [0x23]="End", [0x24]="Home",
        [0x25]="Left", [0x26]="Up", [0x27]="Right", [0x28]="Down", [0x2D]="Insert", [0x2E]="Delete",
        [0x6A]="NumMultiply", [0x6B]="NumAdd", [0x6D]="NumSubtract", [0x6E]="NumDecimal", [0x6F]="NumDivide",
        [0x90]="NumLock", [0x91]="ScrollLock", [0xBA]="Oem1", [0xBB]="OemPlus", [0xBC]="OemComma",
        [0xBD]="OemMinus", [0xBE]="OemPeriod", [0xBF]="Oem2", [0xC0]="Oem3", [0xDB]="Oem4",
        [0xDC]="Oem5", [0xDD]="Oem6", [0xDE]="Oem7", [0xDF]="Oem8", [0xE2]="Oem102"
    };
    public static bool IsModifier(int key) => key is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or >= 0xA0 and <= 0xA5;
    public static bool IsAllowed(int key) => key is >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A or
        >= 0x60 and <= 0x69 || (key is >= 0x70 and <= 0x87 && key != 0x7B) || Names.ContainsKey(key);
    public static bool TryParse(string? value, out ShortcutGesture gesture)
    {
        gesture = default;
        if (value == "None") return true;
        if (string.IsNullOrWhiteSpace(value)) return false;
        string[] parts = value.Split('+', StringSplitOptions.TrimEntries);
        uint modifiers = 0;
        foreach (string token in parts.Take(parts.Length - 1))
        {
            uint flag = token.ToUpperInvariant() switch { "CTRL" or "STRG" => 2, "ALT" => 1, "SHIFT" => 4, "WIN" => 8, _ => 0 };
            if (flag == 0 || (modifiers & flag) != 0) return false;
            modifiers |= flag;
        }
        string keyName = parts[^1];
        int key = 0;
        if (keyName.Length == 1 && char.IsAsciiLetterOrDigit(keyName[0])) key = char.ToUpperInvariant(keyName[0]);
        else if (keyName.StartsWith("F", StringComparison.OrdinalIgnoreCase) && int.TryParse(keyName[1..], out int f) && f is >= 1 and <= 24) key = 0x6F + f;
        else if (keyName.StartsWith("Num", StringComparison.OrdinalIgnoreCase) && int.TryParse(keyName[3..], out int number) && number is >= 0 and <= 9) key = 0x60 + number;
        else key = Names.FirstOrDefault(pair => pair.Value.Equals(keyName, StringComparison.OrdinalIgnoreCase)).Key;
        if (!IsAllowed(key)) return false;
        gesture = new(key, modifiers);
        return true;
    }
    public string Serialize()
    {
        if (Disabled) return "None";
        string key = VirtualKey is >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A ? ((char)VirtualKey).ToString()
            : VirtualKey is >= 0x70 and <= 0x87 ? "F" + (VirtualKey - 0x6F)
            : VirtualKey is >= 0x60 and <= 0x69 ? "Num" + (VirtualKey - 0x60) : Names[VirtualKey];
        return ((Modifiers & 2) != 0 ? "Ctrl+" : "") + ((Modifiers & 1) != 0 ? "Alt+" : "") +
            ((Modifiers & 4) != 0 ? "Shift+" : "") + ((Modifiers & 8) != 0 ? "Win+" : "") + key;
    }
    public string Display(string language) => Disabled ? "—" : Serialize().Replace("Ctrl", language == "de" ? "Strg" : "Ctrl").Replace("+", " + ");
    public bool IsHeld(Func<int, bool> down) => !Disabled && down(VirtualKey) &&
        ((Modifiers & 2) == 0 || down(0x11)) && ((Modifiers & 1) == 0 || down(0x12)) &&
        ((Modifiers & 4) == 0 || down(0x10)) && ((Modifiers & 8) == 0 || down(0x5B) || down(0x5C));
}

public sealed class ShortcutRecorder
{
    public string? Result { get; private set; }
    public bool Canceled { get; private set; }
    public bool Press(int key, uint modifiers)
    {
        if (Result != null || Canceled) return false;
        if (key == 0x1B) { Canceled = true; return true; }
        if (ShortcutGesture.IsModifier(key) || !ShortcutGesture.IsAllowed(key) || (modifiers & ~15u) != 0) return false;
        Result = new ShortcutGesture(key, modifiers).Serialize();
        return true;
    }
}


