using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Jackal : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 16700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Jailer);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem CanUsesSabotage;
    public static OptionItem CanWinBySabotageWhenNoImpAlive;
    public static OptionItem HasImpostorVision;
    private static OptionItem OptionResetKillCooldownWhenSbGetKilled;
    private static OptionItem ResetKillCooldownWhenSbGetKilled;
    private static OptionItem ResetKillCooldownOn;
    private static OptionItem JackalCanKillSidekick;
    private static OptionItem CanRecruitSidekick;
    private static OptionItem SidekickRecruitLimitOpt;
    public static OptionItem KillCooldownSK;
    public static OptionItem CanVentSK;
    public static OptionItem CanUseSabotageSK;
    private static OptionItem RecruitSidekickNeedToKill;
    public static OptionItem SidekickCanKillBeforeInherited;
    private static OptionItem SidekickCanKillJackal;
    private static readonly Dictionary<byte, float> SidekickRecruitTime =[];
    public static bool SidekickAlive;
    private static int NeedtoKill;
    private enum SidekickAssignModeSelectList
    {
        Jackal_SidekickAssignMode_SidekickAndRecruit,
        Jackal_SidekickAssignMode_Sidekick,
        Jackal_SidekickAssignMode_Recruit,
    }
    private enum SidekickCountModeSelectList
    {
        Jackal_SidekickCountMode_Jackal,
        Jackal_SidekickCountMode_None,
        Jackal_SidekickCountMode_Original,
    }

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Jackal, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        CanUsesSabotage = BooleanOptionItem.Create(Id + 12, GeneralOption.CanUseSabotage, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        CanWinBySabotageWhenNoImpAlive = BooleanOptionItem.Create(Id + 14, "JackalCanWinBySabotageWhenNoImpAlive", true, TabGroup.NeutralRoles, false).SetParent(CanUsesSabotage);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        OptionResetKillCooldownWhenSbGetKilled = BooleanOptionItem.Create(Id + 16, "JackalResetKillCooldownWhenPlayerGetKilled", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        ResetKillCooldownOn = FloatOptionItem.Create(Id + 28, "JackalResetKillCooldownOn", new(0f, 180f, 2.5f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(OptionResetKillCooldownWhenSbGetKilled)
            .SetValueFormat(OptionFormat.Seconds);
        CanRecruitSidekick = BooleanOptionItem.Create(Id + 30, "JackalCanRecruitSidekick", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jackal]);
        SidekickRecruitLimitOpt = IntegerOptionItem.Create(Id + 33, "JackalSidekickRecruitLimit", new(0, 15, 1), 2, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
                .SetValueFormat(OptionFormat.Times);
        KillCooldownSK = FloatOptionItem.Create(Id + 20, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
            .SetValueFormat(OptionFormat.Seconds);
        JackalCanKillSidekick = BooleanOptionItem.Create(Id + 15, "JackalCanKillSidekick", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        CanVentSK = BooleanOptionItem.Create(Id + 21, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        CanUseSabotageSK = BooleanOptionItem.Create(Id + 22, GeneralOption.CanUseSabotage, true, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickCanKillBeforeInherited = BooleanOptionItem.Create(Id + 37, "Jackal_SidekickCanKillBeforeInherited", false, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick);
        SidekickCanKillJackal = BooleanOptionItem.Create(Id + 23, "Jackal_SidekickCanKillJackal", false, TabGroup.NeutralRoles, false).SetParent(SidekickCanKillBeforeInherited);
        RecruitSidekickNeedToKill = IntegerOptionItem.Create(Id + 36, "Jackal_RecruitSidekickNeedToKill", new(0, 13, 1), 1, TabGroup.NeutralRoles, false).SetParent(CanRecruitSidekick)
              .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        NeedtoKill = 0;
        ResetKillCooldownWhenSbGetKilled = OptionResetKillCooldownWhenSbGetKilled;
        SidekickRecruitTime.Clear();
        SidekickAlive = false;
    }
    public override void Add(byte playerId)
    {
        NeedtoKill = RecruitSidekickNeedToKill.GetInt();
        if (SidekickRecruitTime.TryGetValue(playerId, out float time))
        {
            AbilityLimit = time;
            SendSkillRPC();
            Utils.NotifyRoles(SpecifySeer: playerId.GetPlayer());
        }
        else
        {
            AbilityLimit = CanRecruitSidekick.GetBool() ? SidekickRecruitLimitOpt.GetInt() : 0;
        }

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(OthersPlayersDead);
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte babuyaga) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = SidekickRecruitTime.ContainsKey(id) ? KillCooldown.GetFloat() : KillCooldownSK.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => CanUsesSabotage.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public static bool JackalKnowRole(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Jackal) && (target.Is(CustomRoles.Sidekick))) return true;
        else if (seer.Is(CustomRoles.Sidekick) && (target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Sidekick))) return true;

        return false;
    }
    private bool CanRecruit(byte id) => AbilityLimit > 0 && NeedtoKill == 0 && !SidekickAlive;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (CanRecruit(playerId))
            hud.KillButton?.OverrideText($"{GetString("GangsterButtonText")}");
        else
            hud.KillButton?.OverrideText($"{GetString("KillButtonText")}");
    }

    private void OthersPlayersDead(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting || target.IsDisconnected()) return;

        if (ResetKillCooldownWhenSbGetKilled.GetBool() && !killer.Is(CustomRoles.Sidekick) && !killer.Is(CustomRoles.Jackal) && !target.Is(CustomRoles.Sidekick) && !target.Is(CustomRoles.Jackal) && !GameStates.IsMeeting)
        {
            Main.AllAlivePlayerControls
                .Where(x => !target.Is(CustomRoles.Jackal) && x.Is(CustomRoles.Jackal))
                .Do(x => x.SetKillCooldown(ResetKillCooldownOn.GetFloat()));
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Jackal)) return false;
        if (!CanRecruitSidekick.GetBool() || AbilityLimit < 1) return true;
        if (NeedtoKill != 0)
        {
            NeedtoKill--;
            SendRPC(NeedtoKill,SidekickAlive);
            return true;
        }
        if (SidekickAlive)
        {
            Logger.Info($"Sidekick alive,cant recruit", "Jackal");
            return true;
        }
        if (CanBeSidekick(target))
        {
            AbilityLimit--;
            NeedtoKill = RecruitSidekickNeedToKill.GetInt();
            SidekickAlive = true;
            SendRPC(NeedtoKill, SidekickAlive);
            SendSkillRPC();

            target.GetRoleClass()?.OnRemove(target.PlayerId);
            target.RpcSetCustomRole(CustomRoles.Sidekick);
            target.GetRoleClass()?.OnAdd(target.PlayerId);

            target.RpcChangeRoleBasis(CustomRoles.Sidekick);

            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("GangsterSuccessfullyRecruited")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), GetString("BeRecruitedByJackal")));

            SidekickRecruitTime[target.PlayerId] = AbilityLimit; 
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();

            target.ResetKillCooldown();
            target.SetKillCooldown();

            Logger.Info($"Target: {target.GetRealName()} : {target.GetCustomRole()} => {CustomRoles.Sidekick}", "Assign Sidekick");


            Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} - Recruit limit:{AbilityLimit}", "Jackal");
            return false;
        }
        
        Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} - Recruit limit:{AbilityLimit}", "Jackal");
        return true;
    }
    public static void SendRPC(int needtokill,bool sidekickalive)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncJackalNeedtoKill, SendOption.Reliable, -1);
        writer.Write(needtokill);
        writer.Write(sidekickalive);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        NeedtoKill = reader.ReadInt32();
        SidekickAlive = reader.ReadBoolean();
    }

    public static bool CanBeSidekick(PlayerControl pc)
    {
        var role = pc.GetCustomRole();
        return !(role is CustomRoles.Sidekick or CustomRoles.Loyal or CustomRoles.Admired 
            or CustomRoles.Rascal or CustomRoles.Madmate or CustomRoles.Charmed 
            or CustomRoles.Infected or CustomRoles.Paranoia or CustomRoles.Contagious)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }

    private string GetRecruitLimit(byte playerId)
        => Utils.ColorString(CanRecruit(playerId)
            ? Utils.GetRoleColor(CustomRoles.Jackal).ShadeColor(0.25f)
            : Color.gray, $"({AbilityLimit})");
    
    public override string GetProgressText(byte playerId, bool comms)
        => CanRecruitSidekick.GetBool() ? GetRecruitLimit(playerId) : "";

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!JackalCanKillSidekick.GetBool())
        {
            // Jackal can kill Sidekick/Recruit
            if (killer.Is(CustomRoles.Jackal) && target.Is(CustomRoles.Sidekick))
                return true;
        }



        if (!SidekickCanKillJackal.GetBool())
        {
            // Recruit/Sidekick can kill Jackal
            if (target.Is(CustomRoles.Jackal) && (killer.Is(CustomRoles.Sidekick)))
                return true;
        }
        return false;
    }
}
