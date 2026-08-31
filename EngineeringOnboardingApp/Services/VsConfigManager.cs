using System.IO;
using System.Text.Json;
using EngineeringOnboardingApp.Models;

namespace EngineeringOnboardingApp.Services;

public class VsConfigManager
{
    public const string Target2022 = "VS2022";
    public const string Target2026 = "VS2026";

    private readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true
    };

    public string FileNameFor(string target)
    {
        return target switch
        {
            Target2022 => "VS2022.vsconfig",
            Target2026 => "vs2026.vsconfig",
            _ => "VS2022.vsconfig"
        };
    }

    public string PathFor(string target)
        => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", FileNameFor(target));

    public bool Exists(string target)
        => File.Exists(PathFor(target));

    public VS_CONFIG_STATE Load(string target)
    {
        var path = PathFor(target);
        if (!File.Exists(path))
            return new VS_CONFIG_STATE(target, new VsConfig(), null);

        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            var config = new VsConfig();

            if (doc.RootElement.TryGetProperty("version", out var ver))
                config.Version = ver.GetString() ?? "1.0";

            if (doc.RootElement.TryGetProperty("components", out var comps) && comps.ValueKind == JsonValueKind.Array)
                foreach (var c in comps.EnumerateArray())
                    config.Components.Add(c.GetString() ?? string.Empty);

            if (doc.RootElement.TryGetProperty("extensions", out var exts) && exts.ValueKind == JsonValueKind.Array)
                foreach (var e in exts.EnumerateArray())
                    config.Extensions.Add(e.GetString() ?? string.Empty);

            config.Components = config.Components.FindAll(c => !string.IsNullOrWhiteSpace(c));

            return new VS_CONFIG_STATE(target, config, null);
        }
        catch (Exception ex)
        {
            return new VS_CONFIG_STATE(target, new VsConfig(), ex);
        }
    }

    public string Save(string target, VsConfig config)
    {
        try
        {
            var path = PathFor(target);
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config, _writeOptions);
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);

            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);

            return string.Empty;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}

public sealed class VS_CONFIG_STATE
{
    public string Target { get; }
    public VsConfig Config { get; }
    public Exception? LoadError { get; }

    public VS_CONFIG_STATE(string target, VsConfig config, Exception? loadError)
    {
        Target = target;
        Config = config;
        LoadError = loadError;
    }
}
