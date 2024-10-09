using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Kamikaze : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 26900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Kamikaze);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem OptMaxMarked;

    private readonly HashSet<byte> KamikazedList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Kamikaze);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Kamikaze])
            .SetValueFormat(OptionFormat.Seconds);
        OptMaxMarked = IntegerOptionItem.Create(Id + 11, "KamikazeMaxMarked", new(1, 14, 1), 14, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Kamikaze])
           .SetValueFormat(OptionFormat.Times);

    }
    public override void Add(byte playerId)
    {
        AbilityLimit = OptMaxMarked.GetInt();

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }
    
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        => KamikazedList.Contains(seen.PlayerId) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), "∇") : string.Empty;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), GetString("KamikazeHostage"))); 
            return false;
        }

        return killer.CheckDoubleTrigger(target, () =>
        {

            if (AbilityLimit >= 1 && !KamikazedList.Contains(target.PlayerId)) 
            {
                KamikazedList.Add(target.PlayerId);
                killer.RpcGuardAndKill(killer);
                killer.SetKillCooldown(KillCooldown.GetFloat());
                Utils.NotifyRoles(SpecifySeer: killer);
                AbilityLimit--;
                SendSkillRPC();
            } 
            else
            {
                killer.RpcMurderPlayer(target);
            }
        });
        
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (_Player == null || _Player.IsDisconnected()) return;

        foreach (var BABUSHKA in KamikazedList)
        {
            var pc = Utils.GetPlayerById(BABUSHKA);
            if (!pc.IsAlive()) continue;

            pc.SetDeathReason(PlayerState.DeathReason.Targeted);
            if (!inMeeting)
            {
                pc.RpcMurderPlayer(pc);
            }
            else
            {
                pc.RpcExileV2();
                Main.PlayerStates[pc.PlayerId].SetDead();
                pc.Data.IsDead = true;
            }
            pc.SetRealKiller(_Player);
        }
        KamikazedList.Clear();
    }

    public override string GetProgressText(byte playerId, bool comms)
        => Utils.ColorString(AbilityLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Kamikaze).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
}

