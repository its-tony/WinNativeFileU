namespace WinNativeFileU.Converters;

internal static class PngCrc
{
    private static readonly uint[] Table = CreateTable();

    internal static uint Update(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes) crc = Table[(crc ^ value) & 255] ^ (crc >> 8);
        return crc;
    }

    private static uint[] CreateTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint value = i;
            for (int bit = 0; bit < 8; bit++) value = (value & 1) == 0 ? value >> 1 : 0xEDB88320u ^ (value >> 1);
            table[i] = value;
        }
        return table;
    }
}
