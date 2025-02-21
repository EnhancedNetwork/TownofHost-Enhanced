using AmongUs.GameOptions;
using TOHE.Roles.Core;

namespace TOHE.Roles.Neutral;

// 来源：https://github.com/Yumenopai/TownOfHost_Y
internal class Stalker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Stalker;
    private const int Id = 18100;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => SnatchesWins ? Custom_RoleType.NeutralEvil : Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanVent;
    private static OptionItem CanCountNeutralKiller;
    private static OptionItem SnatchesWin;

    public static bool SnatchesWins = false;
    public static readonly Dictionary<byte, bool> IsWinKill = [];

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Stalker, 1);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker])
            .SetValueFormat(OptionFormat.Seconds);
        HasImpostorVision = BooleanOptionItem.Create(Id + 11, GeneralOption.ImpostorVision, false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker]);
        CanVent = BooleanOptionItem.Create(Id + 14, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker]);
        CanCountNeutralKiller = BooleanOptionItem.Create(Id + 12, "CanCountNeutralKiller", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker]);
        SnatchesWin = BooleanOptionItem.Create(Id + 13, GeneralOption.SnatchesWin, false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker]);

    }
    public override void Init()
    {
        IsWinKill.Clear();
        SnatchesWins = SnatchesWin.GetBool();
    }
    public override void Add(byte playerId)
    {
        IsWinKill[playerId] = false;

        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (Utils.IsActive(SystemTypes.Electrical) || inMeeting || isSuicide) return;

        // Code from AU: SabotageSystemType.UpdateSystem switch SystemTypes.Electrical
        byte switchId = 4;
        for (int index = 0; index < 5; ++index)
        {
            if (BoolRange.Next())
                switchId |= (byte)(1 << index);
        }
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)(switchId | 128U));
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (_Player == null || !SnatchesWins) return;

        var stalkerId = _Player.PlayerId;
        var targetRole = target.GetCustomRole();
        var succeeded = targetRole.IsImpostor();
        if (CanCountNeutralKiller.GetBool() && !target.Is(CustomRoles.Arsonist) && !target.Is(CustomRoles.Revolutionist))
        {
            succeeded = succeeded || target.IsNeutralKiller();
        }

        if (succeeded) IsWinKill[stalkerId] = true;
    }
}
