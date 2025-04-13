using AmongUs.GameOptions;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Maverick : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Maverick;
    private const int Id = 13200;
    public static bool HasEnabled = CustomRoleManager.HasEnabled(CustomRoles.Maverick);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    public static OptionItem MinKillsForWin;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Maverick, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Maverick])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Maverick]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Maverick]);
        MinKillsForWin = IntegerOptionItem.Create(Id + 14, "Maverick_MinKillsToWin", new(0, 14, 1), 2, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Maverick]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override string GetProgressText(byte playerId, bool comms)
    {
        int minKills = MinKillsForWin.GetInt();
        if (minKills == 0) return string.Empty;

        var ProgressText = new StringBuilder();
        int numKills = (int)playerId.GetAbilityUseLimit();
        Color color = numKills >= minKills ? Color.green : Color.red;

        ProgressText.Append(Utils.ColorString(color, $"({numKills}/{minKills})"));
        return ProgressText.ToString();
    }
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (isSuicide) return;

        killer.RpcIncreaseAbilityUseLimitBy(1);
    }
}
