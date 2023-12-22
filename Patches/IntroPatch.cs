using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Linq;
using System.Threading.Tasks;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
class SetUpRoleTextPatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        if (!GameStates.IsModHost) return;

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

                if (!localPlayer.Is(CustomRoles.Lovers) && !localPlayer.Is(CustomRoles.Ntr) && CustomRoles.Ntr.RoleExist())
                    __instance.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), GetString($"{CustomRoles.Lovers}Info"));

                __instance.RoleText.text += Utils.GetSubRolesText(localPlayer.PlayerId, false, true);
            }
        }, 0.0001f, "Override Role Text");
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
class CoBeginPatch
{
    public static void Prefix()
    {
        var logger = Logger.Handler("Info");

        var allPlayerControlsArray = Main.AllPlayerControls.ToArray();

        logger.Info("------------Player Names------------");
        foreach ( var pc in allPlayerControlsArray)
        {
            logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc.name.PadRightV2(20)}:{pc.cosmetics.nameText.text}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", "")})");
            pc.cosmetics.nameText.text = pc.name;
        }


        logger.Info("------------Roles / Add-ons------------");
        foreach (var pc in allPlayerControlsArray)
        {
            logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc?.Data?.PlayerName?.PadRightV2(20)}:{pc.GetAllRoleName().RemoveHtmlTags()}");
        }


        logger.Info("------------Player Platforms------------");
        foreach (var pc in allPlayerControlsArray)
        {
            try
            {
                var text = pc.AmOwner ? "[*]" : "   ";
                text += $"{pc.PlayerId,-2}:{pc.Data?.PlayerName?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", ""),-11}";

                if (Main.playerVersion.TryGetValue(pc.PlayerId, out PlayerVersion pv))
                {
                    text += $":Mod({pv.forkId}/{pv.version}:{pv.tag})";
                }
                else
                {
                    text += ":Vanilla";
                }
                logger.Info(text);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Platform");
            }
        }

        logger.Info("------------Vanilla Settings------------");
        var tmp = GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1).ToArray();
        foreach (var text in tmp)
        {
            logger.Info(text);
        }


        logger.Info("------------Mod Settings------------");
        var allOptionsArray = OptionItem.AllOptions.ToArray();
        foreach (var option in allOptionsArray)
        {
            if (!option.IsHiddenOn(Options.CurrentGameMode) && (option.Parent == null ? !option.GetString().Equals("0%") : option.Parent.GetBool()))
            {
                logger.Info($"{(option.Parent == null
                    ? option.GetName(true, true).RemoveHtmlTags().PadRightV2(40)
                    : $"┗ {option.GetName(true, true).RemoveHtmlTags()}".PadRightV2(41))}:{option.GetString().RemoveHtmlTags()}");
            }
        }

        logger.Info("-------------Other Information-------------");
        logger.Info($"Number players: {allPlayerControlsArray.Length}");
        foreach (var player in allPlayerControlsArray)
        {
            Main.PlayerStates[player.PlayerId].InitTask(player);
        }

        GameData.Instance.RecomputeTaskCounts();
        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;

        GameStates.InGame = true;

        // Do not move this code, it should be executed at the very end to prevent a visual bug
        Utils.DoNotifyRoles(ForceLoop: true);
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
class BeginCrewmatePatch
{
    public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral) && !PlayerControl.LocalPlayer.Is(CustomRoles.Parasite))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
            //__instance.BeginImpostor(teamToDisplay);
            //__instance.overlayHandle.color = new Color32(127, 140, 141, byte.MaxValue);
        }
        if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral) && !PlayerControl.LocalPlayer.Is(CustomRoles.Crewpostor))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
            //__instance.BeginImpostor(teamToDisplay);
            //__instance.overlayHandle.color = new Color32(127, 140, 141, byte.MaxValue);
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
            __instance.BeginImpostor(teamToDisplay);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Crewpostor))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
            //__instance.BeginImpostor(teamToDisplay);
            //__instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Parasite))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
            //__instance.BeginImpostor(teamToDisplay);
            //__instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
        else if (PlayerControl.LocalPlayer.GetCustomRole().IsMadmate())
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
            //__instance.BeginImpostor(teamToDisplay);
            //__instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
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
        if (PlayerControl.LocalPlayer.Is(CustomRoles.NSerialKiller))
        {
            var serialkillerTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            serialkillerTeam.Add(PlayerControl.LocalPlayer);
            foreach (var ar in PlayerControl.AllPlayerControls)
            {
                if (ar.Is(CustomRoles.NSerialKiller) && ar != PlayerControl.LocalPlayer)
                    serialkillerTeam.Add(ar);
            }
            teamToDisplay = serialkillerTeam;
        }
       
        return true;
    }
    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        //チーム表示変更
        CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();

        __instance.ImpostorText.gameObject.SetActive(false);
        switch (role.GetCustomRoleTypes())
        {
            case CustomRoleTypes.Impostor:
                __instance.TeamTitle.text = GetString("TeamImpostor");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Impostor");
                break;
            case CustomRoleTypes.Crewmate:
                __instance.TeamTitle.text = GetString("TeamCrewmate");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(140, 255, 255, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Crewmate");
                break;
            case CustomRoleTypes.Neutral:
                __instance.TeamTitle.text = GetString("TeamNeutral");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(127, 140, 141, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Neutral");
                break;
        }
        switch (role)
        {
            case CustomRoles.Terrorist:
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
            case CustomRoles.FFF:
            case CustomRoles.Revolutionist:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                break;

            case CustomRoles.EngineerTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Engineer);
                break;

            case CustomRoles.SabotageMaster:
            case CustomRoles.Provocateur:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                break;

            case CustomRoles.Doctor:
            case CustomRoles.Medic:
            case CustomRoles.ScientistTOHE:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Scientist);
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
            case CustomRoles.SwordsMan:
            case CustomRoles.Minimalism:
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
        /*    case CustomRoles.ParityCop:
            case CustomRoles.Mediumshiper:
            case CustomRoles.Mayor:
            case CustomRoles.Dictator:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = HudManager.Instance.Chat.messageSound;
                break; */
        }

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            __instance.TeamTitle.text = GetString("TeamMadmate");
            __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Madmate");
        }

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Parasite))
        {
            __instance.TeamTitle.text = GetString("TeamMadmate");
            __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SubText.Madmate");
        }

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Crewpostor))
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
        return RoleManager.Instance.AllRoles.Where((role) => role.Role == roleType).FirstOrDefault().IntroSound;
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
        if (role is CustomRoles.Crewpostor)
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Parasite))
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Crewpostor))
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        else if (role is CustomRoles.Vigilante or CustomRoles.Sheriff or CustomRoles.Jailer or CustomRoles.Investigator or CustomRoles.SwordsMan or CustomRoles.Medic or CustomRoles.Counterfeiter or CustomRoles.Witness or CustomRoles.Monarch or CustomRoles.Farseer or CustomRoles.Reverie or CustomRoles.Admirer or CustomRoles.Deputy or CustomRoles.Crusader or CustomRoles.CopyCat)
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner).ToArray())
            {
                yourTeam.Add(pc);
            }
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = Palette.CrewmateBlue;
            return false;
        }
        else if (role is CustomRoles.Romantic or CustomRoles.Doppelganger or CustomRoles.Pyromaniac or CustomRoles.Huntsman or CustomRoles.RuthlessRomantic or CustomRoles.VengefulRomantic or CustomRoles.NSerialKiller or CustomRoles.Jackal or CustomRoles.Seeker or CustomRoles.Pixie or CustomRoles.Agitater or CustomRoles.CursedSoul or CustomRoles.Pirate or CustomRoles.Amnesiac or CustomRoles.Arsonist or CustomRoles.Sidekick or CustomRoles.Innocent or CustomRoles.Pelican or CustomRoles.Pursuer or CustomRoles.Revolutionist or CustomRoles.FFF or CustomRoles.Gamer or CustomRoles.Glitch or CustomRoles.Juggernaut or CustomRoles.DarkHide or CustomRoles.Provocateur or CustomRoles.BloodKnight or CustomRoles.NSerialKiller or CustomRoles.Werewolf or CustomRoles.Maverick or CustomRoles.NWitch or CustomRoles.Shroud or CustomRoles.Totocalcio or CustomRoles.Succubus or CustomRoles.Pelican or CustomRoles.Infectious or CustomRoles.Virus or CustomRoles.Pickpocket or CustomRoles.Traitor or CustomRoles.PlagueBearer or CustomRoles.Pestilence or CustomRoles.Spiritcaller or CustomRoles.Necromancer or CustomRoles.Medusa or CustomRoles.HexMaster or CustomRoles.Wraith or CustomRoles.Jinx or CustomRoles.Poisoner or CustomRoles.PotionMaster) //or CustomRoles.Occultist 
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner).ToArray())
            {
                yourTeam.Add(pc);
            }
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = new Color32(127, 140, 141, byte.MaxValue);
            return false;
        }
        BeginCrewmatePatch.Prefix(__instance, ref yourTeam);
        return true;
    }
    public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
class IntroCutsceneDestroyPatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        if (!GameStates.IsInGame) return;
        Main.introDestroyed = true;
        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.NormalOptions.MapId != 4)
            {
                Main.AllPlayerControls.Do(pc => pc.RpcResetAbilityCooldown());
                if (Options.FixFirstKillCooldown.GetBool() && Options.CurrentGameMode != CustomGameMode.FFA)
                    _ = new LateTask(() =>
                    {
                        Main.AllPlayerControls.Do(x => x.ResetKillCooldown());
                        Main.AllPlayerControls.Where(x => (Main.AllPlayerKillCooldown[x.PlayerId] - 2f) > 0f).Do(pc => pc.SetKillCooldown(Options.FixKillCooldownValue.GetFloat() - 2f));
                    }, 2f, "Fix Kill Cooldown Task");
            }

            _ = new LateTask(() => Main.AllPlayerControls.Do(pc => pc.RpcSetRoleDesync(RoleTypes.Shapeshifter, -3)), 2f, "Set Impostor For Server");
            
            if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
            {
                PlayerControl.LocalPlayer.RpcExile();
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            if (Options.RandomSpawn.GetBool() || Options.CurrentGameMode == CustomGameMode.FFA)
            {
                RandomSpawn.SpawnMap map;
                switch (Main.NormalOptions.MapId)
                {
                    case 0:
                        map = new RandomSpawn.SkeldSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 1:
                        map = new RandomSpawn.MiraHQSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 2:
                        map = new RandomSpawn.PolusSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 3:
                        map = new RandomSpawn.DleksSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 5:
                        map = new RandomSpawn.FungleSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                }
            }

            var amDesyncImpostor = Main.ResetCamPlayerList.Contains(PlayerControl.LocalPlayer.PlayerId);
            if (amDesyncImpostor)
            {
                PlayerControl.LocalPlayer.Data.Role.AffectedByLightAffectors = false;
            }
        }
        Logger.Info("OnDestroy", "IntroCutscene");
    }
}
 