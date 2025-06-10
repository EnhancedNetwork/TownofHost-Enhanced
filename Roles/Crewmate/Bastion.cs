using AmongUs.GameOptions;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Bastion : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Bastion;
    private const int Id = 10200;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem BombsClearAfterMeeting;
    private static OptionItem BastionBombCooldown;
    private static OptionItem BastionMaxBombs;

    public readonly HashSet<int> BombedVents = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Bastion);
        BastionMaxBombs = IntegerOptionItem.Create(Id + 12, "AbilityUseLimit", new(1, 20, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bastion]);
        BastionBombCooldown = FloatOptionItem.Create(Id + 11, "BombCooldown", new(0, 180, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bastion])
            .SetValueFormat(OptionFormat.Seconds);
        BombsClearAfterMeeting = BooleanOptionItem.Create(Id + 10, "BombsClearAfterMeeting", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bastion]);
        BastionAbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 4, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bastion])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        BombedVents.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(BastionMaxBombs.GetInt());
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerInVentMaxTime = 1;
        AURoleOptions.EngineerCooldown = BastionBombCooldown.GetFloat();
    }
    public override bool OnCoEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        if (!BombedVents.Contains(ventId)) return false;

        var pc = physics.myPlayer;
        if (pc.Is(Custom_Team.Crewmate) && !pc.Is(CustomRoles.Bastion) && !pc.IsCrewVenter() && !CopyCat.playerIdList.Contains(pc.PlayerId) && !Main.TasklessCrewmate.Contains(pc.PlayerId))
        {
            Logger.Info("Crewmate enter in bombed vent, bombed is cancel", "Bastion.OnCoEnterVentOther");
            return false;
        }
        else if (pc.Is(CustomRoles.DoubleAgent))
        {
            Logger.Info("DoubleAgent enter in bombed vent, bombed is cancel", "Bastion.OnCoEnterVentOther");
            return false;
        }
        else if (pc.IsTransformedNeutralApocalypse())
        {
            Logger.Info("Horseman enter in bombed vent, bombed is cancel", "Bastion.OnCoEnterVentOther");
            return false;
        }
        else
        {
            _ = new LateTask(() =>
            {
                var bastion = _Player;
                bastion.Notify(GetString("BastionNotify"));
                pc.Notify(GetString("EnteredBombedVent"));

                pc.SetDeathReason(PlayerState.DeathReason.Bombed);
                pc.RpcMurderPlayer(pc);
                pc.SetRealKiller(bastion);
                BombedVents.Remove(ventId);
            }, 0.5f, "Player bombed by Bastion");
            return true;
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (pc.GetAbilityUseLimit() >= 1)
        {
            pc.RpcRemoveAbilityUse();
            BombedVents.Add(vent.Id);
            pc.Notify(GetString("VentBombSuccess"));
        }
        else
        {
            pc.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (BombsClearAfterMeeting.GetBool())
        {
            BombedVents.Clear();
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.AbilityButton.buttonLabelText.text = GetString("BastionVentButtonText");
    }
}
