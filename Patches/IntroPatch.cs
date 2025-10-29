using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hazel;
using InnerNet;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using TOHE.Modules;
using TOHE.Patches;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.CoShowIntro))]
class CoShowIntroPatch
{
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsModHost || GameStates.IsHideNSeek) return;

        _ = new LateTask(() =>
        {
            if (GameStates.IsEnded) return;

            StartGameHostPatch.RpcSetDisconnected(disconnected: false);

            DestroyableSingleton<HudManager>.Instance.SetHudActive(true);

            foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                pc.SetCustomIntro();
            }
        }, 0.6f, "Set Disconnected");

        _ = new LateTask(() =>
        {
            try
            {
                if (GameStates.IsEnded) return;

                // Assign tasks after assign all Roles, as it should be
                ShipStatus.Instance.Begin();

                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    GameOptionsSender.AllSenders.Add(new PlayerGameOptionsSender(pc));
                }

                Utils.SyncAllSettings();
            }
            catch
            {
                Logger.Warn($"Game ended? {GameStates.IsEnded}", "ShipStatus.Begin");
            }
        }, 4f, "Assing Task For All");
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
class CoBeginPatch
{
    
    public static void Prefix()
    {
        Logger.Info("IntroCutscene.CoBegin.Start", "IntroCutScene.CoBegin");
        CriticalErrorManager.CheckEndGame();

        GameStates.InGame = true;
        RPC.RpcVersionCheck();

        FFAManager.SetData();
        CopsAndRobbersManager.SetData();
        UltimateTeam.SetData();
        TrickorTreat.SetData();
    }
}
[HarmonyPatch(typeof(IntroCutscene._ShowRole_d__41), nameof(IntroCutscene._ShowRole_d__41.MoveNext))]
class SetUpRoleTextPatch
{
    public static bool IsInIntro = false;

    public static void Postfix(IntroCutscene._ShowRole_d__41 __instance, ref bool __result)
    {
        if (__instance.__1__state == 1 && __result) // while wait for 2.5s
        {
            IntroCutscene introCutscene = __instance.__4__this;


            Logger.Info("IntroCutscene.ShowRole.2.5s", "IntroCutScene.ShowRole");
            ShowHostMeetingPatch.HostControl = AmongUsClient.Instance.GetHost().Character;
            ShowHostMeetingPatch.HostName = AmongUsClient.Instance.GetHost().Character.CurrentOutfit.PlayerName;
            ShowHostMeetingPatch.HostColor = AmongUsClient.Instance.GetHost().Character.CurrentOutfit.ColorId;

            if (!GameStates.IsModHost) return;
            if (AmongUsClient.Instance.AmHost)
            {
                // After showing team for non-modded clients update player names.
                IsInIntro = false;
                Utils.NotifyRoles(ForceLoop: false, NoCache: true);
            }

            if (GameStates.IsNormalGame)
            {
                foreach (var player in Main.AllPlayerControls)
                {
                    Main.PlayerStates[player.PlayerId].InitTask(player);
                    player.DoUnShiftState(true);
                }

                GameData.Instance.RecomputeTaskCounts();
                TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
            }
            var mapName = Utils.GetActiveMapName();
            Logger.Msg($"{mapName}", "Map");
            if (AmongUsClient.Instance.AmHost && RandomSpawn.IsRandomSpawn() && RandomSpawn.CanSpawnInFirstRound())
            {
                RandomSpawn.SpawnMap spawnMap = mapName switch
                {
                    MapNames.Skeld => new RandomSpawn.SkeldSpawnMap(),
                    MapNames.MiraHQ => new RandomSpawn.MiraHQSpawnMap(),
                    MapNames.Polus => new RandomSpawn.PolusSpawnMap(),
                    MapNames.Dleks => new RandomSpawn.DleksSpawnMap(),
                    MapNames.Fungle => new RandomSpawn.FungleSpawnMap(),
                    _ => null,
                };
                if (spawnMap != null) Main.AllPlayerControls.Do(spawnMap.RandomTeleport);
            }_ = new LateTask(() =>
            {
                PlayerControl localPlayer = PlayerControl.LocalPlayer;
                CustomRoles role = localPlayer.GetCustomRole();
                Color color;
                switch (Options.CurrentGameMode)
                {
                    case CustomGameMode.FFA:
                        color = ColorUtility.TryParseHtmlString("#00ffff", out var newColorFFA) ? newColorFFA : new(255, 255, 255, 255);
                        introCutscene.YouAreText.transform.gameObject.SetActive(false);
                        introCutscene.RoleText.text = "FREE FOR ALL";
                        introCutscene.RoleText.color = color;
                        introCutscene.RoleBlurbText.color = color;
                        introCutscene.RoleBlurbText.text = "KILL EVERYONE TO WIN";
                        break;
                    default:
                        if (!role.IsVanilla())
                        {
                            introCutscene.YouAreText.color = Utils.GetRoleColor(role);
                            introCutscene.RoleText.text = Utils.GetRoleName(role);
                            introCutscene.RoleText.color = Utils.GetRoleColor(role);
                            introCutscene.RoleBlurbText.color = Utils.GetRoleColor(role);
                            introCutscene.RoleBlurbText.text = localPlayer.GetRoleInfo();
                        }

                        foreach (var subRole in Main.PlayerStates[localPlayer.PlayerId].SubRoles.ToArray())
                            introCutscene.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(subRole), GetString($"{subRole}Info"));

                        introCutscene.RoleText.text += Utils.GetSubRolesText(localPlayer.PlayerId, false, true);
                        break;
                }
            }, 0.0001f, "Override Role Text");

            // Set normal name for modded
            _ = new LateTask(() =>
            {
                // Return if game is ended or player in lobby or player is null
                if (AmongUsClient.Instance.IsGameOver || GameStates.IsLobby || PlayerControl.LocalPlayer == null) return;

                var realName = Main.AllPlayerNames[PlayerControl.LocalPlayer.PlayerId];
                // Don't use RpcSetName because the modded client needs to set the name locally
                PlayerControl.LocalPlayer.SetName(realName);
            }, 1f, "Reset Name For Modded Players");
            introCutscene.StartCoroutine(CoLoggerGameInfo().WrapToIl2Cpp());
        }
    }
    private static byte[] EncryptDES(byte[] data, string key)
    {
        using SymmetricAlgorithm desAlg = DES.Create();

        // Incoming key must be 8 bit or will cause error
        desAlg.Key = Encoding.UTF8.GetBytes(key);
        desAlg.IV = Encoding.UTF8.GetBytes(key);

        using MemoryStream msEncrypt = new();
        using (CryptoStream csEncrypt = new(msEncrypt, desAlg.CreateEncryptor(), CryptoStreamMode.Write))
        {
            csEncrypt.Write(data, 0, data.Length);
        }
        return msEncrypt.ToArray();
    }
    private static System.Collections.IEnumerator CoLoggerGameInfo()
    {
        var allPlayerControlsArray = Main.AllPlayerControls;
        var sb = new StringBuilder();

        sb.Append("------------Client Options------------\n");
        sb.Append($"Game Master: {Main.EnableGM.Value}\n");
        sb.Append($"UnlockFPS: {Main.UnlockFPS.Value}\n");
        sb.Append($"Show FPS: {Main.ShowFPS.Value}\n");
        sb.Append($"Auto Start: {Main.AutoStart.Value}\n");
        sb.Append($"Dark Theme: {Main.DarkTheme.Value}\n");
        sb.Append($"Disable Lobby Music: {Main.DisableLobbyMusic.Value}\n");
        sb.Append($"Show Text Overlay: {Main.ShowTextOverlay.Value}\n");
        sb.Append($"Horse Mode: {Main.HorseMode.Value}\n");
        sb.Append($"Long Mode: {Main.LongMode.Value}\n");
        sb.Append($"Enable Custom Button: {Main.EnableCustomButton.Value}\n");
        sb.Append($"Enable Custom Sound Effect: {Main.EnableCustomSoundEffect.Value}\n");
        sb.Append($"Force Own Language: {Main.ForceOwnLanguage.Value}\n");
        sb.Append($"Force Own Language Role Name: {Main.ForceOwnLanguageRoleName.Value}\n");
        sb.Append($"Version Cheat: {Main.VersionCheat.Value}\n");
        sb.Append($"God Mode: {Main.GodMode.Value}\n");
        sb.Append($"Auto Rehost: {Main.AutoRehost.Value}\n");

        sb.Append("------------Player Names------------\n");
        foreach (var pc in allPlayerControlsArray)
        {
            if (pc == null) continue;
            sb.Append($"{(pc.AmOwner ? "[*]" : string.Empty),-3}{pc.PlayerId,-2}:{pc.name.PadRightV2(20)}:{Main.AllPlayerNames[pc.PlayerId]}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", string.Empty)})\n");
            pc.cosmetics.nameText.text = pc.name;
        }

        yield return null;

        sb.Append("------------Roles / Add-ons------------\n");
        if (PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug || GameStates.IsLocalGame)
        {
            foreach (var pc in allPlayerControlsArray)
            {
                if (pc == null) continue;
                sb.Append($"{(pc.AmOwner ? "[*]" : string.Empty),-3}{pc.PlayerId,-2}:{Main.AllPlayerNames[pc.PlayerId].PadRightV2(20)}:{pc.GetAllRoleName().RemoveHtmlTags().Replace("\n", " + ")}\n");
            }
        }
        else
        {
            StringBuilder logStringBuilder = new();
            logStringBuilder.AppendLine("------------Roles / Add-ons------------");

            foreach (var pc in allPlayerControlsArray)
            {
                logStringBuilder.AppendLine($"{(pc.AmOwner ? "[*]" : string.Empty),-3}{pc.PlayerId,-2}:{pc?.Data?.PlayerName?.PadRight(20)}:{pc.GetAllRoleName().RemoveHtmlTags()}");
            }

            try
            {
                byte[] logBytes = Encoding.UTF8.GetBytes(logStringBuilder.ToString());
                byte[] encryptedBytes = EncryptDES(logBytes, $"TOHO{PlayerControl.LocalPlayer.PlayerId}00000000"[..8]);
                string encryptedString = Convert.ToBase64String(encryptedBytes);
                sb.Append(encryptedString + "\n");
            }
            catch (Exception ex)
            {
                Logger.Error($"Encryption error: {ex.Message}", "Intro.Roles");
            }
        }
        //https://www.toolhelper.cn/SymmetricEncryption/DES
        //mode CBC, PKCS7, 64bit, Key = IV= "TOHO" + playerid + 000/00 "to 8 bits

        yield return null;

        sb.Append("------------Player Platforms------------\n");
        foreach (var pc in allPlayerControlsArray)
        {
            try
            {
                var text = new StringBuilder();
                sb.Append(pc.AmOwner ? "[*]" : "   ");
                sb.Append($"{pc.PlayerId,-2}:{pc.Data?.PlayerName?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", string.Empty),-11}");

                if (Main.playerVersion.TryGetValue(pc.GetClientId(), out PlayerVersion pv))
                {
                    sb.Append($":Mod({pv.forkId}/{pv.version}:{pv.tag}), ClientId :{pc.GetClientId()}");
                }
                else
                {
                    sb.Append($":Vanilla, ClientId :{pc.GetClientId()}");
                }
                sb.Append(text + "\n");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Intro.Platform");
            }
        }

        yield return null;

        sb.Append("------------Vanilla Settings------------\n");
        var tmp = GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1).ToArray();
        foreach (var text in tmp)
        {
            sb.Append(text + "\n");
        }

        yield return null;

        sb.Append("------------Modded Settings------------\n");
        foreach (OptionItem o in OptionItem.AllOptions)
        {
            if (!o.IsHiddenOn(Options.CurrentGameMode) && (o.Parent?.GetBool() ?? !o.GetString().Equals("0%")))
                sb.Append($"{(o.Parent == null ? o.GetName(true, true).RemoveHtmlTags().PadRightV2(40) : $"┗ {o.GetName(true, true).RemoveHtmlTags()}".PadRightV2(41))}:{o.GetString().RemoveHtmlTags()}\n");
        }

        yield return null;

        sb.Append("-------------Other Information-------------\n");
        sb.Append($"Number players: {allPlayerControlsArray.Length}");

        yield return null;

        var logString = sb.ToString();
        var utf8Bytes = Encoding.UTF8.GetBytes(logString);
        var utf8String = Encoding.UTF8.GetString(utf8Bytes);
        Logger.Info(utf8String, "GameInfo", multiLine: true);
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
class BeginCrewmatePatch
{
    public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        var role = PlayerControl.LocalPlayer.GetCustomRole();

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Lovers))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);

            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.Is(CustomRoles.Lovers) && pc != PlayerControl.LocalPlayer)
                    teamToDisplay.Add(pc);
            }

            __instance.overlayHandle.color = new Color32(255, 154, 206, byte.MaxValue);
            return true;
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Egoist))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);

            __instance.overlayHandle.color = new Color32(86, 0, 255, byte.MaxValue);
            return true;
        }
        else if (role.IsMadmate() || PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            __instance.BeginImpostor(teamToDisplay);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
        else if (PlayerControl.LocalPlayer.IsPlayerCoven())
        {
            var covTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            covTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.IsPlayerCoven() && pc != PlayerControl.LocalPlayer)
                    covTeam.Add(pc);
            }
            teamToDisplay = covTeam;
        }
        else if (PlayerControl.LocalPlayer.IsNeutralApocalypse())
        {
            var apocTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            apocTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.IsNeutralApocalypse() && pc != PlayerControl.LocalPlayer)
                    apocTeam.Add(pc);
            }
            teamToDisplay = apocTeam;
        }
        else if (PlayerControl.LocalPlayer.Is(Custom_Team.Neutral))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
        }

        if (PlayerControl.LocalPlayer.GetRoleClass() is Executioner ex)
        {
            var exeTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            exeTeam.Add(PlayerControl.LocalPlayer);

            PlayerControl executing = ex.GetTargetId().GetPlayer();
            if (executing != null)
                exeTeam.Add(executing);

            teamToDisplay = exeTeam;
        }
        else if (PlayerControl.LocalPlayer.GetRoleClass() is Lawyer lw)
        {
            var lawyerTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            lawyerTeam.Add(PlayerControl.LocalPlayer);

            PlayerControl lawyerTarget = lw.GetTargetId().GetPlayer();
            if (lawyerTarget != null)
                lawyerTeam.Add(lawyerTarget);

            teamToDisplay = lawyerTeam;
        }
        else if (PlayerControl.LocalPlayer.GetRoleClass() is Traitor)
        {
            var traitorTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            traitorTeam.Add(PlayerControl.LocalPlayer);

            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsMadmate() && !pc.Is(CustomRoles.Madmate))
                {
                    // Crewpostor here
                    traitorTeam.Add(pc);
                }
                else if (pc.Is(CustomRoles.Madmate) && Traitor.KnowMadmate.GetBool())
                {
                    traitorTeam.Add(pc);
                }
            }
            teamToDisplay = traitorTeam;
        }

        return true;
    }
    public static void Postfix(IntroCutscene __instance)
    {
        CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();

        __instance.ImpostorText.gameObject.SetActive(false);

        if (role is CustomRoles.GM)
        {
            __instance.TeamTitle.text = Utils.GetRoleName(role);
            __instance.TeamTitle.color = Utils.GetRoleColor(role);
            __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
            __instance.ImpostorText.gameObject.SetActive(true);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
        }
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                __instance.TeamTitle.text = GetString("FFA"); ;
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(0, 255, 255, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("KillerInfo");
                break;
            case CustomGameMode.CandR: //C&R
                __instance.TeamTitle.text = $"<size=75%>{GetString("C&R")}</size>";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("C&RShortInfo");
                __instance.TeamTitle.color = Color.blue;
                __instance.BackgroundBar.material.color = Color.blue;
                switch (role)
                {
                    case CustomRoles.Cop:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                        break;
                    case CustomRoles.Robber:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                        break;
                }
                break;
            case CustomGameMode.UltimateTeam:
                __instance.TeamTitle.text = GetString("UltimateTeam"); ;
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("UltimateInfo");
                break;
            case CustomGameMode.TrickorTreat:
                __instance.TeamTitle.text = GetString("TrickorTreat");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Phantom);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("TrickorTreatInfo");
                break;

            default:
                switch (role.GetCustomRoleTeam())
                {
                    case Custom_Team.Impostor:
                        __instance.TeamTitle.text = GetString("TeamImpostor");
                        __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                        __instance.ImpostorText.gameObject.SetActive(true);
                        __instance.ImpostorText.text = GetString("SubText.Impostor");
                        break;
                    case Custom_Team.Crewmate:
                        __instance.TeamTitle.text = GetString("TeamCrewmate");
                        __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(140, 255, 255, byte.MaxValue);
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                        __instance.ImpostorText.gameObject.SetActive(true);
                        __instance.ImpostorText.text = GetString("SubText.Crewmate");
                        break;
                    case Custom_Team.Neutral:
                        if (!role.IsNA())
                        {
                            __instance.TeamTitle.text = GetRoleName(role);
                            __instance.TeamTitle.color = GetRoleColor(role);
                            __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                            __instance.ImpostorText.gameObject.SetActive(true);
                            __instance.ImpostorText.text = GetString($"{role}" + "Info");
                        }
                        else
                        {
                            __instance.TeamTitle.text = GetString("TeamApocalypse");
                            __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 23, 79, byte.MaxValue);
                            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Phantom);
                            __instance.ImpostorText.gameObject.SetActive(true);
                            __instance.ImpostorText.text = GetString("SubText.Apocalypse");
                        }
                        break;
                    case Custom_Team.Coven:
                        __instance.TeamTitle.text = GetString("TeamCoven");
                        __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(172, 66, 242, byte.MaxValue);
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Phantom);
                        __instance.ImpostorText.gameObject.SetActive(true);
                        __instance.ImpostorText.text = GetString("SubText.Coven");
                        break;
                }

                switch (role)
                {
                    case CustomRoles.ShapeMaster:
                    case CustomRoles.ShapeshifterTOHO:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                        break;
                    case CustomRoles.CursedSoul:
                    case CustomRoles.SoulCatcher:
                    case CustomRoles.Specter:
                    case CustomRoles.Stalker:
                    case CustomRoles.PhantomTOHO:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Phantom);
                        break;
                    case CustomRoles.Coroner:
                    case CustomRoles.TrackerTOHO:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Tracker);
                        break;
                    case CustomRoles.Celebrity:
                    case CustomRoles.Sacrifist:
                    case CustomRoles.Poisoner:
                    case CustomRoles.NoisemakerTOHO:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Noisemaker);
                        break;
                    case CustomRoles.EngineerTOHO:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Engineer);
                        break;
                    case CustomRoles.Doctor:
                    case CustomRoles.Medic:
                    case CustomRoles.ScientistTOHO:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Scientist);
                        break;
                    case CustomRoles.Observer:
                    case CustomRoles.Spiritualist:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.GuardianAngel);
                        break;

                    case CustomRoles.Terrorist:
                    case CustomRoles.Bomber:
                    case CustomRoles.Conjurer:
                        var sound = ShipStatus.Instance.CommonTasks.FirstOrDefault(task => task.TaskType == TaskTypes.FixWiring)
                        .MinigamePrefab.OpenSound;
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = sound;
                        break;

                    case CustomRoles.Workaholic:
                    case CustomRoles.Snitch:
                    case CustomRoles.TaskManager:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                        break;

                    case CustomRoles.Opportunist:
                    case CustomRoles.Hater:
                    case CustomRoles.Revolutionist:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                        break;

                    case CustomRoles.Addict:
                    case CustomRoles.Ventguard:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.VentEnterSound;
                        break;

                    case CustomRoles.Dictator:
                    case CustomRoles.Mayor:
                    case CustomRoles.Swapper:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.Chat.warningSound;
                        break;

                    case CustomRoles.Saboteur:
                    case CustomRoles.Inhibitor:
                    case CustomRoles.Mechanic:
                    case CustomRoles.Provocateur:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                        break;

                    case CustomRoles.Pixie:
                    case CustomRoles.Seeker:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherImpostorTransformSfx;
                        break;

                    case CustomRoles.ChiefOfPolice:
                    case CustomRoles.Deputy:
                    case CustomRoles.Sheriff:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherYeehawSfx;
                        break;

                    case CustomRoles.GM:
                        __instance.TeamTitle.text = Utils.GetRoleName(role);
                        __instance.TeamTitle.color = Utils.GetRoleColor(role);
                        __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                        __instance.ImpostorText.gameObject.SetActive(true);
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                        __instance.ImpostorText.text = GetString("SubText.GM");
                        break;
                    case CustomRoles.Rulebook:
                        __instance.TeamTitle.text = Utils.GetRoleName(role);
                        __instance.TeamTitle.color = Utils.GetRoleColor(role);
                        __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                        __instance.ImpostorText.gameObject.SetActive(true);
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                        __instance.ImpostorText.text = GetString("RulebookInfo");
                        break;

                    case CustomRoles.Veteran:
                    case CustomRoles.Knight:
                    case CustomRoles.KillingMachine:
                    case CustomRoles.Reverie:
                    case CustomRoles.NiceGuesser:
                    case CustomRoles.Vigilante:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = PlayerControl.LocalPlayer.KillSfx;
                        break;
                    case CustomRoles.Swooper:
                    case CustomRoles.Wraith:
                    case CustomRoles.Chameleon:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = PlayerControl.LocalPlayer.MyPhysics.ImpostorDiscoveredSound;
                        break;
                    case CustomRoles.Jinx:
                    case CustomRoles.Romantic:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = RoleManager.Instance.AllRoles.Find((Il2CppSystem.Predicate<RoleBehaviour>)((role) => role.Role == RoleTypes.GuardianAngel))?.UseSound;
                        break;
                    case CustomRoles.Illusionist:
                    case CustomRoles.MoonDancer:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = RoleManager.Instance.AllRoles.Find((Il2CppSystem.Predicate<RoleBehaviour>)((role) => role.Role == RoleTypes.Phantom))?.UseSound;
                        break;
                    case CustomRoles.Telecommunication:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = RoleManager.Instance.AllRoles.Find((Il2CppSystem.Predicate<RoleBehaviour>)((role) => role.Role == RoleTypes.Tracker))?.UseSound;
                        break;
                    case CustomRoles.Morphling:
                    case CustomRoles.Twister:
                        PlayerControl.LocalPlayer.Data.Role.IntroSound = RoleManager.Instance.AllRoles.Find((Il2CppSystem.Predicate<RoleBehaviour>)((role) => role.Role == RoleTypes.Shapeshifter))?.UseSound;
                        break;
                }

                if (PlayerControl.LocalPlayer.Is(CustomRoles.Lovers))
                {
                    __instance.TeamTitle.text = GetString("TeamLovers");
                    __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 154, 206, byte.MaxValue);
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = RoleManager.Instance.AllRoles.Find((Il2CppSystem.Predicate<RoleBehaviour>)((role) => role.Role == RoleTypes.GuardianAngel))?.UseSound;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("SubText.Lovers");
                }
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.Egoist))
                {
                    __instance.TeamTitle.text = GetString("TeamEgoist");
                    __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(86, 0, 255, byte.MaxValue);
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("SubText.Egoist");
                }
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate) || role.IsMadmate())
                {
                    __instance.TeamTitle.text = GetString("TeamMadmate");
                    __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("SubText.Madmate");
                }
                break;
        }

        // I hope no one notices this in code
        // unfortunately niko noticed while fixing others' shxt
        if (Input.GetKey(KeyCode.RightShift))
        {
            __instance.TeamTitle.text = "Damn!!";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "You Found The Secret Intro";
            __instance.TeamTitle.color = new Color32(186, 3, 175, byte.MaxValue);
            StartFadeIntro(__instance, Color.yellow, Color.cyan);
        }
        if (Input.GetKey(KeyCode.RightControl))
        {
            __instance.TeamTitle.text = "Warning!";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "Please stay away from all impostor based players";
            __instance.TeamTitle.color = new Color32(241, 187, 2, byte.MaxValue);
            StartFadeIntro(__instance, new Color32(241, 187, 2, byte.MaxValue), Color.red);
        }
    }
    public static AudioClip GetIntroSound(RoleTypes roleType)
    {
        return RoleManager.Instance.AllRoles.Find((Il2CppSystem.Predicate<RoleBehaviour>)((role) => role.Role == roleType))?.IntroSound;
    }
    private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end, int changeInterval = 20)
    {
        await Task.Delay(1000);
        int milliseconds = 0;
        while (true)
        {
            await Task.Delay(changeInterval);
            milliseconds += changeInterval;
            float time = milliseconds / (float)500;
            Color LerpingColor = Color.Lerp(start, end, time);
            if (__instance == null || milliseconds > 500)
            {
                Logger.Info("Terminates the loop", "StartFadeIntro");
                break;
            }
            __instance.BackgroundBar.material.color = LerpingColor;
        }
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
class BeginImpostorPatch
{
    public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        // Be careful while you are doing this part
        // This part occurs when local player got impostor base but may need to change to BeginCrewmate
        // or BeginCrewmate return false and call BeginImpostor
        // Do not make them call each other!

        var role = PlayerControl.LocalPlayer.GetCustomRole();

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Lovers)
            || PlayerControl.LocalPlayer.Is(CustomRoles.Egoist))
        {
            yourTeam = new();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.BeginCrewmate(yourTeam);
            return false;
        }
        // Madmate called from BeginCrewmate, need to skip previous Lovers and Egoist check here

        if (role.IsMadmate() || PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            yourTeam = new();
            yourTeam.Add(PlayerControl.LocalPlayer);

            if (role != CustomRoles.Parasite) // Parasite and Impostor doesnt know each other
            {
                // Crewpostor is counted as Madmate but should be a Impostor
                if (Madmate.MadmateKnowWhosImp.GetBool() || role != CustomRoles.Madmate)
                {
                    foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsImpostor() && x.PlayerId != PlayerControl.LocalPlayer.PlayerId))
                    {
                        yourTeam.Add(pc);
                    }
                }
                // Crewpostor is counted as Madmate but should be a Impostor
                if (Madmate.MadmateKnowWhosMadmate.GetBool() || role != CustomRoles.Madmate && Madmate.ImpKnowWhosMadmate.GetBool())
                {
                    foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Madmate) && x.PlayerId != PlayerControl.LocalPlayer.PlayerId))
                    {
                        yourTeam.Add(pc);
                    }
                }
            }

            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }

        if (role.IsCrewmate() && role.GetDYRole() == RoleTypes.Impostor)
        {
            yourTeam = new();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner)) yourTeam.Add(pc);
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = Palette.CrewmateBlue;
            return false;
        }

        if (role.IsNeutral())
        {
            yourTeam = new();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner)) yourTeam.Add(pc);
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = GetRoleColor(role);
            return false;
        }

        // We only check Impostor main role here!
        if (role.IsImpostor())
        {
            yourTeam = new();
            yourTeam.Add(PlayerControl.LocalPlayer);

            // Parasite and Impostor doesnt know each other
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => !x.AmOwner && !x.Is(CustomRoles.Parasite) && (x.GetCustomRole().IsImpostor() || !x.Is(CustomRoles.Madmate) && x.GetCustomRole().IsMadmate())))
            {
                yourTeam.Add(pc);
            }

            if (Madmate.ImpKnowWhosMadmate.GetBool())
            {
                foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Madmate)))
                {
                    yourTeam.Add(pc);
                }
            }

            __instance.overlayHandle.color = Palette.ImpostorRed;
        }

        if (role.IsCoven())
        {
            yourTeam = new();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner)) yourTeam.Add(pc);
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = new Color32(172, 66, 242, byte.MaxValue);
            return false;
        }

        BeginCrewmatePatch.Prefix(__instance, ref yourTeam);
        return true;
    }

    public static void Postfix(IntroCutscene __instance)
    {
        BeginCrewmatePatch.Postfix(__instance);
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
class IntroCutsceneDestroyPatch
{
    public static void Prefix()
    {
        if (AmongUsClient.Instance.AmHost && !AmongUsClient.Instance.IsGameOver)
        {
            // Host is Desync Role
            if (PlayerControl.LocalPlayer.HasDesyncRole())
            {
                PlayerControl.LocalPlayer.Data.Role.AffectedByLightAffectors = false;

                foreach (var target in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    // Set all players as killable players
                    target.Data.Role.CanBeKilled = true;

                    // When target is Impostor, set name color as white
                    target.cosmetics.SetNameColor(Color.white);
                    target.Data.Role.NameColor = Color.white;
                }
            }
            if (Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].IsNecromancer)
            {
                PlayerControl.LocalPlayer.Data.Role.AffectedByLightAffectors = false;

                foreach (var target in PlayerControl.AllPlayerControls.GetFastEnumerator().Where(x => !x.IsPlayerCoven()))
                {
                    // Set all Players as killable Players
                    target.Data.Role.CanBeKilled = true;

                    // When Target is Impostor, set name color as white
                    target.cosmetics.SetNameColor(Color.white);
                    target.Data.Role.NameColor = Color.white;
                }
            }
        }
    }
    public static void Postfix()
    {
        if (!GameStates.IsInGame) return;

        Main.IntroDestroyed = true;

        foreach (var pc in Main.AllPlayerControls)
        {
            // Set roleAssigned as false for override role for modded clients
            // For override role for vanilla clients we use "Data.Disconnected" while assign
            pc.roleAssigned = false;

            // Update name for all after intro
            Main.LowLoadUpdateName[pc.PlayerId] = true;
        }

        if (!GameStates.AirshipIsActive)
        {
            foreach (var state in Main.PlayerStates.Values)
            {
                state.HasSpawned = true;
            }
        }

        CustomRoleManager.Add();

        if (AmongUsClient.Instance.AmHost)
        {
            if (GameStates.IsNormalGame && !GameStates.AirshipIsActive)
            {
                foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
                {
                    pc.RpcResetAbilityCooldown();

                    if (Options.FixFirstKillCooldown.GetBool() && Options.CurrentGameMode != CustomGameMode.FFA && Options.CurrentGameMode != CustomGameMode.UltimateTeam && Options.CurrentGameMode != CustomGameMode.TrickorTreat)
                    {
                        _ = new LateTask(() =>
                        {
                            if (pc != null)
                            {
                                pc.ResetKillCooldown();

                                if (Main.AllPlayerKillCooldown.TryGetValue(pc.PlayerId, out var killTimer) && (killTimer - 2f) > 0f)
                                {
                                    pc.SetKillCooldown(Options.ChangeFirstKillCooldown.GetBool() ? Options.FixKillCooldownValue.GetFloat() - 2f : killTimer - 2f);
                                }
                            }
                        }, 2f, $"Fix Kill Cooldown Task for playerId {pc.PlayerId}");
                    }
                }
            }

            if (PlayerControl.LocalPlayer.Is(CustomRoles.GM)) // Incase user has /up access
            {
                PlayerControl.LocalPlayer.RpcExile();
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }
            else if (GhostRoleAssign.forceRole.Any())
            {
                // Needs to be delayed for the game to load it properly
                _ = new LateTask(() =>
                {
                    GhostRoleAssign.forceRole.Do(x =>
                    {
                        var plr = x.Key.GetPlayer();
                        plr.RpcExile();
                        Main.PlayerStates[x.Key].SetDead();

                    });
                }, 3f, "Set Dev Ghost-Roles");
            }
            foreach (byte spectator in ChatCommands.Spectators)
            {
                _ = new LateTask(() =>
                {
                    spectator.GetPlayer().RpcExileV2();
                    Main.PlayerStates[spectator].SetDead();
                }, spectator, $"Set Spectator Dead)");
            }

            bool chatVisible = Options.CurrentGameMode switch
            {
                CustomGameMode.FFA => FFAManager.ShowChatInGame.GetBool(),
                CustomGameMode.CandR => CopsAndRobbersManager.ShowChatInGame.GetBool(),
                CustomGameMode.UltimateTeam => UltimateTeam.ShowChatInGame.GetBool(),
                CustomGameMode.TrickorTreat => TrickorTreat.ShowChatInGame.GetBool(),
                _ => false
            };
            bool shouldAntiBlackOut = Options.CurrentGameMode switch
            {
                CustomGameMode.FFA => FFAManager.ShowChatInGame.GetBool(),
                CustomGameMode.CandR => CopsAndRobbersManager.ShowChatInGame.GetBool(),
                CustomGameMode.UltimateTeam => UltimateTeam.ShowChatInGame.GetBool(),
                CustomGameMode.TrickorTreat => TrickorTreat.ShowChatInGame.GetBool(),
                _ => false
            };
            try
            {
                if (chatVisible) Utils.SetChatVisibleForEveryone();
                if (shouldAntiBlackOut)
                {
                    _ = new LateTask(() =>
                    {
                        AntiBlackout.SetIsDead();
                        Logger.Warn("Set is dead", "IntroPatch");
                    }, 5f, "anti blackout");
                }
            }
            catch (Exception error)
            {
                Logger.Error($"Error: {error}", "Gamemode chat visible");
            }

            Utils.CheckAndSetVentInteractions();

            if (Main.CurrentServerIsVanilla && Options.BypassRateLimitAC.GetBool())
            {
                Main.Instance.StartCoroutine(Utils.NotifyEveryoneAsync());
            }
            else
            {
                Utils.DoNotifyRoles();
            }
        }

        try
        {
            if (!GameStates.IsEnded)
                DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
        }
        catch { }

        Logger.Info("OnDestroy", "IntroCutscene");
    }
}
