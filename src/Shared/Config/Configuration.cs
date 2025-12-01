namespace Shared.Config;

using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
public static class Configuration
{
    private static StringDictionary? appConfiguration;
    // <-- Rest of the code below goes here. 

    private static StringDictionary GetAppConfiguration()
    {
        return appConfiguration == null ?
          appConfiguration = LoadAppConfiguration() : appConfiguration;
    }

    private static StringDictionary LoadAppConfiguration()
    {
        var cfg = new StringDictionary();
        var basePath = Directory.GetCurrentDirectory();
        var paths = new string[] { "appsettings.default.cfg", "appsettings.cfg" };

        foreach (var path in paths)
        {
            var file = Path.Combine(basePath, path);

            if (File.Exists(file))
            {
                var tmp = LoadConfigurationFile(file);

                foreach (string k in tmp.Keys) { cfg[k] = tmp[k]; }
            }
        }

        return cfg;
    }

    public static StringDictionary LoadConfigurationFile(string file)
    {
        string[] lines = File.ReadAllLines(file);

        var cfg = new StringDictionary();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimStart();

            if (string.IsNullOrEmpty(line) || line.StartsWith('#')) { continue; }

            var kv = line.Split('=', 2, StringSplitOptions.TrimEntries);

            cfg[kv[0]] = kv[1];
        }

        return cfg;
    }

    public static string? Get(string key)
    {
        return Get(key, null);
    }

    public static string? Get(string key, string? val)
    {
        return GetAppConfiguration()[key] ?? val;
    }

    public static T Get<T>(string key)
    {
        return Get<T>(key, default!);
    }

    public static T Get<T>(string key, T val)
    {
        string? value = GetAppConfiguration()[key];

        if (string.IsNullOrWhiteSpace(value)) { return val; }

        try
        {
            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (targetType.IsEnum)
            {
                return (T)Enum.Parse(targetType, value, ignoreCase: true);
            }

            var converter = TypeDescriptor.GetConverter(targetType);

            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                return (T)converter.ConvertFromString(null,
                  CultureInfo.InvariantCulture, value)!;
            }

            return (T)Convert.ChangeType(value, targetType,
              CultureInfo.InvariantCulture);
        }
        catch
        {
            return val;
        }
    }
}