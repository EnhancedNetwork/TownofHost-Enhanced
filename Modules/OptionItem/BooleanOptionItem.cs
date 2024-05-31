using System;

namespace TOHE;

public class BooleanOptionItem(int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue, bool vanilla) : OptionItem(id, name, defaultValue ? 1 : 0, tab, isSingleValue, vanillaStr:vanilla)
{
    public const string TEXT_true = "ColoredOn";
    public const string TEXT_false = "ColoredOff";

    public static BooleanOptionItem Create(int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new BooleanOptionItem(1, name, defaultValue, tab, isSingleValue, vanillaText);
    }
    public static BooleanOptionItem Create(int id, Enum name, bool defaultValue, TabGroup tab, bool isSingleValue, bool vanillaText = false)
    {
        return new BooleanOptionItem(1, name.ToString(), defaultValue, tab, isSingleValue, vanillaText);
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