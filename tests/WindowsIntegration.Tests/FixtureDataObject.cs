using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace WinNativeFileU.WindowsIntegration.Tests;

/// <summary>Synthetic producer; not Explorer. Each GetData transfers a fresh native medium.</summary>
internal sealed class FixtureDataObject(string[] paths, bool supported = true) : IDataObject
{
    public int MediumsCreated { get; private set; }

    public int QueryGetData(ref FORMATETC format) =>
        supported && format.cfFormat == 15 && (format.tymed & TYMED.TYMED_HGLOBAL) != 0 ? 0 : unchecked((int)0x80040064);

    public void GetData(ref FORMATETC format, out STGMEDIUM medium)
    {
        Marshal.ThrowExceptionForHR(QueryGetData(ref format));
        byte[] names = Encoding.Unicode.GetBytes(string.Join('\0', paths) + "\0\0");
        byte[] bytes = new byte[20 + names.Length];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, 20);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), 1);
        names.CopyTo(bytes, 20);
        nint handle = GlobalAlloc(0x42, (nuint)bytes.Length);
        if (handle == 0) throw new OutOfMemoryException();
        nint memory = GlobalLock(handle);
        if (memory == 0) { GlobalFree(handle); throw new OutOfMemoryException(); }
        try { Marshal.Copy(bytes, 0, memory, bytes.Length); }
        finally { GlobalUnlock(handle); }
        medium = new STGMEDIUM { tymed = TYMED.TYMED_HGLOBAL, unionmember = handle, pUnkForRelease = null };
        MediumsCreated++;
    }

    public void GetDataHere(ref FORMATETC format, ref STGMEDIUM medium) => throw new NotSupportedException();
    public int GetCanonicalFormatEtc(ref FORMATETC input, out FORMATETC output) { output = input; output.ptd = 0; return 0x40130; }
    public void SetData(ref FORMATETC format, ref STGMEDIUM medium, bool release) => throw new NotSupportedException();
    public IEnumFORMATETC EnumFormatEtc(DATADIR direction) => throw new NotSupportedException();
    public int DAdvise(ref FORMATETC format, ADVF flags, IAdviseSink sink, out int connection) { connection = 0; return unchecked((int)0x80040003); }
    public void DUnadvise(int connection) => throw new NotSupportedException();
    public int EnumDAdvise(out IEnumSTATDATA? data) { data = null; return unchecked((int)0x80040003); }

    [DllImport("kernel32.dll")] private static extern nint GlobalAlloc(uint flags, nuint size);
    [DllImport("kernel32.dll")] private static extern nint GlobalLock(nint handle);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GlobalUnlock(nint handle);
    [DllImport("kernel32.dll")] private static extern nint GlobalFree(nint handle);
}
