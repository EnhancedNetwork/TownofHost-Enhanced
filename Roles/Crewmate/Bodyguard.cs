using System.Collections.Generic;
using System.Linq;
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

    private static HashSet<byte> playerList = [];

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
        playerList = [];
    }
    public override void Add(byte playerId)
    {
        playerList.Add(playerId);
        On = true;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId || playerList.Count <= 0) return true;

        foreach (var bodyguardId in playerList.ToArray())
        {
            var bodyguard = Utils.GetPlayerById(bodyguardId);
            if (bodyguard == null || !bodyguard.IsAlive()) continue;

            var pos = target.transform.position;
            var dis = Vector2.Distance(pos, bodyguard.transform.position);
            if (dis > ProtectRadiusOpt.GetFloat()) return true;

            if (bodyguard.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam())
            {
                Logger.Info($"{bodyguard.GetRealName()} He was a traitor, so he chose to ignore the murder scene", "Bodyguard");
            }
            else if (bodyguard.CheckForInvalidMurdering(killer))
            {
                Main.PlayerStates[bodyguardId].deathReason = PlayerState.DeathReason.Sacrifice;
                bodyguard.RpcMurderPlayer(killer);
                bodyguard.SetRealKiller(killer);
                bodyguard.RpcMurderPlayer(bodyguard);
                Logger.Info($"{bodyguard.GetRealName()} Stand up and die with the gangster {killer.GetRealName()}", "Bodyguard");
                return false;
            }
        }

        return true;
    }
}
