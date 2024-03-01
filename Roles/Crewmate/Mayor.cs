using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal partial class Mayor : RoleBase
{
    public const int Id = 12000;
    public static bool On = false;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => MayorHasPortableButton.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;

    public static OptionItem MayorAdditionalVote;
    public static OptionItem MayorHasPortableButton;
    public static OptionItem MayorNumOfUseButton;
    public static OptionItem MayorHideVote;
    public static OptionItem MayorRevealWhenDoneTasks;

    public static OverrideTasksData MayorTasks;

    public static Dictionary<byte, int> MayorUsedButtonCount = [];

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mayor);
        MayorAdditionalVote = IntegerOptionItem.Create(Id + 10, "MayorAdditionalVote", new(1, 20, 1), 3, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor])
            .SetValueFormat(OptionFormat.Votes);
        MayorHasPortableButton = BooleanOptionItem.Create(Id + 11, "MayorHasPortableButton", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        MayorNumOfUseButton = IntegerOptionItem.Create(Id + 12, "MayorNumOfUseButton", new(1, 20, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(MayorHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
        MayorHideVote = BooleanOptionItem.Create(Id + 13, "MayorHideVote", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        MayorRevealWhenDoneTasks = BooleanOptionItem.Create(Id + 14, "MayorRevealWhenDoneTasks", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
        MayorTasks = OverrideTasksData.Create(Id + 15, TabGroup.CrewmateRoles, CustomRoles.Mayor);
    }

    public override void Init()
    {
        On = false;
        MayorUsedButtonCount = [];
    }
    public override void Add(byte playerId)
    {
        MayorUsedButtonCount[playerId] = 0;
        On = true;
    }
    public override void Remove(byte playerId)
    {
        MayorUsedButtonCount[playerId] = 0;
    }

    public override int CalcVote(PlayerVoteArea PVA)
    {
        return MayorAdditionalVote.GetInt();
    }

    public override void OnPressEmergencyButton(PlayerControl reporter)
    {
        if (reporter.Is(CustomRoles.Mayor))
        {
            MayorUsedButtonCount[reporter.PlayerId] += 1;
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown =
                !MayorUsedButtonCount.TryGetValue(playerId, out var count) || count < MayorNumOfUseButton.GetInt()
                ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                : 300f;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {

        if (pc.Is(CustomRoles.Mayor) && MayorHasPortableButton.GetBool() && !CopyCat.playerIdList.Contains(pc.PlayerId))
        {
            if (MayorUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < MayorNumOfUseButton.GetInt())
            {
                pc?.MyPhysics?.RpcBootFromVent(vent.Id);
                pc?.NoCheckStartMeeting(pc?.Data);
            }
        }
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser)
    {
        if (MayorRevealWhenDoneTasks.GetBool())
        {
            if (target.Is(CustomRoles.Mayor) && target.GetPlayerTaskState().IsTaskFinished)
            {
                if (!isUI) Utils.SendMessage(GetString("GuessMayor"), guesser.PlayerId);
                else guesser.ShowPopUp(GetString("GuessMayor"));
                return true;
            }
        }
        return false;
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => MayorRevealWhenDoneTasks.GetBool() && target.Is(CustomRoles.Mayor) && target.GetPlayerTaskState().IsTaskFinished;
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("MayorVentButtonText");
    }
    public override Sprite AbilityButtonSprite => CustomButton.Get("Collective");


}
