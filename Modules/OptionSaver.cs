using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TOHE.Modules;

// https://github.com/tukasa0001/TownOfHost/blob/main/Modules/OptionSaver.cs
public static class OptionSaver
{
    private static readonly DirectoryInfo SaveDataDirectoryInfo = new("./TOHE-DATA/SaveData/");
    private static readonly FileInfo OptionSaverFileInfo = new($"{SaveDataDirectoryInfo.FullName}/Options.json");

    public static void Initialize()
    {
        if (!SaveDataDirectoryInfo.Exists)
        {
            SaveDataDirectoryInfo.Create();
            SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
        }
        if (!OptionSaverFileInfo.Exists)
        {
            OptionSaverFileInfo.Create().Dispose();
        }
    }
    /// <summary>Generate object for json serialization from current options</summary>
    private static SerializableOptionsData GenerateOptionsData()
    {
        Dictionary<int, int> singleOptions = new();
        Dictionary<int, int[]> presetOptions = new();
        foreach (var option in OptionItem.AllOptions)
        {
            if (option.IsSingleValue)
            {
                if (!singleOptions.TryAdd(option.Id, option.SingleValue))
                {
                    Logger.Warn($"Duplicate SingleOption ID: {option.Id}", "Option Saver");
                }
            }
            else if (!presetOptions.TryAdd(option.Id, option.AllValues))
            {
                Logger.Warn($"Duplicate preset option ID: {option.Id}", "Option Saver");
            }
        }
        return new SerializableOptionsData
        {
            Version = Version,
            SingleOptions = singleOptions,
            PresetOptions = presetOptions,
        };
    }
    /// <summary>Read deserialized object and set option values</summary>
    private static void LoadOptionsData(SerializableOptionsData serializableOptionsData)
    {
        if (serializableOptionsData.Version != Version)
        {
            // If you want to provide a method for migrating between versions in the future, you can distribute the conversion method for each version here
            Logger.Info($"Loaded option version {serializableOptionsData.Version} does not match current version {Version}, overwriting with default value", "Option Saver");
            Save();
            return;
        }
        Dictionary<int, int> singleOptions = serializableOptionsData.SingleOptions;
        Dictionary<int, int[]> presetOptions = serializableOptionsData.PresetOptions;
        foreach (var singleOption in singleOptions)
        {
            var id = singleOption.Key;
            var value = singleOption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetValue(value, doSave: false);
            }
        }
        foreach (var presetOption in presetOptions)
        {
            var id = presetOption.Key;
            var values = presetOption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetAllValues(values);
            }
        }
    }
    /// <summary>Save current options to json file</summary>
    public static void Save()
    {
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return;

        var jsonString = JsonSerializer.Serialize(GenerateOptionsData(), new JsonSerializerOptions { WriteIndented = true, });
        File.WriteAllText(OptionSaverFileInfo.FullName, jsonString);
    }
    /// <summary>Read options from json file</summary>
    public static void Load()
    {
        var jsonString = File.ReadAllText(OptionSaverFileInfo.FullName);
        // 空なら読み込まず，デフォルト値をセーブする
        if (jsonString.Length <= 0)
        {
            Logger.Info("Save default value as option data is empty", "Option Saver");
            Save();
            return;
        }
        LoadOptionsData(JsonSerializer.Deserialize<SerializableOptionsData>(jsonString));
    }

    /// <summary>Optional data suitable for json storage</summary>
    public class SerializableOptionsData
    {
        public int Version { get; init; }
        /// <summary>Non-preset options</summary>
        public Dictionary<int, int> SingleOptions { get; init; }
        /// <summary>Options in the preset</summary>
        public Dictionary<int, int[]> PresetOptions { get; init; }
    }

    /// <summary>Raise the number here when making incompatible changes to the format of an option (e.g., changing the number of presets)</summary>
    public static readonly int Version = 0;
}
