using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Utils;
using static TOHE.Main;
using static TOHE.Options;
using TOHE.Modules;
using TOHE.Roles;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Nuancer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Nuancer;
    private const int Id = 35700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Nuancer);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem OptionVampireDelay;
    private static OptionItem NuancerCanSuicide;

    // For delayed kills (Vampire style)
    private static readonly Dictionary<byte, (PlayerControl Killer, float Timer)> DelayedKills = [];

    // For cursed kills (Witch style)
    private static readonly HashSet<byte> CursedPlayers = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Nuancer);

        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nuancer])
            .SetValueFormat(OptionFormat.Seconds);
        OptionVampireDelay = FloatOptionItem.Create(Id + 11, "NuancerVampireDelay", new(1f, 60f, 1f), 10f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nuancer])
            .SetValueFormat(OptionFormat.Seconds);

        NuancerCanSuicide = BooleanOptionItem.Create(Id + 12, "NuancerSuicideOption", false, TabGroup.ImpostorRoles, false)
        .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Nuancer]);
    }

    public override void Init()
    {
        DelayedKills.Clear();
        CursedPlayers.Clear();
    }

    public override void Add(byte playerId)
    {
        // No initialization required
    }

    public override void SetKillCooldown(byte id)
    {
        AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        RandomKillEvent(killer, target);
        return false;
    }

    private void RandomKillEvent(PlayerControl killer, PlayerControl target)
    { 
          int n;
        if (NuancerCanSuicide.GetBool())
            n = 5;
        else n = 4;
      
       
        var randomDeath = IRandom.Instance.Next(0, n);

        switch (randomDeath)
        {
            case 0: // Normal Kill
                NormalKill(killer, target);
                break;

            case 1: // Delayed kill like vampire
                DelayedKill(killer, target);
                break;

            case 2: // Cursed like witch dies after next meeting
                CursedKill(killer, target);
                break;

            case 3: // Explosive - kills nearby players
                ExplosiveKill(killer, target);
                break;

            case 4: // Suicide - killer dies instead
                SuicideKill(killer);
                break;


            default:  //Should not happen 
                NormalKill(killer, target);
                break;
        }

        killer.SetKillCooldown();
    }

    private static void NormalKill(PlayerControl killer, PlayerControl target)
    {
        killer.RpcMurderPlayer(target);
        target.SetDeathReason(PlayerState.DeathReason.Kill);
        target.SetRealKiller(killer);
        Main.PlayerStates[target.PlayerId].SetDead();
    }

    private static void DelayedKill(PlayerControl killer, PlayerControl target)
    {
        if (!DelayedKills.ContainsKey(target.PlayerId))
        {
            DelayedKills.Add(target.PlayerId, (killer, 0f));
            killer.RPCPlayCustomSound("Bite");
            killer.Notify("You've infected target with a delayed kill!");
        }
    }

    private static void CursedKill(PlayerControl killer, PlayerControl target)
    {
        if (!CursedPlayers.Contains(target.PlayerId))
        {
            CursedPlayers.Add(target.PlayerId);
            killer.RPCPlayCustomSound("Curse");
            killer.Notify("You've cursed the target!");
        }
    }

    private static void SuicideKill(PlayerControl killer)
    {
        killer.RpcMurderPlayer(killer);
        killer.SetDeathReason(PlayerState.DeathReason.Suicide);
        killer.SetRealKiller(killer);
        Main.PlayerStates[killer.PlayerId].SetDead();
        killer.Notify("Your attack backfired!");
    }

    private static void ExplosiveKill(PlayerControl killer, PlayerControl target)
    {
        killer.RpcMurderPlayer(target);
        target.SetDeathReason(PlayerState.DeathReason.Bombed);
        target.SetRealKiller(killer);
        Main.PlayerStates[target.PlayerId].SetDead();

        // Kill nearby players (2m radius)
        foreach (var player in Main.AllAlivePlayerControls)
        {
            if (player.PlayerId == target.PlayerId || player.PlayerId == killer.PlayerId) continue;

            if (player.IsAlive() && Vector2.Distance(target.transform.position, player.transform.position) <= 2f)
            {
                player.RpcMurderPlayer(player);
                player.SetDeathReason(PlayerState.DeathReason.Bombed);
                player.SetRealKiller(killer);
                Main.PlayerStates[player.PlayerId].SetDead();
            }
        }

        killer.RPCPlayCustomSound("Bomb");
        killer.Notify("Your attack exploded!");
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        // Handle delayed kills
        List<byte> toRemove = [];
        foreach (var (targetId, (killer, timer)) in DelayedKills)
        {
            if (timer >= OptionVampireDelay.GetFloat())
            {
                var target = Utils.GetPlayerById(targetId);
                if (target != null && target.IsAlive())
                {
                    target.RpcMurderPlayer(target);
                    target.SetDeathReason(PlayerState.DeathReason.Bite);
                    target.SetRealKiller(killer);
                    Main.PlayerStates[target.PlayerId].SetDead();
                }
                toRemove.Add(targetId);
            }
            else
            {
                DelayedKills[targetId] = (killer, timer + Time.fixedDeltaTime);
            }
        }

        foreach (var id in toRemove)
        {
            DelayedKills.Remove(id);
        }
    }

    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        // Handle cursed kills
        var cursedToDie = new List<byte>();
        foreach (var playerId in CursedPlayers)
        {
            var pc = Utils.GetPlayerById(playerId);
            if (pc != null && pc.IsAlive())
            {
                cursedToDie.Add(playerId);
            }
        }

        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Curse, [.. cursedToDie]);
        CursedPlayers.Clear();
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody)
    {
        // Kill all delayed targets when meeting starts
        foreach (var (targetId, (killer, _)) in DelayedKills)
        {
            var target = Utils.GetPlayerById(targetId);
            if (target != null && target.IsAlive())
            {
                target.RpcMurderPlayer(target);
                target.SetDeathReason(PlayerState.DeathReason.Bite);
                target.SetRealKiller(killer);
                Main.PlayerStates[target.PlayerId].SetDead();
            }
        }
        DelayedKills.Clear();
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (isForMeeting && CursedPlayers.Contains(seen.PlayerId))
        {
            return Utils.ColorString(Palette.ImpostorRed, "†");
        }
        return string.Empty;
    }
}