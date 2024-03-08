using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Bodyguard : RoleBase
{
    private const int Id = 10300;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Bodyguard.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    private static OptionItem ProtectRadiusOpt;

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Bodyguard);
        ProtectRadiusOpt = FloatOptionItem.Create(10302, "BodyguardProtectRadius", new(0.5f, 5f, 0.5f), 1.5f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bodyguard])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
    public static bool OnNearKilling(PlayerControl pc, PlayerControl killer, PlayerControl target)
    {
        var pos = target.transform.position;
        var dis = Vector2.Distance(pos, pc.transform.position);
        if (dis > ProtectRadiusOpt.GetFloat()) return true;

        if (pc.Is(CustomRoles.Bodyguard))
        {
            if (pc.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam())
                Logger.Info($"{pc.GetRealName()} He was a traitor, so he chose to ignore the murder scene", "Bodyguard");
            else
            {
                Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
                pc.RpcMurderPlayerV3(killer);
                pc.SetRealKiller(killer);
                pc.RpcMurderPlayerV3(pc);
                Logger.Info($"{pc.GetRealName()} Stand up and die with the gangster {killer.GetRealName()}", "Bodyguard");
                return false;
            }
        }
        return true;
    }
}
