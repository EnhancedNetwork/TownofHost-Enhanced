using static TOHE.Options;
using static TOHE.Utils;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Technician : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 31800;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override CustomRoles Role => CustomRoles.Technician;

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem SeeAllIDs;
    private static OptionItem SeePlayerInteractions;
    private static OptionItem NotifyWhenAnyoneVents;
    private static OptionItem NotifyWhenAnyoneShapeshifts;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Technician);
        SeeAllIDs = BooleanOptionItem.Create(Id + 2, "TechnicianSeeAllIds", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Technician]);
        SeePlayerInteractions = BooleanOptionItem.Create(Id + 3, "TechnicianSeePlayerInteractions", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Technician]);
        NotifyWhenAnyoneVents = BooleanOptionItem.Create(Id + 4, "TechnicianNotifyWhenAnyoneVents", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Technician]);
        NotifyWhenAnyoneShapeshifts = BooleanOptionItem.Create(Id + 5, "TechnicianNotifyWhenAnyoneShapeshifts", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Technician]);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (!seer.IsAlive() || !seen.IsAlive()) return string.Empty;

        return ColorString(GetRoleColor(CustomRoles.Lookout), $" {seen.Data.PlayerId}");
    }
    public static bool ActivateGuardAnimation(byte killerId, PlayerControl target, int colorId)
    {
        foreach (var technicianId in playerIdList.ToArray())
        {
            if (SeePlayerInteractions.GetBool())
            {
                if (SeePlayerInteractions.GetBool())
                    if (technicianId == killerId) continue;
                var technician = Utils.GetPlayerById(technicianId);
                if (technician == null) continue;


                technician.RpcGuardAndKill(target, false, true);
            }
        }
        return false;
    }
    public override bool OnCoEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        if (NotifyWhenAnyoneVents.GetBool())
        {
            _ = new LateTask(() =>
            {
                var technician = _Player;
                technician.Notify(GetString("TechnicianNotifyVent"));
            }, 0.5f, "Technician Notified of vent");
            return true;
        }
        return false;
    }

    public override void OnOthersShapeshift()
    {
        if (NotifyWhenAnyoneShapeshifts.GetBool())
        {
            _ = new LateTask(() =>
            {
                var technician = _Player;
                technician.Notify(GetString("TechnicianNotifyShapeshift"));
            }, 0.5f, "Technician Notified of Shapeshift");
        }
    }
}

