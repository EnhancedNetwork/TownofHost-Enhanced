using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using Rewired.Demos;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Neutral;

internal class Shocker : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 31000;
    private static readonly HashSet<byte> PlayerIds = new();
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem ShockerAbilityCooldown;
    private static OptionItem ShockerAbilityDuration;
    private static OptionItem ShockerAbilityResetAfterMeeting;
    private static OptionItem ShockerAbilityPerRound;
    private static OptionItem ShockeShockInVents;
    private static OptionItem ShockerOutsideRadius;
    private static OptionItem ShockerHideBody;
    private static OptionItem ShockerImpostorVision;

    private static Dictionary<byte, List<Collider2D>> ShockedRooms = new();
    private static List<byte> IsShocking = new();

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Shocker);
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
        ShockeShockInVents = BooleanOptionItem.Create(Id + 14, "ShockeShockInVents", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]);
        ShockerOutsideRadius = FloatOptionItem.Create(Id + 15, "ShockerOutsideRadius", new(0f, 5f, 0.5f), 3, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]);
        ShockerHideBody = BooleanOptionItem.Create(Id + 16, "ShockerHideBody", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]);
        ShockerImpostorVision = BooleanOptionItem.Create(Id + 17, "ShockerImpostorVision", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Shocker]);
        OverrideTasksData.Create(Id + 20, TabGroup.NeutralRoles, CustomRoles.Shocker);
    }

    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
        AbilityLimit = ShockerAbilityPerRound.GetValue();
    }
    public override void Remove(byte playerId)
    {
        PlayerIds.Remove(playerId);
    }
    public override void AfterMeetingTasks()
    {
        AbilityLimit = ShockerAbilityPerRound.GetValue();
        if (ShockerAbilityResetAfterMeeting.GetBool())
        {
            IsShocking.Clear();
            ShockedRooms.Clear();
        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = AbilityLimit > 0 ? ShockerAbilityCooldown.GetFloat() : 300;
        AURoleOptions.EngineerInVentMaxTime = 1;
        opt.SetVision(ShockerImpostorVision.GetBool());
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (AbilityLimit < 1)
            return;
        if (IsShocking.Contains(pc.PlayerId))
        {
            pc.Notify(Translator.GetString("ShockerIsShocking"));
            return;
        }
        AbilityLimit--;
        pc.Notify(Translator.GetString("ShockerAbilityActivate"));
        IsShocking.Add(pc.PlayerId);
        _ = new LateTask(() =>
        {
            ShockedRooms.Remove(pc.PlayerId);
            IsShocking.Remove(pc.PlayerId);
            pc.Notify(Translator.GetString("ShockerAbilityDeactivate"));
        }, ShockerAbilityDuration.GetValue(), "Shocker Is Shocking");
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (completedTaskCount == totalTaskCount)
            AbilityLimit++;

        if (IsShocking.Contains(player.PlayerId))
        {
            player.Notify(Translator.GetString("ShockerIsShocking"));
            return false;
        }
        Vector2 location = player.GetTruePosition();
        bool IsRoom = false;
        ShipStatus.Instance.AllRooms.ForEach(room =>
        {
            if (room.roomArea.OverlapPoint(location))
            {
                if (!ShockedRooms.ContainsKey(player.PlayerId))
                {
                    ShockedRooms[player.PlayerId] = new List<Collider2D>();
                }
                ShockedRooms[player.PlayerId].Add(room.roomArea);
                IsRoom = true;
                Logger.Info($"Added {room.RoomId} ({room.roomArea.name}) to {player.PlayerId} shocked rooms", "Shocker");
            }
        });
        if (!IsRoom)
        {
            Logger.Info($"Player {player.PlayerId} is not in a room", "Shocker");
            Collider2D collider2D = new GameObject("Outside").AddComponent<CircleCollider2D>();
            collider2D.transform.position = location;
            ((CircleCollider2D)collider2D).radius = ShockerOutsideRadius.GetFloat();
            collider2D.isTrigger = true;
            if (!ShockedRooms.ContainsKey(player.PlayerId))
            {
                ShockedRooms[player.PlayerId] = new List<Collider2D>();
            }
            ShockedRooms[player.PlayerId].Add(collider2D);
        }
        return true;
    }
    public override bool CanUseKillButton(PlayerControl pc) => false;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("ShockerVentButtonText"));
        hud.AbilityButton.SetUsesRemaining((int)AbilityLimit);
    }
    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shocker).ShadeColor(0.25f), $"({AbilityLimit})");
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;
    public static void OnUpdate(PlayerControl player)
    {
        if (!player.IsAlive())
            return;

        foreach (byte playerId in PlayerIds)
        {
            if (!IsShocking.Contains(playerId))
                continue;

            if (ShockedRooms.ContainsKey(playerId))
            {
                foreach (Collider2D collider in ShockedRooms[playerId])
                {
                    if (collider.OverlapPoint(player.GetTruePosition()))
                    {
                        if (!ShockeShockInVents.GetBool() && player.inVent)
                            break;
                        Logger.Info($"{player.PlayerId} overlaps {collider.name}", "Shocker.OnUpdate");
                        if (ShockerHideBody.GetBool())
                            player.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());
                        player.RpcMurderPlayer(player);
                        player.SetRealKiller(Utils.GetPlayerById(playerId));
                        player.SetDeathReason(PlayerState.DeathReason.Electrocuted);
                        break;
                    }
                }
            }
        }
    }
}
