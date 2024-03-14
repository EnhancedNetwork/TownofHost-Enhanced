using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Jackal : RoleBase
{

    //===========================SETUP================================\\
    private const int Id = 16700;
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    //==================================================================\\

    public static OptionItem KillCooldown;
    public static OptionItem CanVent;
    public static OptionItem CanUsesSabotage;
    public static OptionItem CanWinBySabotageWhenNoImpAlive;
    public static OptionItem HasImpostorVision;
    public static OptionItem OptionResetKillCooldownWhenSbGetKilled;
    public static OptionItem ResetKillCooldownWhenSbGetKilled;
    public static OptionItem ResetKillCooldownOn;
    public static OptionItem JackalCanKillSidekick;
    public static OptionItem CanRecruitSidekick;
    public static OptionItem SidekickRecruitLimitOpt;
    public static OptionItem SidekickCountMode;
    public static OptionItem SidekickAssignMode;
    // public static OptionItem SidekickCanWinWithOriginalTeam;
    public static OptionItem KillCooldownSK;
    public static OptionItem CanVentSK;
    public static OptionItem CanUseSabotageSK;
    public static OptionItem SidekickCanKillJackal;
    public static OptionItem SidekickCanKillSidekick;
    public static Dictionary<byte, int> RecruitLimit = [];

    public static readonly string[] sidekickAssignMode =
    [
        "SidekickAssignMode.SidekickAndRecruit",
        "SidekickAssignMode.Sidekick",
        "SidekickAssignMode.Recruit",
    ];

    public static readonly string[] sidekickCountMode =
    [
        "SidekickCountMode.Jackal",
        "SidekickCountMode.None",
        "SidekickCountMode.Original",
    ];

    public static void SetupCustomOption()
    {
        //Jackalは1人固定
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Jackal, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 12, "CanUseSabotage", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        CanWinBySabotageWhenNoImpAlive = BooleanOptionItem.Create(Id + 14, "JackalCanWinBySabotageWhenNoImpAlive", true, TabGroup.NeutralRoles, false).SetParent(CanUsesSabotage);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        OptionResetKillCooldownWhenSbGetKilled = BooleanOptionItem.Create(Id + 16, "ResetKillCooldownWhenPlayerGetKilled", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        ResetKillCooldownOn = FloatOptionItem.Create(Id + 28, "ResetKillCooldownOn", new(0f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(OptionResetKillCooldownWhenSbGetKilled)
            .SetValueFormat(OptionFormat.Seconds);
        JackalCanKillSidekick = BooleanOptionItem.Create(Id + 15, "JackalCanKillSidekick", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        CanRecruitSidekick = BooleanOptionItem.Create(Id + 30, "JackalCanRecruitSidekick", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        SidekickAssignMode = StringOptionItem.Create(Id + 34, "SidekickAssignMode", sidekickAssignMode, 0, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
                .SetHidden(false);
        SidekickRecruitLimitOpt = IntegerOptionItem.Create(Id + 33, "JackalSidekickRecruitLimit", new(0, 15, 1), 2, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
                .SetValueFormat(OptionFormat.Times);
        KillCooldownSK = FloatOptionItem.Create(Id + 20, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
            .SetValueFormat(OptionFormat.Seconds);
        CanVentSK = BooleanOptionItem.Create(Id + 21, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        CanUseSabotageSK = BooleanOptionItem.Create(Id + 22, "CanUseSabotage", true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickCanKillJackal = BooleanOptionItem.Create(Id + 23, "SidekickCanKillJackal", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
      //  SidekickKnowOtherSidekick = BooleanOptionItem.Create(6050585, "SidekickKnowOtherSidekick", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
      //  SidekickKnowOtherSidekickRole = BooleanOptionItem.Create(6050590, "SidekickKnowOtherSidekickRole", false, TabGroup.NeutralRoles, false).SetParent(SidekickKnowOtherSidekick);
        SidekickCanKillSidekick = BooleanOptionItem.Create(Id + 24, "SidekickCanKillSidekick", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickCountMode = StringOptionItem.Create(Id + 25, "SidekickCountMode", sidekickCountMode, 0, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
            .SetHidden(false);
     //   SidekickCanWinWithOriginalTeam = BooleanOptionItem.Create(6050795, "SidekickCanWinWithOriginalTeam", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
    }
    public override void Init()
    {
        playerIdList = [];
        RecruitLimit = [];
        ResetKillCooldownWhenSbGetKilled = OptionResetKillCooldownWhenSbGetKilled;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        RecruitLimit.TryAdd(playerId, SidekickRecruitLimitOpt.GetInt());

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetJackalRecruitLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(RecruitLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (RecruitLimit.ContainsKey(PlayerId))
            RecruitLimit[PlayerId] = Limit;
        else
            RecruitLimit.Add(PlayerId, SidekickRecruitLimitOpt.GetInt());
    }
    public static bool JackalKnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) return true;
        else if (seer.Is(CustomRoles.Sidekick) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick))) return true;
        else if (seer.Is(CustomRoles.Recruit) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit))) return true;

        return false;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static bool CanRecruit(byte id) => RecruitLimit.TryGetValue(id, out var x) && x > 0;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (CanRecruit(playerId))
            HudManager.Instance.KillButton.OverrideText($"{GetString("GangsterButtonText")}");
        else
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
    }
    public override void ApplyGameOptions(IGameOptions opt, byte babuyaga) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool jackal_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(jackal_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = jackal_canUse;
    }

    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public static void AfterPlayerDiedTask(PlayerControl target)
    {
        Main.AllAlivePlayerControls
            .Where(x => !target.Is(CustomRoles.Jackal) && x.Is(CustomRoles.Jackal))
            .Do(x => x.SetKillCooldown(ResetKillCooldownOn.GetFloat()));
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Pestilence)) return true;
        if (target.Is(CustomRoles.Jackal)) return true;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CantRecruit")));
            return false;
        }
        if (!CanRecruitSidekick.GetBool() || RecruitLimit[killer.PlayerId] < 1) return false;
        
        if (SidekickAssignMode.GetValue() != 2)
        {
            if (CanBeSidekick(target))
            {
                RecruitLimit[killer.PlayerId]--;
                SendRPC(killer.PlayerId);
                //if (!AttendantCantRoles.GetBool() && Mini.Age == 18 || !AttendantCantRoles.GetBool() &&  Mini.Age != 18 && !(target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
                
                if (CopyCat.playerIdList.Contains(target.PlayerId))
                    target.GetRoleClass()?.Remove(target.PlayerId);

                target.RpcSetCustomRole(CustomRoles.Sidekick);

                if (!Main.ResetCamPlayerList.Contains(target.PlayerId))
                    Main.ResetCamPlayerList.Add(target.PlayerId);

                Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
                Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("BeRecruitedByJackal")));

                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                killer.ResetKillCooldown();
                killer.SetKillCooldown();

                target.ResetKillCooldown();
                target.SetKillCooldown();

                Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Sidekick.ToString(), "Assign " + CustomRoles.Sidekick.ToString());
                
                if (RecruitLimit[killer.PlayerId] < 0)
                    HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");

                Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Jackal");
                return true;
            }
        }
        if (SidekickAssignMode.GetValue() != 1)
        {
            if (!target.GetCustomRole().IsNeutral() && !target.Is(CustomRoles.Sidekick) && !target.Is(CustomRoles.Recruit) && !target.Is(CustomRoles.Loyal) && !target.Is(CustomRoles.Admired))
            {
                RecruitLimit[killer.PlayerId]--;
                SendRPC(killer.PlayerId);
                target.RpcSetCustomRole(CustomRoles.Recruit);

                Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
                Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterSuccessfullyRecruited")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("BeRecruitedByJackal")));

                if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);

                killer.ResetKillCooldown();
                killer.SetKillCooldown();

                target.ResetKillCooldown();
                target.SetKillCooldown();

                Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Sidekick.ToString(), "Assign " + CustomRoles.Sidekick.ToString());
                
                if (RecruitLimit[killer.PlayerId] < 0)
                    HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");

                Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Jackal");
                return true;
            }
        }
        if (RecruitLimit[killer.PlayerId] < 0)
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        
        //killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterRecruitmentFailure")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Jackal");
        return false;
    }
    public static bool CanBeSidekick(PlayerControl pc)
    {
        return pc != null && !pc.Is(CustomRoles.Sidekick) && !pc.Is(CustomRoles.Recruit) 
            && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Rascal) && !pc.Is(CustomRoles.Madmate) 
            && !pc.Is(CustomRoles.Charmed) && !pc.Is(CustomRoles.Infected) && !pc.Is(CustomRoles.Schizophrenic) 
            && !pc.Is(CustomRoles.Contagious) && pc.GetCustomRole().IsAbleToBeSidekicked() 
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
    private static string GetRecruitLimit(byte playerId) => Utils.ColorString(CanRecruit(playerId) ? Utils.GetRoleColor(CustomRoles.Jackal).ShadeColor(0.25f) : Color.gray, RecruitLimit.TryGetValue(playerId, out var recruitLimit) ? $"({recruitLimit})" : "Invalid");
    public override string GetProgressText(byte playerId, bool comms)
        => CanRecruitSidekick.GetBool() ? GetRecruitLimit(playerId) : "";

    public static bool RpcCheckAndMurder(PlayerControl killer, PlayerControl target)
    {

        if (!Jackal.JackalCanKillSidekick.GetBool())
        {
            // Jackal can kill Sidekick/Recruit
            if (killer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit)))
                return false;

            // Sidekick/Recruit can kill Jackal
            else if ((killer.Is(CustomRoles.Sidekick) || killer.Is(CustomRoles.Recruit)) && target.Is(CustomRoles.Jackal))
                return false;
        }

        if (!Jackal.SidekickCanKillSidekick.GetBool())
        {
            // Sidekick can kill Sidekick/Recruit
            if (killer.Is(CustomRoles.Sidekick) && (target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit)))
                return false;

            // Recruit can kill Recruit/Sidekick
            if (killer.Is(CustomRoles.Recruit) && (target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Sidekick)))
                return false;
        }
        return true;
    }
}