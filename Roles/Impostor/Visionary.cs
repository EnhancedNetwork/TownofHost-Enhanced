﻿namespace TOHE.Roles.Impostor;

internal class Visionary : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 3900;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    public override void SetupCustomOption()
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
            if (SubRole is CustomRoles.Charmed
                or CustomRoles.Infected
                or CustomRoles.Contagious
                or CustomRoles.Egoist
                or CustomRoles.Recruit
                or CustomRoles.Soulless
                or CustomRoles.Admired)
                return Main.roleColors[CustomRoles.Knight];
        }
        if (Main.PlayerStates[target.PlayerId].IsRandomizer)
        {
            return Main.roleColors[CustomRoles.Crewmate];
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
