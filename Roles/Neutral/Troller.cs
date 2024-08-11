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

    private SystemTypes CurrantActiveSabotage = SystemTypes.Hallway;
    private static readonly HashSet<SystemTypes> AllSabotages =
    [
        SystemTypes.Reactor,
        SystemTypes.Laboratory,
        SystemTypes.HeliSabotage,
        SystemTypes.LifeSupp,
        SystemTypes.Comms,
        SystemTypes.Electrical,
        SystemTypes.MushroomMixupSabotage,
    ];

    enum RandomEvent
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
        GetBadAddon,
        TelepostEveryoneToVents,
        //CallMeeting,
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Troller);
        TrollsPerRound = IntegerOptionItem.Create(Id + 10, "Troller_TrollsPerRound", new(1, 10, 1), 1, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Troller]);
        OverrideTasksData.Create(Id + 15, TabGroup.NeutralRoles, CustomRoles.Troller);
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = TrollsPerRound.GetInt();
    }
    public override void Remove(byte playerId)
    {
        AbilityLimit = 0;
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (!player.IsAlive() || AbilityLimit <= 0) return true;
        
        AbilityLimit--;
        var allEvents = EnumHelper.GetAllValues<RandomEvent>().ToList();

        if (Utils.AnySabotageIsActive())
        {
            allEvents.Remove(RandomEvent.SabotageActivated);
        }
        else
        {
            allEvents.Remove(RandomEvent.SabotageDisabled);
        }

        var randomEvent = allEvents.RandomElement();

        switch (randomEvent)
        {
            case RandomEvent.LowSpeed:
            case RandomEvent.HighSpeed:
                var newSpeed = randomEvent is RandomEvent.LowSpeed ? 0.3f : 1.8f;
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
                        Main.AllPlayerSpeed[pcSpeed.PlayerId] = tempSpeed[pcSpeed.PlayerId];
                        pcSpeed.Notify(GetString("TrollerSpeedOut"));
                    }
                    Utils.MarkEveryoneDirtySettings();
                }, 10f, "Alchemist: Set Speed to default");
                break;
            case RandomEvent.SabotageActivated:

                break;
            case RandomEvent.SabotageDisabled:
                var shipStatus = ShipStatus.Instance;
                switch (CurrantActiveSabotage)
                {
                    case SystemTypes.Reactor:
                        shipStatus.RpcUpdateSystem(SystemTypes.Reactor, 16);
                        shipStatus.RpcUpdateSystem(SystemTypes.Reactor, 17);
                        break;
                    case SystemTypes.Laboratory:
                        shipStatus.RpcUpdateSystem(SystemTypes.Laboratory, 67);
                        shipStatus.RpcUpdateSystem(SystemTypes.Laboratory, 66);
                        break;
                    case SystemTypes.LifeSupp:
                        shipStatus.RpcUpdateSystem(SystemTypes.LifeSupp, 67);
                        shipStatus.RpcUpdateSystem(SystemTypes.LifeSupp, 66);
                        break;
                    case SystemTypes.Comms:
                        shipStatus.RpcUpdateSystem(SystemTypes.Comms, 16);
                        shipStatus.RpcUpdateSystem(SystemTypes.Comms, 17);
                        break;
                }
                break;
            case RandomEvent.AllDoorsOpen:
                try
                {
                    DoorsReset.OpenAllDoors();
                }
                catch
                {
                }
                break;
            case RandomEvent.AllDoorsClose:
                try
                {
                    DoorsReset.CloseAllDoors();
                }
                catch
                {
                }
                break;
            case RandomEvent.SetDoorsRandomly:
                try
                {
                    DoorsReset.OpenOrCloseAllDoorsRandomly();
                }
                catch
                {
                }
                break;
            case RandomEvent.CooldownsResetToDefault:
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.HasImpKillButton() && pc.CanUseKillButton())
                    {
                        pc.SetKillCooldown();
                    }
                }
                break;
            case RandomEvent.CooldownsResetToZero:
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.HasImpKillButton() && pc.CanUseKillButton())
                    {
                        pc.SetKillCooldown(0.3f);
                    }
                }
                break;
            case RandomEvent.LoseAddon:

                break;
            case RandomEvent.GetBadAddon:

                break;
            case RandomEvent.TelepostEveryoneToVents:
                foreach (var pcTeleport in Main.AllAlivePlayerControls)
                {
                    pcTeleport.RpcRandomVentTeleport();
                }
                break;
            //case RandomEvent.CallMeeting:
            //    var pcCallMeeting = Main.AllAlivePlayerControls.RandomElement();
            //    pcCallMeeting.NoCheckStartMeeting(null);
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
                SystemTypes.Electrical or
                SystemTypes.LifeSupp or
                SystemTypes.Comms or
                SystemTypes.MushroomMixupSabotage)
        {
            CurrantActiveSabotage = systemType;
        }
    }
    public override void SwitchSystemUpdate(SwitchSystem __instance, byte amount, PlayerControl player)
    {
        if (!Main.MeetingIsStarted)
        {
            CurrantActiveSabotage = SystemTypes.Electrical;
        }
    }
}
