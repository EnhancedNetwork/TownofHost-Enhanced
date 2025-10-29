using Hazel;
using System;
using TOHE.Patches;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
class ShipFixedUpdatePatch
{
    public static void Postfix(/*ShipStatus __instance*/)
    {
        //Above here, all of us will execute
        if (!AmongUsClient.Instance.AmHost) return;

        //Below here, only the Host performs
        if (Main.IsFixedCooldown && Main.RefixCooldownDelay >= 0)
        {
            Main.RefixCooldownDelay -= Time.fixedDeltaTime;
        }
        else if (!float.IsNaN(Main.RefixCooldownDelay))
        {
            Utils.MarkEveryoneDirtySettings();
            Main.RefixCooldownDelay = float.NaN;
            Logger.Info("Refix Cooldown", "CoolDown");
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(MessageReader))]
public static class MessageReaderUpdateSystemPatch
{
    public static bool Prefix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes systemType, [HarmonyArgument(1)] PlayerControl player, [HarmonyArgument(2)] MessageReader reader)
    {
        if (systemType is
            SystemTypes.Ventilation
            or SystemTypes.Security
            or SystemTypes.Decontamination
            or SystemTypes.Decontamination2
            or SystemTypes.Decontamination3
            or SystemTypes.MedBay) return true;

        if (GameStates.IsHideNSeek) return true;

        var amount = MessageReader.Get(reader).ReadByte();
        if (EAC.RpcUpdateSystemCheck(player, systemType, amount))
        {
            Logger.Info("Eac patched Sabotage RPC", "MessageReaderUpdateSystemPatch");
            return false;
        }

        return UpdateSystemPatch.Prefix(__instance, systemType, player, amount);
    }
    public static void Postfix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes systemType, [HarmonyArgument(1)] PlayerControl player, [HarmonyArgument(2)] MessageReader reader)
    {
        if (systemType is
            SystemTypes.Ventilation
            or SystemTypes.Security
            or SystemTypes.Decontamination
            or SystemTypes.Decontamination2
            or SystemTypes.Decontamination3
            or SystemTypes.MedBay) return;

        if (GameStates.IsHideNSeek) return;

        UpdateSystemPatch.Postfix(__instance, systemType, player, MessageReader.Get(reader).ReadByte());
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(byte))]
class UpdateSystemPatch
{
    public static bool Prefix(ShipStatus __instance,
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] byte amount)
    {
        Logger.Msg($"SystemType: {systemType}, PlayerName: {player.GetNameWithRole().RemoveHtmlTags()}, amount: {amount}", "ShipStatus.UpdateSystem");

        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            Logger.SendInGame($"SystemType: {systemType}, PlayerName: {player.GetNameWithRole().RemoveHtmlTags()}, amount: {amount}");
        }

        if (!AmongUsClient.Instance.AmHost) return true;

        // ###### Can Be Sabotage Started? ######
        if ((Options.CurrentGameMode == CustomGameMode.FFA || Options.CurrentGameMode == CustomGameMode.UltimateTeam || Options.CurrentGameMode == CustomGameMode.TrickorTreat) && systemType == SystemTypes.Sabotage) return false;

        if (Options.DisableSabotage.GetBool() && systemType == SystemTypes.Sabotage) return false;


        // ###### Roles/Add-ons During Sabotages ######

        if (Fool.IsEnable && Fool.BlockFixSabotage(player, systemType))
        {
            return false;
        }


        if (player.Is(CustomRoles.Unlucky) && player.IsAlive()
            && (systemType is SystemTypes.Doors))
        {
            if (Unlucky.SuicideRand(player, Unlucky.StateSuicide.OpenDoor))
                return false;
        }

        player.GetRoleClass()?.UpdateSystem(__instance, systemType, amount, player);

        if (Quizmaster.HasEnabled)
            Quizmaster.OnSabotageCall(systemType);

        return true;
    }

    // Fast fix lights
    public static void Postfix(ShipStatus __instance,
        [HarmonyArgument(0)] SystemTypes systemType,
        [HarmonyArgument(1)] PlayerControl player,
        [HarmonyArgument(2)] byte amount)
    {
        Camouflage.CheckCamouflage();

        if (systemType == SystemTypes.Electrical && 0 <= amount && amount <= 4)
        {
            var SwitchSystem = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
            if (SwitchSystem != null && SwitchSystem.IsActive)
            {
                player.GetRoleClass()?.SwitchSystemUpdate(SwitchSystem, amount, player);
            }
        }
    }
    public static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max)
    {
        var Ids = new List<int>();
        for (var i = min; i <= max; i++)
        {
            Ids.Add(i);
        }
        CheckAndOpenDoors(__instance, amount, [.. Ids]);
    }
    private static void CheckAndOpenDoors(ShipStatus __instance, int amount, params int[] DoorIds)
    {
        if (!DoorIds.Contains(amount)) return;
        foreach (var id in DoorIds)
        {
            __instance.RpcUpdateSystem(SystemTypes.Doors, (byte)id);
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
class ShipStatusCloseDoorsPatch
{
    public static bool Prefix(/*ShipStatus __instance,*/ SystemTypes room)
    {
        Logger.Info($"Trying to close the door in the room: {room}", "CloseDoorsOfType");

        bool allow;
        if (Options.CurrentGameMode == CustomGameMode.FFA || Options.CurrentGameMode == CustomGameMode.UltimateTeam || Options.DisableCloseDoor.GetBool()) allow = false;
        else allow = true;

        if (allow)
        {
            Logger.Info($"The door is closed in room: {room}", "CloseDoorsOfType");
        }
        return allow;
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
class StartPatch
{
    public static void Postfix(ShipStatus __instance)
    {
        Logger.CurrentMethod();
        Logger.Info("-----------Start of game-----------", "Phase");

        Utils.CountAlivePlayers(sendLog: true, checkGameEnd: false);

        if (Options.AllowConsole.GetBool() && PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug)
        {
            if (!BepInEx.ConsoleManager.ConsoleActive && BepInEx.ConsoleManager.ConsoleEnabled)
                BepInEx.ConsoleManager.CreateConsole();
        }
        else
        {
            if (BepInEx.ConsoleManager.ConsoleActive && !DebugModeManager.AmDebugger)
            {
                BepInEx.ConsoleManager.DetachConsole();
                Logger.SendInGame(GetString("Warning.CanNotUseBepInExConsole"));
            }
        }

        switch (Utils.GetActiveMapName())
        {
            case MapNames.Skeld:
                var halloweenDecorationIsActive = Options.HalloweenDecorationsSkeld.GetBool();
                var birthdayDecorationIsActive = Options.EnableBirthdayDecorationSkeld.GetBool();
                var halloweenDecorationObject = __instance.transform.FindChild("Helloween");
                var birthdayDecorationObject = __instance.transform.FindChild("BirthdayDecorSkeld");

                if (Options.RandomBirthdayAndHalloweenDecorationSkeld.GetBool() && halloweenDecorationIsActive && birthdayDecorationIsActive)
                {
                    var random = IRandom.Instance.Next(0, 100);
                    if (random < 50)
                        halloweenDecorationObject?.gameObject.SetActive(true);
                    else
                        birthdayDecorationObject?.gameObject.SetActive(true);
                    break;
                }
                if (halloweenDecorationIsActive)
                    __instance.transform.FindChild("Helloween")?.gameObject.SetActive(true);

                if (birthdayDecorationIsActive)
                    __instance.transform.FindChild("BirthdayDecorSkeld")?.gameObject.SetActive(true);
                break;
            case MapNames.MiraHQ when Options.HalloweenDecorationsMira.GetBool():
                __instance.transform.FindChild("Halloween")?.gameObject.SetActive(true);
                break;
            case MapNames.Dleks when Options.HalloweenDecorationsDleks.GetBool():
                __instance.transform.FindChild("Helloween")?.gameObject.SetActive(true);
                break;
            case MapNames.Polus when Main.EnableCustomDecorations.Value:
                var Dropship = GameObject.Find("Dropship/panel_fuel");
                if (Dropship != null)
                {
                    var Decorations = UnityEngine.Object.Instantiate(Dropship, GameObject.Find("Dropship")?.transform);
                    Decorations.name = "Dropship_Decorations";
                    Decorations.transform.DestroyChildren();
                    UnityEngine.Object.Destroy(Decorations.GetComponent<Console>());
                    UnityEngine.Object.Destroy(Decorations.GetComponent<BoxCollider2D>());
                    UnityEngine.Object.Destroy(Decorations.GetComponent<PassiveButton>());
                    Decorations.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TOHE.Resources.Images.TOHO_decor.png", 100f);
                    Decorations.transform.SetSiblingIndex(1);
                    Decorations.transform.localPosition = new(0.0709f, 0.73f);
                }
                break;
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
class StartMeetingPatch
{
    public static void Prefix([HarmonyArgument(1)] NetworkedPlayerInfo target)
    {
        if (GameStates.IsHideNSeek) return;

        MeetingStates.ReportTarget = target;
        MeetingStates.DeadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
    }
    public static void Postfix()
    {
        foreach (var state in Main.PlayerStates.Values)
        {
            state.HasSpawned = false;
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
class ShipStatusBeginPatch
{
    public static void Prefix()
    {
        RpcSetTasksPatch.decidedCommonTasks.Clear();
    }
    public static void Postfix()
    {
        Logger.CurrentMethod();
    }
}

/*
    // Since SnapTo is unstable on the Server side
    // after a meeting, sometimes not all Players appear on the table
    // it's better to manually teleport them
*/
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.SpawnPlayer))]
class ShipStatusSpawnPlayerPatch
{
    public static bool Prefix(ShipStatus __instance, PlayerControl player, int numPlayers, bool initialSpawn)
    {
        // Skip first Spawn and modded clients
        if (!AmongUsClient.Instance.AmHost || initialSpawn || !player.IsAlive()) return true;

        Vector2 direction = Vector2.up.Rotate((player.PlayerId - 1) * (360f / numPlayers));
        Vector2 position = __instance.MeetingSpawnCenter + direction * __instance.SpawnRadius + new Vector2(0.0f, 0.3636f);

        // Delay teleport because the map will stop updating player positions too late
        _ = new LateTask(() =>
        {
            player?.RpcTeleport(position, isRandomSpawn: true, sendInfoInLogs: false);
        }, 1.5f, $"ShipStatus Spawn Player {player.PlayerId}", shoudLog: false);
        return false;
    }
}
[HarmonyPatch(typeof(PolusShipStatus), nameof(PolusShipStatus.SpawnPlayer))]
class PolusShipStatusSpawnPlayerPatch
{
    public static bool Prefix(PolusShipStatus __instance, PlayerControl player, int numPlayers, bool initialSpawn)
    {
        // Skip first Spawn and modded clients
        if (!AmongUsClient.Instance.AmHost || initialSpawn || !player.IsAlive()) return true;

        int num1 = Mathf.FloorToInt(numPlayers / 2f);
        int num2 = player.PlayerId % 15;

        Vector2 position = num2 >= num1
            ? __instance.MeetingSpawnCenter2 + Vector2.right * (num2 - num1) * 0.6f
            : __instance.MeetingSpawnCenter + Vector2.right * num2 * 0.6f;

        // Delay teleport because the map will stop updating player positions too late
        _ = new LateTask(() =>
        {
            player?.RpcTeleport(position, isRandomSpawn: true, sendInfoInLogs: false);
        }, 1.5f, $"PolusShipStatus Spawn Player {player.PlayerId}", shoudLog: false);
        return false;
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Serialize))]
class ShipStatusSerializePatch
{
    // Patch the global way of Serializing ShipStatus
    // If we are patching any other systemTypes, just add below like Ventilation
    public static List<int> ReactorFlashList = [];

    public static bool Prefix(ShipStatus __instance, [HarmonyArgument(0)] MessageWriter writer, [HarmonyArgument(1)] bool initialState, ref bool __result)
    {
        __result = false;
        if (!AmongUsClient.Instance.AmHost) return true;
        if (initialState) return true;

        // Original methods
        short num = 0;
        while (num < SystemTypeHelpers.AllTypes.Length)
        {
            SystemTypes systemTypes = SystemTypeHelpers.AllTypes[num];

            if (systemTypes is SystemTypes.Ventilation)
            {
                // Skip Ventilation here
                // Further new systems should skip original methods here and add new patches below
                num++;
                continue;
            }

            if (ReactorFlashList.Count > 0)
            {
                var sysSkip = Utils.GetCriticalSabotageSystemType();

                if (systemTypes == sysSkip)
                {
                    num++;
                    continue;
                }
            }

            if (__instance.Systems.TryGetValue(systemTypes, out ISystemType systemType) && systemType.IsDirty) // initialState used here in vanilla code. Removed it.
            {
                __result = true;
                writer.StartMessage((byte)systemTypes);
                systemType.Serialize(writer, initialState);
                writer.EndMessage();
            }
            num++;
        }

        // Ventilation part
        {
            // Logger.Info("doing Ventilation Serialize", "ShipStatusSerializePatch");
            // Serialize Ventilation with our own patches to clients specifically if needed
            bool customVentilation = false;

            if (GameStates.IsInGame)
            {
                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc.BlockVentInteraction())
                    {
                        customVentilation = true;
                        break;
                    }
                }
            }

            var ventilationSystem = __instance.Systems[SystemTypes.Ventilation].Cast<VentilationSystem>();
            if (ventilationSystem != null && ventilationSystem.IsDirty)
            {
                if (customVentilation)
                {
                    Utils.SetAllVentInteractions();
                }
                else
                {
                    var subwriter = MessageWriter.Get(SendOption.Reliable);
                    subwriter.StartMessage(5);
                    {
                        subwriter.Write(AmongUsClient.Instance.GameId);
                        subwriter.StartMessage(1);
                        {
                            subwriter.WritePacked(__instance.NetId);
                            subwriter.StartMessage((byte)SystemTypes.Ventilation);
                            ventilationSystem.Serialize(subwriter, false);
                            subwriter.EndMessage();
                        }
                        subwriter.EndMessage();
                    }
                    subwriter.EndMessage();
                    AmongUsClient.Instance.SendOrDisconnect(subwriter);
                    subwriter.Recycle();
                }
                ventilationSystem.IsDirty = false;
            }
        }

        // Reactor Flash
        {
            var reactor = Utils.GetCriticalSabotageSystemType();

            foreach (var player in Main.AllPlayerControls)
            {
                if (player.AmOwner) continue;
                var rwriter = MessageWriter.Get(SendOption.Reliable);
                rwriter.StartMessage(6);
                rwriter.Write(AmongUsClient.Instance.GameId);
                rwriter.WritePacked(player.OwnerId);

                rwriter.StartMessage(1);
                rwriter.WritePacked(__instance.NetId);
                rwriter.StartMessage((byte)reactor);

                if (ReactorFlashList.Contains(player.OwnerId))
                {
                    switch (reactor)
                    {
                        case SystemTypes.Reactor:
                        case SystemTypes.Laboratory:
                            rwriter.Write((float)1f);
                            rwriter.WritePacked(0);
                            break;
                        case SystemTypes.HeliSabotage:
                            rwriter.Write((float)1f);
                            rwriter.Write((float)1f);
                            rwriter.WritePacked(0);
                            rwriter.WritePacked(0);
                            break;
                    }

                }
                else
                {
                    __instance.Systems.TryGetValue(reactor, out ISystemType systemType);
                    systemType.Serialize(rwriter, false);
                }
                rwriter.EndMessage();
                rwriter.EndMessage();

                rwriter.EndMessage();


                AmongUsClient.Instance.SendOrDisconnect(rwriter);
                rwriter.Recycle();
            }
        }

        return false;
    }
}
