using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles._Ghosts_.Crewmate;

internal class Hawk : RoleBase
{

    //===========================SETUP================================\\
    private const int Id = 28000;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;

    //==================================================================\\
    public static OptionItem KillCooldown;
    public static OptionItem HawkCanKillNum;
    public static OptionItem MinimumPlayersAliveToKill;
    public static OptionItem MissChance;
    public static OptionItem IncreaseByOneIfConvert;
    
    public static Dictionary<byte, int> KillCount = [];
    public static Dictionary<byte, float> KillerChanceMiss = [];
    public static int KeepCount = 0;
    public static void SetupCustomOptions()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Hawk);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 120f, 2.5f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Seconds);
        HawkCanKillNum = IntegerOptionItem.Create(Id + 11, "HawkCanKillNum", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Players);
        MissChance = FloatOptionItem.Create(Id + 12, "MissChance", new(0f, 100f, 2.5f), 85f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Percent);
        MinimumPlayersAliveToKill = IntegerOptionItem.Create(Id + 13, "MinimumPlayersAliveToKill", new(0, 15, 1), 4, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Players);
        IncreaseByOneIfConvert = BooleanOptionItem.Create(Id + 14, "IncreaseByOneIfConvert", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk]);
    }

    public override void Init()
    {
        KeepCount = 0;
        KillerChanceMiss.Clear();
        KillCount.Clear();
    }
    public override void Add(byte PlayerId)
    {
        KillCount.Add(PlayerId, HawkCanKillNum.GetInt());

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.IsAnySubRole(x => x.IsConverted()))
            {
                KeepCount++;
            }
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        int ThisCount = 0;
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.IsAnySubRole(x => x.IsConverted()))
            {
                ThisCount++;
            }
        }
        if (ThisCount > KeepCount && IncreaseByOneIfConvert.GetBool())
        {
            KeepCount++;
            var hawk = PlayerIds.ToList().First();
            KillCount[hawk]++;
        }

    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Hawk);
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
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = KillCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (!KillerChanceMiss.ContainsKey(killer.PlayerId))
            KillerChanceMiss.Add(killer.PlayerId, MissChance.GetFloat());

        if (CheckRetriConflicts(killer, target) && killer.RpcCheckAndMurder(target, true))
        {
            killer.RpcMurderPlayer(target);
            killer.RpcResetAbilityCooldown();
            KillCount[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
        }
        else if (KillCount[killer.PlayerId] <= 0) killer.Notify(GetString("HawkKillMax"));
        else if (Main.AllAlivePlayerControls.Length < MinimumPlayersAliveToKill.GetInt()) killer.Notify(GetString("HawkKillTooManyDead"));
        else
        {
            killer.RpcResetAbilityCooldown();
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Hawk), GetString("HawkMissed")));
        }

        KillerChanceMiss[killer.PlayerId] -= KillerChanceMiss[killer.PlayerId] <= 35 ? 0 : 35f;
        Logger.Info($" {killer.GetRealName()}'s DieChance is :{100f - KillerChanceMiss[killer.PlayerId]}%","Hawk");
        return false;
    }

    private static bool CheckRetriConflicts(PlayerControl killer, PlayerControl target)
    {
        var rnd = IRandom.Instance;

        return target != null && Main.AllAlivePlayerControls.Length >= MinimumPlayersAliveToKill.GetInt()
            && KillCount[killer.PlayerId] > 0
            && rnd.Next(100) >= KillerChanceMiss[killer.PlayerId]
            && !target.Is(CustomRoles.Pestilence)
            && (!target.Is(CustomRoles.NiceMini) || Mini.Age > 18);
    }
    public static bool CanKill(byte id) => KillCount.TryGetValue(id, out var x) && x > 0;
    public override string GetProgressText(byte playerId, bool coms) 
        => ColorString(CanKill(playerId) ? GetRoleColor(CustomRoles.Hawk).ShadeColor(0.25f) : Color.gray, KillCount.TryGetValue(playerId, out var killLimit) ? $"({killLimit})" : "Invalid");

}


