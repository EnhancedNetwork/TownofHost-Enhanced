using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Bodyguard : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Bodyguard;
    private const int Id = 10300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Bodyguard);

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem ProtectRadiusOpt;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Bodyguard);
        ProtectRadiusOpt = FloatOptionItem.Create(10302, "BodyguardProtectRadius", new(0.5f, 5f, 0.5f), 1.5f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bodyguard])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        var bodyguard = _Player;
        if (!bodyguard.IsAlive() || killer.PlayerId == target.PlayerId || bodyguard.PlayerId == target.PlayerId) return false;

        var killerRole = killer.GetCustomRole();
        // Not should kill
        if (killerRole is CustomRoles.Taskinator
            or CustomRoles.Crusader
            or CustomRoles.Veteran
            or CustomRoles.Deputy)
            return false;
        if (killer.IsTransformedNeutralApocalypse())
        {
            Logger.Info($"{bodyguard.GetRealName()} was too scared of {killer.GetRealName()}'s power, so they could not protect {target.GetRealName()}", "Bodyguard");
        }

        var pos = target.transform.position;
        var dis = Utils.GetDistance(pos, bodyguard.transform.position);
        if (dis > ProtectRadiusOpt.GetFloat()) return false;

        if (bodyguard.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam())
        {
            Logger.Info($"{bodyguard.GetRealName()} He was a impostor, so he chose to ignore the murder scene", "Bodyguard");
        }
        else if (bodyguard.Is(CustomRoles.Enchanted) && killer.GetCustomRole().IsCoven())
        {
            Logger.Info($"{bodyguard.GetRealName()} He was a Coven, so he chose to ignore the murder scene", "Bodyguard");
        }
        else if (bodyguard.CheckForInvalidMurdering(killer))
        {
            bodyguard.SetDeathReason(PlayerState.DeathReason.Sacrifice);
            bodyguard.RpcMurderPlayer(killer);
            bodyguard.SetRealKiller(killer);
            bodyguard.RpcMurderPlayer(bodyguard);
            Logger.Info($"{bodyguard.GetRealName()} Stand up and die with the gangster {killer.GetRealName()}", "Bodyguard");
            return true;
        }

        return false;
    }
}
