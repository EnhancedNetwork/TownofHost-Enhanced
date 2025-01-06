namespace TOHE;

public class PresetOptionItem(int defaultValue, TabGroup tab, bool vanilla) : OptionItem(0, "Preset", defaultValue, tab, true, vanillaStr:vanilla)
{
    public IntegerValueRule Rule = (0, NumPresets - 1, 1);
    public int ValuePresets = NumPresets;

    public static PresetOptionItem Create(int defaultValue, TabGroup tab, bool vanillaText = false)
    {
        return new PresetOptionItem(defaultValue, tab, vanillaText);
    }

    // Getter
    public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
    public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
    public override string GetString()
    {
        return CurrentValue switch
        {
            0 => Main.Preset1.Value == (string)Main.Preset1.DefaultValue ? Translator.GetString("Preset_1") : Main.Preset1.Value,
            1 => Main.Preset2.Value == (string)Main.Preset2.DefaultValue ? Translator.GetString("Preset_2") : Main.Preset2.Value,
            2 => Main.Preset3.Value == (string)Main.Preset3.DefaultValue ? Translator.GetString("Preset_3") : Main.Preset3.Value,
            3 => Main.Preset4.Value == (string)Main.Preset4.DefaultValue ? Translator.GetString("Preset_4") : Main.Preset4.Value,
            4 => Main.Preset5.Value == (string)Main.Preset5.DefaultValue ? Translator.GetString("Preset_5") : Main.Preset5.Value,
            _ => null,
        };
    }
    public override int GetValue()
        => Rule.RepeatIndex(base.GetValue());

    // Setter
    public override void SetValue(int value, bool doSync = true)
    {
        base.SetValue(Rule.RepeatIndex(value), doSync);
        SwitchPreset(Rule.RepeatIndex(value));
    }
    public override void SetValue(int afterValue, bool doSave, bool doSync = true)
    {
        base.SetValue(Rule.RepeatIndex(afterValue), doSave, doSync);
        SwitchPreset(Rule.RepeatIndex(afterValue));
    }
}