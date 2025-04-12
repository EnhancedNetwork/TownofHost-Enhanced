using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class YinYanger : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.YinYanger;
    const int Id = 29100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.YinYanger);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public static OptionItem KillCooldown;
    public static Dictionary<byte, (PlayerControl yin, PlayerControl yang)> Yanged = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.YinYanger);
        KillCooldown = FloatOptionItem.Create(Id + 2, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.YinYanger])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        Yanged.Clear();
    }
    public override void Add(byte playerId)
    {
        Yanged[playerId] = new();
        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    private static bool CheckAvailability()
    {
        var tocheck = Main.AllAlivePlayerControls.Length - Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.YinYanger));
        var result = Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.YinYanger)) * 2;
        return tocheck >= result;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        var (yin, yang) = Yanged[killer.PlayerId];
        if (yin && yang || !CheckAvailability()) return true;
        if (Yanged.Where(x => x.Key != killer.PlayerId).Any(x => x.Value.yin == target || x.Value.yang == target))
        {
            killer.Notify(string.Format(GetString("YinYangerAlreadyMarked"), target.GetRealName(clientData: true)));
            return false;
        }

        if (yin)
        {
            if (target.PlayerId == yin.PlayerId)
                return false;

            Yanged[killer.PlayerId] = (yin, target);

        }
        else
        {
            Yanged[killer.PlayerId] = (target, yang);
        }

        killer.SetKillCooldown();
        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Yanged[_state.PlayerId] = new();
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting) return;
        if (Yanged.TryGetValue(target.PlayerId, out _))
            Yanged[target.PlayerId] = new();
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        var (yin, yang) = Yanged[seer.PlayerId];
        Color col = seen.PlayerId == yin?.PlayerId ? Color.white : new Color32(46, 46, 46, 255);

        return seen.PlayerId == yin?.PlayerId || seen.PlayerId == yang?.PlayerId ? ColorString(col, "☯") : string.Empty;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;
        var (yin, yang) = Yanged[player.PlayerId];
        if (!yin || !yang) return;

        if (GetDistance(yin.GetCustomPosition(), yang.GetCustomPosition()) < 1.5f && !(yin.IsTransformedNeutralApocalypse() || yang.IsTransformedNeutralApocalypse()))
        {
            yin.SetDeathReason(PlayerState.DeathReason.Equilibrium);
            yin.RpcMurderPlayer(yang);

            yang.SetDeathReason(PlayerState.DeathReason.Equilibrium);
            yang.RpcMurderPlayer(yin);
            Yanged[player.PlayerId] = new();
        }
    }

}
