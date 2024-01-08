namespace TOHE;

public class FloatOptionItem : OptionItem
{
    public FloatValueRule Rule;

    // コンストラクタ
    public FloatOptionItem(int id, string name, float defaultValue, TabGroup tab, bool isSingleValue, FloatValueRule rule, bool vanilla)
    : base(id, name, rule.GetNearestIndex(defaultValue), tab, isSingleValue, vanila:vanilla)
    {
        Rule = rule;
    }
    public static FloatOptionItem Create(int id, string name, FloatValueRule rule, float defaultValue, TabGroup tab, bool isSingleValue, bool vanilla = false)
    {
        return new FloatOptionItem(id, name, defaultValue, tab, isSingleValue, rule, vanilla);
    }

    // Getter
    public override int GetInt() => (int)Rule.GetValueByIndex(CurrentValue);
    public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
    public override string GetString()
    {
        return ApplyFormat(((float)((int)(Rule.GetValueByIndex(CurrentValue) * 100) * 1.0) / 100).ToString());
    }
    public override int GetValue()
        => Rule.RepeatIndex(base.GetValue());

    // Setter
    public override void SetValue(int value, bool doSync = true)
    {
        base.SetValue(Rule.RepeatIndex(value), doSync);
    }
}