using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles._Ghosts_.Impostor;

internal class Bloodmoon : RoleBase
{

    //===========================SETUP================================\\
    private const int Id = 28100;
    private static HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;

    //==================================================================\\


    public static OptionItem MinimumPlayersAliveToKill;
    public static OptionItem KillCooldown;
    public static OptionItem CanKillNum;
    public static Dictionary<byte, int> KillCount = [];

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
    public override void Init()
    {
        KillCount.Clear();
        PlayerIds.Clear();
    }
    public override void Add(byte PlayerId)
    {
        KillCount.TryAdd(PlayerId, CanKillNum.GetInt());
        PlayerIds.Add(PlayerId);
        if (KillCount.ContainsKey(PlayerId))
        {
            Logger.Info($"Succesfully added {Utils.GetPlayerById(PlayerId).GetRealName()}", "BloodMoon ADD") ;
        }
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
            && Main.AllAlivePlayerControls.Length >= MinimumPlayersAliveToKill.GetInt()
            && killer.RpcCheckAndMurder(target, true))
        {
            killer.RpcMurderPlayer(target);
            killer.RpcResetAbilityCooldown();
            KillCount[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
        }
        else if (Main.AllAlivePlayerControls.Length < MinimumPlayersAliveToKill.GetInt()) killer.Notify(GetString("HawkTooManyDied"));
        return false;
    }
    public static bool CanKill(byte id) => KillCount.TryGetValue(id, out var x) && x > 0;
    public override string GetProgressText(byte playerId, bool cooms) => Utils.ColorString(CanKill(playerId) ? Utils.GetRoleColor(CustomRoles.Bloodmoon).ShadeColor(0.25f) : Color.gray, KillCount.TryGetValue(playerId, out var killLimit) ? $"({killLimit})" : "Invalid");

}
