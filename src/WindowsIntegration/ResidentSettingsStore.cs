using System.Text.Json;
using System.Text.Json.Serialization;
using WinNativeFileU.Core;

namespace WinNativeFileU.WindowsIntegration;

/// <summary>Owned settings only. Invalid existing content is preserved and requires repair outside the app.</summary>
public sealed class ResidentSettingsStore(string directory)
{
    private readonly string path = Path.Combine(Path.GetFullPath(directory), "settings.json");
    private static readonly JsonSerializerOptions Options = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, WriteIndented = true };

    public ResidentSettings Load()
    {
        if (!File.Exists(path)) return new();
        CheckOrdinary();
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (input.Length > 4096) throw new InvalidDataException("Settings exceed the supported size.");
        var settings = JsonSerializer.Deserialize<ResidentSettings>(input, Options);
        if (settings == null || !settings.IsValid) throw new InvalidDataException("Unsupported settings version or values.");
        return settings;
    }

    public void Save(ResidentSettings settings)
    {
        if (!settings.IsValid) throw new ArgumentException("Unsupported settings.", nameof(settings));
        _ = Load(); // Never silently replace corrupted or future-version settings.
        CheckOrdinary();
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".partial";
        bool owned = false;
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                owned = true;
                JsonSerializer.Serialize(stream, settings, Options);
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, path + "." + Guid.NewGuid().ToString("N") + ".previous");
            else File.Move(temporary, path, overwrite: false);
            owned = false;
        }
        finally { if (owned) File.Delete(temporary); }
    }

    private void CheckOrdinary()
    {
        for (string? current = path; current != null; current = Path.GetDirectoryName(current))
        {
            if (current == path && !File.Exists(current)) continue;
            if ((File.GetAttributes(current) & (FileAttributes.ReparsePoint | FileAttributes.Offline)) != 0)
                throw new IOException("Settings paths must not be links or offline files.");
        }
        if (Directory.Exists(path)) throw new IOException("The settings filename is a directory.");
    }
}
