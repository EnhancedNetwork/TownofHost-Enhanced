using System;

namespace TOHE;

public class IntegerOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, IntegerValueRule rule, bool vanilla) : OptionItem(id, name, rule.GetNearestIndex(defaultValue), tab, isSingleValue, vanillaStr: vanilla)
{
    // 必須情報
    public IntegerValueRule Rule = rule;

    public static IntegerOptionItem Create(int id, string name, IntegerValueRule rule, int defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new IntegerOptionItem(id, name, defaultValue, tab, isSingleValue, rule, vanillaText);
    }
    public static IntegerOptionItem Create(int id, Enum name, IntegerValueRule rule, int defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new IntegerOptionItem(id, name.ToString(), defaultValue, tab, isSingleValue, rule, vanillaText);
    }

    // Getter
    public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
    public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
    public override string GetString()
    {
        return ApplyFormat(Rule.GetValueByIndex(CurrentValue).ToString());
    }
    public override int GetValue()
        => Rule.RepeatIndex(base.GetValue());

    // Setter
    public override void SetValue(int value, bool doSync = true)
    {
        base.SetValue(Rule.RepeatIndex(value), doSync);
    }
}