using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TOHE.Roles.Crewmate;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Poisoner
{
    private class PoisonedInfo
    {
        public byte PoisonerId;
        public float KillTimer;

        public PoisonedInfo(byte poisonerId, float killTimer)
        {
            PoisonerId = poisonerId;
            KillTimer = killTimer;
        }
    }

    private static readonly int Id = 12700;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;
    private static OptionItem OptionKillDelay;
    private static float KillDelay;
    public static OptionItem CanVent;
    public static OptionItem KillCooldown;
    public static OptionItem HasImpostorVision;
    private static readonly Dictionary<byte, PoisonedInfo> PoisonedPlayers = new();
    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Poisoner, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "PoisonCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner])
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillDelay = FloatOptionItem.Create(Id + 11, "PoisonerKillDelay", new(1f, 60f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 12, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner]);
    }

    public static void Init()
    {
        playerIdList = new();
        PoisonedPlayers.Clear();
        IsEnable = false;

        KillDelay = OptionKillDelay.GetFloat();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!IsThisRole(killer.PlayerId)) return true;
        if (target.Is(CustomRoles.Bait)) return true;
        if (target.Is(CustomRoles.Guardian) && target.AllTasksCompleted()) return true;
        if (target.Is(CustomRoles.Opportunist) && target.AllTasksCompleted() && Options.OppoImmuneToAttacksWhenTasksDone.GetBool()) return false;
        if (target.Is(CustomRoles.Glitch)) return true;
        if (target.Is(CustomRoles.Pestilence)) return true;
        if (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)) return true;
        if (Medic.ProtectList.Contains(target.PlayerId)) return false;

        killer.SetKillCooldown();

        //誰かに噛まれていなければ登録
        if (!PoisonedPlayers.ContainsKey(target.PlayerId))
        {
            PoisonedPlayers.Add(target.PlayerId, new(killer.PlayerId, 0f));
        }
        return false;
    }

    public static void OnFixedUpdate(PlayerControl poisoner)
    {
        var poisonerID = poisoner.PlayerId;
        if (!IsThisRole(poisoner.PlayerId)) return;

        List<byte> targetList = new(PoisonedPlayers.Where(b => b.Value.PoisonerId == poisonerID).Select(b => b.Key));

        foreach (var targetId in targetList)
        {
            var poisonedPoisoner = PoisonedPlayers[targetId];
            if (poisonedPoisoner.KillTimer >= KillDelay)
            {
                var target = Utils.GetPlayerById(targetId);
                KillPoisoned(poisoner, target);
                PoisonedPlayers.Remove(targetId);
            }
            else
            {
                poisonedPoisoner.KillTimer += Time.fixedDeltaTime;
                PoisonedPlayers[targetId] = poisonedPoisoner;
            }
        }
    }
    public static void KillPoisoned(PlayerControl poisoner, PlayerControl target, bool isButton = false)
    {
        if (poisoner == null || target == null || target.Data.Disconnected) return;
        if (target.IsAlive())
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Poison;
            target.SetRealKiller(poisoner);
            target.RpcMurderPlayerV3(target);
            Medic.IsDead(target);
            Logger.Info($"Poisonerに噛まれている{target.name}を自爆させました。", "Poisoner");
            if (!isButton && poisoner.IsAlive())
            {
                RPC.PlaySoundRPC(poisoner.PlayerId, Sounds.KillSound);
                if (target.Is(CustomRoles.Trapper))
                    poisoner.TrapperKilled(target);
                poisoner.Notify(GetString("PoisonerTargetDead"));
                poisoner.SetKillCooldown();
            }
        }
        else
        {
            Logger.Info("Poisonerに噛まれている" + target.name + "はすでに死んでいました。", "Poisoner");
        }
    }
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());

    public static void OnStartMeeting()
    {
        foreach (var targetId in PoisonedPlayers.Keys)
        {
            var target = Utils.GetPlayerById(targetId);
            var poisoner = Utils.GetPlayerById(PoisonedPlayers[targetId].PoisonerId);
            KillPoisoned(poisoner, target);
        }
        PoisonedPlayers.Clear();
    }
    public static void SetKillButtonText()
    {
        HudManager.Instance.KillButton.OverrideText(GetString("PoisonerPoisonButtonText"));
    }
}
