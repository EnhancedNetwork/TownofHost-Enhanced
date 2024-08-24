using AmongUs.GameOptions;
using Hazel;
using System;
using UnityEngine;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Core.AssignManager;
using static TOHE.Translator;

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

                EAC.LogAllRoles();
                Utils.SyncAllSettings();
                return;
            }

            Logger.Msg("Is Started", "AssignRoles");

            //Initialization of CustomRpcSender and RpcSetRoleReplacer
            RpcSetRoleReplacer.StartReplace();

            //Not in use rn, but is gonna make it able to have neutral players of the same team spawn together
            //Important to remember that all team players need to have all teamplayers in their lists
            //gonna make a seperate thing making so that it's a rolebasething in another PR.
            //Dictionary<PlayerControl, List<PlayerControl>> DesyncImpTeammates = [];

            //foreach (var blotnik in Main.AllPlayerControls)
            //{
            //    RoleAssign.RoleResult[blotnik].GetStaticRoleClass().SetDesyncImpostorBuddies(ref DesyncImpTeammates, blotnik);
            //}

            CreateRoleMap();

            // Set RoleType by "RpcSetRole"
            RpcSetRoleReplacer.Release();

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
            Logger.Error(ex.ToString(), "SetRolesAfterSelect");
        }
    }
    private static void AssignCustomRole(CustomRoles role, PlayerControl player)
    {
        if (player == null) return;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);
        Logger.Info($"Registered Role： {player?.Data?.PlayerName} => {role}", "AssignRoles");
    }
    private static void CreateRoleMap()
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            var isModded = seer.OwnedByHost() || seer.IsModClient();
            var seerRole = RoleAssign.RoleResult[seer];
            foreach (var target in Main.AllPlayerControls)
            {
                RoleTypes targetRoleType;
                var isSelf = seer.PlayerId == target.PlayerId;
                var targetRole = RoleAssign.RoleResult[target];
                if (targetRole.IsDesyncRole())
                {
                    if (isSelf)
                    {
                        if (isModded)
                        {
                            if (targetRole.GetDYRole() == RoleTypes.Shapeshifter)
                                targetRoleType = RoleTypes.Shapeshifter;
                            else
                                targetRoleType = RoleTypes.Crewmate;
                        }
                        else
                            targetRoleType = RoleTypes.Impostor;
                    }
                    else
                    {
                        targetRoleType = RoleTypes.Scientist;
                    }
                }
                else
                {
                    if (seerRole.IsDesyncRole() && !isModded)
                    {
                        if (target.GetCustomRole() is CustomRoles.Noisemaker or CustomRoles.NoisemakerTOHE)
                        {
                            targetRoleType = RoleTypes.Noisemaker;
                        }
                        else
                        {
                            targetRoleType = RoleTypes.Scientist;
                        }
                    }
                    else
                    {
                        targetRoleType = targetRole.GetRoleTypes();
                    }
                }
                RpcSetRoleReplacer.RoleMap[(seer, target)] = (targetRoleType, targetRole);
            }
        }

        foreach (var seer1 in Main.AllPlayerControls)
        {
            foreach (var target1 in Main.AllPlayerControls)
            {
                RpcSetRoleReplacer.RoleMap.TryGetValue((seer1, target1), out var map);
                Logger.Info($"seer {seer1?.Data?.PlayerName}-{seer1.PlayerId}, target {target1?.Data?.PlayerName}-{target1.PlayerId} => {map.roleType}, {map.customRole}", "Role Map");
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    public static class RpcSetRoleReplacer
    {
        public static bool BlockSetRole = false;
        public static Dictionary<byte, CustomRpcSender> Senders = [];
        public static Dictionary<(PlayerControl seer, PlayerControl target), (RoleTypes roleType, CustomRoles customRole)> RoleMap = [];
        public static void Initialize()
        {
            Senders = [];
            RoleMap = [];
            BlockSetRole = true;
        }
        public static bool Prefix()
        {
            return !BlockSetRole;
        }
        public static void StartReplace()
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                Senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
        }
        public static void Release()
        {
            foreach (var ((seer, target), (roleType, _)) in RoleMap)
            {
                if (seer == target) continue;

                if (seer.OwnedByHost())
                {
                    target.SetRole(roleType);
                    continue;
                }

                var sender = Senders[seer.PlayerId];
                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetRole, seer.GetClientId())
                    .Write((ushort)roleType)
                    .Write(true)
                    .EndRpc();
            }
            SetSelfRoles();

            BlockSetRole = false;
            Senders.Do(kvp => kvp.Value.SendMessage());
            EndReplace();
        }

        //Self roles set seperately so that we can trick the game into intro-cutsene via disconnecting everyone temporarily for client.
        private static void SetSelfRoles()
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                var roleType = RoleMap[(pc, pc)].roleType;

                var stream = MessageWriter.Get(SendOption.Reliable);
                stream.StartMessage(6);
                stream.Write(AmongUsClient.Instance.GameId);
                stream.WritePacked(pc.GetClientId());
                {
                    RpcSetDisconnect(stream, true);

                    //if (pc.OwnedByHost())
                    //{
                    //    pc.SetRole(roleType);
                    //}

                    stream.StartMessage(2);
                    stream.WritePacked(pc.NetId);
                    stream.Write((byte)RpcCalls.SetRole);
                    {
                        stream.Write((ushort)roleType);
                        stream.Write(true); //canOverrideRole
                    }
                    stream.EndMessage();
                    Logger.Info($"SetSelfRole to:{pc?.name}({pc.GetClientId()}) player:{pc?.name}({roleType})", "★RpcSetRole");

                    RpcSetDisconnect(stream, false);
                }
                stream.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(stream);
                stream.Recycle();
            }
        }
        private static void RpcSetDisconnect(MessageWriter stream, bool disconnected)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.Disconnected = disconnected;

                stream.StartMessage(1);
                stream.WritePacked(pc.Data.NetId);
                pc.Data.Serialize(stream, false);
                stream.EndMessage();
            }
        }
        private static void EndReplace()
        {
            Senders = null;
        }
    }
}
