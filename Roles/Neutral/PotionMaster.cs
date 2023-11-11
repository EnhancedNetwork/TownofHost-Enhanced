using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral
{
    public static class PotionMaster
    {
        private static readonly int Id = 17700;
        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        private static OptionItem KillCooldown;
        private static OptionItem RitualMaxCount;
        public static OptionItem CanVent;
        public static OptionItem HasImpostorVision;

        public static Dictionary<byte, int> RitualCount = new();
        public static Dictionary<byte, List<byte>> RitualTarget = new();


        public static void SetupCustomOption()
        {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PotionMaster, 1, zeroOne: false);
            KillCooldown = FloatOptionItem.Create(Id + 14, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster])
                .SetValueFormat(OptionFormat.Seconds);
            RitualMaxCount = IntegerOptionItem.Create(Id + 11, "RitualMaxCount", new(1, 15, 1), 5, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster])
                .SetValueFormat(OptionFormat.Times);
        CanVent = BooleanOptionItem.Create(Id + 12, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
        }
        public static void Init()
        {
            playerIdList = new();
            RitualCount = new();
            RitualTarget = new();
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            RitualCount.TryAdd(playerId, RitualMaxCount.GetInt());
            RitualTarget.TryAdd(playerId, new());
            IsEnable = true;

            var pc = Utils.GetPlayerById(playerId);
            pc.AddDoubleTrigger();

            if (!AmongUsClient.Instance.AmHost) return;
                if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }

        private static void SendRPC(byte playerId, byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPotionMaster, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(RitualCount[playerId]);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            {
                if (RitualCount.ContainsKey(playerId))
                    RitualCount[playerId] = reader.ReadInt32();
                else
                    RitualCount.Add(playerId, RitualMaxCount.GetInt());
            }{
                if (RitualCount.ContainsKey(playerId))
                    RitualTarget[playerId].Add(reader.ReadByte());
                else
                    RitualTarget.Add(playerId, new());
            }
        }
        public static void SetKillCooldown(byte id)
        {
            Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {

            if (RitualCount[killer.PlayerId] > 0)
            {
                return killer.CheckDoubleTrigger(target, () => { SetRitual(killer, target); });
            }
            else return true;  
        }

        public static bool IsRitual(byte seer, byte target)
        {
            if (RitualTarget[seer].Contains(target))
            {
                return true;
            }
            return false;
        }
        public static void SetRitual(PlayerControl killer, PlayerControl target)
        {
            if (!IsRitual(killer.PlayerId, target.PlayerId))
            {
                RitualCount[killer.PlayerId]--;
                RitualTarget[killer.PlayerId].Add(target.PlayerId);
                Logger.Info($"{killer.GetNameWithRole()}：占った 占い先→{target.GetNameWithRole()} || 残り{RitualCount[killer.PlayerId]}回", "PotionMaster");
                Utils.NotifyRoles(SpecifySeer: killer);

                SendRPC(killer.PlayerId, target.PlayerId);
                //キルクールの適正化
                killer.SetKillCooldown();
                //killer.RpcGuardAndKill(target);
            }
        }
        public static bool IsShowTargetRole(PlayerControl seer, PlayerControl target)
        {
            var IsWatch = false;
            RitualTarget.Do(x =>
            {
                if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                    IsWatch = true;
            });
            return IsWatch;
        }
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
        public static void CanUseVent(PlayerControl player)
        {
            bool canUse = CanVent.GetBool();
            DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(canUse && !player.Data.IsDead);
            player.Data.Role.CanVent = canUse;
        }

        public static string GetRitualCount(byte playerId) => Utils.ColorString(RitualCount[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.PotionMaster).ShadeColor(0.25f) : Color.gray, RitualCount.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
    }
}