using AmongUs.GameOptions;
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
    public static class Bloodmoon 
    {
        private static readonly int Id = 28100;

        public static OptionItem MinimumPlayersAliveToKill;
        public static OptionItem KillCooldown;
        public static OptionItem CanKillNum;
        public static Dictionary<byte, int> KillCount;

        public static void SetupCustomOption()
        {
            SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Bloodmoon);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 120f, 2.5f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
            .SetValueFormat(OptionFormat.Seconds);
            CanKillNum = IntegerOptionItem.Create(Id + 11, "HawkCanKillNum", new(1, 15, 1), 1, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
                .SetValueFormat(OptionFormat.Players);
            MinimumPlayersAliveToKill = IntegerOptionItem.Create(Id + 12, "MinimumPlayersAliveToKill", new(0, 15, 1), 4, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
            .SetValueFormat(OptionFormat.Players);
        }
        public static void Init()
        {
            KillCount = [];
        }
        public static void Add(byte PlayerId)
        {
            KillCount.Add(PlayerId, CanKillNum.GetInt());
        }
        public static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
            writer.WritePacked((int)CustomRoles.Bloodmoon);
            writer.Write(playerId);
            writer.Write(KillCount[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void OReceiveRPC(MessageReader reader)
        {
            byte PlayerId = reader.ReadByte();
            int Limit = reader.ReadInt32();
            KillCount[PlayerId] = Limit;
        }
        public static void ApplyGameOptions()
        {
            AURoleOptions.GuardianAngelCooldown = KillCooldown.GetFloat();
            AURoleOptions.ProtectionDurationSeconds = 0f;
        }
        public static bool OnCheckProtect(PlayerControl killer, PlayerControl target)
        {
            if (!target.Is(CustomRoles.Pestilence) 
                && KillCount[killer.PlayerId] > 0 
                && Main.AllAlivePlayerControls.Length >= MinimumPlayersAliveToKill.GetInt()
                && (!target.Is(CustomRoles.NiceMini) || Mini.Age > 18))
            {
                killer.RpcMurderPlayerV3(target);
                killer.RpcResetAbilityCooldown();
                KillCount[killer.PlayerId]--;
                SendRPC(killer.PlayerId);
            }
            else if (Main.AllAlivePlayerControls.Length < MinimumPlayersAliveToKill.GetInt()) killer.Notify(GetString("HawkTooManyDied"));
            return false;
        }
        public static bool CanKill(byte id) => KillCount.TryGetValue(id, out var x) && x > 0;
        public static string GetRevengeLimit(byte playerId) => Utils.ColorString(CanKill(playerId) ? Utils.GetRoleColor(CustomRoles.Bloodmoon).ShadeColor(0.25f) : Color.gray, KillCount.TryGetValue(playerId, out var killLimit) ? $"({killLimit})" : "Invalid");

    }
}
