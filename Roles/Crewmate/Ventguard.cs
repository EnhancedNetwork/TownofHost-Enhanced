using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Ventguard : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 30000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Ventguard);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    public readonly HashSet<int> BlockedVents = [];

    public static OptionItem BlockDoesNotAffectCrew;
    public static OptionItem AbilityUseGainWithEachTaskCompleted;
    public static OptionItem MaxGuards;
    public static OptionItem BlocksResetOnMeeting;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Ventguard);
        MaxGuards = IntegerOptionItem.Create(Id + 10, "Ventguard_MaxGuards", new(1, 30, 1), 3, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Ventguard]);
        BlockDoesNotAffectCrew = BooleanOptionItem.Create(Id + 11, "Ventguard_BlockDoesNotAffectCrew", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Ventguard]);
        BlocksResetOnMeeting = BooleanOptionItem.Create(Id + 12, "Ventguard_BlocksResetOnMeeting", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Ventguard]);
        AbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 13, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.05f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Ventguard])
            .SetValueFormat(OptionFormat.Times);
    }

    public override void Init()
    {
        BlockedVents.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = MaxGuards.GetInt();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerInVentMaxTime = 1f;
        AURoleOptions.EngineerCooldown = 15f;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("VentguardVentButtonText");
    }

    public override void OnEnterVent(PlayerControl ventguard, Vent vent)
    {
        if (AbilityLimit >= 1)
        {
            AbilityLimit--;
            SendSkillRPC();
            BlockedVents.Add(vent.Id);
            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsAlive()) continue;
                if (ventguard.PlayerId != player.PlayerId && BlockDoesNotAffectCrew.GetBool() && player.Is(Custom_Team.Crewmate)) continue;

                CustomRoleManager.BlockedVentsList[player.PlayerId].Add(vent.Id);
                player.RpcSetVentInteraction();
            }
            ventguard.Notify(GetString("VentIsBlocked"));
            ventguard.MyPhysics.RpcBootFromVent(vent.Id);
        }
        else
        {
            ventguard.Notify(GetString("OutOfAbilityUsesDoMoreTasks"));
        }
    }

    public override void AfterMeetingTasks()
    {
        if (BlocksResetOnMeeting.GetBool() && BlockedVents.Any())
        {
            foreach (var ventId in BlockedVents)
            {
                foreach (var player in Main.AllPlayerControls)
                {
                    if (!player.IsAlive()) continue;
                    if (player.PlayerId != _Player?.PlayerId && BlockDoesNotAffectCrew.GetBool() && player.Is(Custom_Team.Crewmate)) continue;

                    CustomRoleManager.BlockedVentsList[player.PlayerId].Remove(ventId);
                }
            }
            BlockedVents.Clear();
        }
    }
}
