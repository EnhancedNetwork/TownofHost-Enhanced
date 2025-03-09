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

}

public class Shifter : RoleBase
{
    public override CustomRoles Role => CustomRoles.Shifter;

    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

    public override Custom_RoleType ThisRoleType => Custom_RoleType.None;
}

