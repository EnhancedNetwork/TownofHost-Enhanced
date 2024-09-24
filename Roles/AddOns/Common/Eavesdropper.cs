using static TOHE.Options;
using static TOHE.Roles.AddOns.Common.Sloth;

namespace TOHE.Roles.AddOns.Common;

public class Eavesdropper : IAddon
{
    public const int Id = 29900;
    public static readonly HashSet<byte> IsActive = [];
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Helpful;
    private static readonly HashSet<byte> playerList = [];

    public static OptionItem ImpCanBeEavesdropper;
    public static OptionItem CrewCanBeEavesdropper;
    public static OptionItem NeutralCanBeEavesDropper;

    public static readonly Dictionary<byte, string> EavesdropperNotify = [];

    public static OptionItem EavesdropPercentChance;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Eavesdropper, canSetNum: true, teamSpawnOptions: true);
        EavesdropPercentChance = FloatOptionItem.Create(Id + 10, "EavesdropPercentChance", new(5f, 100f, 5f), 50f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eavesdropper])
            .SetValueFormat(OptionFormat.Percent);
    }

    public void Init()
    {
        IsEnable = false;
        playerList.Clear();
    }

    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    {
        playerList.Remove(playerId);

        if (!playerList.Any())
            IsEnable = false;
    }
}
