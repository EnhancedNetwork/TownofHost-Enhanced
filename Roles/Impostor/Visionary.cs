using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

internal class Visionary : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 3900;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Visionary);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        if (!seer.IsAlive() || !target.IsAlive() || target.Data.IsDead) return string.Empty;

        var customRole = target.GetCustomRole();

        foreach (var SubRole in target.GetCustomSubRoles())
        {
            if (SubRole.Is(CustomRoles.Charmed)
                || SubRole.Is(CustomRoles.Infected)
                || SubRole.Is(CustomRoles.Contagious)
                || SubRole.Is(CustomRoles.Egoist)
                || SubRole.Is(CustomRoles.Recruit)
                || SubRole.Is(CustomRoles.Soulless)
                || SubRole.Is(CustomRoles.Refugee)
                || SubRole.Is(CustomRoles.Admired))
                return Main.roleColors[CustomRoles.Knight];
        }

        if (customRole.IsImpostorTeamV2() || customRole.IsMadmate())
        {
            return Main.roleColors[CustomRoles.Impostor];
        }

        if (customRole.IsCrewmate())
        {
            return Main.roleColors[CustomRoles.Bait];
        }

        return Main.roleColors[CustomRoles.Knight];
    }
}
