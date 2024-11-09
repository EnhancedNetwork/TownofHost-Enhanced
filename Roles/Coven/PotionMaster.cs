using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using MS.Internal.Xml.XPath;

namespace TOHE.Roles.Coven;

internal class PotionMaster : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 17700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.PotionMaster);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenUtility;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem RevealMaxCount;
    private static OptionItem BarrierMaxCount;
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
        //CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
        //HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.PotionMaster]);
    }
    public override void Init()
    {
        RevealList.Clear();
        RevealLimit.Clear();
        BarrierLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        RevealList[playerId] = [];
        BarrierList[playerId] = [];
        RevealLimit[playerId] = RevealMaxCount.GetInt();
        BarrierLimit[playerId] = RevealMaxCount.GetInt();
        PotionMode = 0;

        var pc = Utils.GetPlayerById(playerId);
        pc?.AddDoubleTrigger();
    }

    private void SendRPC(byte playerId, byte targetId, int operate)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(operate);
        writer.Write(playerId);
        writer.Write(targetId);
        if (operate == 0)
            writer.Write(RevealLimit[playerId]);
        else
            writer.Write(BarrierLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int operate = reader.ReadInt32();
        byte playerId = reader.ReadByte();
        if (operate == 0)
        {
            RevealLimit[playerId] = reader.ReadInt32();
            RevealList[playerId].Add(reader.ReadByte());
        }
        if (operate == 1)
        {
            BarrierLimit[playerId] = reader.ReadInt32();
            BarrierList[playerId].Add(reader.ReadByte());
        }
        
    }
    //public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    //public override bool CanUseSabotage(PlayerControl pc) => true;


    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (HasNecronomicon(killer))
        {
            return killer.CheckDoubleTrigger(target, () => { SetRitual(killer, target); });
        }
        else
        {
            SetRitual(killer, target);
            return false;
        }
    }

    public static bool IsReveal(byte seer, byte target)
    {
        if (RevealList[seer].Contains(target))
        {
            return true;
        }
        return false;
    }
    private void SetRitual(PlayerControl killer, PlayerControl target)
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

                    Utils.NotifyRoles(SpecifySeer: killer);
                    SendRPC(killer.PlayerId, target.PlayerId, 0);

                    killer.SetKillCooldown();
                }
                else if (RevealLimit[killer.PlayerId] <= 0)
                {
                    killer.Notify(string.Format(GetString("PotionMasterNoPotions"),GetString("PotionMasterReveal")));
                }
                break;
            case 1:
                if (!BarrierList[killer.PlayerId].Contains(target.PlayerId) && BarrierLimit[killer.PlayerId] > 0)
                {
                    BarrierLimit[killer.PlayerId]--;
                    BarrierList[killer.PlayerId].Add(target.PlayerId);
                    Logger.Info($"{killer.GetNameWithRole()}: Barrier destination -> {target.GetNameWithRole()} || remaining {BarrierLimit[killer.PlayerId]} times", "PotionMaster");

                    SendRPC(killer.PlayerId, target.PlayerId, 1);

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
        switch (PotionMode) { 
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
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (_Player == null || !_Player.IsAlive()) return false;
        if (!BarrierList[_Player.PlayerId].Contains(target.PlayerId)) return false;

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
        if (BarrierList[_Player.PlayerId].Contains(target.PlayerId) && seer.IsPlayerCoven() && seer.PlayerId != _Player.PlayerId)
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.PotionMaster), "✚");
        }
        return string.Empty;
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
        => KnowRoleTarget(seer, target);

    public override string GetProgressText(byte playerId, bool coooonms) => Utils.ColorString(RevealLimit[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.PotionMaster).ShadeColor(0.25f) : Color.gray, $"({RevealLimit[playerId]})")+ Utils.ColorString(BarrierLimit[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.Medic).ShadeColor(0.25f) : Color.gray, $" ({BarrierLimit[playerId]})");
}