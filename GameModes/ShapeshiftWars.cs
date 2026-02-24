using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE;
public static class ShapeshiftWars
{
    public static OptionItem SW_NeedToDoTasksToKill;
    public static OptionItem SW_CommonTasks;
    public static OptionItem SW_ShortTasks;
    public static OptionItem SW_LongTasks;
    public static OptionItem SW_GenerateSameTasks;

    public static OptionItem SW_NormalSpeed;
    public static OptionItem SW_ShapeShiftSpeed;
    public static OptionItem SW_BoostWhenBeTarget;
    public static OptionItem SW_BoostWhenKill;

    public static OptionItem SW_FirstSSCoolDown;
    public static OptionItem SW_ShapeshiftCooldown;
    public static OptionItem SW_ShapeshiftDuration;
    public static OptionItem SW_ShapeshiftAnimation;

    public static OptionItem SW_NormalKcd;
    public static OptionItem SW_MisClickResetKcd;
    public static OptionItem SW_MisClickKcd;
    public static OptionItem SW_AllowMiskill;
    public static OptionItem SW_MissKillNextSSCoolDown;

    public static OptionItem SW_SsSeeTargetArrow;
    public static OptionItem SW_SsTargetSeeSArror;
}

public class Shifter : RoleBase
{
    public override CustomRoles Role => CustomRoles.Shifter;

    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
}
