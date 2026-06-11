using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace XuruVoipClient.Services;

/// <summary>
/// A Win32 utility that simulates keyboard keypresses using keybd_event.
/// Useful for simulating Star Citizen game hotkeys from voice triggers.
/// </summary>
public static class InputSimulator
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static byte GetVirtualKey(string keyName)
    {
        if (string.IsNullOrEmpty(keyName)) return 0;

        return keyName.ToUpperInvariant() switch
        {
            "A" => 0x41, "B" => 0x42, "C" => 0x43, "D" => 0x44, "E" => 0x45, "F" => 0x46,
            "G" => 0x47, "H" => 0x48, "I" => 0x49, "J" => 0x4A, "K" => 0x4B, "L" => 0x4C,
            "M" => 0x4D, "N" => 0x4E, "O" => 0x4F, "P" => 0x50, "Q" => 0x51, "R" => 0x52,
            "S" => 0x53, "T" => 0x54, "U" => 0x55, "V" => 0x56, "W" => 0x57, "X" => 0x58,
            "Y" => 0x59, "Z" => 0x5A,
            "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33, "4" => 0x34,
            "5" => 0x35, "6" => 0x36, "7" => 0x37, "8" => 0x38, "9" => 0x39,
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73, "F5" => 0x74, "F6" => 0x75,
            "F7" => 0x76, "F8" => 0x77, "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            "UP" => 0x26, "DOWN" => 0x28, "LEFT" => 0x25, "RIGHT" => 0x27,
            "SPACE" => 0x20, "ENTER" => 0x0D, "ESC" => 0x1B, "ESCAPE" => 0x1B,
            "TAB" => 0x09, "BACK" => 0x08, "BACKSPACE" => 0x08,
            "LALT" => 0xA4, "RALT" => 0xA5, "ALT" => 0x12,
            "LSHIFT" => 0xA0, "RSHIFT" => 0xA1, "SHIFT" => 0x10,
            "LCONTROL" => 0xA2, "RCONTROL" => 0xA3, "CTRL" => 0x11, "CONTROL" => 0x11,
            "CAPSLOCK" => 0x14, "NUMLOCK" => 0x90, "SCROLL" => 0x91,
            "INSERT" => 0x2D, "DELETE" => 0x2E, "HOME" => 0x24, "END" => 0x23,
            "PRIOR" => 0x21, "NEXT" => 0x22, "PAGEUP" => 0x21, "PAGEDOWN" => 0x22,
            _ => (byte)0
        };
    }

    private static byte GetModifierKey(string modName)
    {
        if (string.IsNullOrEmpty(modName)) return 0;
        return modName.ToUpperInvariant() switch
        {
            "ALT" => 0x12,
            "CTRL" => 0x11,
            "SHIFT" => 0x10,
            _ => 0
        };
    }

    /// <summary>
    /// Simulates pressing a key down, holding it for 50ms, and letting it go,
    /// along with an optional modifier (Alt, Ctrl, Shift).
    /// </summary>
    public static void SimulateKeyPress(string keyName, string modifierName = "None")
    {
        byte vk = GetVirtualKey(keyName);
        if (vk == 0) return;

        byte mod = GetModifierKey(modifierName);

        // Press modifier
        if (mod != 0)
        {
            keybd_event(mod, 0, 0, 0);
            Thread.Sleep(15);
        }

        // Press key
        keybd_event(vk, 0, 0, 0);
        Thread.Sleep(50); // Hold down key

        // Release key
        keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);

        // Release modifier
        if (mod != 0)
        {
            Thread.Sleep(15);
            keybd_event(mod, 0, KEYEVENTF_KEYUP, 0);
        }
    }
}
