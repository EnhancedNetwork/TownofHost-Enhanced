
namespace TOHE;

public class MultipleStringOptionItem(int startId, int maxCount, int startingCount, string nameKey, string[] selections, int defaultIndex, TabGroup tab, bool isSingleValue, bool vanillaText, bool useGetString)
{
    public int MaxCount = maxCount;
    public int Count = startingCount;
    private readonly int StartId = startId;
    private readonly string Key = nameKey;
    private readonly string[] Selections = selections;
    private readonly int DefaultIndex = defaultIndex;
    private readonly TabGroup Tab = tab;
    private readonly bool IsSingleValue = isSingleValue;
    private readonly bool Vanilla = vanillaText;
    private readonly bool UseGetString = useGetString;

    private readonly List<IndividualStringOptionItem> Options = [];

    public static MultipleStringOptionItem Create(int startId, int maxCount, int startingCount, string nameKey, string[] selections, int defaultIndex, TabGroup tab, bool isSingleValue, bool vanillaText = false, bool useGetString = true)
    {
        var made = new MultipleStringOptionItem(startId, maxCount, startingCount, nameKey, selections, defaultIndex, tab, isSingleValue, vanillaText, useGetString);
        made.InitOptions();
        return made;
    }

    public void InitOptions()
    {
        for (int i = 0; i < MaxCount; i++)
        {
            var option = IndividualStringOptionItem.Create(StartId + i, i + 1, Key, Selections, DefaultIndex, Tab, IsSingleValue, Vanilla, UseGetString);
            option.SetHidden(i >= Count);
            Options.Add(option);
        }
    }

    public MultipleStringOptionItem SetParent(OptionItem parent, bool OverrideRoleNames = true)
    {
        foreach (var option in Options) option.SetParent(parent, OverrideRoleNames);

        return this;
    }

    public MultipleStringOptionItem SetGameMode(CustomGameMode value)
    {
        foreach (var option in Options) option.SetGameMode(value);

        return this;
    }

    public void Refresh()
    {
        for (int i = 0; i < MaxCount; i++)
        {
            Options[i].SetHidden(i >= Count);
        }
    }

    public List<IndividualStringOptionItem> GetOptions(bool includeHidden = false)
    {
        if (includeHidden) return Options;

        return [.. Options.Take(Count)];
    }
}

public class IndividualStringOptionItem(int id, int number, string name, int defaultValue, TabGroup tab, bool isSingleValue, string[] selections, bool vanilla, bool useGetString) : StringOptionItem(id, name, defaultValue, tab, isSingleValue, selections, vanilla, useGetString)
{
    public int Number = number;
    public static IndividualStringOptionItem Create(int id, int number, string name, string[] selections, int defaultIndex, TabGroup tab, bool isSingleValue, bool vanillaText = false, bool useGetString = true)
    {
        return new IndividualStringOptionItem(id, number, name, defaultIndex, tab, isSingleValue, selections, vanillaText, useGetString);
    }

    public override string GetName(bool disableColor = false, bool console = false)
    {
        return disableColor ?
            $"{Translator.GetString(Name, ReplacementDictionary, console)} {Number}" :
            $"{Utils.ColorString(NameColor, Translator.GetString(Name, ReplacementDictionary))} {Number}";
    }
    public override string GetNameVanilla()
    {
        return $"{Translator.GetString(Name, ReplacementDictionary)} {Number}";
    }
}