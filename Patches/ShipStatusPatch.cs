using Hazel;
using System;
using UnityEngine;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Neutral;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
class ShipFixedUpdatePatch
{
    public static void Postfix(/*ShipStatus __instance*/)
    {
        //Above here, all of us will execute
        if (!AmongUsClient.Instance.AmHost) return;

        //Below here, only the host performs
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
        if ((Options.CurrentGameMode == CustomGameMode.FFA) && systemType == SystemTypes.Sabotage) return false;

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
        if (Options.CurrentGameMode == CustomGameMode.FFA || Options.DisableCloseDoor.GetBool()) allow = false;
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
    public static void Postfix()
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

        if (GameStates.PolusIsActive && Main.EnableCustomDecorations.Value)
        {
            var Dropship = GameObject.Find("Dropship/panel_fuel");
            if (Dropship != null)
            {
                var Decorations = UnityEngine.Object.Instantiate(Dropship, GameObject.Find("Dropship")?.transform);
                Decorations.name = "Dropship_Decorations";
                Decorations.transform.DestroyChildren();
                UnityEngine.Object.Destroy(Decorations.GetComponent<Console>());
                UnityEngine.Object.Destroy(Decorations.GetComponent<BoxCollider2D>());
                UnityEngine.Object.Destroy(Decorations.GetComponent<PassiveButton>());
                Decorations.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TOHE.Resources.Images.Dropship-Decorations.png", 100f);
                Decorations.transform.SetSiblingIndex(1);
                Decorations.transform.localPosition = new(0.0709f, 0.73f);
            }
        }
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
class StartMeetingPatch
{
    public static void Prefix(ShipStatus __instance, PlayerControl reporter, NetworkedPlayerInfo target)
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
    //Prevent code from running twice as it gets activated later in LateTask
    public static bool RolesIsAssigned = false;
    public static void Postfix()
    {
        Logger.CurrentMethod();

        if (RolesIsAssigned && !Main.introDestroyed)
        {
            foreach (var player in Main.AllPlayerControls)
            {
                Main.PlayerStates[player.PlayerId].InitTask(player);
            }

            GameData.Instance.RecomputeTaskCounts();
            TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;

            Utils.DoNotifyRoles(ForceLoop: true, NoCache: true);
        }
    }
}
[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
class CheckTaskCompletionPatch
{
    public static bool Prefix(ref bool __result)
    {
        if (Options.DisableTaskWin.GetBool() || Options.NoGameEnd.GetBool() || TaskState.InitialTotalTasks == 0 || Options.CurrentGameMode == CustomGameMode.FFA)
        {
            __result = false;
            return false;
        }
        return true;
    }
}
