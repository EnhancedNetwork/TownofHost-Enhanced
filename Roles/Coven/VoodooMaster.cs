using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class VoodooMaster : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.VoodooMaster;
    private const int Id = 30700;
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
        PlayerId.SetAbilityUseLimit(VoodoosPerRound.GetInt());
        GetPlayerById(PlayerId)?.AddDoubleTrigger();
    }

    private void SendRPC(PlayerControl player, PlayerControl target)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
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
        => IsDoll(seer.PlayerId, seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.VoodooMaster), "✂") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (_Player == null) return string.Empty;
        if (IsDoll(_Player.PlayerId, target.PlayerId) && ((seer.GetCustomRole().IsCovenTeam() && seer.PlayerId != _Player.PlayerId) || !seer.IsAlive()))
        {
            return ColorString(GetRoleColor(CustomRoles.VoodooMaster), "✂");
        }
        return string.Empty;
    }
    public static bool IsDoll(byte pc, byte target) => Dolls.TryGetValue(pc, out var dollList) && dollList.Contains(target);

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!HasNecronomicon(killer))
        {
            SetDoll(killer, target);
            return false;
        }
        if (killer.CheckDoubleTrigger(target, () => { SetDoll(killer, target); }))
        {
            if (HasNecronomicon(killer))
            {
                if (target.GetCustomRole().IsCovenTeam())
                {
                    killer.Notify(GetString("CovenDontKillOtherCoven"));
                    return false;
                }
                else return true;
            }
        }
        return false;
    }
    private void SetDoll(PlayerControl killer, PlayerControl target)
    {
        if (IsDoll(killer.PlayerId, target.PlayerId)) return;
        if (killer.GetAbilityUseLimit() > 0 && (!target.GetCustomRole().IsCovenTeam() || (target.GetCustomRole().IsCovenTeam() && CanDollCoven.GetBool())))
        {
            Dolls[killer.PlayerId].Add(target.PlayerId);
            killer.RpcRemoveAbilityUse();
            SendRPC(killer, target);

            killer.RpcGuardAndKill(target);
            killer.Notify(string.Format(GetString("VoodooMasterDolledSomeone"), target.GetRealName()));
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (HasNecronomicon(killer)) ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
        }
        else if (target.GetCustomRole().IsCovenTeam() && CanDollCoven.GetBool()) killer.Notify(GetString("VoodooMasterNoDollCoven"));
        else killer.Notify(GetString("VoodooMasterNoDollsLeft"));
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (Dolls[_Player.PlayerId].Count < 1) return true;
        if (killer.GetCustomRole().IsCovenTeam()) return true;

        PlayerControl ChoosenTarget = GetPlayerById(Dolls[target.PlayerId].Where(x => GetPlayerById(x).IsAlive()).ToList().RandomElement());

        if (killer.CheckForInvalidMurdering(ChoosenTarget) && killer.RpcCheckAndMurder(ChoosenTarget, check: true) && !ChoosenTarget.IsTransformedNeutralApocalypse())
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
        if (_Player == null) return;

        Dolls[_Player.PlayerId].Clear();
        _Player.SetAbilityUseLimit(VoodoosPerRound.GetInt());
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!IsDoll(_Player.PlayerId, target.PlayerId)) return false;
        if (!HasNecronomicon(_Player)) return false;
        if (!killer.GetCustomRole().IsCovenTeam() || (killer.GetCustomRole().IsCovenTeam() && NecroAbilityCanKillCov.GetBool()))
        {
            killer.SetDeathReason(PlayerState.DeathReason.Sacrifice);
            killer.RpcMurderPlayer(killer);
            killer.SetRealKiller(target);
        }
        return false;
    }
}
