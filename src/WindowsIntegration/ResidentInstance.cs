using System.Security.Cryptography;
using System.Text;

namespace WinNativeFileU.WindowsIntegration;

/// <summary>One resident per settings root in this Windows session. Must be disposed on its acquiring thread.</summary>
public sealed class ResidentInstance : IDisposable
{
    private readonly Mutex mutex;
    public bool Acquired { get; }
    private readonly int thread = Environment.CurrentManagedThreadId;
    private bool disposed;
    public ResidentInstance(string settingsRoot)
    {
        string identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(settingsRoot).TrimEnd('\\').ToUpperInvariant())));
        mutex = new Mutex(false, @"Local\WinNativeFileU.Resident." + identity);
        try { Acquired = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { Acquired = true; }
    }
    public void Dispose()
    {
        if (disposed) return;
        if (Environment.CurrentManagedThreadId != thread) throw new InvalidOperationException("Dispose resident ownership on its UI thread.");
        if (Acquired) mutex.ReleaseMutex();
        mutex.Dispose(); disposed = true;
    }
}
