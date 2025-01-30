using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Shocker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Shocker;
    private const int Id = 31000;
    public static byte? playerId;
    public static bool HasEnabled => playerId.HasValue;
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem ShockerAbilityCooldown;
    private static OptionItem ShockerAbilityDuration;
    private static OptionItem ShockerAbilityResetAfterMeeting;
    private static OptionItem ShockerAbilityPerRound;
    private static OptionItem ShockerShockInVents;
    private static OptionItem ShockerOutsideRadius;
    private static OptionItem ShockerCanShockHimself;
    private static OptionItem HasImpostorVision;

    private static List<Collider2D> markedRooms = new();
    private static List<Collider2D> shockedRooms = new();
    private static List<Collider2D> customRooms = new();
    private static bool isShocking = false;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Shocker);
        ShockerAbilityCooldown = FloatOptionItem.Create(Id + 10, "ShockerAbilityCooldown", new(0, 180, 1), 10, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker])
            .SetValueFormat(OptionFormat.Seconds);
        ShockerAbilityDuration = FloatOptionItem.Create(Id + 11, "ShockerAbilityDuration", new(0, 180, 1), 10, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker])
            .SetValueFormat(OptionFormat.Seconds);
        ShockerAbilityPerRound = IntegerOptionItem.Create(Id + 12, "ShockerAbilityPerRound", new(0, 10, 1), 2, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]);
        ShockerAbilityResetAfterMeeting = BooleanOptionItem.Create(Id + 13, "ShockerAbilityResetAfterMeeting", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]);
        ShockerShockInVents = BooleanOptionItem.Create(Id + 14, "ShockerShockInVents", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]);
        ShockerOutsideRadius = FloatOptionItem.Create(Id + 15, "ShockerOutsideRadius", new(0f, 5f, 0.5f), 3, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]).SetValueFormat(OptionFormat.Multiplier);
        ShockerCanShockHimself = BooleanOptionItem.Create(Id + 16, "ShockerCanShockHimself", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 20, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]);
        OverrideTasksData.Create(18, TabGroup.NeutralRoles, CustomRoles.Shocker);
    }
    public override void Init()
    {
        playerId = null;
        markedRooms.Clear();

        foreach (var custom in customRooms)
        {
            if (custom != null && custom.gameObject != null)
            {
                Object.Destroy(custom.gameObject);
            }
        }
        customRooms.Clear();
        shockedRooms.Clear();
    }

    public override void Add(byte playerId)
    {
        Shocker.playerId = playerId;
        AbilityLimit = ShockerAbilityPerRound.GetValue();
        if (AmongUsClient.Instance.AmHost)
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateShocker);
    }
    public override void Remove(byte playerId)
    {
        Shocker.playerId = null;
        markedRooms.Clear();
        shockedRooms.Clear();

        foreach (var custom in customRooms)
        {
            if (custom != null && custom.gameObject != null)
            {
                Object.Destroy(custom.gameObject);
            }
        }
        customRooms.Clear();

        CustomRoleManager.OnFixedUpdateOthers.Remove(OnFixedUpdateShocker);
    }
    public override void AfterMeetingTasks()
    {
        AbilityLimit = ShockerAbilityPerRound.GetValue();
        SendSkillRPC();
        if (ShockerAbilityResetAfterMeeting.GetBool())
        {
            isShocking = false;
            markedRooms.Clear();
            shockedRooms.Clear();
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = AbilityLimit > 0 ? ShockerAbilityCooldown.GetFloat() : 300;
        AURoleOptions.EngineerInVentMaxTime = 1;
        opt.SetVision(HasImpostorVision.GetBool());
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (AbilityLimit < 1 || playerId != pc.PlayerId)
            return;
        if (isShocking)
        {
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shocker), GetString("ShockerIsShocking")));
            return;
        }
        AbilityLimit--;
        SendSkillRPC();
        pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shocker), GetString("ShockerAbilityActivate")));
        isShocking = true;
        shockedRooms = new List<Collider2D>(markedRooms);
        markedRooms.Clear();
        _ = new LateTask(() =>
        {
            shockedRooms.Clear();
            isShocking = false;
            pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shocker), GetString("ShockerAbilityDeactivate")));
        }, ShockerAbilityDuration.GetValue(), "Shocker Is Shocking");
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (completedTaskCount == totalTaskCount)
        {
            TaskState taskState = player.GetPlayerTaskState();
            player.Data.RpcSetTasks(new Il2CppStructArray<byte>(0));
            taskState.CompletedTasksCount = 0;
            taskState.AllTasksCount = player.Data.Tasks.Count;
        }
        if (player.GetPlainShipRoom() != null)
        {
            PlainShipRoom room = player.GetPlainShipRoom();
            markedRooms.Add(room.roomArea);
            Logger.Info($"Player {player.PlayerId} is in a room {room.RoomId} {room.name}", "Shocker");
            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shocker), GetString("ShockerRoomMarked")));
        }
        else
        {
            Logger.Info($"Player {player.PlayerId} is not in a room {player.GetTruePosition()}", "Shocker");
            Collider2D collider2D = new GameObject("ShockerOutside").AddComponent<CircleCollider2D>();
            collider2D.transform.position = player.GetTruePosition();
            ((CircleCollider2D)collider2D).radius = ShockerOutsideRadius.GetFloat();
            collider2D.isTrigger = true;
            markedRooms.Add(collider2D);
            customRooms.Add(collider2D);
            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shocker), GetString("ShockerRoomMarked")));
        }
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("ShockerVentButtonText"));
        hud.AbilityButton.SetUsesRemaining((int)AbilityLimit);
    }
    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shocker).ShadeColor(0.25f), $"({AbilityLimit})");
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute && _Player.IsAlive();
    private void OnFixedUpdateShocker(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (!player.IsAlive() || !playerId.HasValue)
            return;

        if (!ShockerShockInVents.GetBool() && player.inVent)
            return;

        if (!ShockerCanShockHimself.GetBool() && playerId == player.PlayerId)
            return;

        if (isShocking)
        {
            foreach (Collider2D collider in shockedRooms)
            {
                if (collider.IsTouching(player.Collider))
                {
                    Logger.Info($"{player.PlayerId} overlaps {collider.name}", "Shocker.OnUpdate");
                    player.SetDeathReason(PlayerState.DeathReason.Electrocuted);
                    player.RpcMurderPlayer(player);
                    player.SetRealKiller(_Player);
                    break;
                }
            }
        }
    }
}
