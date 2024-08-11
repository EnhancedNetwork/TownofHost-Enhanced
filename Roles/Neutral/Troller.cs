using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Troller : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 28700;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Troller);
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem TrollsPerRound;
    private static OptionItem CanHaveCallMeetingEvent;

    private SystemTypes CurrantActiveSabotage = SystemTypes.Hallway;
    private List<Events> AllEvents = [];

    enum Events
    {
        LowSpeed,
        HighSpeed,
        SabotageActivated,
        SabotageDisabled,
        AllDoorsOpen,
        AllDoorsClose,
        SetDoorsRandomly,
        CooldownsResetToDefault,
        CooldownsResetToZero,
        LoseAddon,
        /* GetBadAddon, */
        TelepostEveryoneToVents,
        PullEveryone,
        TwistEveryone,
        CallMeeting
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Troller);
        TrollsPerRound = IntegerOptionItem.Create(Id + 10, "Troller_TrollsPerRound", new(1, 10, 1), 1, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Troller]);
        CanHaveCallMeetingEvent = BooleanOptionItem.Create(Id + 11, "Troller_CanHaveCallMeetingEvent", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Troller]);
        OverrideTasksData.Create(Id + 15, TabGroup.NeutralRoles, CustomRoles.Troller);
    }
    public override void Init()
    {
        AllEvents.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = TrollsPerRound.GetInt();

        AllEvents = [.. EnumHelper.GetAllValues<Events>()];

        if (Utils.GetActiveMapName() is not (MapNames.Airship or MapNames.Polus or MapNames.Fungle))
        {
            AllEvents.Remove(Events.AllDoorsOpen);
            AllEvents.Remove(Events.AllDoorsClose);
            AllEvents.Remove(Events.SetDoorsRandomly);
        }
        if (!CanHaveCallMeetingEvent.GetBool())
        {
            AllEvents.Remove(Events.CallMeeting);
        }
    }
    public override void Remove(byte playerId)
    {
        AbilityLimit = 0;
    }
    public override bool OnTaskComplete(PlayerControl troller, int completedTaskCount, int totalTaskCount)
    {
        if (!troller.IsAlive() || AbilityLimit <= 0) return true;

        AbilityLimit--;

        if (Utils.IsActive(SystemTypes.MushroomMixupSabotage) || Utils.IsActive(SystemTypes.Electrical))
        {
            AllEvents.Remove(Events.SabotageActivated);
            AllEvents.Remove(Events.SabotageDisabled);
        }
        else if (Utils.AnySabotageIsActive())
        {
            AllEvents.Remove(Events.SabotageActivated);
        }
        else
        {
            AllEvents.Remove(Events.SabotageDisabled);
        }

        var randomEvent = AllEvents.RandomElement();

        switch (randomEvent)
        {
            case Events.LowSpeed:
            case Events.HighSpeed:
                var newSpeed = randomEvent is Events.LowSpeed ? 0.3f : 1.8f;
                var tempSpeed = Main.AllPlayerSpeed.ToDictionary(k => k.Key, v => v.Value);

                foreach (var pcSpeed in Main.AllAlivePlayerControls)
                {
                    Main.AllPlayerSpeed[pcSpeed.PlayerId] = newSpeed;
                    pcSpeed.Notify(GetString("TrollerChangesSpeed"));
                }
                Utils.MarkEveryoneDirtySettings();

                _ = new LateTask(() =>
                {
                    foreach (var pcSpeed in Main.AllAlivePlayerControls)
                    {
                        Main.AllPlayerSpeed[pcSpeed.PlayerId] = Main.AllPlayerSpeed[pcSpeed.PlayerId] - newSpeed + tempSpeed[pcSpeed.PlayerId];
                        pcSpeed.Notify(GetString("TrollerSpeedOut"));
                    }
                    Utils.MarkEveryoneDirtySettings();
                }, 10f, "Alchemist: Set Speed to default");
                break;
            case Events.SabotageActivated:
                var shipStatusActivated = ShipStatus.Instance;
                List<SystemTypes> allSabotage = [];
                switch ((MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId)
                {
                    case MapNames.Skeld:
                    case MapNames.Dleks:
                    case MapNames.Mira:
                        allSabotage.Add(SystemTypes.Reactor);
                        allSabotage.Add(SystemTypes.LifeSupp);
                        allSabotage.Add(SystemTypes.Comms);
                        break;
                    case MapNames.Polus:
                        allSabotage.Add(SystemTypes.Laboratory);
                        allSabotage.Add(SystemTypes.Comms);
                        break;
                    case MapNames.Airship:
                        allSabotage.Add(SystemTypes.HeliSabotage);
                        allSabotage.Add(SystemTypes.Comms);
                        break;
                    case MapNames.Fungle:
                        allSabotage.Add(SystemTypes.Reactor);
                        allSabotage.Add(SystemTypes.Comms);
                        allSabotage.Add(SystemTypes.MushroomMixupSabotage);
                        break;
                }
                var randomSabotage = allSabotage.RandomElement();
                switch (randomSabotage)
                {
                    case SystemTypes.Reactor:
                    case SystemTypes.Laboratory:
                    case SystemTypes.HeliSabotage:
                    case SystemTypes.LifeSupp:
                    case SystemTypes.Comms:
                        shipStatusActivated.RpcUpdateSystem(randomSabotage, 128);
                        break;
                    case SystemTypes.MushroomMixupSabotage:
                        shipStatusActivated.RpcUpdateSystem(randomSabotage, 1);
                        break;
                }
                break;
            case Events.SabotageDisabled:
                var shipStatusDisabled = ShipStatus.Instance;
                switch (CurrantActiveSabotage)
                {
                    case SystemTypes.Reactor:
                    case SystemTypes.Laboratory:
                        shipStatusDisabled.RpcUpdateSystem(CurrantActiveSabotage, 16);
                        break;
                    case SystemTypes.HeliSabotage:
                        shipStatusDisabled.RpcUpdateSystem(CurrantActiveSabotage, 16);
                        shipStatusDisabled.RpcUpdateSystem(CurrantActiveSabotage, 17);
                        break;
                    case SystemTypes.LifeSupp:
                        shipStatusDisabled.RpcUpdateSystem(CurrantActiveSabotage, 66);
                        shipStatusDisabled.RpcUpdateSystem(CurrantActiveSabotage, 67);
                        break;
                    case SystemTypes.Comms:
                        var mapId = Utils.GetActiveMapId();

                        shipStatusDisabled.RpcUpdateSystem(CurrantActiveSabotage, 16);
                        if (mapId is 1 or 5) // Mira HQ or The Fungle
                            shipStatusDisabled.RpcUpdateSystem(CurrantActiveSabotage, 17);
                        break;
                }
                break;
            case Events.AllDoorsOpen:
                DoorsReset.OpenAllDoors();
                break;
            case Events.AllDoorsClose:
                DoorsReset.CloseAllDoors();
                break;
            case Events.SetDoorsRandomly:
                DoorsReset.OpenOrCloseAllDoorsRandomly();
                break;
            case Events.CooldownsResetToDefault:
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.HasKillButton() && pc.CanUseKillButton())
                    {
                        pc.SetKillCooldown();
                    }
                }
                break;
            case Events.CooldownsResetToZero:
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.HasKillButton() && pc.CanUseKillButton())
                    {
                        pc.SetKillCooldown(0.3f);
                    }
                }
                break;
            case Events.LoseAddon:
                var randomPC = Main.AllAlivePlayerControls.RandomElement();
                var addons = Main.PlayerStates[randomPC.PlayerId].SubRoles.ToList();
                foreach (var role in addons)
                {
                    if (role is CustomRoles.LastImpostor ||
                        role is CustomRoles.Lovers || // Causes issues involving Lovers Suicide
                        role.IsBetrayalAddon())
                    {
                        addons.Remove(role);
                    }
                }
                if (!addons.Any())
                {
                    Logger.Info("No addons found on the target", "Troller");
                    break;
                }
                var addon = addons.RandomElement();
                Main.PlayerStates[randomPC.PlayerId].RemoveSubRole(addon);
                randomPC.MarkDirtySettings();
                break;
            case Events.TelepostEveryoneToVents:
                foreach (var pcTeleport in Main.AllAlivePlayerControls)
                {
                    pcTeleport.RpcRandomVentTeleport();
                }
                break;
            case Events.PullEveryone:
                ExtendedPlayerControl.RpcTeleportAllPlayers(troller.GetCustomPosition());
                break;
            case Events.TwistEveryone:
                List<byte> changePositionPlayers = [];
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (changePositionPlayers.Contains(pc.PlayerId) || Pelican.IsEaten(pc.PlayerId) || pc.onLadder || pc.inVent || pc.inMovingPlat || GameStates.IsMeeting) continue;

                    var filtered = Main.AllAlivePlayerControls.Where(a => !a.inVent && !Pelican.IsEaten(a.PlayerId) && !a.onLadder && a.PlayerId != pc.PlayerId && !changePositionPlayers.Contains(a.PlayerId)).ToArray();
                    if (filtered.Length == 0) break;

                    var target = filtered.RandomElement();

                    changePositionPlayers.Add(target.PlayerId);
                    changePositionPlayers.Add(pc.PlayerId);

                    pc.RPCPlayCustomSound("Teleport");

                    var originPs = target.GetCustomPosition();
                    target.RpcTeleport(pc.GetCustomPosition());
                    pc.RpcTeleport(originPs);
                }
                break;
            case Events.CallMeeting:
                var pcCallMeeting = Main.AllAlivePlayerControls.RandomElement();
                pcCallMeeting.NoCheckStartMeeting(null);
                break;
                //case Events.GetBadAddon:
                //    break;
        }

        return true;
    }
    public override void UpdateSystem(ShipStatus __instance, SystemTypes systemType, byte amount, PlayerControl player)
    {
        if (!Main.MeetingIsStarted
            && systemType is
                SystemTypes.HeliSabotage or
                SystemTypes.Laboratory or
                SystemTypes.Reactor or
                SystemTypes.LifeSupp or
                SystemTypes.Comms)
        {
            CurrantActiveSabotage = systemType;
        }
    }
}
