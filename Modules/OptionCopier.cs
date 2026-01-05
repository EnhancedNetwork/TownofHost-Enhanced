using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;

namespace TOHE.Modules;

public static class OptionCopier
{
    [Obfuscation(Exclude = true)]
    private static readonly DirectoryInfo SaveDataDirectoryInfo = new("./TOHE-DATA/Presets/");

    private static FileInfo OptionCopierFileInfo(string fileName) => new($"{SaveDataDirectoryInfo.FullName}/{(fileName.EndsWith(".preset") ? fileName : fileName + ".preset")}");

    public static void Initialize()
    {
        if (!SaveDataDirectoryInfo.Exists)
        {
            SaveDataDirectoryInfo.Create();
        }
    }
    /// <summary>Generate object for json serialization from current options</summary>
    private static SerializablePresetData GenerateOptionsData(int presetNum)
    {
        Dictionary<int, Dictionary<string, object>> presetOptions = [];
        foreach (var option in OptionItem.AllOptions)
        {
            Dictionary<string, object> optionData = [];
            optionData["option_name"] = option.GetOptionPath();
            optionData["value"] = option.GetValueObject();
            optionData["valid_values"] = option.GetValidValues();
            if (!option.IsSingleValue && !presetOptions.TryAdd(option.Id, optionData))
            {
                Logger.Warn($"Duplicate preset option ID: {option.Id}", "Option Copier");
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
            Logger.Info($"Loaded option version {serializableOptionsData.Version} does not match current version {Version}, overwriting with default value", "Option Copier");
            Save(presetNum, fileName);
            return;
        }

        Loader = CoLoadOptionsData(serializableOptionsData);
        _ = new LateTask(DoCoLoadOptionsData, LOAD_SPEED, shoudLog: false);
    }

    private const int LOAD_AMOUNT = 10;
    private const float LOAD_SPEED = 0.01f;
    public static void DoCoLoadOptionsData()
    {
        lock (Loader)
        {
            Logger.Info($"Loading... {Loader.Current}", "DoCoLoadOptionsData");
            for (int i = 0; i < LOAD_AMOUNT; i++)
            {
                if (!Loader.MoveNext())
                {
                    Loader = null;
                    return;
                }
            }
            _ = new LateTask(DoCoLoadOptionsData, LOAD_SPEED, shoudLog: false);
        }
    }
    private static IEnumerator<bool> Loader;
    private static IEnumerator<bool> CoLoadOptionsData(SerializablePresetData serializableOptionsData)
    {
        Dictionary<int, Dictionary<string, object>> presetOptions = serializableOptionsData.PresetOptions;

        foreach (var presetOption in presetOptions)
        {
            var id = presetOption.Key;
            var dict = presetOption.Value;

            var value = dict["value"];
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                if (value is JsonElement j)
                    value = optionItem.ParseJson(j);
                optionItem.SetValue(value, doSync: false);
                yield return true;
            }
            else
                yield return false;
        }
        OptionItem.SyncAllOptions();
        yield break;
    }
    /// <summary>Save current options to json file</summary>
    /// <returns>file path saved to</returns>
    public static string Save(int presetNum = -1, string fileName = "template")
    {
        // if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return;

        if (presetNum == -1) presetNum = OptionItem.CurrentPreset;

        if (!OptionCopierFileInfo(fileName).Exists)
        {
            OptionCopierFileInfo(fileName).Create().Dispose();
        }

        try
        {
            var jsonString = JsonSerializer.Serialize(GenerateOptionsData(presetNum), new JsonSerializerOptions { WriteIndented = true, });
            File.WriteAllText(OptionCopierFileInfo(fileName).FullName, jsonString);

            ProcessStartInfo psi = new("Explorer.exe") { Arguments = "/e,/select," + OptionCopierFileInfo(fileName).FullName.Replace("/", "\\") };
            Process.Start(psi);

            return OptionCopierFileInfo(fileName).FullName;
        }
        catch (System.Exception error)
        {
            Logger.Error($"Error: {error}", "OptionCopier.Save");
            return $"Error: {error.Message}";
        }
    }
    /// <summary>Read options from json file</summary>
    /// /// <returns>file path loaded from</returns>
    public static string Load(int presetNum = -1, string fileName = "template")
    {
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return "Error: Not Host";
        if (presetNum == -1) presetNum = OptionItem.CurrentPreset;

        var jsonString = File.ReadAllText(OptionCopierFileInfo(fileName).FullName);
        // if empty, do not read, save default value
        if (jsonString.Length <= 0)
        {
            Logger.Info("Save default value as option data is empty", "Option Copier");
            Save(presetNum, fileName);
            return "Preset was empty, saving over it";
        }
        LoadOptionsData(JsonSerializer.Deserialize<SerializablePresetData>(jsonString), presetNum, fileName);
        return OptionCopierFileInfo(fileName).FullName;
    }

    public static string GetOptionPath(this OptionItem option)
    {
        if (option.Parent == null)
            return option.Name;

        var name = option.Name;

        if (option.ReplacementDictionary != null)
        {
            foreach (var rd in option.ReplacementDictionary)
            {
                name = name.Replace(rd.Key, rd.Value);
            }
        }

        return $"{option.Parent.Name}/{name.RemoveHtmlTags()}";
    }

    public static object GetValueObject(this OptionItem option) => option switch
    {
        BooleanOptionItem b => b.GetBool(),
        FloatOptionItem f => f.GetFloat(),
        IntegerOptionItem i => i.GetInt(),
        StringOptionItem s => s.Selections[s.Rule.GetValueByIndex(s.CurrentValue)],
        _ => option.GetValue()
    };

    public static string GetValidValues(this OptionItem option)
    {
        if (option is BooleanOptionItem b)
            return "true | false";

        if (option is FloatOptionItem f)
        {
            var rule = f.Rule;
            return $"min: {rule.MinValue}, max: {rule.MaxValue}, step: {rule.Step}";
        }
        if (option is IntegerOptionItem i)
        {
            var rule = i.Rule;
            return $"min: {rule.MinValue}, max: {rule.MaxValue}, step: {rule.Step}";
        }
        if (option is StringOptionItem s)
        {
            return string.Join(" | ", s.Selections);
        }
        return string.Empty;
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    /// <summary>Optional data suitable for json storage</summary>
    public class SerializablePresetData
    {
        public int Version { get; init; }
        /// <summary>Options in the preset</summary>
        public Dictionary<int, Dictionary<string, object>> PresetOptions { get; init; }
    }

    /// <summary>Raise the number here when making incompatible changes to the format of an option (e.g., changing the number of presets)</summary>
    public static readonly int Version = 1;
}
