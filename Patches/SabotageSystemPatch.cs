using HarmonyLib;
using Hazel;

using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;

namespace TOHE;

//参考
//https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/MeltDownBoost.cs

[HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.Deteriorate))]
public static class ReactorSystemTypePatch
{
    public static void Prefix(ReactorSystemType __instance)
    {
        if (!__instance.IsActive || !Options.SabotageTimeControl.GetBool())
            return;
        if (ShipStatus.Instance.Type == ShipStatus.MapType.Pb)
        {
            if (__instance.Countdown >= Options.PolusReactorTimeLimit.GetFloat())
                __instance.Countdown = Options.PolusReactorTimeLimit.GetFloat();
            return;
        }
        return;
    }
}
[HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.Deserialize))]
public static class HeliSabotageSystemPatch
{
    public static void Prefix(HeliSabotageSystem __instance)
    {
        if (!__instance.IsActive || !Options.SabotageTimeControl.GetBool())
            return;
        if (ShipStatus.Instance != null)
            if (__instance.Countdown >= Options.AirshipReactorTimeLimit.GetFloat())
                __instance.Countdown = Options.AirshipReactorTimeLimit.GetFloat();
    }
}
[HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.UpdateSystem))]
public static class SwitchSystemRepairDamagePatch
{
    public static bool Prefix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            return true;
        }

        var reader = MessageReader.Get(msgReader);
        var amount = reader.ReadByte();

        // No matter if the blackout sabotage is sounded (beware of misdirection as it flies under the host's name)
        if (amount.HasBit(SwitchSystem.DamageSystem))
        {
            return true;
        }

        // Broken
        switch (player.GetCustomRole())
        {
            case CustomRoles.SabotageMaster:
                SabotageMaster.SwitchSystemRepair(__instance, amount, player.PlayerId);
                break;
            case CustomRoles.Alchemist when Alchemist.FixNextSabo == true:
                __instance.ActualSwitches = 0;
                __instance.ExpectedSwitches = 0;
                Alchemist.FixNextSabo = false;
                break;
        }
        // Broken
        if (player.Is(CustomRoles.Repairman))
            Repairman.SwitchSystemRepair(__instance, amount);

        if (player.Is(CustomRoles.Fool))
        {
            return false;
        }

        // Cancel if player can't fix a specific outage on Airship
        if (Main.NormalOptions.MapId == 4)
        {
            var truePosition = player.GetTruePosition();
            if (Options.DisableAirshipViewingDeckLightsPanel.GetBool() && Vector2.Distance(truePosition, new(-12.93f, -11.28f)) <= 2f) return false;
            if (Options.DisableAirshipGapRoomLightsPanel.GetBool() && Vector2.Distance(truePosition, new(13.92f, 6.43f)) <= 2f) return false;
            if (Options.DisableAirshipCargoLightsPanel.GetBool() && Vector2.Distance(truePosition, new(30.56f, 2.12f)) <= 2f) return false;
        }


        if (!amount.HasBit(SwitchSystem.DamageSystem) && Options.BlockDisturbancesToSwitches.GetBool())
        {
            // Shift 1 to the left by amount
            // Each digit corresponds to each switch
            // Far left switch - (amount: 0) 00001
            // Far right switch - (amount: 4) 10000
            // ref: SwitchSystem.RepairDamage, SwitchMinigame.FixedUpdate
            var switchedKnob = (byte)(0b_00001 << amount);

            // ExpectedSwitches: Up and down state of switches when all are on
            // ActualSwitches: Actual up/down state of switch
            // if Expected and Actual are the same for the operated knob, the knob is already fixed
            if ((__instance.ActualSwitches & switchedKnob) == (__instance.ExpectedSwitches & switchedKnob))
            {
                return false;
            }
        }
        return true;
    }
}
[HarmonyPatch(typeof(ElectricTask), nameof(ElectricTask.Initialize))]
public static class ElectricTaskInitializePatch
{
    public static void Postfix()
    {
        Utils.MarkEveryoneDirtySettings();
        if (!GameStates.IsMeeting)
            Utils.NotifyRoles();
    }
}
[HarmonyPatch(typeof(ElectricTask), nameof(ElectricTask.Complete))]
public static class ElectricTaskCompletePatch
{
    public static void Postfix()
    {
        Utils.MarkEveryoneDirtySettings();
        if (!GameStates.IsMeeting)
            Utils.NotifyRoles();
    }
}
// https://github.com/tukasa0001/TownOfHost/blob/357f7b5523e4bdd0bb58cda1e0ff6cceaa84813d/Patches/SabotageSystemPatch.cs
// Method called when sabotage occurs
[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.UpdateSystem))] // SetInitialSabotageCooldown - set sabotage cooldown in start game
public static class SabotageSystemTypeRepairDamagePatch
{
    private static bool isCooldownModificationEnabled;
    private static float modifiedCooldownSec;

    public static void Initialize()
    {
        isCooldownModificationEnabled = Options.SabotageCooldownControl.GetBool();
        modifiedCooldownSec = Options.SabotageCooldown.GetFloat();
    }

    public static bool Prefix(SabotageSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        var newReader = MessageReader.Get(msgReader);
        var amount = newReader.ReadByte();
        var nextSabotage = (SystemTypes)amount;

        if (Options.DisableSabotage.GetBool())
        {
            return false;
        }

        Logger.Info("Sabotage" + ", PlayerName: " + player.GetNameWithRole() + ", SabotageType: " + nextSabotage.ToString(), "RepairSystem");

        return CanSabotage(player, nextSabotage);
    }
    private static bool CanSabotage(PlayerControl player, SystemTypes systemType)
    {
        var playerRole = player.GetCustomRole();

        if (systemType == SystemTypes.Comms)
        {
            if (playerRole.Is(CustomRoles.Camouflager) && !Camouflager.CanUseCommsSabotage.GetBool()) 
                return false;
        }

        if (player.Is(CustomRoleTypes.Impostor) && (player.IsAlive() || !Options.DeadImpCantSabotage.GetBool())) return true;

        switch (playerRole)
        {
            case CustomRoles.Jackal when Jackal.CanUseSabotage.GetBool():
                return true;

            case CustomRoles.Sidekick when Jackal.CanUseSabotageSK.GetBool():
                return true;

            case CustomRoles.Bandit when Bandit.CanUseSabotage.GetBool():
                return true;

            case CustomRoles.Glitch:
                Glitch.Mimic(player);
                return false;

            case CustomRoles.Parasite when player.IsAlive():
                return true;

            case CustomRoles.PotionMaster when player.IsAlive():
                return true;

            case CustomRoles.Refugee when player.IsAlive():
                return true;

            case CustomRoles.EvilMini when player.IsAlive():
                return true;
        }

        return false;
    }

    public static void Postfix(SabotageSystemType __instance)
    {
        if (!isCooldownModificationEnabled || !AmongUsClient.Instance.AmHost)
        {
            return;
        }
        __instance.Timer = modifiedCooldownSec;
        __instance.IsDirty = true;
    }

    [HarmonyPatch(typeof(SecurityCameraSystemType), nameof(SecurityCameraSystemType.UpdateSystem))]
    public static class SecurityCameraSystemTypeUpdateSystemPatch
    {
        public static bool Prefix([HarmonyArgument(1)] MessageReader msgReader)
        {
            var newReader = MessageReader.Get(msgReader);
            var amount = newReader.ReadByte();

            // When the camera is disabled, the vanilla player opens the camera so it does not blink.
            if (amount == SecurityCameraSystemType.IncrementOp)
            {
                var camerasDisabled = (MapNames)Main.NormalOptions.MapId switch
                {
                    MapNames.Skeld => Options.DisableSkeldCamera.GetBool(),
                    MapNames.Polus => Options.DisablePolusCamera.GetBool(),
                    MapNames.Airship => Options.DisableAirshipCamera.GetBool(),
                    MapNames.Fungle => Options.DisableFungleBinoculars.GetBool(),
                    _ => false,
                };
                return !camerasDisabled;
            }
            return true;
        }
        public static void Postfix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
        {
            var newReader = MessageReader.Get(msgReader);
            var amount = newReader.ReadByte();
        }
    }
}