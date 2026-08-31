using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EngineeringOnboardingApp.Services;

public class AdminService
{
    public static AdminService Shared { get; } = new();

    private const string FileName = "Configs\\admin.json";

    private readonly string _path;
    private string _hash = string.Empty;

    public bool Authenticated { get; private set; }

    private AdminService()
    {
        _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
    }

    public bool HasPasscode()
    {
        EnsureLoaded();
        return !string.IsNullOrEmpty(_hash);
    }

    public bool TryLogin(string passcode)
    {
        EnsureLoaded();

        if (string.IsNullOrEmpty(_hash))
        {
            // First run: any passcode becomes the admin passcode.
            if (string.IsNullOrWhiteSpace(passcode))
                return false;

            _hash = Hash(passcode);
            SaveToDisk();
            Authenticated = true;
            return true;
        }

        if (TimingSafeEquals(Hash(passcode), _hash))
        {
            Authenticated = true;
            return true;
        }

        return false;
    }

    public bool ChangePasscode(string current, string newPasscode)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(newPasscode))
            return false;

        if (!string.IsNullOrEmpty(_hash) && !TimingSafeEquals(Hash(current), _hash))
            return false;

        _hash = Hash(newPasscode);
        SaveToDisk();
        return true;
    }

    public void Logout() => Authenticated = false;

    private void EnsureLoaded()
    {
        if (!string.IsNullOrEmpty(_hash) || File.Exists(_path))
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("passcodeHash", out var p))
                        _hash = p.GetString() ?? string.Empty;
                }
            }
            catch
            {
            }
        }
    }

    private void SaveToDisk()
    {
        try
        {
            var dir = Path.GetDirectoryName(_path)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new { passcodeHash = _hash }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
        catch
        {
        }
    }

    private static string Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static bool TimingSafeEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];

        return diff == 0;
    }
}
