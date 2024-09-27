using Hazel;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE;

//参考
//https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/MeltDownBoost.cs

public class SabotageSystemPatch
{
    private static bool SetDurationForReactorSabotage = true;
    private static bool SetDurationForO2Sabotage = true;
    private static bool SetDurationMushroomMixupSabotage = true;

    [HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.Deteriorate))]
    private static class ReactorSystemTypePatch
    {
        private static void Prefix(ReactorSystemType __instance)
        {
            if (!Options.SabotageTimeControl.GetBool()) return;
            if (GameStates.AirshipIsActive) return;

            // If Reactor sabotage is end
            if (!__instance.IsActive || !SetDurationForReactorSabotage)
            {
                if (!SetDurationForReactorSabotage && !__instance.IsActive)
                {
                    SetDurationForReactorSabotage = true;
                }
                return;
            }

            Logger.Info($" {ShipStatus.Instance.Type}", "ReactorSystemTypePatch - ShipStatus.Instance.Type");
            Logger.Info($" {SetDurationForReactorSabotage}", "ReactorSystemTypePatch - SetDurationCriticalSabotage");
            SetDurationForReactorSabotage = false;

            // Set time limit reactor (The Skeld/Mira HQ/Polus/The Fungle)
            switch (ShipStatus.Instance.Type)
            {
                case ShipStatus.MapType.Ship: //The Skeld
                    __instance.Countdown = Options.SkeldReactorTimeLimit.GetFloat();
                    return;
                case ShipStatus.MapType.Hq: //Mira HQ
                    __instance.Countdown = Options.MiraReactorTimeLimit.GetFloat();
                    return;
                case ShipStatus.MapType.Pb: //Polus
                    __instance.Countdown = Options.PolusReactorTimeLimit.GetFloat();
                    return;
                case ShipStatus.MapType.Fungle: //The Fungle
                    __instance.Countdown = Options.FungleReactorTimeLimit.GetFloat();
                    return;
                default:
                    return;
            }
        }
    }
    [HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.Deteriorate))]
    private static class HeliSabotageSystemPatch
    {
        private static void Prefix(HeliSabotageSystem __instance)
        {
            if (!Options.SabotageTimeControl.GetBool()) return;
            if (!GameStates.AirshipIsActive) return;

            // If Reactor sabotage is end (Airship)
            if (!__instance.IsActive || ShipStatus.Instance == null || !SetDurationForReactorSabotage)
            {
                if (!SetDurationForReactorSabotage && !__instance.IsActive)
                {
                    SetDurationForReactorSabotage = true;
                }
                return;
            }

            Logger.Info($" {ShipStatus.Instance.Type}", "HeliSabotageSystemPatch - ShipStatus.Instance.Type");
            Logger.Info($" {SetDurationForReactorSabotage}", "HeliSabotageSystemPatch - SetDurationCriticalSabotage");
            SetDurationForReactorSabotage = false;

            // Set time limit reactor (The Airship)
            __instance.Countdown = Options.AirshipReactorTimeLimit.GetFloat();
        }
    }
    [HarmonyPatch(typeof(LifeSuppSystemType), nameof(LifeSuppSystemType.Deteriorate))]
    private static class LifeSuppSystemTypePatch
    {
        private static void Prefix(LifeSuppSystemType __instance)
        {
            if (!Options.SabotageTimeControl.GetBool()) return;
            if (Utils.GetActiveMapName() is MapNames.Polus or MapNames.Airship or MapNames.Fungle) return;

            // If O2 sabotage is end
            if (!__instance.IsActive || !SetDurationForO2Sabotage)
            {
                if (!SetDurationForO2Sabotage && !__instance.IsActive)
                {
                    SetDurationForO2Sabotage = true;
                }
                return;
            }

            Logger.Info($" {ShipStatus.Instance.Type}", "LifeSuppSystemType - ShipStatus.Instance.Type");
            Logger.Info($" {SetDurationForO2Sabotage}", "LifeSuppSystemType - SetDurationCriticalSabotage");
            SetDurationForO2Sabotage = false;

            // Set time limit reactor (The Skeld/Mira HQ)
            switch (ShipStatus.Instance.Type)
            {
                case ShipStatus.MapType.Ship: // The Skeld
                    __instance.Countdown = Options.SkeldO2TimeLimit.GetFloat();
                    return;
                case ShipStatus.MapType.Hq: // Mira HQ
                    __instance.Countdown = Options.MiraO2TimeLimit.GetFloat();
                    return;
                default:
                    return;
            }
        }
    }
    [HarmonyPatch(typeof(MushroomMixupSabotageSystem), nameof(MushroomMixupSabotageSystem.UpdateSystem))]
    public static class MushroomMixupSabotageSystemUpdateSystemPatch
    {
        public static void Postfix()
        {
            Logger.Info($" IsActive", "MushroomMixupSabotageSystem.UpdateSystem.Postfix");

            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (!pc.Is(Custom_Team.Impostor) && pc.HasDesyncRole())
                {
                    // Need for hiding player names if player is desync Impostor
                    Utils.NotifyRoles(SpecifySeer: pc, ForceLoop: true, MushroomMixupIsActive: true);
                }
            }
        }
    }
    [HarmonyPatch(typeof(MushroomMixupSabotageSystem), nameof(MushroomMixupSabotageSystem.Deteriorate))]
    private static class MushroomMixupSabotageSystemPatch
    {
        private static void Prefix(MushroomMixupSabotageSystem __instance, ref bool __state)
        {
            __state = __instance.IsActive;

            if (!Options.SabotageTimeControl.GetBool()) return;
            if (!GameStates.FungleIsActive) return;

            // If Mushroom Mixup sabotage is end
            if (!__instance.IsActive || !SetDurationMushroomMixupSabotage)
            {
                if (!SetDurationMushroomMixupSabotage && !__instance.IsActive)
                {
                    SetDurationMushroomMixupSabotage = true;
                }
                return;
            }

            Logger.Info($" {ShipStatus.Instance.Type}", "MushroomMixupSabotageSystem - ShipStatus.Instance.Type");
            Logger.Info($" {SetDurationMushroomMixupSabotage}", "MushroomMixupSabotageSystem - SetDurationCriticalSabotage");
            SetDurationMushroomMixupSabotage = false;

            // Set duration Mushroom Mixup (The Fungle)
            __instance.currentSecondsUntilHeal = Options.FungleMushroomMixupDuration.GetFloat();
        }
        public static void Postfix(MushroomMixupSabotageSystem __instance, bool __state)
        {
            if (GameStates.IsHideNSeek) return;

            // if Mushroom Mixup Sabotage is end
            if (__instance.IsActive != __state && !Main.MeetingIsStarted)
            {
                Logger.Info($" IsEnd", "MushroomMixupSabotageSystem.Deteriorate.Postfix");

                if (AmongUsClient.Instance.AmHost)
                {
                    _ = new LateTask(() =>
                    {
                        // After MushroomMixup sabotage, shapeshift cooldown sets to 0
                        foreach (var pc in Main.AllAlivePlayerControls)
                        {
                            // Reset Ability Cooldown To Default For Alive Players
                            pc.RpcResetAbilityCooldown();
                        }
                    }, 1.2f, "Reset Ability Cooldown Arter Mushroom Mixup");

                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (!pc.Is(Custom_Team.Impostor) && pc.HasDesyncRole())
                        {
                            // Need for display player names if player is desync Impostor
                            Utils.NotifyRoles(SpecifySeer: pc, ForceLoop: true);
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.UpdateSystem))]
    private static class SwitchSystemUpdatePatch
    {
        private static bool Prefix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
        {
            if (GameStates.IsHideNSeek) return false;

            byte amount;
            {
                var newReader = MessageReader.Get(msgReader);
                amount = newReader.ReadByte();
                newReader.Recycle();
            }

            if (!AmongUsClient.Instance.AmHost)
            {
                return true;
            }

            // No matter if the blackout sabotage is sounded (beware of misdirection as it flies under the host's name)
            if (amount.HasBit(SwitchSystem.DamageSystem))
            {
                return true;
            }

            // Cancel if player can't fix a specific outage on Airship
            if (GameStates.AirshipIsActive)
            {
                var truePosition = player.GetCustomPosition();
                if (Options.DisableAirshipViewingDeckLightsPanel.GetBool() && Utils.GetDistance(truePosition, new(-12.93f, -11.28f)) <= 2f) return false;
                if (Options.DisableAirshipGapRoomLightsPanel.GetBool() && Utils.GetDistance(truePosition, new(13.92f, 6.43f)) <= 2f) return false;
                if (Options.DisableAirshipCargoLightsPanel.GetBool() && Utils.GetDistance(truePosition, new(30.56f, 2.12f)) <= 2f) return false;
            }

            if (Fool.IsEnable && player.Is(CustomRoles.Fool))
            {
                return false;
            }

            if (Options.BlockDisturbancesToSwitches.GetBool())
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
                Utils.NotifyRoles(ForceLoop: true);
        }
    }
    [HarmonyPatch(typeof(ElectricTask), nameof(ElectricTask.Complete))]
    public static class ElectricTaskCompletePatch
    {
        public static void Postfix()
        {
            Utils.MarkEveryoneDirtySettings();
            if (!GameStates.IsMeeting)
                Utils.NotifyRoles(ForceLoop: true);
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

        private static bool Prefix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
        {
            if (GameStates.IsHideNSeek) return false;

            byte amount;
            {
                var newReader = MessageReader.Get(msgReader);
                amount = newReader.ReadByte();
                newReader.Recycle();
            }
            var nextSabotage = (SystemTypes)amount;

            if (Options.DisableSabotage.GetBool())
            {
                return false;
            }

            Logger.Info($"PlayerName: {player.GetNameWithRole()}, SabotageType: {nextSabotage}, amount {amount}", "SabotageSystemType.UpdateSystem");

            return CanSabotage(player, nextSabotage);
        }
        private static bool CanSabotage(PlayerControl player, SystemTypes systemType)
        {
            if (systemType is SystemTypes.Comms)
            {
                if (Camouflager.CantPressCommsSabotageButton(player))
                    return false;
            }

            if (player.GetRoleClass() is Glitch gc)
            {
                gc.Mimic(player);
                return false;
            }

            return player.CanUseSabotage();
        }

        public static void Postfix(SabotageSystemType __instance, bool __runOriginal)
        {
            // __runOriginal - the result that was returned from Prefix
            if (!AmongUsClient.Instance.AmHost || GameStates.IsHideNSeek || !__runOriginal || !isCooldownModificationEnabled)
            {
                return;
            }

            // Set cooldown sabotages
            __instance.Timer = modifiedCooldownSec;
            __instance.IsDirty = true;
        }

        [HarmonyPatch(typeof(SecurityCameraSystemType), nameof(SecurityCameraSystemType.UpdateSystem))]
        private static class SecurityCameraSystemTypeUpdateSystemPatch
        {
            private static bool Prefix([HarmonyArgument(1)] MessageReader msgReader)
            {
                byte amount;
                {
                    var newReader = MessageReader.Get(msgReader);
                    amount = newReader.ReadByte();
                    newReader.Recycle();
                }

                // When the camera is disabled, the vanilla player opens the camera so it does not blink.
                if (amount == SecurityCameraSystemType.IncrementOp)
                {
                    var camerasDisabled = Utils.GetActiveMapName() switch
                    {
                        MapNames.Skeld or MapNames.Dleks => Options.DisableSkeldCamera.GetBool(),
                        MapNames.Polus => Options.DisablePolusCamera.GetBool(),
                        MapNames.Airship => Options.DisableAirshipCamera.GetBool(),
                        _ => false,
                    };
                    return !camerasDisabled;
                }
                return true;
            }
        }
    }
    [HarmonyPatch(typeof(DoorsSystemType), nameof(DoorsSystemType.UpdateSystem))]
    public static class DoorsSystemTypePatch
    {
        public static void Prefix(/*DoorsSystemType __instance,*/ PlayerControl player, MessageReader msgReader)
        {
            byte amount;
            {
                var newReader = MessageReader.Get(msgReader);
                amount = newReader.ReadByte();
                newReader.Recycle();
            }

            Logger.Info($"Door is opened by {player?.Data?.PlayerName}, amount: {amount}", "DoorsSystemType.UpdateSystem");
        }
    }
}