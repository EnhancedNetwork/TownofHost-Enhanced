using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor
{
    public static class Nemesis
    {
        private static readonly int Id = 3600;


        public static OptionItem MinimumPlayersAliveToRevenge;
        public static OptionItem KillCooldown;
        public static OptionItem MafiaCanKillNum;
        public static Dictionary<byte, int> KillCount;

        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Nemesis);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 120f, 2.5f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis])
            .SetValueFormat(OptionFormat.Seconds);
            MafiaCanKillNum = IntegerOptionItem.Create(Id + 11, "NemesisCanKillNum", new(1, 15, 1), 1, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis])
                .SetValueFormat(OptionFormat.Players);
            MinimumPlayersAliveToRevenge = IntegerOptionItem.Create(Id + 12, "MinimumPlayersAliveToRetri", new(0, 15, 1), 4, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis])
            .SetValueFormat(OptionFormat.Players);
        }
        public static void Init()
        {
            KillCount = [];
        }
        public static void Add(byte PlayerId)
        {
            KillCount.Add(PlayerId, MafiaCanKillNum.GetInt());
        }
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
            writer.WritePacked((int)CustomRoles.Nemesis);
            writer.Write(playerId);
            writer.Write(KillCount[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte PlayerId = reader.ReadByte();
            int Limit = reader.ReadInt32();
            KillCount[PlayerId] = Limit;
        }
        public static void SetKillCooldown()
        {
            AURoleOptions.GuardianAngelCooldown = KillCooldown.GetFloat();
            AURoleOptions.ProtectionDurationSeconds = 0f;
        }
        public static bool OnCheckProtect(PlayerControl killer, PlayerControl target)
        {
            if (!target.Is(CustomRoles.Pestilence) 
                && KillCount[killer.PlayerId] > 0 
                && Main.AllAlivePlayerControls.Length >= MinimumPlayersAliveToRevenge.GetInt()
                && (target.Is(CustomRoles.NiceMini) ? Mini.Age > 18 : true))
            {
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
                killer.RpcMurderPlayerV3(target);
                killer.RpcResetAbilityCooldown();
                KillCount[killer.PlayerId]--;
                SendRPC(killer.PlayerId);
            }
            else if (Main.AllAlivePlayerControls.Count() < MinimumPlayersAliveToRevenge.GetInt()) killer.Notify(GetString("NemesisTooManyDied"));
            CustomRoles.Necromancer.IsEnable();
            return false;
        }
        public static bool CanKill(byte id) => KillCount.TryGetValue(id, out var x) && x > 0;
        public static string GetRevengeLimit(byte playerId) => Utils.ColorString(CanKill(playerId) ? Utils.GetRoleColor(CustomRoles.Nemesis).ShadeColor(0.25f) : Color.gray, KillCount.TryGetValue(playerId, out var killLimit) ? $"({killLimit})" : "Invalid");

    }
}
