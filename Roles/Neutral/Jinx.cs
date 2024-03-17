using AmongUs.GameOptions;
using System.Collections.Generic;
using static TOHE.Options;
using UnityEngine;
using Hazel;

namespace TOHE.Roles.Neutral;

internal class Jinx : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 16800;
    private static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem JinxSpellTimes;
    private static OptionItem killAttacker;

    private static Dictionary<byte, int> JinxSpellCount = [];

    public static void SetupCustomOption()
    
    {
        //Jinxは1人固定
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Jinx, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        JinxSpellTimes = IntegerOptionItem.Create(Id + 14, "JinxSpellTimes", new(1, 15, 1), 3, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
        .SetValueFormat(OptionFormat.Times);
        killAttacker = BooleanOptionItem.Create(Id + 15, "killAttacker", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);

    }
    public override void Init()
    {
        playerIdList = [];
        JinxSpellCount = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        JinxSpellCount.Add(playerId, JinxSpellTimes.GetInt());

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void SendRPCJinxSpellCount(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetJinxSpellCount, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.WritePacked(JinxSpellCount[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte JinxId = reader.ReadByte();
        int JinxGuardNum = reader.ReadInt32();
        if (JinxSpellCount.ContainsKey(JinxId))
            JinxSpellCount[JinxId] = JinxGuardNum;
        else
            JinxSpellCount.Add(JinxId, JinxSpellTimes.GetInt());
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (JinxSpellCount[target.PlayerId] <= 0) return true;
        if (killer.Is(CustomRoles.Pestilence)) return true;
        if (killer == target) return true;
        
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
       
        JinxSpellCount[target.PlayerId] -= 1;
        SendRPCJinxSpellCount(target.PlayerId);
        
        if (killAttacker.GetBool())
        {
            killer.SetRealKiller(target);
            Logger.Info($"{target.GetNameWithRole()} : {JinxSpellCount[target.PlayerId]}回目", "Jinx");
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Jinx;
            killer.RpcMurderPlayerV3(killer);
        }
        return false;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte babushka) => opt.SetVision(HasImpostorVision.GetBool());

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl player) => CanVent.GetBool();

    public override string GetProgressText(byte playerId, bool comms) 
        => Utils.ColorString(CanJinx(playerId) ? Utils.GetRoleColor(CustomRoles.Gangster).ShadeColor(0.25f) : Color.gray, JinxSpellCount.TryGetValue(playerId, out var recruitLimit) ? $"({recruitLimit})" : "Invalid");
    
    private static bool CanJinx(byte id) => JinxSpellCount.TryGetValue(id, out var x) && x > 0;
}
