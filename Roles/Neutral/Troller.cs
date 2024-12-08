using AmongUs.GameOptions;
using System;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

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

    private SystemTypes CurrentActiveSabotage = SystemTypes.Hallway;
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
        StartMeeting
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Troller);
        TrollsPerRound = IntegerOptionItem.Create(Id + 10, "Troller_TrollsPerRound", new(1, 10, 1), 2, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Troller]);
        CanHaveCallMeetingEvent = BooleanOptionItem.Create(Id + 11, "Troller_CanHaveStartMeetingEvent", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Troller]);
        OverrideTasksData.Create(Id + 15, TabGroup.NeutralRoles, CustomRoles.Troller);
    }
    public override void Init()
    {
        AllEvents.Clear();
    }
    public override void Add(byte playerId)
    {
        ResetAbility();

        AllEvents = [.. EnumHelper.GetAllValues<Events>()];

        if (GetActiveMapName() is not (MapNames.Airship or MapNames.Polus or MapNames.Fungle))
        {
            AllEvents.Remove(Events.AllDoorsOpen);
            AllEvents.Remove(Events.AllDoorsClose);
            AllEvents.Remove(Events.SetDoorsRandomly);
        }
        if (!CanHaveCallMeetingEvent.GetBool())
        {
            AllEvents.Remove(Events.StartMeeting);
        }
    }
    public override void Remove(byte playerId)
    {
        AbilityLimit = 0;
    }
    private void ResetAbility() => AbilityLimit = TrollsPerRound.GetInt();
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public override void AfterMeetingTasks() => ResetAbility();
    public override bool OnTaskComplete(PlayerControl troller, int completedTaskCount, int totalTaskCount)
    {
        if (!troller.IsAlive() || AbilityLimit <= 0) return true;

        AbilityLimit--;

        if (IsActive(SystemTypes.MushroomMixupSabotage) || IsActive(SystemTypes.Electrical))
        {
            AllEvents.Remove(Events.SabotageActivated);
            AllEvents.Remove(Events.SabotageDisabled);
        }
        else if (AnySabotageIsActive())
        {
            AllEvents.Remove(Events.SabotageActivated);
        }
        else
        {
            AllEvents.Remove(Events.SabotageDisabled);
        }

        var randomEvent = AllEvents.RandomElement();

        Logger.Info($"Random Event: {randomEvent}", "Troller");

        switch (randomEvent)
        {
            case Events.LowSpeed:
            case Events.HighSpeed:
                var newSpeed = randomEvent is Events.LowSpeed ? 0.3f : 5f;
                var tempSpeed = Main.AllPlayerSpeed.ToDictionary(k => k.Key, v => v.Value);

                foreach (var pcSpeed in Main.AllAlivePlayerControls)
                {
                    Main.AllPlayerSpeed[pcSpeed.PlayerId] = newSpeed;
                    pcSpeed.Notify(GetString("Troller_ChangesSpeed"));
                }
                MarkEveryoneDirtySettings();

                _ = new LateTask(() =>
                {
                    foreach (var pcSpeed in Main.AllAlivePlayerControls)
                    {
                        Main.AllPlayerSpeed[pcSpeed.PlayerId] = Main.AllPlayerSpeed[pcSpeed.PlayerId] - newSpeed + tempSpeed[pcSpeed.PlayerId];
                        pcSpeed.Notify(GetString("Troller_SpeedOut"));
                    }
                    MarkEveryoneDirtySettings();
                }, 10f, "Troller: Set Speed to default");
                break;
            case Events.SabotageActivated:
                var shipStatusActivated = ShipStatus.Instance;
                List<SystemTypes> allSabotage = [];
                switch (GetActiveMapName())
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
                    case SystemTypes.MushroomMixupSabotage:
                        shipStatusActivated.RpcUpdateSystem(randomSabotage, 1);
                        break;
                    default:
                        shipStatusActivated.RpcUpdateSystem(randomSabotage, 128);
                        break;
                }
                troller.Notify(GetString("Troller_YouCausedSabotage"));
                break;
            case Events.SabotageDisabled:
                var shipStatusDisabled = ShipStatus.Instance;
                switch (CurrentActiveSabotage)
                {
                    case SystemTypes.Reactor:
                    case SystemTypes.Laboratory:
                        shipStatusDisabled.RpcUpdateSystem(CurrentActiveSabotage, 16);
                        break;
                    case SystemTypes.HeliSabotage:
                        shipStatusDisabled.RpcUpdateSystem(CurrentActiveSabotage, 16);
                        shipStatusDisabled.RpcUpdateSystem(CurrentActiveSabotage, 17);
                        break;
                    case SystemTypes.LifeSupp:
                        shipStatusDisabled.RpcUpdateSystem(CurrentActiveSabotage, 66);
                        shipStatusDisabled.RpcUpdateSystem(CurrentActiveSabotage, 67);
                        break;
                    case SystemTypes.Comms:
                        var mapId = GetActiveMapId();

                        shipStatusDisabled.RpcUpdateSystem(CurrentActiveSabotage, 16);
                        if (mapId is 1 or 5) // Mira HQ or The Fungle
                            shipStatusDisabled.RpcUpdateSystem(CurrentActiveSabotage, 17);
                        break;
                }
                troller.Notify(GetString("Troller_YouFixedSabotage"));
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
            case Events.CooldownsResetToZero:
                var setToDefault = randomEvent is Events.CooldownsResetToDefault;
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.HasKillButton() && pc.CanUseKillButton())
                    {
                        if (setToDefault)
                            pc.SetKillCooldown();
                        else
                            pc.SetKillCooldown(0.3f);

                        pc.Notify(GetString("Troller_ChangeYourCooldown"));
                    }
                }
                troller.Notify(GetString("Troller_YouChangedCooldown"));
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
                    troller.Notify(GetString("Troller_NoAddons"));
                    break;
                }
                var addon = addons.RandomElement();
                Main.PlayerStates[randomPC.PlayerId].RemoveSubRole(addon);
                troller.Notify(GetString("Troller_RemoveRandomAddon"));
                randomPC.Notify(GetString("Troller_RemoveYourAddon"));
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
                    if (changePositionPlayers.Contains(pc.PlayerId) || !pc.CanBeTeleported()) continue;

                    var filtered = Main.AllAlivePlayerControls.Where(a => a.CanBeTeleported() && a.PlayerId != pc.PlayerId && !changePositionPlayers.Contains(a.PlayerId)).ToArray();
                    if (!filtered.Any()) break;

                    var target = filtered.RandomElement();

                    changePositionPlayers.Add(target.PlayerId);
                    changePositionPlayers.Add(pc.PlayerId);

                    pc.RPCPlayCustomSound("Teleport");

                    var originPs = target.GetCustomPosition();
                    target.RpcTeleport(pc.GetCustomPosition());
                    pc.RpcTeleport(originPs);
                }
                break;
            case Events.StartMeeting:
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
            CurrentActiveSabotage = systemType;
        }
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState8 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor8;
        var NonCompleteColor8 = Color.white;
        TextColor8 = comms ? Color.gray : NonCompleteColor8;
        string Completed8 = comms ? "?" : $"{taskState8.CompletedTasksCount}";
        ProgressText.Append(ColorString(TextColor8, $"({Completed8}/{taskState8.AllTasksCount}) "));
        ProgressText.Append(ColorString((AbilityLimit > 0) ? GetRoleColor(CustomRoles.Troller).ShadeColor(0.25f) : Color.gray, $" <color=#ffffff>-</color> {Math.Round(AbilityLimit, 1)}"));
        return ProgressText.ToString();
    }
}
