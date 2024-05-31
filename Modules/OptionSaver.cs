using Newtonsoft.Json.Bson;
using System.IO;
using System.Text.Json;
using UnityEngine;
using static TOHE.Roles.Impostor.EvilHacker;

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
    private static System.Collections.IEnumerator GenerateOptionsDataCoroutine(System.Action<SerializableOptionsData> onComplete)
    {
        Dictionary<string, int> singleOptions = [];
        Dictionary<string, int[]> presetOptions = [];

        foreach (var option in OptionItem.AllOptions)
        {
            if (option.IsSingleValue)
            {
                if (!singleOptions.TryAdd(option.FullName, option.SingleValue))
                {
                    Logger.Warn($"Duplicate SingleOption Name: {option.Name}", "Option Saver");
                }
            }
            else if (!presetOptions.TryAdd(option.FullName, option.AllValues))
            {
                Logger.Warn($"Duplicate preset option Name: {option.Name}", "Option Saver");
            }

            Application.targetFrameRate = -1;
            yield return null; // Yield to prevent blocking the main thread
        }

        var optionsData = new SerializableOptionsData
        {
            Version = Version,
            SingleOptions = singleOptions,
            PresetOptions = presetOptions,
        };

        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
        onComplete?.Invoke(optionsData);
        yield break;
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
        Dictionary<string, int> singleOptions = serializableOptionsData.SingleOptions;
        Dictionary<string, int[]> presetOptions = serializableOptionsData.PresetOptions;
        foreach (var singleOption in singleOptions)
        {
            var id = singleOption.Key;
            var value = singleOption.Value;
            var optionItem = OptionItem.FastOptions.FirstOrDefault(x => x.FullName == id);
            optionItem?.SetValue(value, doSave: false);
        }
        foreach (var presetOption in presetOptions)
        {
            var id = presetOption.Key;
            var values = presetOption.Value;
            var optionItem = OptionItem.FastOptions.FirstOrDefault(x => x.FullName == id);
            optionItem?.SetAllValues(values);
        }
    }
    /// <summary>Save current options to json file</summary>
    public static void Save()
    {
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return;

        Main.Instance.StopCoroutine(GenerateOptionsDataCoroutine(SaveAfterCoro));
        Main.Instance.StartCoroutine(GenerateOptionsDataCoroutine(SaveAfterCoro));
    }
    private static void SaveAfterCoro(SerializableOptionsData data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, });
        File.WriteAllText(OptionSaverFileInfo.FullName, json);
    }
    /// <summary>Read options from json file</summary>
    public static void Load()
    {
        var jsonString = File.ReadAllText(OptionSaverFileInfo.FullName);
        // if empty, do not read, save default value
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
        public Dictionary<string, int> SingleOptions { get; init; }
        /// <summary>Options in the preset</summary>
        public Dictionary<string, int[]> PresetOptions { get; init; }
    }

    /// <summary>Raise the number here when making incompatible changes to the format of an option (e.g., changing the number of presets)</summary>
    public static readonly int Version = 1;
}
