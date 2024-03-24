using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles._Ghosts_.Impostor;

internal class Bloodmoon : RoleBase
{

    //===========================SETUP================================\\
    private const int Id = 28100;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;

    //==================================================================\\

    public static OptionItem KillCooldown;
    public static OptionItem CanKillNum;
    private static OptionItem TimeTilDeath;
    
    public static Dictionary<byte, int> KillCount = [];
    public static Dictionary<byte, int> PlayerDie = [];
    public static Dictionary<byte, long> LastTime = [];
    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Bloodmoon);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
        .SetValueFormat(OptionFormat.Seconds);
        CanKillNum = IntegerOptionItem.Create(Id + 11, "BloodMoonCanKillNum", new(1, 15, 1), 1, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
            .SetValueFormat(OptionFormat.Players);
        TimeTilDeath = IntegerOptionItem.Create(Id + 12, "BloodMoonTimeTilDie", new(1, 120, 1), 60, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
        .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        KillCount.Clear();
        PlayerIds.Clear();
        PlayerDie.Clear();
        LastTime.Clear();
    }
    public override void Add(byte PlayerId)
    {
        KillCount.Add(PlayerId, CanKillNum.GetInt());
        PlayerIds.Add(PlayerId);
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixUpdateOthers);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Bloodmoon);
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
        if (KillCount[killer.PlayerId] > 0 
            && killer.RpcCheckAndMurder(target, true)
            && !PlayerDie.ContainsKey(target.PlayerId))
        {
            PlayerDie.Add(target.PlayerId, TimeTilDeath.GetInt());
            LastTime.Add(target.PlayerId, GetTimeStamp());
            killer.RpcResetAbilityCooldown();
            KillCount[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
        }
        return false;
    }
    private static void OnFixUpdateOthers(PlayerControl pc)
    {
        if (PlayerDie.ContainsKey(pc.PlayerId) && GameStates.IsInTask)
            DoNotifyRoles(SpecifyTarget: pc);
    }
    private static bool CanKill(byte id) => KillCount.TryGetValue(id, out var x) && x > 0;
    public override string GetProgressText(byte playerId, bool cooms) => ColorString(CanKill(playerId) ? Utils.GetRoleColor(CustomRoles.Bloodmoon).ShadeColor(0.25f) : Color.gray, KillCount.TryGetValue(playerId, out var killLimit) ? $"({killLimit})" : "Invalid");
    public static void RemoveId( PlayerControl target)
    {
        var targetid = target.PlayerId;

        if (PlayerDie.ContainsKey(targetid))
            PlayerDie.Remove(targetid);

        if (LastTime.ContainsKey(targetid))
            LastTime.Remove(targetid);
    }
    public static string OthersNameText(byte playerid) 
    {
        if (GameStates.IsMeeting) return "";
        var player = GetPlayerById(playerid);
        if (LastTime.TryGetValue(playerid, out var lastTime) && lastTime + 1 <= GetTimeStamp() && !GameStates.IsMeeting) // Progress text not register onMeeting anyways
        {
            LastTime[playerid] = GetTimeStamp();
            PlayerDie[playerid]--;
            if (PlayerDie[playerid] <= 0)
            {
                PlayerDie.Remove(playerid);
                LastTime.Remove(playerid);
                player.SetDeathReason(PlayerState.DeathReason.BloodLet);
                player.RpcMurderPlayer(player);
            }
        }

        return PlayerDie.TryGetValue(playerid, out var DeathTimer) ? ColorString(GetRoleColor(CustomRoles.Bloodmoon), GetString("DeathTimer").Replace("{DeathTimer}", DeathTimer.ToString())) : "";
    }
}
