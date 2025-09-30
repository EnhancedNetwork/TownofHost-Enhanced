using System.IO;
using System.Text.Json;

namespace TOHE.Modules;

public static class OptionCopier
{
    [Obfuscation(Exclude = true)]
    private static readonly DirectoryInfo SaveDataDirectoryInfo = new("./TOHE-DATA/Presets/");

    private static FileInfo OptionSaverFileInfo(string fileName) => new($"{SaveDataDirectoryInfo.FullName}/{fileName}.json");

    public static void Initialize()
    {
        if (!SaveDataDirectoryInfo.Exists)
        {
            SaveDataDirectoryInfo.Create();
            SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
        }
    }
    /// <summary>Generate object for json serialization from current options</summary>
    private static SerializablePresetData GenerateOptionsData(int presetNum)
    {
        Dictionary<int, int> presetOptions = [];
        foreach (var option in OptionItem.AllOptions)
        {
            if (!option.IsSingleValue && !presetOptions.TryAdd(option.Id, option.AllValues[presetNum]))
            {
                Logger.Warn($"Duplicate preset option ID: {option.Id}", "Option Saver");
            }
        }
        return new SerializablePresetData
        {
            Version = Version,
            PresetOptions = presetOptions,
        };
    }
    /// <summary>Read deserialized object and set option values</summary>
    private static void LoadOptionsData(SerializablePresetData serializableOptionsData, int presetNum, string fileName)
    {
        if (serializableOptionsData.Version != Version)
        {
            // If you want to provide a method for migrating between versions in the future, you can distribute the conversion method for each version here
            Logger.Info($"Loaded option version {serializableOptionsData.Version} does not match current version {Version}, overwriting with default value", "Option Saver");
            Save(presetNum, fileName);
            return;
        }
        Dictionary<int, int> presetOptions = serializableOptionsData.PresetOptions;

        foreach (var presetOption in presetOptions)
        {
            var id = presetOption.Key;
            var value = presetOption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetValue(value);
            }
        }
    }
    /// <summary>Save current options to json file</summary>
    public static void Save(int presetNum, string fileName)
    {
        // if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return;

        if (!OptionSaverFileInfo(fileName).Exists)
        {
            OptionSaverFileInfo(fileName).Create().Dispose();
        }

        var prev = OptionItem.CurrentPreset;
        OptionItem.CurrentPreset = presetNum;

        try
        {
            var jsonString = JsonSerializer.Serialize(GenerateOptionsData(presetNum), new JsonSerializerOptions { WriteIndented = true, });
            File.WriteAllText(OptionSaverFileInfo(fileName).FullName, jsonString);
        }
        catch (System.Exception error)
        {
            Logger.Error($"Error: {error}", "OptionSaver.Save");
        }
        OptionItem.CurrentPreset = prev;
    }
    /// <summary>Read options from json file</summary>
    public static void Load(int presetNum, string fileName)
    {
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return;
        OptionItem.CurrentPreset = presetNum;

        var jsonString = File.ReadAllText(OptionSaverFileInfo(fileName).FullName);
        // if empty, do not read, save default value
        if (jsonString.Length <= 0)
        {
            Logger.Info("Save default value as option data is empty", "Option Saver");
            Save(presetNum, fileName);
            return;
        }
        LoadOptionsData(JsonSerializer.Deserialize<SerializablePresetData>(jsonString), presetNum, fileName);
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    /// <summary>Optional data suitable for json storage</summary>
    public class SerializablePresetData
    {
        public int Version { get; init; }
        /// <summary>Options in the preset</summary>
        public Dictionary<int, int> PresetOptions { get; init; }
    }

    /// <summary>Raise the number here when making incompatible changes to the format of an option (e.g., changing the number of presets)</summary>
    public static readonly int Version = 1;
}
