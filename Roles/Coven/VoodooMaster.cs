using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using Hazel;
using InnerNet;
using UnityEngine;

namespace TOHE.Roles.Coven;

internal class VoodooMaster : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 30700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.VoodooMaster);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenUtility;
    //==================================================================\\

    private static OptionItem VoodooCooldown;
    private static OptionItem VoodoosPerRound;
    private static OptionItem CanDollCoven;
    private static OptionItem NecroAbilityCanKillCov;

    public static readonly Dictionary<byte, List<byte>> Dolls = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.VoodooMaster, 1, zeroOne: false);
        VoodooCooldown = FloatOptionItem.Create(Id + 10, "VoodooCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.VoodooMaster])
            .SetValueFormat(OptionFormat.Seconds);
        VoodoosPerRound = IntegerOptionItem.Create(Id + 11, "VoodooMasterPerRound", new(1, 15, 1), 1, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.VoodooMaster])
            .SetValueFormat(OptionFormat.Players);
        CanDollCoven = BooleanOptionItem.Create(Id + 12, "VoodooMasterCanDollCoven", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.VoodooMaster]);
        NecroAbilityCanKillCov = BooleanOptionItem.Create(Id + 13, "VoodooMasterNecroCanKillCov", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.VoodooMaster]);
    }
    public override void Init()
    {
        Dolls.Clear();
    }
    public override void Add(byte PlayerId)
    {
        Dolls[PlayerId] = [];
        AbilityLimit = VoodoosPerRound.GetInt();
        GetPlayerById(PlayerId)?.AddDoubleTrigger();
    }

    private void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(player.PlayerId);
        writer.Write(AbilityLimit);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte VMId = reader.ReadByte();
        byte DollId = reader.ReadByte();
        Dolls[VMId].Add(DollId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = VoodooCooldown.GetFloat();
    public override void SetAbilityButtonText(HudManager hud, byte playerId) =>
        hud.KillButton.OverrideText(GetString("ShamanButtonText"));
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    => Dolls[seer.PlayerId].Contains(seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.VoodooMaster), "✂") : string.Empty;
    public override string GetProgressText(byte playerId, bool comms)
        => ColorString(AbilityLimit >= 1 ? GetRoleColor(CustomRoles.VoodooMaster).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        return HasNecronomicon(killer) && killer.CheckDoubleTrigger(target, () => {
            if (AbilityLimit > 0 && (!target.IsPlayerCoven() || (target.IsPlayerCoven() && CanDollCoven.GetBool())))
            {
                Dolls[killer.PlayerId].Add(target.PlayerId);
                AbilityLimit--;
                SendRPC(killer, target);
                killer.RpcGuardAndKill(target);
                killer.Notify(string.Format(GetString("VoodooMasterDolledSomeone"), target.GetRealName()));
                if (HasNecronomicon(killer)) ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
            }
            else if (target.IsPlayerCoven() && CanDollCoven.GetBool()) killer.Notify(GetString("VoodooMasterNoDollCoven"));
            else killer.Notify(GetString("VoodooMasterNoDollsLeft"));
        });
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (Dolls[_Player.PlayerId].Count < 1) return true;
        if (killer.IsPlayerCoven()) return true;

        PlayerControl ChoosenTarget = GetPlayerById(Dolls[target.PlayerId].Where(x => GetPlayerById(x).IsAlive()).ToList().RandomElement());

        if (killer.CheckForInvalidMurdering(ChoosenTarget) && killer.RpcCheckAndMurder(ChoosenTarget, check: true))
        {
            killer.RpcMurderPlayer(ChoosenTarget);
            ChoosenTarget.SetRealKiller(_Player);
        }
        else
        {
            _Player.Notify(GetString("Shaman_KillerCannotMurderChosenTarget"), time: 10f);
        }
        Dolls[_Player.PlayerId].Remove(ChoosenTarget.PlayerId);
        return false;
    }
    public override void AfterMeetingTasks()
    {
        Dolls[_Player.PlayerId].Clear();
        AbilityLimit = VoodoosPerRound.GetInt();
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!Dolls[_Player.PlayerId].Contains(target.PlayerId)) return false;
        if (!HasNecronomicon(_Player)) return false;
        if (!killer.IsPlayerCoven() || (killer.IsPlayerCoven() && NecroAbilityCanKillCov.GetBool())) { 
            killer.SetDeathReason(PlayerState.DeathReason.Sacrifice);
            killer.RpcMurderPlayer(killer);
            killer.SetRealKiller(target); 
        }
        return false;
    }
}
