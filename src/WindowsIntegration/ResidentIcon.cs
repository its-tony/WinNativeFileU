using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinNativeFileU.WindowsIntegration;

/// <summary>Original symmetric blue disc/white diamond, generated once; caller owns the returned HICON.</summary>
internal static class ResidentIcon
{
    internal static nint Create()
    {
        byte[] mask = Enumerable.Repeat((byte)255, 128).ToArray();
        byte[] pixels = new byte[32 * 32 * 4];
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            double dx = x - 15.5, dy = y - 15.5;
            if (dx * dx + dy * dy > 14 * 14) continue;
            mask[y * 4 + x / 8] &= (byte)~(1 << (7 - x % 8));
            bool diamond = Math.Abs(dx) + Math.Abs(dy) is >= 4 and <= 10;
            int offset = (y * 32 + x) * 4;
            pixels[offset] = diamond ? (byte)255 : (byte)230;
            pixels[offset + 1] = diamond ? (byte)255 : (byte)105;
            pixels[offset + 2] = diamond ? (byte)255 : (byte)20;
            pixels[offset + 3] = 255;
        }
        nint icon = ResidentNative.CreateIcon(Native.GetModuleHandleW(null), 32, 32, 1, 32, mask, pixels);
        if (icon == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "The notification icon could not be created.");
        return icon;
    }
}
