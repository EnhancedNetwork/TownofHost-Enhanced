using AmongUs.GameOptions;
using Hazel;
using System;
using UnityEngine;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using static TOHE.Roles.Core.AssignManager.RoleAssign;
using TOHE.Roles.Core;
using TOHE.Roles.Core.AssignManager;
using static TOHE.Translator;
using System.Linq;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
internal class ChangeRoleSettings
{
    public static void Postfix(AmongUsClient __instance)
    {
        if (AmongUsClient.Instance.AmHost)
            SetUpRoleTextPatch.IsInIntro = true;

        Main.OverrideWelcomeMsg = "";

        Logger.Msg("Is Started", "Initialization");

        try
        {
            // Note: No positions are set at this time.
            if (GameStates.IsNormalGame)
            {
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
                if (Options.DisableVanillaRoles.GetBool())
                {
                    Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
                    Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                    Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
                    Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
                    Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
                    Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
                }
            }
            else if (GameStates.IsHideNSeek)
            {
                Main.HideNSeekOptions.NumImpostors = Options.NumImpostorsHnS.GetInt();
                Main.AliveImpostorCount = Main.HideNSeekOptions.NumImpostors;
            }

            Main.PlayerStates = [];

            KillTimerManager.Initializate();
            Main.AllPlayerKillCooldown.Clear();
            Main.AllPlayerSpeed.Clear();
            Main.AllPlayerCustomRoles.Clear();
            Main.TasklessCrewmate.Clear();
            Main.UnreportableBodies.Clear();

            Main.LastEnteredVent.Clear();
            Main.LastEnteredVentLocation.Clear();

            Main.DesyncPlayerList.Clear();
            Main.PlayersDiedInMeeting.Clear();
            GuessManager.GuesserGuessed.Clear();
            Main.AfterMeetingDeathPlayers.Clear();
            Main.clientIdList.Clear();

            PlayerControlSetRolePatch.DidSetGhost.Clear();

            Main.CheckShapeshift.Clear();
            Main.ShapeshiftTarget.Clear();
            Main.AllKillers.Clear();
            Main.OverDeadPlayerList.Clear();
            Main.UnShapeShifter.Clear();
            Main.OvverideOutfit.Clear();
            Main.GameIsLoaded = false;
            Utils.LateExileTask.Clear();

            Main.LastNotifyNames.Clear();
            Main.PlayerColors.Clear();

            Main.FirstDiedPrevious = Options.ShieldPersonDiedFirst.GetBool() ? Main.FirstDied : "";
            Main.FirstDied = "";
            Main.MadmateNum = 0;
            Main.BardCreations = 0;
            Main.MeetingsPassed = 0;
            Main.MeetingIsStarted = false;
            Main.introDestroyed = false;
            GameEndCheckerForNormal.ShouldNotCheck = false;
            GameEndCheckerForNormal.ForEndGame = false;
            GameEndCheckerForNormal.ShowAllRolesWhenGameEnd = false;
            GameStartManagerPatch.GameStartManagerUpdatePatch.AlredyBegin = false;

            ChatManager.ResetHistory();
            ReportDeadBodyPatch.CanReport = [];
            Options.UsedButtonCount = 0;

            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            if (GameStates.IsNormalGame)
            {
                GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor = false;
                GameOptionsManager.Instance.currentNormalGameOptions.SetBool(BoolOptionNames.ConfirmImpostor, false);

                MeetingTimeManager.Init();

                Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
                Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);
            }

            // Clear last exiled
            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = null;

            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            Main.DoBlockNameChange = true;

            // Sync Player Names
            RPC.SyncAllPlayerNames();

            GhostRoleAssign.Init();

            Camouflage.Init();

            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Any())
            {
                var msg = GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += " " + string.Join(", ", invalidColor.Select(p => $"{p.Data.PlayerName}"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }

            foreach (var pc in Main.AllPlayerControls)
            {
                var outfit = pc.Data.DefaultOutfit;
                var colorId = pc.Data.DefaultOutfit.ColorId;
                var currentName = "";
                if (AmongUsClient.Instance.AmHost)
                {
                    if (Options.FormatNameMode.GetInt() == 1)
                    {
                        var coloredName = Palette.GetColorName(colorId);
                        currentName = coloredName;
                        pc.RpcSetName(coloredName);
                    }
                    else
                    {
                        string realName = Main.AllPlayerNames.TryGetValue(pc.PlayerId, out var name) ? name : "";
                        //Logger.Info($"player id: {pc.PlayerId} {realName}", "FinallyBegin");
                        if (realName == "") continue;

                        currentName = realName;
                        pc.RpcSetName(realName);
                    }
                }

                Main.PlayerStates[pc.PlayerId] = new(pc.PlayerId)
                {
                    NormalOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set(currentName, pc.CurrentOutfit.ColorId, pc.CurrentOutfit.HatId, pc.CurrentOutfit.SkinId, pc.CurrentOutfit.VisorId, pc.CurrentOutfit.PetId, pc.CurrentOutfit.NamePlateId),
                };

                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];

                if (GameStates.IsNormalGame)
                    Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);

                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = [];
                pc.cosmetics.nameText.text = pc.name;

                Camouflage.PlayerSkins[pc.PlayerId] = new NetworkedPlayerInfo.PlayerOutfit().Set(currentName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId, outfit.NamePlateId);
                Main.clientIdList.Add(pc.GetClientId());
            }

            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                Main.RefixCooldownDelay = 0;
            }

            // Initialize all custom roles
            foreach (var role in EnumHelper.GetAllValues<CustomRoles>().Where(role => role < CustomRoles.NotAssigned).ToArray())
            {
                var RoleClass = CustomRoleManager.GetStaticRoleClass(role);
                RoleClass?.OnInit();
            }

            LastImpostor.Init();
            TargetArrow.Init();
            LocateArrow.Init();
            DoubleTrigger.Init();
            Workhorse.Init();
            Diseased.Init();
            Clumsy.Init();
            Aware.Init();
            Radar.Init();
            Glow.Init();
            Sleuth.Init();
            Bait.Init();
            Antidote.Init();
            Fool.Init();
            Burst.Init();
            DoubleShot.Init();
            Lucky.Init();
            Bewilder.Init();
            //ChiefOfPolice.Init();
            Cyber.Init();
            Oiiai.Init();
            Tired.Init();
            Statue.Init();
            Ghoul.Init();
            Rainbow.Init();
            Rebirth.Init();

            //FFA
            FFAManager.Init();

            FallFromLadder.Reset();
            CustomRoleManager.Initialize();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            NameNotifyManager.Reset();

            SabotageSystemPatch.SabotageSystemTypeRepairDamagePatch.Initialize();
            DoorsReset.Initialize();

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
            EAC.ReportTimes = [];

            SetEverythingUpPatch.LastWinsText = "";
            SetEverythingUpPatch.LastWinsReason = "";

            Logger.Msg("End", "Initialization");
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Change Role Setting Postfix");
            Utils.ThrowException(ex);
        }
    }
}
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
internal class SelectRolesPatch
{
    private static RoleOptionsCollectionV08 RoleOpt => Main.NormalOptions.roleOptions;
    private static Dictionary<RoleTypes, int> RoleTypeNums = [];
    public static void UpdateRoleTypeNums()
    {
        RoleTypeNums = new()
        {
            { RoleTypes.Scientist, RoleAssign.AddScientistNum },
            { RoleTypes.Engineer, RoleAssign.AddEngineerNum },
            { RoleTypes.Shapeshifter, RoleAssign.AddShapeshifterNum },
            { RoleTypes.Noisemaker, RoleAssign.AddNoisemakerNum },
            { RoleTypes.Phantom, RoleAssign.AddPhantomNum },
            { RoleTypes.Tracker, RoleAssign.AddTrackerNum }
        };
    }
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsHideNSeek)
        {
            if (Main.EnableGM.Value)
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate, true);
                PlayerControl.LocalPlayer.Data.IsDead = true;
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            EAC.OriginalRoles = [];
            return;
        }

        try
        {
            // Block "RpcSetRole" for set desync roles for some players
            RpcSetRoleReplacer.Initialize();

            // Set GM for Host
            if (Main.EnableGM.Value && Options.CurrentGameMode == CustomGameMode.Standard)
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate, true);
                PlayerControl.LocalPlayer.Data.IsDead = true;
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            // Select custom roles / add-ons
            EAC.OriginalRoles = [];
            RoleAssign.StartSelect();
            AddonAssign.StartSelect();

            // Set count vanilla roles
            RoleAssign.CalculateVanillaRoleCount();

            UpdateRoleTypeNums();
            // Set Rate For Vanilla Roles
            foreach (var roleType in RoleTypeNums)
            {
                int roleNum = Options.DisableVanillaRoles.GetBool() ? 0 : RoleOpt.GetNumPerGame(roleType.Key);
                roleNum += roleType.Value;
                RoleOpt.SetRoleRate(roleType.Key, roleNum, roleType.Value > 0 ? 100 : RoleOpt.GetChancePerGame(roleType.Key));
            }
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Select Role Prefix");
            Utils.ThrowException(ex);
        }
    }
    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        //There is a delay of 0.8 seconds because after the player exits during the assign of desync roles, either a black screen will occur or the Scientist role will be set
        _ = new LateTask(() => {

            try
            {
                // Set roles
                SetRolesAfterSelect();

                // Assign tasks again
                ShipStatus.Instance.Begin();
            }
            catch (Exception ex)
            {
                Utils.ErrorEnd("Set Roles After Select In LateTask");
                Utils.ThrowException(ex);
            }
        }, 1f, "Set Role Types After Select");
    }
    private static void SetRolesAfterSelect()
    {
        try
        {
            if (GameStates.IsHideNSeek)
            {
                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in Main.AllPlayerControls)
                {
                    GameOptionsSender.AllSenders.Add(
                        new PlayerGameOptionsSender(pc)
                    );
                }

                //Utils.CountAlivePlayers(true);

                EAC.LogAllRoles();
                Utils.SyncAllSettings();

                return;
            }

            Logger.Msg("Is Started", "AssignRoles");
            //Main.AssignRolesIsStarted = true;

            //Initialization of CustomRpcSender and RpcSetRoleReplacer
            Dictionary<byte, CustomRpcSender> senders = [];
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            Dictionary<(byte, byte), RoleTypes> rolesMap = [];
            // Assign desync roles
            foreach (var (pc, role) in RoleAssign.RoleResult.Where(x => x.Value.IsDesyncRole()))
                AssignDesyncRole(role, pc, senders, rolesMap, BaseRole: role.GetDYRole());

            // Set Desync RoleType by "RpcSetRole"
            //MakeDesyncSender(senders, rolesMap);

            RpcSetRoleReplacer.DesyncPlayers.Clear();
            // Override RoleType for others players
            foreach (var (pc, role) in RoleAssign.RoleResult)
            {
                if (pc == null) continue;
                var realrole = role;
                if (role.IsDesyncRole())
                {
                    realrole = CustomRoles.Crewmate;
                    RpcSetRoleReplacer.DesyncPlayers.Add(pc, role);
                }

                RpcSetRoleReplacer.StoragedData.Add(pc, realrole.GetRoleTypes());

                Logger.Warn($"Set original role type => {pc.GetRealName()}: {role} => {role.GetRoleTypes()}", "Override Role Select");
            }

            // Set RoleType by "RpcSetRole"
            RpcSetRoleReplacer.Release(senders, rolesMap); //Write RpcSetRole for all players
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            // Delete unwanted objects
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Main.AssignRolesIsStarted = false;
            //Utils.ApplySuffix();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false;
                if (Main.PlayerStates[pc.PlayerId].MainRole != CustomRoles.NotAssigned) continue; // Skip if a custom role has already been assigned
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.GuardianAngel:
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        role = CustomRoles.Shapeshifter;
                        break;
                    case RoleTypes.Noisemaker:
                        role = CustomRoles.Noisemaker;
                        break;
                    case RoleTypes.Phantom:
                        role = CustomRoles.Phantom;
                        break;
                    case RoleTypes.Tracker:
                        role = CustomRoles.Tracker;
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                Main.PlayerStates[pc.PlayerId].SetMainRole(role);
            }

            if (Options.CurrentGameMode == CustomGameMode.FFA)
            {
                foreach (var pair in Main.PlayerStates)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }
                goto EndOfSelectRolePatch;
            }

            var rd = IRandom.Instance;

            foreach (var kv in RoleAssign.RoleResult)
            {
                if (kv.Value.IsDesyncRole()) continue;

                AssignCustomRole(kv.Value, kv.Key);
            }

            try
            {
                AddonAssign.InitAndStartAssignLovers();
                AddonAssign.StartSortAndAssign();
            }
            catch (Exception error)
            {
                Logger.Warn($"Error after addons assign - error: {error}", "AddonAssign");
            }

            // Sync for non-host modded clients by RPC
            foreach (var pair in Main.PlayerStates)
            {
                // Set roles
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                // Set add-ons
                foreach (var subRole in pair.Value.SubRoles.ToArray())
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
            }



            GhostRoleAssign.Add();

            foreach (var pc in Main.AllPlayerControls)
            {
                if (Utils.IsMethodOverridden(pc.GetRoleClass(), "UnShapeShiftButton"))
                {
                    Main.UnShapeShifter.Add(pc.PlayerId);
                    Logger.Info($"Added {pc.GetRealName()} because of {pc.GetCustomRole()}", "UnShapeShift..OnGameStartedPatch");
                }

                var roleClass = pc.GetRoleClass();

                roleClass?.OnAdd(pc.PlayerId);

                // if based role is Shapeshifter
                if (roleClass?.ThisRoleBase.GetRoleTypes() == RoleTypes.Shapeshifter)
                {
                    // Is Desync Shapeshifter
                    if (pc.HasDesyncRole())
                    {
                        foreach (var target in Main.AllPlayerControls)
                        {
                            // Set all players as killable players
                            target.Data.Role.CanBeKilled = true;

                            // When target is impostor, set name color as white
                            target.cosmetics.SetNameColor(Color.white);
                            target.Data.Role.NameColor = Color.white;
                        }
                    }
                    Main.CheckShapeshift.Add(pc.PlayerId, false);
                }

                foreach (var subRole in pc.GetCustomSubRoles().ToArray())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Aware:
                            Aware.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Radar:
                            Radar.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Glow:
                            Glow.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Oiiai:
                            Oiiai.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Tired:
                            Tired.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Rainbow:
                            Rainbow.Add();
                            break;
                        case CustomRoles.Statue:
                            Statue.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Ghoul:
                            Ghoul.Add();
                            break;
                        case CustomRoles.Diseased:
                            Diseased.Add();
                            break;
                        case CustomRoles.Antidote:
                            Antidote.Add();
                            break;
                        case CustomRoles.Burst:
                            Burst.Add();
                            break;
                        case CustomRoles.Bewilder:
                            Bewilder.Add();
                            break;
                        case CustomRoles.Lucky:
                            Lucky.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Clumsy:
                            Clumsy.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Fool:
                            Fool.Add();
                            break;
                        case CustomRoles.Bloodthirst:
                            Bloodthirst.Add();
                            break;
                        case CustomRoles.Rebirth:
                            Rebirth.Add(pc.PlayerId);
                            break;
                        default:
                            break;
                    }
                }
            }

            Spurt.Add();

        EndOfSelectRolePatch:

            try
            {
                if (!AmongUsClient.Instance.IsGameOver)
                    DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
            }
            catch { }
            //HudManager.Instance.Chat.SetVisible(true);

            foreach (var pc in Main.AllPlayerControls)
                pc.ResetKillCooldown();

            // Role types
            foreach (var roleType in RoleTypeNums)
            {
                int roleNum = Options.DisableVanillaRoles.GetBool() ? 0 : RoleOpt.GetNumPerGame(roleType.Key);
                roleNum -= roleType.Value;
                RoleOpt.SetRoleRate(roleType.Key, roleNum, RoleOpt.GetChancePerGame(roleType.Key));
            }

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard:
                    GameEndCheckerForNormal.SetPredicateToNormal();
                    break;
                case CustomGameMode.FFA:
                    GameEndCheckerForNormal.SetPredicateToFFA();
                    break;
            }

            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }

            EAC.LogAllRoles();

            Utils.CountAlivePlayers(sendLog: true, checkGameEnd: false);
            Utils.SyncAllSettings();

            Logger.Msg("Ended", "AssignRoles");
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Set Roles After Select");
            Utils.ThrowException(ex);
        }
    }
    private static void AssignDesyncRole(CustomRoles role, PlayerControl player, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        if (player == null) return;

        var hostId = PlayerControl.LocalPlayer.PlayerId;
        var isHost = player.PlayerId == hostId;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);

        var selfRole = isHost ? hostBaseRole : BaseRole;
        var othersRole = isHost ? RoleTypes.Crewmate : RoleTypes.Scientist;

        // Set Desync role for self and for others
        foreach (var target in Main.AllPlayerControls)
            rolesMap[(player.PlayerId, target.PlayerId)] = player.PlayerId != target.PlayerId ? othersRole : selfRole;

        // Set Desync role for others
        foreach (var seer in Main.AllPlayerControls.Where(x => player.PlayerId != x.PlayerId).ToArray())
            rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;

        RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);

        //Set role for host
        player.SetRole(othersRole, true);

        // Override RoleType for host
        if (isHost && BaseRole == RoleTypes.Shapeshifter)
        {
            DestroyableSingleton<RoleManager>.Instance.SetRole(player, BaseRole);
            DestroyableSingleton<RoleBehaviour>.Instance.CanBeKilled = true;
        }
       // player.Data.IsDead = true;

        Logger.Info($"Registered Role: {player?.Data?.PlayerName} => {role} : RoleType for self => {selfRole}, for others => {othersRole}", "AssignDesyncRoles");
    }
    private static void MakeDesyncSender(PlayerControl target, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
    {
        foreach (var seer in Main.AllPlayerControls) // HOW {target} see other people
        {
            if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var roleType))
            {
                try
                {
                    // Change Scientist to Noisemaker when role is desync and target have Noisemaker role
                    if (roleType is RoleTypes.Scientist && RoleAssign.RoleResult.Any(x => x.Key.PlayerId == seer.PlayerId && x.Value is CustomRoles.NoisemakerTOHE or CustomRoles.Noisemaker))
                    {
                        Logger.Info($"seer: {seer.PlayerId}, target: {target.PlayerId}, {roleType} => {RoleTypes.Noisemaker}", "OverrideRoleForDesync");
                        roleType = RoleTypes.Noisemaker;
                    }


                    Logger.Info($"seer: {seer.GetCustomRole()} target: {target.GetCustomRole()} , Roletype {roleType}", "Desync Neutral Roles [FOR OTHERS]");
                    var sender = senders[target.PlayerId];
                    sender.RpcSetRole(seer, roleType, target.GetClientId());
                }
                catch
                { }
            }
            else
            {
                Logger.Fatal($"seer: {seer.GetRealName()}/ target: {target.GetCustomRole()} Was desync but cannot get out Rolesmap group", "OnGameStartedPatch.MakeDesyncSender");
            }
        }
        foreach (var VTAR in Main.AllPlayerControls) // HOW other people see {target}
        {
            if (rolesMap.TryGetValue((target.PlayerId, VTAR.PlayerId), out var roleType))
            {
                try
                {
                    // Change Scientist to Noisemaker when role is desync and target have Noisemaker role
                    if (roleType is RoleTypes.Scientist && RoleAssign.RoleResult.Any(x => x.Key.PlayerId == VTAR.PlayerId && x.Value is CustomRoles.NoisemakerTOHE or CustomRoles.Noisemaker))
                    {
                        Logger.Info($"seer: {target.PlayerId}, target: {VTAR.PlayerId}, {roleType} => {RoleTypes.Noisemaker}", "OverrideRoleForDesync");
                        roleType = RoleTypes.Noisemaker;
                    }


                    Logger.Info($"seer: {target.GetCustomRole()} target: {VTAR.GetCustomRole()} , Roletype {roleType}", "Desync Neutral Roles [FOR SELF]");
                    var sender = senders[VTAR.PlayerId];
                    sender.RpcSetRole(target, roleType, VTAR.GetClientId());
                }
                catch
                { }
            }
            else
            {
                Logger.Fatal($" seer: {target.GetRealName()}/ target: {VTAR.GetCustomRole()} Was desync but cannot get out Rolesmap group", "OnGameStartedPatch.MakeDesyncSender");
            }
        }
    }
    
    private static void AssignCustomRole(CustomRoles role, PlayerControl player)
    {
        if (player == null) return;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);
        Logger.Info($"Registered Role： {player?.Data?.PlayerName} => {role}", "AssignRoles");
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    public static class RpcSetRoleReplacer
    {
        public static bool doReplace = false;
        public static Dictionary<byte, CustomRpcSender> senders;
        public static Dictionary<PlayerControl, RoleTypes> StoragedData = [];
        public static Dictionary<PlayerControl, CustomRoles> DesyncPlayers = [];
        // List of Senders that do not require additional writing because SetRoleRpc has already been written by another process such as Position Desync
        public static List<CustomRpcSender> OverriddenSenderList;
        public static bool Prefix()
        {
            return !doReplace;
        }
        public static void Release(Dictionary<byte, CustomRpcSender> senderDY, Dictionary<(byte, byte), RoleTypes> RolesMapDY)
        {
            foreach (var sender in senders)
            {
                //if (OverriddenSenderList.Contains(sender.Value)) continue;
                if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                    throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                foreach (var (seer, roleType) in StoragedData)
                {
                    try
                    {
                        SetDisconnectedMessage(sender.Value.stream, true);

                        if (!DesyncPlayers.TryGetValue(seer, out var role) || role.IsCrewmate())
                        {
                            seer.SetRole(roleType, true);
                            sender.Value.AutoStartRpc(seer.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                                .Write((ushort)roleType)
                                .Write(true)
                                .EndRpc();
                        }
                        else
                        {
                            MakeDesyncSender(seer, senderDY, RolesMapDY);
                        }

                        SetDisconnectedMessage(sender.Value.stream, false);
                    }
                    catch
                    { }
                }
                sender.Value.EndMessage();
            }
            doReplace = false;
        }
        private static void SetDisconnectedMessage(MessageWriter stream, bool disconnected)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                //if (pc.PlayerId != target.PlayerId) continue;
                pc.Data.Disconnected = disconnected;

                stream.StartMessage(1);
                stream.WritePacked(pc.Data.NetId);
                pc.Data.Serialize(stream, false);
                stream.EndMessage();
            }
        }
        public static void Initialize()
        {
            StoragedData = [];
            OverriddenSenderList = [];
            doReplace = true;
        }
        public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
        {
            RpcSetRoleReplacer.senders = senders;
            doReplace = true;
        }
    }
}
