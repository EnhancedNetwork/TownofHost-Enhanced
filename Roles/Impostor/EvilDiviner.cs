using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Impostor
{
    public static class EvilDiviner
    {
        private static readonly int Id = 3100;
        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        private static OptionItem KillCooldown;
        private static OptionItem DivinationMaxCount;

        public static Dictionary<byte, int> DivinationCount = new();
        public static Dictionary<byte, List<byte>> DivinationTarget = new();


        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EvilDiviner);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilDiviner])
                .SetValueFormat(OptionFormat.Seconds);
            DivinationMaxCount = IntegerOptionItem.Create(Id + 11, "DivinationMaxCount", new(1, 15, 1), 5, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilDiviner])
                .SetValueFormat(OptionFormat.Times);
        }
        public static void Init()
        {
            playerIdList = new();
            DivinationCount = new();
            DivinationTarget = new();
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            DivinationCount.TryAdd(playerId, DivinationMaxCount.GetInt());
            DivinationTarget.TryAdd(playerId, new());
            IsEnable = true;

            var pc = Utils.GetPlayerById(playerId);
            pc.AddDoubleTrigger();
        }

        private static void SendRPC(byte playerId, byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEvilDiviner, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(DivinationCount[playerId]);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            {
                if (DivinationCount.ContainsKey(playerId))
                    DivinationCount[playerId] = reader.ReadInt32();
                else
                    DivinationCount.Add(playerId, DivinationMaxCount.GetInt());
            }{
                if (DivinationCount.ContainsKey(playerId))
                    DivinationTarget[playerId].Add(reader.ReadByte());
                else
                    DivinationTarget.Add(playerId, new());
            }
        }

        public static void SetKillCooldown(byte id)
        {
            Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (DivinationCount[killer.PlayerId] > 0)
            {
                return killer.CheckDoubleTrigger(target, () => { SetDivination(killer, target); });
            }
            else return true;  
        }

        public static bool IsDivination(byte seer, byte target)
        {
            if (DivinationTarget[seer].Contains(target))
            {
                return true;
            }
            return false;
        }
        public static void SetDivination(PlayerControl killer, PlayerControl target)
        {
            if (!IsDivination(killer.PlayerId, target.PlayerId))
            {
                DivinationCount[killer.PlayerId]--;
                DivinationTarget[killer.PlayerId].Add(target.PlayerId);
                Logger.Info($"{killer.GetNameWithRole()}：占った 占い先→{target.GetNameWithRole()} || 残り{DivinationCount[killer.PlayerId]}回", "EvilDiviner");
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
            DivinationTarget.Do(x =>
            {
                if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                    IsWatch = true;
            });
            return IsWatch;
        }
        public static string GetDivinationCount(byte playerId) => Utils.ColorString(DivinationCount[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.EvilDiviner).ShadeColor(0.25f) : Color.gray, DivinationCount.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
    }
}