namespace TOHE;

public class BooleanOptionItem : OptionItem
{
    public const string TEXT_true = "ColoredOn";
    public const string TEXT_false = "ColoredOff";

    // Constructor
    public BooleanOptionItem(int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue, bool vanilla)
    : base(id, name, defaultValue ? 1 : 0, tab, isSingleValue, vanillaStr:vanilla)
    {
    }
    public static BooleanOptionItem Create(int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText)
    {
        return new BooleanOptionItem(id, name, defaultValue, tab, isSingleValue, vanillaText);
    }

    // Getter
    public override string GetString()
    {
        return Translator.GetString(GetBool() ? TEXT_true : TEXT_false);
    }

    // Setter
    public override void SetValue(int value, bool doSync = true)
    {
        base.SetValue(value % 2 == 0 ? 0 : 1, doSync);
    }
}