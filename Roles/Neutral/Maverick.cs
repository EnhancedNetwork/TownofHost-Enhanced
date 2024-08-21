using AmongUs.GameOptions;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Maverick : RoleBase
{
    //===========================SETUP================================\\
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

    public int NumKills = new();

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
        NumKills = 0;
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override string GetProgressText(byte playerId, bool comms)
    {
        int minKills = MinKillsForWin.GetInt();
        if (minKills == 0) return string.Empty;

        if (Main.PlayerStates[playerId].RoleClass is not Maverick mr) return string.Empty;
        int numKills = mr.NumKills;
        Color color = numKills >= minKills ? Color.green : Color.red;
        return Utils.ColorString(color, $"({numKills}/{minKills})");
    }
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (isSuicide) return;

        NumKills++;
    }
}
