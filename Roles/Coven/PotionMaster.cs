using Hazel;
using System.Text;
using TOHE.Modules.Rpc;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class PotionMaster : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.PotionMaster;
    private const int Id = 17700;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenUtility;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem RevealMaxCount;
    private static OptionItem BarrierMaxCount;
    private static OptionItem CovenCanSeeReveals;
    //private static OptionItem CanVent;
    //private static OptionItem HasImpostorVision;

    private static readonly Dictionary<byte, HashSet<byte>> RevealList = [];
    private static readonly Dictionary<byte, HashSet<byte>> BarrierList = [];
    private static readonly Dictionary<byte, int> RevealLimit = [];
    private static readonly Dictionary<byte, int> BarrierLimit = [];
    private static byte PotionMode = 0;



    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.PotionMaster, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 14, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster])
            .SetValueFormat(OptionFormat.Seconds);
        RevealMaxCount = IntegerOptionItem.Create(Id + 11, "PotionMasterMaxReveals", new(1, 15, 1), 5, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster])
            .SetValueFormat(OptionFormat.Times);
        BarrierMaxCount = IntegerOptionItem.Create(Id + 15, "PotionMasterMaxBarriers", new(1, 100, 1), 5, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster])
            .SetValueFormat(OptionFormat.Times);
        CovenCanSeeReveals = BooleanOptionItem.Create(Id + 12, "PotionMasterCovenCanSeeReveals", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
        //CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
        //HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
    }
    public override void Init()
    {
        RevealList.Clear();
        BarrierList.Clear();
        RevealLimit.Clear();
        BarrierLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        RevealList[playerId] = [];
        BarrierList[playerId] = [];
        RevealLimit[playerId] = RevealMaxCount.GetInt();
        BarrierLimit[playerId] = BarrierMaxCount.GetInt();
        PotionMode = 0;

        var pc = Utils.GetPlayerById(playerId);
        pc?.AddDoubleTrigger();
    }

    private static void SendRPC(byte typeId, PlayerControl player, PlayerControl target)
    {
        if (!player.IsNonHostModdedClient()) return;
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(typeId);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        if (typeId == 0) writer.Write(RevealLimit[player.PlayerId]);
        else if (typeId == 1) writer.Write(BarrierLimit[player.PlayerId]);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte typeId = reader.ReadByte();
        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        switch (typeId)
        {
            case 0:
                RevealList[playerId].Add(targetId);
                RevealLimit[playerId] = reader.ReadInt32();
                break;
            case 1:
                BarrierList[playerId].Add(targetId);
                BarrierLimit[playerId] = reader.ReadInt32();
                break;
        }
    }
    //public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    //public override bool CanUseSabotage(PlayerControl pc) => true;


    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!HasNecronomicon(killer))
        {
            SetRitual(killer, target);
            return false;
        }
        if (killer.CheckDoubleTrigger(target, () => { SetRitual(killer, target); }))
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

    public static bool IsReveal(byte seer, byte target) => RevealList[seer].Contains(target);
    private static void SetRitual(PlayerControl killer, PlayerControl target)
    {
        switch (PotionMode)
        {
            case 0:
                if (target.IsPlayerCoven())
                {
                    killer.Notify(GetString("PotionMasterRevealCoven"));
                }
                else if (!IsReveal(killer.PlayerId, target.PlayerId) && RevealLimit[killer.PlayerId] > 0)
                {
                    RevealLimit[killer.PlayerId]--;
                    RevealList[killer.PlayerId].Add(target.PlayerId);
                    Logger.Info($"{killer.GetNameWithRole()}: Divined divination destination -> {target.GetNameWithRole()} || remaining {RevealLimit[killer.PlayerId]} times", "PotionMaster");

                    NotifyRoles(SpecifySeer: killer);
                    SendRPC(PotionMode, killer, target);

                    killer.ResetKillCooldown();
                    killer.SetKillCooldown();
                }
                else if (RevealLimit[killer.PlayerId] <= 0)
                {
                    killer.Notify(string.Format(GetString("PotionMasterNoPotions"), GetString("PotionMasterReveal")));
                }
                break;
            case 1:
                if (!IsBarriered(killer.PlayerId, target.PlayerId) && BarrierLimit[killer.PlayerId] > 0)
                {
                    BarrierLimit[killer.PlayerId]--;
                    BarrierList[killer.PlayerId].Add(target.PlayerId);
                    Logger.Info($"{killer.GetNameWithRole()}: Barrier destination -> {target.GetNameWithRole()} || remaining {BarrierLimit[killer.PlayerId]} times", "PotionMaster");

                    SendRPC(PotionMode, killer, target);

                    killer.ResetKillCooldown();
                    killer.SetKillCooldown();
                }
                else if (BarrierLimit[killer.PlayerId] <= 0)
                {
                    killer.Notify(string.Format(GetString("PotionMasterNoPotions"), GetString("PotionMasterBarrier")));
                }
                break;
        }
    }
    public override void UnShapeShiftButton(PlayerControl pm)
    {
        switch (PotionMode)
        {
            case 0:
                PotionMode = 1;
                pm.Notify(string.Format(GetString("PotionMasterPotionSwitch"), GetString("PotionMasterBarrier")));
                break;
            case 1:
                PotionMode = 0;
                pm.Notify(string.Format(GetString("PotionMasterPotionSwitch"), GetString("PotionMasterReveal")));
                break;
        }
    }
    public static byte CurrentPotion() => PotionMode;
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (seer == null || !seer.IsAlive() || isForMeeting || !isForHud) return string.Empty;
        else
        {
            var sb = new StringBuilder();
            switch (PotionMode)
            {
                case 0: // Reveal
                    sb.Append(GetString("PotionMasterPotionCurrent") + GetString("PotionMasterReveal"));
                    break;
                case 1: // Barrier
                    sb.Append(GetString("PotionMasterPotionCurrent") + GetString("PotionMasterBarrier"));
                    break;
            }
            return sb.ToString();
        }
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        var IsWatch = false;
        RevealList.Do(x =>
        {
            if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                IsWatch = true;
        });
        return IsWatch;
    }
    public static bool CovenKnowRoleTarget(PlayerControl coven, PlayerControl target)
    {
        if (coven == null || !coven.IsPlayerCovenTeam()) return false;
        if (!CovenCanSeeReveals.GetBool()) return false;
        bool result = false;
        foreach (var pm in RevealList.Keys)
        {
            if (RevealList[pm].Contains(target.PlayerId)) result = true;
        }
        return result;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (_Player == null || !_Player.IsAlive()) return false;
        if (!IsBarriered(killer.PlayerId, target.PlayerId)) return false;

        killer.RpcGuardAndKill(target);
        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);
        return true;
    }
    public override void AfterMeetingTasks()
    {
        BarrierList[_Player.PlayerId].Clear();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (PotionMode == 0)
        {
            hud.AbilityButton.OverrideText(GetString("PotionMasterReveal"));
        }
        else if (PotionMode == 1)
        {
            hud.AbilityButton.OverrideText(GetString("PotionMasterBarrier"));
        }
        hud.KillButton.OverrideText(GetString("PotionMasterKillButtonText"));
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    => BarrierList[seer.PlayerId].Contains(seen.PlayerId) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.PotionMaster), "✚") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (_Player == null) return string.Empty;
        if (IsBarriered(seer.PlayerId, target.PlayerId) && ((seer.GetCustomRole().IsCovenTeam() && seer.PlayerId != _Player.PlayerId) || !seer.IsAlive()))
        {
            return ColorString(GetRoleColor(CustomRoles.PotionMaster), "✚");
        }
        return string.Empty;
    }
    public static bool IsBarriered(byte pc, byte target) => BarrierList.TryGetValue(pc, out var protectList) && protectList.Contains(target);

    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
        => KnowRoleTarget(seer, target);

    public override string GetProgressText(byte playerId, bool coooonms) => Utils.ColorString(RevealLimit[playerId] > 0 ? GetRoleColor(CustomRoles.PotionMaster).ShadeColor(0.25f) : Color.gray, $"({RevealLimit[playerId]})") + ColorString(BarrierLimit[playerId] > 0 ? GetRoleColor(CustomRoles.Medic).ShadeColor(0.25f) : Color.gray, $" ({BarrierLimit[playerId]})");
}
