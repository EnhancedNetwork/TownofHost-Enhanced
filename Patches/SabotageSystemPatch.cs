using HarmonyLib;
using Hazel;
using System.Linq;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;

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
            if ((MapNames)Main.NormalOptions.MapId is MapNames.Airship) return;

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
            if ((MapNames)Main.NormalOptions.MapId is not MapNames.Airship) return;

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
            if ((MapNames)Main.NormalOptions.MapId is MapNames.Polus or MapNames.Airship or MapNames.Fungle) return;

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

            foreach (var pc in Main.AllAlivePlayerControls.Where(player => !player.Is(CustomRoleTypes.Impostor) && Main.ResetCamPlayerList.Contains(player.PlayerId)).ToArray())
            {
                // Need for hiding player names if player is desync Impostor
                Utils.NotifyRoles(SpecifySeer: pc, ForceLoop: true, MushroomMixupIsActive: true);
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
            if ((MapNames)Main.NormalOptions.MapId is not MapNames.Fungle) return;

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
            // if Mushroom Mixup Sabotage is end
            if (__instance.IsActive != __state && !Main.MeetingIsStarted)
            {
                Logger.Info($" IsEnd", "MushroomMixupSabotageSystem.Deteriorate.Postfix");

                _ = new LateTask(() =>
                {
                    // After MushroomMixup sabotage, shapeshift cooldown sets to 0
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        // Reset Ability Cooldown To Default For Alive Players
                        pc.RpcResetAbilityCooldown();
                    }
                }, 1.2f, "Reset Ability Cooldown Arter Mushroom Mixup");

                foreach (var pc in Main.AllAlivePlayerControls.Where(player => !player.Is(CustomRoleTypes.Impostor) && Main.ResetCamPlayerList.Contains(player.PlayerId)).ToArray())
                {
                    // Need for display player names if player is desync Impostor
                    Utils.NotifyRoles(SpecifySeer: pc, ForceLoop: true);
                }
            }
        }
    }
    [HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.UpdateSystem))]
    private static class SwitchSystemRepairDamagePatch
    {
        private static bool Prefix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
        {
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
            byte systemtype, amount;
            {
                var newReader = MessageReader.Get(msgReader);
                systemtype = newReader.ReadByte();
                amount = newReader.ReadByte();
                newReader.Recycle();
            }
            var nextSabotage = (SystemTypes)systemtype;

            if (Options.DisableSabotage.GetBool())
            {
                return false;
            }

            Logger.Info("Sabotage" + ", PlayerName: " + player.GetNameWithRole() + ", SabotageType: " + nextSabotage.ToString() + ", amount: " + amount.ToString(), "SabotageSystemType");
            if (EAC.SabotageSystemCheck(player, nextSabotage, amount))
            {
                Logger.Info("EAC action patch Sabotage", "SabotageSystemType.UpdateSystem");
                return false;
            }

            return CanSabotage(player, nextSabotage);
        }
        private static bool CanSabotage(PlayerControl player, SystemTypes systemType)
        {
            var playerRole = player.GetCustomRole();

            if (player.Is(CustomRoles.Minimalism)) return false;

            if (systemType is SystemTypes.Comms)
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

        public static void Postfix(SabotageSystemType __instance, bool __runOriginal)
        {
            // __runOriginal - the result that was returned from Prefix
            if (!AmongUsClient.Instance.AmHost || !(isCooldownModificationEnabled && __runOriginal))
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
                    var camerasDisabled = (MapNames)Main.NormalOptions.MapId switch
                    {
                        MapNames.Skeld => Options.DisableSkeldCamera.GetBool(),
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
}