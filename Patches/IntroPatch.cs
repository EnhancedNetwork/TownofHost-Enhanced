using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TOHE.Modules;
using TOHE.Patches;
using TOHE.Roles.Core;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.CoShowIntro))]
class CoShowIntroPatch
{
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsModHost) return;

        _ = new LateTask(() =>
        {
            try
            {
                // Update name players
                Utils.DoNotifyRoles(NoCache: true);
            }
            catch (Exception ex)
            {
                Utils.ThrowException(ex);
            }
        }, 0.35f, "Do Notify Roles In Show Intro");
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
class SetUpRoleTextPatch
{
    public static bool IsInIntro = false;

    public static void Postfix(IntroCutscene __instance)
    {
        if (!GameStates.IsModHost) return;

        if (AmongUsClient.Instance.AmHost)
        {
            // After showing team for non-modded clients update player names.
            IsInIntro = false;
            Utils.DoNotifyRoles(NoCache: true);
        }

        _ = new LateTask(() =>
        {
            PlayerControl localPlayer = PlayerControl.LocalPlayer;
            CustomRoles role = localPlayer.GetCustomRole();
            if (Options.CurrentGameMode == CustomGameMode.FFA)
            {
                var color = ColorUtility.TryParseHtmlString("#00ffff", out var c) ? c : new(255, 255, 255, 255);
                __instance.YouAreText.transform.gameObject.SetActive(false);
                __instance.RoleText.text = "FREE FOR ALL";
                __instance.RoleText.color = color;
                __instance.RoleBlurbText.color = color;
                __instance.RoleBlurbText.text = "KILL EVERYONE TO WIN";
            }
            else 
            { 
                if (!role.IsVanilla())
                {
                    __instance.YouAreText.color = Utils.GetRoleColor(role);
                    __instance.RoleText.text = Utils.GetRoleName(role);
                    __instance.RoleText.color = Utils.GetRoleColor(role);
                    __instance.RoleBlurbText.color = Utils.GetRoleColor(role);
                    __instance.RoleBlurbText.text = localPlayer.GetRoleInfo();
                }

                foreach (var subRole in Main.PlayerStates[localPlayer.PlayerId].SubRoles.ToArray())
                    __instance.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(subRole), GetString($"{subRole}Info"));

                __instance.RoleText.text += Utils.GetSubRolesText(localPlayer.PlayerId, false, true);
            }
        }, 0.0001f, "Override Role Text");

        // Fixed bug where NotifyRoles works on modded clients during loading and it's name set as double
        // Run this code only for clients
        if (!AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                // Return if game is ended or player in lobby or player is null
                if (AmongUsClient.Instance.IsGameOver || GameStates.IsLobby || PlayerControl.LocalPlayer == null) return;

                var realName = Main.AllPlayerNames[PlayerControl.LocalPlayer.PlayerId];
                // Don't use RpcSetName because the modded client needs to set the name locally
                PlayerControl.LocalPlayer.SetName(realName);
            }, 1f, "Reset Name For Modded Client");
        }
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
class CoBeginPatch
{
    public static void Prefix(IntroCutscene __instance)
    {
        if (RoleBasisChanger.IsChangeInProgress) return;

        if (GameStates.IsNormalGame)
        {
            foreach (var player in Main.AllPlayerControls)
            {
                Main.PlayerStates[player.PlayerId].InitTask(player);
            }

            GameData.Instance.RecomputeTaskCounts();
            TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
        }

        __instance.StartCoroutine(CoLoggerGameInfo().WrapToIl2Cpp());

        GameStates.InGame = true;
        RPC.RpcVersionCheck();

        // Do not move this code, it should be executed at the very end to prevent a visual bug
        Utils.DoNotifyRoles(ForceLoop: true);

        if (AmongUsClient.Instance.AmHost && GameStates.IsHideNSeek && RandomSpawn.IsRandomSpawn())
        {
            RandomSpawn.SpawnMap map = Utils.GetActiveMapId() switch
            {
                0 => new RandomSpawn.SkeldSpawnMap(),
                1 => new RandomSpawn.MiraHQSpawnMap(),
                2 => new RandomSpawn.PolusSpawnMap(),
                3 => new RandomSpawn.DleksSpawnMap(),
                5 => new RandomSpawn.FungleSpawnMap(),
                _ => null,
            };
            if (map != null) Main.AllPlayerControls.Do(map.RandomTeleport);
        }
    }
    public static byte[] EncryptDES(byte[] data, string key)
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

        sb.Append("------------Player Names------------\n");
        foreach (var pc in allPlayerControlsArray)
        {
            if (pc == null) continue;
            sb.Append($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{Main.AllPlayerNames[pc.PlayerId].PadRightV2(20)}:{pc.cosmetics.nameText.text}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", "")})\n");
            pc.cosmetics.nameText.text = pc.name;
        }

        yield return null;

        sb.Append("------------Roles / Add-ons------------\n");
        if (PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug || GameStates.IsLocalGame)
        {
            foreach (var pc in allPlayerControlsArray)
            {
                if (pc == null) continue;
                sb.Append($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{Main.AllPlayerNames[pc.PlayerId].PadRightV2(20)}:{pc.GetAllRoleName().RemoveHtmlTags().Replace("\n", " + ")}\n");
            }
        }
        else
        {
            StringBuilder logStringBuilder = new();
            logStringBuilder.AppendLine("------------Roles / Add-ons------------");

            foreach (var pc in allPlayerControlsArray)
            {
                logStringBuilder.AppendLine($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc?.Data?.PlayerName?.PadRight(20)}:{pc.GetAllRoleName().RemoveHtmlTags()}");
            }

            try
            {
                byte[] logBytes = Encoding.UTF8.GetBytes(logStringBuilder.ToString());
                byte[] encryptedBytes = EncryptDES(logBytes, $"TOHE{PlayerControl.LocalPlayer.PlayerId}00000000"[..8]);
                string encryptedString = Convert.ToBase64String(encryptedBytes);
                sb.Append(encryptedString + "\n");
            }
            catch (Exception ex)
            {
                Logger.Error($"Encryption error: {ex.Message}", "Intro.Roles");
            }
        }
        //https://www.toolhelper.cn/SymmetricEncryption/DES
        //mode CBC, PKCS7, 64bit, Key = IV= "TOHE" + playerid + 000/00 "to 8 bits

        yield return null;

        sb.Append("------------Player Platforms------------\n");
        foreach (var pc in allPlayerControlsArray)
        {
            try
            {
                var text = new StringBuilder();
                sb.Append(pc.AmOwner ? "[*]" : "   ");
                sb.Append($"{pc.PlayerId,-2}:{pc.Data?.PlayerName?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", ""),-11}");

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

        Logger.Info(sb.ToString(), "GameInfo", multiLine: true);
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
class BeginCrewmatePatch
{
    public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        var role = PlayerControl.LocalPlayer.GetCustomRole();

        if (role.IsMadmate() || PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
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

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Executioner))
        {
            var exeTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            exeTeam.Add(PlayerControl.LocalPlayer);
            foreach (var execution in Executioner.Target.Values)
            {
                PlayerControl executing = Utils.GetPlayerById(execution);
                exeTeam.Add(executing);
            }
            teamToDisplay = exeTeam;
        }
        if (PlayerControl.LocalPlayer.Is(CustomRoles.Lawyer))
        {
            var lawyerTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            lawyerTeam.Add(PlayerControl.LocalPlayer);
            foreach (var help in Lawyer.Target.Values)
            {
                PlayerControl helping = Utils.GetPlayerById(help);
                lawyerTeam.Add(helping);
            }
            teamToDisplay = lawyerTeam;
        }
       
        return true;
    }
    public static void Postfix(IntroCutscene __instance)
    {
        CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();

        __instance.ImpostorText.gameObject.SetActive(false);

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
                __instance.TeamTitle.text = GetString("TeamNeutral");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(127, 140, 141, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Neutral");
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
            case CustomRoles.ShapeshifterTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                break;
            case CustomRoles.PhantomTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Phantom);
                break;
            case CustomRoles.TrackerTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Tracker);
                break;
            case CustomRoles.NoisemakerTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Noisemaker);
                break;
            case CustomRoles.EngineerTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Engineer);
                break;
            case CustomRoles.Doctor:
            case CustomRoles.Medic:
            case CustomRoles.ScientistTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Scientist);
                break;

            case CustomRoles.Terrorist:
            case CustomRoles.Bomber:
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

            case CustomRoles.Mechanic:
            case CustomRoles.Provocateur:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                break;

            case CustomRoles.GM:
                __instance.TeamTitle.text = Utils.GetRoleName(role);
                __instance.TeamTitle.color = Utils.GetRoleColor(role);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                __instance.ImpostorText.gameObject.SetActive(false);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                break;

            case CustomRoles.Sheriff:
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
        }

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate) || role.IsMadmate())
        {
            __instance.TeamTitle.text = GetString("TeamMadmate");
            __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = GetString("SubText.Madmate");
        }

        if (Options.CurrentGameMode == CustomGameMode.FFA)
        {
            __instance.TeamTitle.text = "FREE FOR ALL";
            __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(0, 255, 255, byte.MaxValue);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "KILL EVERYONE TO WIN";
        }

        if (Input.GetKey(KeyCode.RightShift))
        {
            __instance.TeamTitle.text = "明天就跑路啦";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "嘿嘿嘿嘿嘿嘿";
            __instance.TeamTitle.color = Color.cyan;
            StartFadeIntro(__instance, Color.cyan, Color.yellow);
        }
        if (Input.GetKey(KeyCode.RightControl))
        {
            __instance.TeamTitle.text = "警告";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "请远离无知的玩家";
            __instance.TeamTitle.color = Color.magenta;
            StartFadeIntro(__instance, Color.magenta, Color.magenta);
        }
    }
    public static AudioClip GetIntroSound(RoleTypes roleType)
    {
        return RoleManager.Instance.AllRoles.FirstOrDefault((role) => role.Role == roleType)?.IntroSound;
    }
    private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end)
    {
        await Task.Delay(1000);
        int milliseconds = 0;
        while (true)
        {
            await Task.Delay(20);
            milliseconds += 20;
            float time = milliseconds / (float)500;
            Color LerpingColor = Color.Lerp(start, end, time);
            if (__instance == null || milliseconds > 500)
            {
                Logger.Info("ループを終了します", "StartFadeIntro");
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
        var role = PlayerControl.LocalPlayer.GetCustomRole();
        
        if (role.IsMadmate() || PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            yourTeam = new();
            yourTeam.Add(PlayerControl.LocalPlayer);
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
            __instance.overlayHandle.color = new Color32(127, 140, 141, byte.MaxValue);
            return false;
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
    public static void Postfix()
    {
        if (!GameStates.IsInGame || RoleBasisChanger.SkipTasksAfterAssignRole) return;

        Main.IntroDestroyed = true;

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
            if (GameStates.IsNormalGame)
            {
                if (!GameStates.AirshipIsActive)
                {
                    Main.AllPlayerControls.Do(pc => pc.RpcResetAbilityCooldown());
                    if (Options.FixFirstKillCooldown.GetBool() && Options.CurrentGameMode != CustomGameMode.FFA)
                    {
                        _ = new LateTask(() =>
                        {
                            Main.AllPlayerControls.Do(x => x.ResetKillCooldown());
                            Main.AllPlayerControls.Where(x => (Main.AllPlayerKillCooldown[x.PlayerId] - 2f) > 0f).Do(pc => pc.SetKillCooldown(Options.FixKillCooldownValue.GetFloat() - 2f));
                        }, 2f, "Fix Kill Cooldown Task");
                    }
                }

                // Not entirely sure if this is really necessary
                //_ = new LateTask(() => Main.AllPlayerControls.Do(pc => pc.RpcSetRoleDesync(RoleTypes.Shapeshifter, false, -3)), 2f, "Set Impostor For Server");
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
                        var plr = Utils.GetPlayerById(x.Key);
                        plr.RpcExile();
                        Main.PlayerStates[x.Key].SetDead();

                    });
                }, 3f, "Set Dev Ghost-Roles");
            }

            bool chatVisible = Options.CurrentGameMode switch
            {
                CustomGameMode.FFA => true,
                _ => false
            };
            try
            {
                if (chatVisible) Utils.SetChatVisibleForEveryone();
            }
            catch (Exception error)
            {
                Logger.Error($"Error: {error}", "FFA chat visible");
            }

            if (Main.UnShapeShifter.Any())
            {
                _ = new LateTask(() =>
                {
                    Main.UnShapeShifter.Do(x =>
                    {
                        var PC = Utils.GetPlayerById(x);
                        var firstPlayer = Main.AllPlayerControls.FirstOrDefault(x => x != PC);
                        PC.RpcShapeshift(firstPlayer, false);
                        PC.RpcRejectShapeshift();
                        PC.ResetPlayerOutfit(force: true);
                        Main.CheckShapeshift[x] = false;
                    });
                    Main.GameIsLoaded = true;
                }, 3f, "Set UnShapeShift Button");
            }

            if (GameStates.IsNormalGame && (RandomSpawn.IsRandomSpawn() || Options.CurrentGameMode == CustomGameMode.FFA))
            {
                RandomSpawn.SpawnMap map = Utils.GetActiveMapId() switch
                {
                    0 => new RandomSpawn.SkeldSpawnMap(),
                    1 => new RandomSpawn.MiraHQSpawnMap(),
                    2 => new RandomSpawn.PolusSpawnMap(),
                    3 => new RandomSpawn.DleksSpawnMap(),
                    5 => new RandomSpawn.FungleSpawnMap(),
                    _ => null,
                };
                if (map != null) Main.AllPlayerControls.Do(map.RandomTeleport);
            }

            var amDesyncImpostor = PlayerControl.LocalPlayer.HasDesyncRole();
            if (amDesyncImpostor)
            {
                PlayerControl.LocalPlayer.Data.Role.AffectedByLightAffectors = false;
            }

            bool shouldPerformVentInteractions = false;
            foreach (var pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (pc.BlockVentInteraction())
                {
                    VentSystemDeterioratePatch.LastClosestVent[pc.PlayerId] = pc.GetVentsFromClosest()[0].Id;
                    shouldPerformVentInteractions = true;
                }
            }

            if (shouldPerformVentInteractions)
            {
                Utils.SetAllVentInteractions();
            }
        }
        Logger.Info("OnDestroy", "IntroCutscene");
    }
}
 