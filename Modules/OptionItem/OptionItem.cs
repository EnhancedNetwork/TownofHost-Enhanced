using System;
using TOHE.Modules;
using UnityEngine;

namespace TOHE;

public abstract class OptionItem
{
    #region static
    public static IReadOnlyList<OptionItem> AllOptions => _allOptions;
    private static readonly List<OptionItem> _allOptions = new(1024);
    public static IReadOnlyDictionary<int, OptionItem> FastOptions => _fastOptions;
    private static readonly Dictionary<int, OptionItem> _fastOptions = new(1024);
    private static readonly Dictionary<int, string> nameSettings = [];

    public static int CurrentPreset { get; set; }
    #endregion

    // Constructor variables
    public int Id { get; }
    public string Name { get; }
    public int DefaultValue { get; }
    public TabGroup Tab { get; }
    public bool IsSingleValue { get; }

    // Nullable/Empty Variables
    public Color NameColor { get; protected set; }
    public OptionFormat ValueFormat { get; protected set; }
    public CustomGameMode GameMode { get; protected set; }
    public CustomGameMode HideOptionInFFA { get; protected set; }
    public CustomGameMode HideOptionInCandR { get; protected set; }
    public CustomGameMode HideOptionInUltimate { get; protected set; }
    public CustomGameMode HideOptionInHnS { get; protected set; }
    public bool IsHeader { get; protected set; }
    public bool IsHidden { get; protected set; }
    public bool IsText { get; protected set; }
    public bool IsVanillaText { get; protected set; }
    public Dictionary<string, string> ReplacementDictionary
    {
        get => _replacementDictionary;
        set
        {
            if (value == null) _replacementDictionary?.Clear();
            else _replacementDictionary = value;
        }
    }
    private Dictionary<string, string> _replacementDictionary;

    public int[] AllValues { get; private set; } = new int[NumPresets];
    public int CurrentValue
    {
        get => GetValue();
        set => SetValue(value);
    }
    public int SingleValue { get; private set; }

    // Parent and Child Info
    public OptionItem Parent { get; private set; }
    public List<OptionItem> Children;

    public OptionBehaviour OptionBehaviour;

    public event EventHandler<UpdateValueEventArgs> UpdateValueEvent;

    // Constructor
    public OptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, bool vanillaStr)
    {
        // Info Setting
        Id = id;
        Name = name;
        DefaultValue = defaultValue;
        Tab = tab;
        IsSingleValue = isSingleValue;
        IsVanillaText = vanillaStr;

        // Nullable Info Setting
        NameColor = Color.white;
        ValueFormat = OptionFormat.None;
        GameMode = CustomGameMode.All;
        HideOptionInFFA = CustomGameMode.All;
        HideOptionInHnS = CustomGameMode.All;
        IsHeader = false;
        IsHidden = false;
        IsText = false;

        // Initialize Objects
        Children = [];

        // Set default value
        if (Id == PresetId)
        {
            SingleValue = DefaultValue;
            CurrentPreset = SingleValue;
        }
        else if (IsSingleValue)
        {
            SingleValue = DefaultValue;
        }
        else
        {
            for (int i = 0; i < NumPresets; i++)
            {
                AllValues[i] = DefaultValue;
            }
        }

        if (_fastOptions.TryAdd(id, this))
        {
            _allOptions.Add(this);
            nameSettings.Add(id, name);
        }
        else
        {
            Logger.Error($"Duplicate ID: {id} Name: {name}", "OptionItem");

            nameSettings.TryGetValue(id, out var setting);
            Logger.Error($"Duplicate from: {setting}", "OptionItem");
        }
    }

    // Setter
    public OptionItem Do(Action<OptionItem> action)
    {
        action(this);
        return this;
    }

    public OptionItem SetColor(Color value) => Do(i => i.NameColor = value);
    public OptionItem SetValueFormat(OptionFormat value) => Do(i => i.ValueFormat = value);
    public OptionItem SetGameMode(CustomGameMode value) => Do(i => i.GameMode = value);
    public OptionItem SetHeader(bool value) => Do(i => i.IsHeader = value);
    public OptionItem SetHidden(bool value) => Do(i => i.IsHidden = value);
    public OptionItem SetText(bool value) => Do(i => i.IsText = value);
    public OptionItem HideInFFA(CustomGameMode value = CustomGameMode.FFA) => Do(i => i.HideOptionInFFA = value);
    public OptionItem HideInUltimate(CustomGameMode value = CustomGameMode.UltimateTeam) => Do(i => i.HideOptionInUltimate = value);
    public OptionItem HideInTOT(CustomGameMode value = CustomGameMode.TrickorTreat) => Do(i => i.HideOptionInUltimate = value);
    public OptionItem HideInCandR(CustomGameMode value = CustomGameMode.CandR) => Do(i => i.HideOptionInCandR = value); //C&R
    public OptionItem HideInHnS(CustomGameMode value = CustomGameMode.HidenSeekTOHO) => Do(i => i.HideOptionInHnS = value);

    public OptionItem SetParent(OptionItem parent) => Do(i =>
    {
        foreach (var role in Options.CustomRoleSpawnChances.Where(x => x.Value.Name == parent.Name).ToArray())
        {
            var roleName = Translator.GetString(Enum.GetName(typeof(CustomRoles), role.Key));
            ReplacementDictionary ??= [];
            ReplacementDictionary.TryAdd(roleName, Utils.ColorString(Utils.GetRoleColor(role.Key), roleName));
            break;
        }
        i.Parent = parent;
        parent.SetChild(i);
    });
    public OptionItem SetChild(OptionItem child) => Do(i => i.Children.Add(child));
    public OptionItem RegisterUpdateValueEvent(EventHandler<UpdateValueEventArgs> handler)
        => Do(i => UpdateValueEvent += handler);

    // 置き換え辞書
    public OptionItem AddReplacement((string key, string value) kvp)
        => Do(i =>
        {
            ReplacementDictionary ??= [];
            ReplacementDictionary.Add(kvp.key, kvp.value);
        });
    public OptionItem RemoveReplacement(string key)
        => Do(i => ReplacementDictionary?.Remove(key));

    // Getter
    public virtual string GetName(bool disableColor = false, bool console = false)
    {
        return disableColor ?
            Translator.GetString(Name, ReplacementDictionary, console) :
            Utils.ColorString(NameColor, Translator.GetString(Name, ReplacementDictionary));
    }
    public virtual string GetNameVanilla()
    {
        return Translator.GetString(Name, ReplacementDictionary, vanilla: true);
    }
    public virtual bool GetBool() => CurrentValue != 0 && (Parent == null || Parent.GetBool());
    public virtual int GetInt() => CurrentValue;
    public virtual float GetFloat() => CurrentValue;
    public virtual string GetString()
    {
        return ApplyFormat(CurrentValue.ToString());
    }
    public virtual int GetValue() => IsSingleValue ? SingleValue : AllValues[CurrentPreset];

    // Deprecated IsHidden function
    public virtual bool IsHiddenOn(CustomGameMode mode)
    {
            return IsHidden || this.Parent?.IsHiddenOn(Options.CurrentGameMode) == true || (HideOptionInCandR != CustomGameMode.All && HideOptionInCandR == mode) || (HideOptionInUltimate != CustomGameMode.All && HideOptionInUltimate == mode) || (HideOptionInFFA != CustomGameMode.All && HideOptionInFFA == mode) || (HideOptionInHnS != CustomGameMode.All && HideOptionInHnS == mode) || (GameMode != CustomGameMode.All && GameMode != mode);
    }
    public string ApplyFormat(string value)
    {
        if (ValueFormat == OptionFormat.None) return value;
        return string.Format(Translator.GetString("Format." + ValueFormat), value);
    }

    public virtual void Refresh()
    {
        if (OptionBehaviour is not null and StringOption opt)
        {
            if (IsVanillaText == true)
            {
                opt.TitleText.text = GetNameVanilla();
            }
            else
            {
                opt.TitleText.text = GetName();
            }
            opt.ValueText.text = GetString();
            opt.oldValue = opt.Value = CurrentValue;
        }
    }
    public virtual void SetValue(int afterValue, bool doSave, bool doSync = true)
    {
        int beforeValue = CurrentValue;
        if (IsSingleValue)
        {
            SingleValue = afterValue;
        }
        else
        {
            AllValues[CurrentPreset] = afterValue;
        }

        CallUpdateValueEvent(beforeValue, afterValue);
        Refresh();
        if (doSync)
        {
            SyncAllOptions();
            // RPC.SyncCustomSettingsRPCforOneOption(this);
        }
        if (doSave)
        {
            OptionSaver.Save();
        }
    }
    public virtual void SetValue(int afterValue, bool doSync = true)
    {
        SetValue(afterValue, true, doSync);
    }
    public void SetAllValues(int[] values)
    {
        AllValues = values;
    }
    // This Code For Reset All TOHE Setting To Default
    public virtual void SetValueNoRpc(int value)
    {
        int beforeValue = CurrentValue;
        int afterValue = CurrentValue = value;

        CallUpdateValueEvent(beforeValue, afterValue);
        Refresh();
    }

    public static OptionItem operator ++(OptionItem item)
        => item.Do(item => item.SetValue(item.CurrentValue + 1));
    public static OptionItem operator --(OptionItem item)
        => item.Do(item => item.SetValue(item.CurrentValue - 1));

    public static void SwitchPreset(int newPreset)
    {
        CurrentPreset = Math.Clamp(newPreset, 0, NumPresets - 1);

        foreach (var op in AllOptions.ToArray())
            op.Refresh();

        SyncAllOptions();
    }
    public static void SyncAllOptions(int targetId = -1)
    {
        if (
            Main.AllPlayerControls.Length <= 1 ||
            AmongUsClient.Instance.AmHost == false ||
            PlayerControl.LocalPlayer == null
        ) return;

        RPC.SyncCustomSettingsRPC(targetId);
    }


    // EventArgs
    private void CallUpdateValueEvent(int beforeValue, int currentValue)
    {
        if (UpdateValueEvent == null) return;
        try
        {
            UpdateValueEvent(this, new UpdateValueEventArgs(beforeValue, currentValue));
        }
        catch (Exception ex)
        {
            Logger.Error($"[{Name}] - Exception occurred when calling UpdateValueEvent", "OptionItem.UpdateValueEvent");
            Logger.Exception(ex, "OptionItem.UpdateValueEvent");
        }
    }

    public class UpdateValueEventArgs(int beforeValue, int currentValue) : EventArgs
    {
        public int CurrentValue { get; set; } = currentValue;
        public int BeforeValue { get; set; } = beforeValue;
    }

    public const int NumPresets = 5;
    public const int PresetId = 0;
}
[Obfuscation(Exclude = true)]
public enum TabGroup
{
    SystemSettings,
    ModSettings,
    ImpostorRoles,
    CrewmateRoles,
    NeutralRoles,
    CovenRoles,
    Addons
}
[Obfuscation(Exclude = true)]
public enum OptionFormat
{
    None,
    Players,
    Seconds,
    Percent,
    Times,
    Multiplier,
    Votes,
    Pieces,
    Health,
    Level,
}
