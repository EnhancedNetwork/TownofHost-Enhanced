using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hazel;
using InnerNet;
using System;
using System.Text;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Patches;
using TOHE.Roles.Core;
using TOHE.Roles.Core.AssignManager;
using UnityEngine;
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
        CriticalErrorManager.Initialize();

        Logger.Msg("Is Started", "Initialization");

        try
        {
            // Note: No positions are set at this time
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
            AbilityUseManager.Initializate();

            Main.AllPlayerKillCooldown.Clear();
            Main.AllPlayerSpeed.Clear();
            Main.AllPlayerCustomRoles.Clear();
            Main.TasklessCrewmate.Clear();
            Main.UnreportableBodies.Clear();

            Main.LastEnteredVent.Clear();
            Main.LastEnteredVentLocation.Clear();

            Main.MurderedThisRound.Clear();
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
            Main.DeadPassedMeetingPlayers.Clear();
            Main.OvverideOutfit.Clear();
            Main.GameIsLoaded = false;
            Main.CurrentServerIsVanilla = GameStates.IsVanillaServer && !GameStates.IsLocalGame;

            Main.LastNotifyNames.Clear();

            Main.FirstDiedPrevious = Options.CurrentGameMode == CustomGameMode.Standard && Options.ShieldPersonDiedFirst.GetBool() ? Main.FirstDied : "";
            Main.FirstDied = "";
            Main.MadmateNum = 0;
            Main.BardCreations = 0;
            Main.MeetingsPassed = 0;
            Main.MeetingIsStarted = false;
            Main.IntroDestroyed = false;
            GameEndCheckerForNormal.ShouldNotCheck = false;
            GameEndCheckerForNormal.ForEndGame = false;
            GameEndCheckerForNormal.GameIsEnded = false;
            GameStartManagerPatch.GameStartManagerUpdatePatch.AlredyBegin = false;
            OnPlayerLeftPatch.LeftPlayerId = byte.MaxValue;
            FixedUpdateInNormalGamePatch.RoleTextCache.Clear();

            VentSystemDeterioratePatch.LastClosestVent.Clear();
            VentSystemDeterioratePatch.PlayerHadBlockedVentLastTime.Clear();

            ChatManager.ResetHistory();
            ReportDeadBodyPatch.CanReport.Clear();
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
            CustomRoleManager.Initialize();

            if (AmongUsClient.Instance.AmHost)
            {
                var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
                if (invalidColor.Any())
                {
                    StringBuilder sb = new();
                    sb.Append(GetString("Error.InvalidColor"));
                    Logger.SendInGame(sb.ToString());
                    sb.Append($" {string.Join(", ", invalidColor.Where(pc => pc != null).Select(p => $"{Main.AllPlayerNames.GetValueOrDefault(p.PlayerId, "PlayerNotFound")}"))}");
                    var msg = sb.ToString();
                    Utils.SendMessage(msg);
                    CriticalErrorManager.SetCriticalError("Player Have Invalid Color", true);
                    Logger.Error(msg, "CoStartGame");
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
                        string realName = Main.AllPlayerNames.GetValueOrDefault(pc.PlayerId, string.Empty);
                        //Logger.Info($"player id: {pc.PlayerId} {realName}", "FinallyBegin");
                        if (realName == string.Empty) continue;

                        currentName = realName;
                        pc.RpcSetName(realName);
                    }
                }

                foreach (var target in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, pc.PlayerId);
                    Main.LastNotifyNames[pair] = currentName;
                }

                Main.LowLoadUpdateName[pc.PlayerId] = true;

                Main.PlayerStates[pc.PlayerId] = new(pc.PlayerId)
                {
                    NormalOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set(currentName, pc.Data.Outfits[PlayerOutfitType.Default].ColorId, pc.Data.Outfits[PlayerOutfitType.Default].HatId, pc.Data.Outfits[PlayerOutfitType.Default].SkinId, pc.Data.Outfits[PlayerOutfitType.Default].VisorId, pc.Data.Outfits[PlayerOutfitType.Default].PetId, pc.Data.Outfits[PlayerOutfitType.Default].NamePlateId),
                };

                if (GameStates.IsNormalGame)
                    Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);

                RoleAssign.RoleResult[pc.PlayerId] = CustomRoles.NotAssigned;

                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = [];

                VentSystemDeterioratePatch.LastClosestVent[pc.PlayerId] = 99;
                VentSystemDeterioratePatch.PlayerHadBlockedVentLastTime[pc.PlayerId] = false;

                CustomRoleManager.BlockedVentsList[pc.PlayerId] = [];
                CustomRoleManager.DoNotUnlockVentsList[pc.PlayerId] = [];

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

            // Initialize all Roles
            foreach (var role in EnumHelper.GetAllValues<CustomRoles>().Where(role => role < CustomRoles.NotAssigned).ToArray())
            {
                var RoleClass = role.GetStaticRoleClass();
                RoleClass?.OnInit();
            }

            // Initialize all Add-ons
            foreach (var addOn in CustomRoleManager.AddonClasses.Values)
            {
                addOn?.Init();
            }

            TargetArrow.Init();
            LocateArrow.Init();
            DoubleTrigger.Init();

            //FFA
            FFAManager.Init();

            //Speed Run
            SpeedRun.Init();

            FallFromLadder.Reset();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            NameNotifyManager.Reset();
            CustomNetObject.Reset();

            SabotageSystemPatch.SabotageSystemTypeRepairDamagePatch.Initialize();
            DoorsReset.Initialize();
            ShipStatusSerializePatch.ReactorFlashList.Clear();

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
            CriticalErrorManager.SetCriticalError("Change Role Setting Postfix", true, ex.ToString());
            Utils.ThrowException(ex);
        }
    }
}
[HarmonyPatch]
internal class StartGameHostPatch
{
    private static AmongUsClient thiz;

    private static RoleOptionsCollectionV09 RoleOpt => Main.NormalOptions.roleOptions;
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

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGameHost))]
    [HarmonyPrefix]
    public static bool CoStartGameHost_Prefix(AmongUsClient __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (GameStates.IsHideNSeek)
        {
            return true;
        }

        thiz = __instance;
        __result = StartGameHost().WrapToIl2Cpp();
        return false;
    }

    public static System.Collections.IEnumerator StartGameHost()
    {
        if (LobbyBehaviour.Instance)
        {
            LobbyBehaviour.Instance.Despawn();
        }
        if (!ShipStatus.Instance)
        {
            int num = Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.MapId, 0, Constants.MapNames.Length - 1);
            // Don't need this because Dleks map sets in settings
            /* try
            {
                if (num == 0 && AprilFoolsMode.ShouldFlipSkeld())
                {
                    num = 3;
                }
                else if (num == 3 && !AprilFoolsMode.ShouldFlipSkeld())
                {
                    num = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }*/
            thiz.ShipLoadingAsyncHandle = thiz.ShipPrefabs[num].InstantiateAsync(null, false);
            yield return thiz.ShipLoadingAsyncHandle;
            GameObject result = thiz.ShipLoadingAsyncHandle.Result;
            ShipStatus.Instance = result.GetComponent<ShipStatus>();
            thiz.Spawn(ShipStatus.Instance, -2, SpawnFlags.None);
        }
        float timer = 0f;
        while (true)
        {
            bool stopWaiting = true;
            int maxTimer = 10;
            if (GameOptionsManager.Instance.CurrentGameOptions.MapId is 4 or 5)
            {
                maxTimer = 15;
            }
            var allClients = thiz.allClients.ToManaged();
            lock (allClients)
            {
                for (int i = 0; i < thiz.allClients.Count; i++)
                {
                    ClientData clientData = thiz.allClients[i];
                    if (clientData.Id != thiz.ClientId && !clientData.IsReady)
                    {
                        if (timer < maxTimer)
                        {
                            stopWaiting = false;
                        }
                        else
                        {
                            thiz.SendLateRejection(clientData.Id, DisconnectReasons.ClientTimeout);
                            clientData.IsReady = true;
                            thiz.OnPlayerLeft(clientData, DisconnectReasons.ClientTimeout);
                        }
                    }
                }
            }
            yield return null;
            if (stopWaiting)
            {
                break;
            }
            timer += Time.deltaTime;
        }
        thiz.SendClientReady();
        yield return new WaitForSeconds(2f);
        yield return AssignRoles();
        //ShipStatus.Instance.Begin(); // Tasks sets in IntroPatch
        yield break;
    }

    public static System.Collections.IEnumerator AssignRoles()
    {
        if (GameStates.IsEnded) yield break;

        try
        {
            // Block "RpcSetRole" for set Desync Roles for some players
            RpcSetRoleReplacer.Initialize();

            // Select custom Roles/Add-ons
            EAC.OriginalRoles = [];
            RoleAssign.StartSelect();
            AddonAssign.StartSelect();

            // Set count Vanilla Roles
            RoleAssign.CalculateVanillaRoleCount();

            UpdateRoleTypeNums();
            // Set rate for Vanilla Roles
            foreach (var roleType in RoleTypeNums)
            {
                int roleNum = Options.DisableVanillaRoles.GetBool() ? 0 : RoleOpt.GetNumPerGame(roleType.Key);
                roleNum += roleType.Value;
                RoleOpt.SetRoleRate(roleType.Key, roleNum, roleType.Value > 0 ? 100 : RoleOpt.GetChancePerGame(roleType.Key));
            }

            Logger.Msg("Is Started", "AssignRoles");

            //Start CustomRpcSender
            RpcSetRoleReplacer.StartReplace();

            // Assign Roles and create Role Map for Desync Roles
            RpcSetRoleReplacer.AssignDesyncRoles();
            RpcSetRoleReplacer.SendRpcForDesync();

            // Assign Roles and create Role Map for Normal Roles
            RpcSetRoleReplacer.AssignNormalRoles();
            RpcSetRoleReplacer.SendRpcForNormal();
        }
        catch (Exception ex)
        {
            CriticalErrorManager.SetCriticalError("Select Role Prefix - Building Role Sender", true, ex.ToString());
            Utils.ThrowException(ex);
            yield break;
        }

        if (Main.CurrentServerIsVanilla && Options.BypassRateLimitAC.GetBool())
        {
            yield return RpcSetRoleReplacer.ReleaseVanilla();
        }
        else
        {
            // Send all RPC for modded region
            RpcSetRoleReplacer.Release();
        }

        try
        {
            foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (Main.PlayerStates[pc.PlayerId].MainRole != CustomRoles.NotAssigned) continue;
                var role = pc.Data.Role.Role switch
                {
                    RoleTypes.Crewmate => CustomRoles.Crewmate,
                    RoleTypes.Impostor => CustomRoles.Impostor,
                    RoleTypes.Scientist => CustomRoles.Scientist,
                    RoleTypes.Engineer => CustomRoles.Engineer,
                    RoleTypes.GuardianAngel => CustomRoles.GuardianAngel,
                    RoleTypes.Shapeshifter => CustomRoles.Shapeshifter,
                    RoleTypes.Noisemaker => CustomRoles.Noisemaker,
                    RoleTypes.Phantom => CustomRoles.Phantom,
                    RoleTypes.Tracker => CustomRoles.Tracker,
                    _ => CustomRoles.NotAssigned
                };
                if (role == CustomRoles.NotAssigned) Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                Main.PlayerStates[pc.PlayerId].SetMainRole(role);
            }

            if (Options.CurrentGameMode is CustomGameMode.FFA)
            {
                foreach (var pair in RoleAssign.RoleResult)
                {
                    pair.Key.GetPlayer()?.RpcSetCustomRole(pair.Value, false, false);
                }
                goto EndOfSelectRolePatch;
            }

            foreach (var kv in RoleAssign.RoleResult)
            {
                if (kv.Value.IsDesyncRole()) continue;

                AssignCustomRole(kv.Value, Utils.GetPlayerById(kv.Key));
            }

            try
            {
                if (Options.CurrentGameMode == CustomGameMode.Standard)
                {
                    AddonAssign.InitAndStartAssignLovers();
                    AddonAssign.StartSortAndAssign();
                }
            }
            catch (Exception error)
            {
                Logger.Warn($"Error after addons assign - error: {error}", "AddonAssign");
            }

            var setCustomRoleSender = CustomRpcSender.Create("SetCustomRole Release Sender", SendOption.Reliable);

            // Sync for non-host modded clients by RPC
            foreach (var pair in Main.PlayerStates)
            {
                // Set roles
                setCustomRoleSender.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                // Set Add-ons
                foreach (var subRole in pair.Value.SubRoles.ToArray())
                    setCustomRoleSender.RpcSetCustomRole(pair.Key, subRole);
            }

            setCustomRoleSender.SendMessage();

            GhostRoleAssign.Add();

            foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (Utils.IsMethodOverridden(pc.GetRoleClass(), "UnShapeShiftButton"))
                {
                    Main.UnShapeShifter.Add(pc.PlayerId);
                    Logger.Info($"Added {pc.GetRealName()} because of {pc.GetCustomRole()}", "UnShapeShift.OnGameStartedPatch");
                }

                var roleClass = pc.GetRoleClass();

                roleClass?.OnAdd(pc.PlayerId);

                // If based role is Shapeshifter
                if (roleClass?.ThisRoleBase.GetRoleTypes() == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(pc.PlayerId, false);
            }

        EndOfSelectRolePatch:

            foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
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
                case CustomGameMode.SpeedRun:
                    GameEndCheckerForNormal.SetPredicateToSpeedRun();
                    break;
            }

            EAC.LogAllRoles();
            //Utils.CountAlivePlayers(sendLog: true, checkGameEnd: false);

            Logger.Msg("Ended", "AssignRoles");
        }
        catch (Exception ex)
        {
            CriticalErrorManager.SetCriticalError("Select Role Prefix - Building Role classes", true, ex.ToString());
            Utils.ThrowException(ex);
            yield break;
        }

        Logger.Info("Others assign finished", "AssignRoleTypes");
        yield return new WaitForSeconds(GameStates.IsLocalGame ? 1f : 2f);

        Logger.Info("Send rpc disconnected for all", "AssignRoleTypes");
        DataDisconnected.Clear();
        RpcSetDisconnected(disconnected: true);

        yield return new WaitForSeconds(GameStates.IsLocalGame ? 2f : 4f);

        Logger.Info("Assign self", "AssignRoleTypes");
        SetRoleSelf();

        RpcSetRoleReplacer.EndReplace();
        yield break;
    }

    public static void AssignDesyncRole(CustomRoles role, PlayerControl player, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), (RoleTypes, CustomRoles)> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        if (player == null) return;

        var hostId = PlayerControl.LocalPlayer.PlayerId;
        var isHost = player.PlayerId == hostId;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);

        var selfRole = isHost ? BaseRole == RoleTypes.Shapeshifter ? RoleTypes.Shapeshifter : hostBaseRole : BaseRole;
        var othersRole = isHost ? RoleTypes.Crewmate : RoleTypes.Scientist;

        // Set Desync Role for self and for others
        foreach (var target in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            var targetRoleType = othersRole;
            var targetCustomRole = RoleAssign.RoleResult.GetValueOrDefault(target.PlayerId, CustomRoles.CrewmateTOHE);

            if (targetCustomRole.GetVNRole() is CustomRoles.Noisemaker)
                targetRoleType = RoleTypes.Noisemaker;

            rolesMap[(player.PlayerId, target.PlayerId)] = player.PlayerId != target.PlayerId ? (targetRoleType, targetCustomRole) : (selfRole, role);
        }

        // Set Desync Role for others
        foreach (var seer in Main.AllPlayerControls.Where(x => player.PlayerId != x.PlayerId).ToArray())
            rolesMap[(seer.PlayerId, player.PlayerId)] = (othersRole, role);


        RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
        // Set Role for Host, but not self
        // canOverride should be false for the Host during assign
        if (!isHost)
        {
            player.SetRole(othersRole, false);
        }

        Logger.Info($"Registered Role: {player?.Data?.PlayerName} => {role} : RoleType for self => {selfRole}, for others => {othersRole}", "AssignDesyncRoles");
    }
    public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), (RoleTypes, CustomRoles)> rolesMap)
    {
        foreach (var seer in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            foreach (var target in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (seer.PlayerId == target.PlayerId || target.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var roleMap))
                {
                    try
                    {
                        var targetClientId = target.GetClientId();
                        if (targetClientId == -1) continue;

                        var roleType = roleMap.Item1;
                        var sender = senders[seer.PlayerId];
                        sender.RpcSetRole(seer, roleType, targetClientId);
                    }
                    catch
                    { }
                }
            }
        }
    }
    private static void SetRoleSelf()
    {
        var sender = CustomRpcSender.Create("SetRoleSelf Sender", SendOption.Reliable);
        foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            try
            {
                DoSetRoleSelf(sender, pc);
            }
            catch { }
        }
        sender.SendMessage();
    }
    private static void DoSetRoleSelf(CustomRpcSender sender, PlayerControl target)
    {
        if (target == null) return;

        RoleTypes roleType;
        int targetClientId = target.GetClientId();
        if (targetClientId == -1) return;

        if (RpcSetRoleReplacer.RoleMap.TryGetValue((target.PlayerId, target.PlayerId), out var roleMap))
        {
            roleType = roleMap.roleType;
        }
        else
        {
            roleType = RpcSetRoleReplacer.StoragedData[target.PlayerId];
        }

        sender.RpcSetRole(target, roleType, targetClientId);
    }

    public static readonly Dictionary<byte, bool> DataDisconnected = [];
    public static void RpcSetDisconnected(bool disconnected)
    {
        foreach (var playerInfo in GameData.Instance.AllPlayers.GetFastEnumerator())
        {
            if (disconnected)
            {
                // If player left the game, remember current data
                DataDisconnected[playerInfo.PlayerId] = playerInfo.Disconnected;

                playerInfo.Disconnected = true;
                playerInfo.IsDead = false;
            }
            else
            {
                var data = DataDisconnected.GetValueOrDefault(playerInfo.PlayerId, true);
                playerInfo.Disconnected = data;
                playerInfo.IsDead = data;
            }

            var stream = MessageWriter.Get(SendOption.Reliable);
            stream.StartMessage(5);
            stream.Write(AmongUsClient.Instance.GameId);
            {
                stream.StartMessage(1);
                stream.WritePacked(playerInfo.NetId);
                playerInfo.Serialize(stream, false);
                stream.EndMessage();
            }
            stream.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(stream);
            stream.Recycle();
        }
    }

    private static void AssignCustomRole(CustomRoles role, PlayerControl player)
    {
        if (player == null) return;
        Main.PlayerStates[player.PlayerId].SetMainRole(role);
    }
}
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
internal class SelectRolesPatch
{
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsHideNSeek)
        {
            if (Main.EnableGM.Value)
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate, false);
                PlayerControl.LocalPlayer.Data.IsDead = true;
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            foreach (var player in Main.AllPlayerControls)
            {
                if (!player.IsDisconnected() && TagManager.AssignGameMaster(player.FriendCode))
                {
                    Logger.Info($"Setting GM role for [{player.PlayerId}]{player.GetRealName()}", "SelectRolesPatch.HnS");
                    player.RpcSetCustomRole(CustomRoles.GM);
                    player.RpcSetRole(RoleTypes.Crewmate, false);
                    player.Data.IsDead = true;
                    Main.PlayerStates[player.PlayerId].SetDead();
                }
            }

            EAC.OriginalRoles = [];

            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }

            EAC.LogAllRoles();
            Utils.SyncAllSettings();
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole)), HarmonyPriority(Priority.High)]
public static class RpcSetRoleReplacer
{
    public static bool BlockSetRole = false;
    public static Dictionary<byte, CustomRpcSender> Senders = [];
    public static Dictionary<byte, RoleTypes> StoragedData = [];
    public static Dictionary<(byte seerId, byte targetId), (RoleTypes roleType, CustomRoles customRole)> RoleMap = [];
    // List of Senders that do not require additional writing because SetRoleRpc has already been written by another process such as Position Desync
    public static List<CustomRpcSender> OverriddenSenderList = [];
    public static void Initialize()
    {
        BlockSetRole = true;
        Senders = [];
        RoleMap = [];
        StoragedData = [];
        OverriddenSenderList = [];
    }
    public static bool Prefix()
    {
        return !BlockSetRole;
    }
    public static void StartReplace()
    {
        foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
        {
            Senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                    .StartMessage(pc.GetClientId());
        }
    }
    public static void AssignDesyncRoles()
    {
        foreach (var (playerId, role) in RoleAssign.RoleResult.Where(x => x.Value.IsDesyncRole()))
            StartGameHostPatch.AssignDesyncRole(role, Utils.GetPlayerById(playerId), Senders, RoleMap, BaseRole: role.GetDYRole());
    }
    public static void SendRpcForDesync()
    {
        StartGameHostPatch.MakeDesyncSender(Senders, RoleMap);
    }
    public static void AssignNormalRoles()
    {
        foreach (var (playerId, role) in RoleAssign.RoleResult)
        {
            var player = Utils.GetPlayerById(playerId);
            if (player == null || role.IsDesyncRole()) continue;

            var roleType = role.GetRoleTypes();

            StoragedData.Add(playerId, roleType);

            foreach (var target in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (RoleAssign.RoleResult[target.PlayerId].IsDesyncRole() && !target.IsHost()) continue;

                RoleMap[(target.PlayerId, playerId)] = (roleType, role);
            }

            if (playerId != PlayerControl.LocalPlayer.PlayerId)
            {
                // canOverride should be false for the Host during assign
                player.SetRole(roleType, false);
            }

            Logger.Info($"Set original role type => {player.GetRealName()}: {role} => {role.GetRoleTypes()}", "AssignNormalRoles");
        }
    }
    public static void SendRpcForNormal()
    {
        foreach (var (targetId, sender) in Senders)
        {
            var target = Utils.GetPlayerById(targetId);
            if (OverriddenSenderList.Contains(sender)) continue;
            if (sender.CurrentState != CustomRpcSender.State.InRootMessage)
                throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

            foreach (var (seerId, roleType) in StoragedData)
            {
                if (targetId == seerId || targetId == PlayerControl.LocalPlayer.PlayerId) continue;
                var seer = Utils.GetPlayerById(seerId);
                if (seer == null || target == null) continue;

                try
                {
                    var targetClientId = target.GetClientId();
                    if (targetClientId == -1) continue;

                    // Send RPC set Role for others clients
                    sender.AutoStartRpc(seer.NetId, (byte)RpcCalls.SetRole, targetClientId)
                        .Write((ushort)roleType)
                        .Write(true) // canOverride
                        .EndRpc();
                }
                catch
                { }
            }
            sender.EndMessage();
        }
    }
    public static void Release()
    {
        BlockSetRole = false;
        Senders.Do(kvp => kvp.Value.SendMessage());
    }

    public static System.Collections.IEnumerator ReleaseVanilla()
    {
        BlockSetRole = false;
        foreach (var kvp in Senders)
        {
            kvp.Value.SendMessage();
            yield return new WaitForSeconds(0.3f);
        }
    }

    public static void EndReplace()
    {
        Senders = null;
        OverriddenSenderList = null;
        StoragedData = null;
    }
}
