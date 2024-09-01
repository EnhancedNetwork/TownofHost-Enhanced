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
            RoleAssign.RoleResult = [];
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
            ShipStatusBeginPatch.RolesIsAssigned = false;

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

                RoleAssign.RoleResult[pc.PlayerId] = CustomRoles.NotAssigned;

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
            Evader.Init();

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

        //There is a delay of 1 seconds because after the player exits during the assign of desync roles, either a black screen will occur or the Scientist role will be set
        _ = new LateTask(() => {

            try
            {
                // Set roles
                SetRolesAfterSelect();
            }
            catch (Exception ex)
            {
                Utils.ErrorEnd("Set Roles After Select In LateTask");
                Utils.ThrowException(ex);
            }
        }, 1f, "Set Role Types After Select");

        // There is a delay of 3 seconds because after assign roles player data "Disconnected" does not allow assigning tasks due AU code side
        _ = new LateTask(() =>
        {
            try
            {
                ShipStatusBeginPatch.RolesIsAssigned = true;

                // Assign tasks
                ShipStatus.Instance.Begin();
            }
            catch (Exception ex)
            {
                Utils.ErrorEnd("Set Tasks In LateTask");
                Utils.ThrowException(ex);
            }
        }, 3f, "Set Tasks For All Players");
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

            RpcSetRoleReplacer.AssignDesyncRoles();
            RpcSetRoleReplacer.AssignNormalRoles();

            RpcSetRoleReplacer.Release();

            foreach (var pc in Main.AllPlayerControls)
            {
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

                AssignCustomRole(kv.Value, Utils.GetPlayerById(kv.Key));
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
                    Logger.Info($"Added {pc.GetRealName()} because of {pc.GetCustomRole()}", "UnShapeShift.OnGameStartedPatch");
                }

                var roleClass = pc.GetRoleClass();

                roleClass?.OnAdd(pc.PlayerId);

                // if based role is Shapeshifter
                if (roleClass?.ThisRoleBase.GetRoleTypes() == RoleTypes.Shapeshifter)
                {
                    // Is Desync Shapeshifter
                    if (pc.AmOwner && pc.HasDesyncRole())
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
                        case CustomRoles.Evader:
                            Evader.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Spurt:
                            Spurt.Add();
                            break;
                        default:
                            break;
                    }
                }
            }

        EndOfSelectRolePatch:

            try
            {
                if (!AmongUsClient.Instance.IsGameOver)
                    DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
            }
            catch { }

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
    public static void AssignDesyncRole(CustomRoles role, PlayerControl player, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), (RoleTypes, CustomRoles)> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        if (player == null) return;

        var hostId = PlayerControl.LocalPlayer.PlayerId;
        var isHost = player.PlayerId == hostId;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);

        var selfRole = isHost ? BaseRole == RoleTypes.Shapeshifter ? RoleTypes.Shapeshifter : hostBaseRole : BaseRole;
        var othersRole = isHost ? RoleTypes.Crewmate : RoleTypes.Scientist;

        // Set Desync role for self and for others
        foreach (var target in Main.AllPlayerControls)
        {
            var roleType = othersRole;

            if (RoleAssign.RoleResult[target.PlayerId].GetVNRole() is CustomRoles.Noisemaker)
                roleType = RoleTypes.Noisemaker;

            rolesMap[(player.PlayerId, target.PlayerId)] = player.PlayerId != target.PlayerId ? (roleType, RoleAssign.RoleResult[target.PlayerId]) : (selfRole, role);
        }

        // Set Desync role for others
        foreach (var seer in Main.AllPlayerControls.Where(x => player.PlayerId != x.PlayerId).ToArray())
            rolesMap[(seer.PlayerId, player.PlayerId)] = (othersRole, role);

        
        if (!isHost)
        {
            RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
            //Set role for host
            player.SetRole(othersRole);
        }

        Logger.Info($"Registered Role: {player?.Data?.PlayerName} => {role} : RoleType for self => {selfRole}, for others => {othersRole}", "AssignDesyncRoles");
    }
    public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), (RoleTypes, CustomRoles)> rolesMap)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            if (seer.OwnedByHost()) continue;

            foreach (var target in Main.AllPlayerControls)
            {
                if (target.OwnedByHost()) continue;

                if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var roleMap))
                {
                    try
                    {
                        var roleType = roleMap.Item1;
                        var sender = senders[seer.PlayerId];
                        sender.RpcSetRole(seer, roleType, target.GetClientId());
                    }
                    catch
                    { }
                }
            }
        }
    }

    private static void AssignCustomRole(CustomRoles role, PlayerControl player)
    {
        if (player == null) return;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);
        //Logger.Info($"Registered Role： {player?.Data?.PlayerName} => {role}", "AssignCustomRoles");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
public static class RpcSetRoleReplacer
{
    public static bool BlockSetRole = false;
    public static Dictionary<byte, CustomRpcSender> Senders = [];
    public static Dictionary<PlayerControl, RoleTypes> StoragedData = [];
    public static Dictionary<NetworkedPlayerInfo, bool> DataDisconnected = [];
    public static Dictionary<(byte seerId, byte targetId), (RoleTypes roleType, CustomRoles customRole)> RoleMap = [];
    // List of Senders that do not require additional writing because SetRoleRpc has already been written by another process such as Position Desync
    public static List<CustomRpcSender> OverriddenSenderList = [];
    public static void Initialize()
    {
        BlockSetRole = true;
        Senders = [];
        RoleMap = [];
        StoragedData = [];
        DataDisconnected = [];
        OverriddenSenderList = [];
    }
    public static bool Prefix()
    {
        return !BlockSetRole;
    }
    public static void StartReplace()
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.OwnedByHost()) continue;

            Senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                    .StartMessage(pc.GetClientId());
        }
    }
    public static void AssignDesyncRoles()
    {
        // Assign desync roles
        foreach (var (playerId, role) in RoleAssign.RoleResult.Where(x => x.Value.IsDesyncRole()))
            SelectRolesPatch.AssignDesyncRole(role, Utils.GetPlayerById(playerId), Senders, RoleMap, BaseRole: role.GetDYRole());

        // Set Desync RoleType by "RpcSetRole"
        SelectRolesPatch.MakeDesyncSender(Senders, RoleMap);
    }
    public static void AssignNormalRoles()
    {
        foreach (var (playerId, role) in RoleAssign.RoleResult)
        {
            var player = Utils.GetPlayerById(playerId);
            if (player == null || role.IsDesyncRole()) continue;

            var roleType = role.GetRoleTypes();

            if (!player.OwnedByHost())
                StoragedData.Add(player, roleType);

            foreach (var target in Main.AllPlayerControls)
            {
                if (target.HasDesyncRole()) continue;

                RoleMap[(target.PlayerId, playerId)] = (roleType, role);
            }

            Logger.Info($"Set original role type => {player.GetRealName()}: {role} => {role.GetRoleTypes()}", "AssignNormalRoles");
        }
    }
    public static void Release()
    {
        foreach (var sender in Senders)
        {
            if (OverriddenSenderList.Contains(sender.Value)) continue;
            if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

            foreach (var (seer, roleType) in StoragedData)
            {
                try
                {
                    seer.SetRole(roleType);
                    sender.Value.AutoStartRpc(seer.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                        .Write((ushort)roleType)
                        .Write(true)
                        .EndRpc();
                }
                catch
                { }
            }
            sender.Value.EndMessage();
        }
        
        BlockSetRole = false;
        Senders.Do(kvp => kvp.Value.SendMessage());
        EndReplace();

        SetRoleForHost();
    }
    private static void EndReplace()
    {
        Senders = null;
        OverriddenSenderList = null;
        StoragedData = null;
    }
    private static void SetRoleForHost()
    {
        try
        {
            RpcSetDisconnected(disconnected: true, doSync: true);
            foreach (var seer in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                foreach (var target in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    if (!target.OwnedByHost()) continue;

                    RoleMap.TryGetValue((seer.PlayerId, target.PlayerId), out var map);
                    target.RpcSetRoleDesync(map.roleType, seer.GetClientId());
                }
            }
            RpcSetDisconnected(disconnected: false, doSync: false);
        }
        catch { }
    }
    public static void RpcSetDisconnected(bool disconnected, bool doSync)
    {
        foreach (var playerinfo in GameData.Instance.AllPlayers)
        {
            if (disconnected)
            {
                // if player left the game, remember current data
                DataDisconnected[playerinfo] = playerinfo.Disconnected;
                playerinfo.Disconnected = disconnected;
                playerinfo.IsDead = false;
            }
            else
            {
                playerinfo.Disconnected = DataDisconnected[playerinfo];
                playerinfo.IsDead = DataDisconnected[playerinfo];
            }

            if (!doSync) continue;

            MessageWriter writer = MessageWriter.Get(SendOption.None);
            writer.StartMessage(5); //0x05 GameData
            writer.Write(AmongUsClient.Instance.GameId);
            {
                writer.StartMessage(1); //0x01 Data
                {
                    writer.WritePacked(playerinfo.NetId);
                    playerinfo.Serialize(writer, true);
                }
                writer.EndMessage();
            }
            writer.EndMessage();

            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }
    }
}