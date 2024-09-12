using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Diagnostics.Metrics;
using System.Text;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class CovenLeader : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 29800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.CovenLeader);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenPower;
    //==================================================================\\

    private static OptionItem RetrainCooldown;
    public static OptionItem MaxRetrains;

    public static readonly Dictionary<byte, CustomRoles> retrainPlayer = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.CovenLeader, 1, zeroOne: false);
        MaxRetrains = IntegerOptionItem.Create(Id + 10, "CovenLeaderMaxRetrains", new(1, 15, 1), 2, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CovenLeader])
            .SetValueFormat(OptionFormat.Times);
        RetrainCooldown = FloatOptionItem.Create(Id + 11, "CovenLeaderRetrainCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CovenLeader])
                .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        retrainPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = MaxRetrains.GetInt();
    }

    private void SendRPC(byte playerId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(playerId);
        writer.Write(AbilityLimit);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();
        AbilityLimit = reader.ReadSingle();
    }
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override string GetProgressText(byte playerId, bool comms)
        => ColorString(AbilityLimit >= 1 ? GetRoleColor(CustomRoles.CovenLeader).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    public override void SetKillCooldown(byte id) => RetrainCooldown.GetFloat();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (HasNecronomicon(killer)) return true;
        if (AbilityLimit <= 0)
        {
            killer.Notify(GetString("CovenLeaderNoRetrain"));
            return false;
        }
        if (killer.IsPlayerCoven() && !target.IsPlayerCoven())
        {
            killer.Notify(GetString("CovenLeaderRetrainNonCoven"));
            return false;
        }
        var roleList = CustomRolesHelper.AllRoles.Where(role => (role.IsCoven() && (role.IsEnable() && !role.RoleExist(countDead: true)))).ToList();
        retrainPlayer[target.PlayerId] = roleList.RandomElement();
        foreach (byte cov in retrainPlayer.Keys)
        {
            SendMessage(string.Format(GetString("RetrainNotification"), CustomRoles.CovenLeader.ToColoredString(), retrainPlayer[cov].ToColoredString()), cov);
        }
        killer.Notify(GetString("CovenLeaderRetrain"));
        killer.ResetKillCooldown();
        return false;
    }
    
}
