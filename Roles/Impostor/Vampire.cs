using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using UnityEngine;
using TOHE.Roles.AddOns.Common;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Vampire
{
    private class BittenInfo(byte vampierId, float killTimer)
    {
        public byte VampireId = vampierId;
        public float KillTimer = killTimer;
    }

    private static readonly int Id = 5000;
    private static readonly List<byte> PlayerIdList = [];
    public static bool IsEnable = false;

    public static OptionItem OptionKillDelay;
    public static OptionItem CanVent;
    public static OptionItem VampiressChance;
    private static float KillDelay;
    private static readonly Dictionary<byte, BittenInfo> BittenPlayers = [];
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Vampire);
        OptionKillDelay = FloatOptionItem.Create(Id + 10, "VampireKillDelay", new(1f, 60f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampire])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampire]);
        VampiressChance = IntegerOptionItem.Create(Id + 12, "VampiressChance", new(0, 100, 5), 25, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampire])
            .SetValueFormat(OptionFormat.Percent);
    }
    public static void Init()
    {
        PlayerIdList.Clear();
        BittenPlayers.Clear();
        IsEnable = false;

        KillDelay = OptionKillDelay.GetFloat();
    }
    public static void Add(byte playerId)
    {
        PlayerIdList.Add(playerId);
        IsEnable = true;
    }
    public static bool IsThisRole(byte playerId) => PlayerIdList.Contains(playerId);

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!IsThisRole(killer.PlayerId)) return true;
        if (target.Is(CustomRoles.Bait)) return true;
        if (target.Is(CustomRoles.Pestilence)) return true;
        if (target.Is(CustomRoles.Guardian) && target.AllTasksCompleted()) return true;
        if (target.Is(CustomRoles.Opportunist) && target.AllTasksCompleted() && Options.OppoImmuneToAttacksWhenTasksDone.GetBool()) return false;
        if (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)) return true;
        if (Medic.ProtectList.Contains(target.PlayerId)) return false;

        killer.SetKillCooldown();
        killer.RPCPlayCustomSound("Bite");

        //誰かに噛まれていなければ登録
        if (!BittenPlayers.ContainsKey(target.PlayerId))
        {
            BittenPlayers.Add(target.PlayerId, new(killer.PlayerId, 0f));
        }
        return false;
    }

    public static void OnFixedUpdate(PlayerControl vampire)
    {
        if (!IsThisRole(vampire.PlayerId)) return;

        var vampireID = vampire.PlayerId;
        List<byte> targetList = new(BittenPlayers.Where(b => b.Value.VampireId == vampireID).Select(b => b.Key));

        for (var id = 0; id < targetList.Count; id++)
        {
            var targetId = targetList[id];
            var bitten = BittenPlayers[targetId];

            if (bitten.KillTimer >= KillDelay)
            {
                var target = Utils.GetPlayerById(targetId);
                KillBitten(vampire, target);
                BittenPlayers.Remove(targetId);
            }
            else
            {
                bitten.KillTimer += Time.fixedDeltaTime;
                BittenPlayers[targetId] = bitten;
            }
        }
    }
    public static void KillBitten(PlayerControl vampire, PlayerControl target, bool isButton = false)
    {
        if (vampire == null || target == null || target.Data.Disconnected) return;
        if (target.IsAlive())
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Bite;
            target.SetRealKiller(vampire);
            //target.RpcMurderPlayer(target, true);
            //target.RpcMurderPlayerV2(target);
            target.RpcMurderPlayerV3(target);

            Medic.IsDead(target);
            Logger.Info($"{target.name} self-kill while being bitten by Vampire.", "Vampire");
            if (!isButton && vampire.IsAlive())
            {
                RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);
                if (target.Is(CustomRoles.Trapper))
                    vampire.TrapperKilled(target);
                vampire.Notify(GetString("VampireTargetDead"));
                vampire.SetKillCooldown();
            }
        }
        else
        {
            Logger.Info($"{target.name} was dead after being bitten by Vampire", "Vampire");
        }
    }

    public static void OnStartMeeting()
    {
        foreach (var targetId in BittenPlayers.Keys)
        {
            var target = Utils.GetPlayerById(targetId);
            var vampire = Utils.GetPlayerById(BittenPlayers[targetId].VampireId);
            KillBitten(vampire, target);
        }
        BittenPlayers.Clear();
    }
    public static void SetKillButtonText()
    {
        HudManager.Instance.KillButton.OverrideText(GetString("VampireBiteButtonText"));
    }
}
