using System.IO;
using System.Text.Json;

namespace TOHE.Modules;

public static class OptionCopier
{
    [Obfuscation(Exclude = true)]
    private static readonly DirectoryInfo SaveDataDirectoryInfo = new("./TOHE-DATA/Presets/");

    private static FileInfo OptionCopierFileInfo(string fileName) => new($"{SaveDataDirectoryInfo.FullName}/{(fileName.EndsWith(".json") ? fileName : fileName + ".json")}");

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
        Dictionary<int, Dictionary<string, object>> presetOptions = serializableOptionsData.PresetOptions;

        foreach (var presetOption in presetOptions)
        {
            var id = presetOption.Key;
            var dict = presetOption.Value;

            var value = dict["value"];
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetValue(value);
            }
        }
    }
    /// <summary>Save current options to json file</summary>
    public static void Save(int presetNum = -1, string fileName = "template")
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
        }
        catch (System.Exception error)
        {
            Logger.Error($"Error: {error}", "OptionCopier.Save");
        }
    }
    /// <summary>Read options from json file</summary>
    public static void Load(int presetNum = -1, string fileName = "template")
    {
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost) return;
        if (presetNum == -1) presetNum = OptionItem.CurrentPreset;

        var jsonString = File.ReadAllText(OptionCopierFileInfo(fileName).FullName);
        // if empty, do not read, save default value
        if (jsonString.Length <= 0)
        {
            Logger.Info("Save default value as option data is empty", "Option Copier");
            Save(presetNum, fileName);
            return;
        }
        LoadOptionsData(JsonSerializer.Deserialize<SerializablePresetData>(jsonString), presetNum, fileName);
    }

    public static string GetOptionPath(this OptionItem option)
    {
        if (option.Parent == null)
            return option.Name;

        return $"{option.Parent.Name}/{option.Name}";
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
