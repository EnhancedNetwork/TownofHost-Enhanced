using AmongUs.Data;
using System;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

// Enhanced File Checker.
internal class EFC
{
    private static readonly ReadOnlyCollection<string> BannedBepInExMods = new(new List<string> { "MalumMenu", "AUnlocker" }); // Put BepInEx BepInPlugin name, not dll name lol.
    private static readonly ReadOnlyCollection<string> KeyWordsInVersionInfo = new(new List<string> { "Malum", "Sicko" });
    public static string UnauthorizedReason = string.Empty;
    public static List<string> CheatTags = []; // For API report
    public static bool HasUnauthorizedFile = false;
    public static bool HasShownPopUp = false;
    public static string ClientPUIDHash = string.Empty;

    // Set up if unauthorized files have been found.
    public static void UpdateUnauthorizedFiles()
    {
        if (HasUnauthorizedFile)
        {
            GameObject playOnlineButton = GameObject.Find("PlayOnlineButton");

            if (playOnlineButton != null)
            {
                PassiveButton PassiveButtonComponent = playOnlineButton.GetComponent<PassiveButton>();
                PlayOnlineButtonSprite PlayOnlineButtonSpriteComponent = playOnlineButton.GetComponent<PlayOnlineButtonSprite>();

                if (PassiveButtonComponent != null)
                {
                    if (PassiveButtonComponent != null)
                        PassiveButtonComponent.enabled = false;

                    PlayOnlineButtonSpriteComponent?.SetGreyscale();
                }
            }

            GameObject.Find("Account_CTA")?.SetActive(false);
            GameObject.Find("AccountTab/GameHeader/LeftSide/FriendCode")?.SetActive(false);
            if (GameObject.Find("Stats_CTA") != null) GameObject.Find("Stats_CTA").transform.position = new Vector2(1.7741f, -0.2442f);

            if (AmongUsClient.Instance.mode != InnerNet.MatchMakerModes.None)
            {
                DisconnectPlayer();
            }

            DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.Offline;
            DataManager.Player.Save();

            GameObject.Find("SplashArt")?.SetActive(false);
            SoundManager.instance.ChangeMusicVolume(0);
            return;
        }

        if (BanManager.CheckEACList(PlayerControl.LocalPlayer.FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid()) && GameStates.IsOnlineGame && AmongUsClient.Instance.mode != InnerNet.MatchMakerModes.None)
        {
            DisconnectPlayer();
        }
    }

    // Check if there's any unauthorized files.
    public static bool CheckIfUnauthorizedFiles()
    {
        // Skip check if player is a dev for testing... I promise (- ‿◦ )
        // if (DevManager.GetDevUser(EOSManager.Instance?.friendCode).IsDev) return false;

        // Get user info for later use with API.
        string ClientUserName = GameObject.Find("AccountTab")?.GetComponent<AccountTab>()?.userName.text;
        string ClientFriendCode = EOSManager.Instance.friendCode;

        // Check EAC list
        foreach (var user in BanManager.EACDict)
        {
            if ((user["friendcode"].ToString().ToLower().Trim() == ClientFriendCode.ToLower().Trim())
                || (user["hashPUID"].ToString().ToLower().Trim() == ClientPUIDHash.ToLower().Trim())) //
            {
                if (!HasUnauthorizedFile) Logger.Warn($"{ClientFriendCode}, {ClientPUIDHash} banned by EAC reason : {user["reason"]}", "EFC - CheckEACList");
                if (!HasUnauthorizedFile) UnauthorizedReason = GetString("EFC.EACDetected");
                if (!CheatTags.Contains("EAC-List")) CheatTags.Add("EAC-List");
                HasUnauthorizedFile = true;
            }
        }

        // Check for Banned BepInEx Mods
        foreach (var bannedMod in BannedBepInExMods)
        {
            if (IsBepInExModLoaded(bannedMod))
            {
                if (!HasUnauthorizedFile) Logger.Warn($"{bannedMod} BepInEx Mod detected, disabling online play!", "EFC");
                if (!HasUnauthorizedFile) UnauthorizedReason = GetString("EFC.BepInExMod");
                if (!CheatTags.Contains($"{bannedMod}-BepInEx")) CheatTags.Add($"{bannedMod}-BepInEx");
                HasUnauthorizedFile = true;
            }
        }

        // Check for version.dll
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "version.dll")))
        {
            string versiondll = "<color=#ffffff>'</color><color=#ffca2b>version.dll</color><color=#ffffff>'</color>";
            if (!HasUnauthorizedFile) Logger.Warn("version.dll detected, disabling online play!", "EFC");
            if (!HasUnauthorizedFile) UnauthorizedReason = string.Format(GetString("EFC.UnauthorizedTextDetected"), versiondll);
            if (!CheatTags.Contains("version.dll")) CheatTags.Add("version.dll");
            HasUnauthorizedFile = true;
        }

        // Check for banned words in VersionInfo display. Aka check cheat developers ego
        foreach (var WordInVersionInfo in KeyWordsInVersionInfo)
        {
            if (UnityEngine.Object.FindFirstObjectByType<VersionShower>().text.text.Contains(WordInVersionInfo))
            {
                if (!HasUnauthorizedFile) Logger.Warn($"{WordInVersionInfo} VersionInfo in  detected, disabling online play!", "EFC");
                if (!HasUnauthorizedFile) UnauthorizedReason = GetString("EFC.UnauthorizedFileMsg");
                if (!CheatTags.Contains($"{WordInVersionInfo}-VersionInfo")) CheatTags.Add($"{WordInVersionInfo}-VersionInfo");
                HasUnauthorizedFile = true;
            }
        }

        // Check for Sicko leftover files
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-settings.json")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-log.txt")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-prev-log.txt")))
        {
            if (!HasUnauthorizedFile) Logger.Warn("Sicko files detected, disabling online play!", "EFC");
            if (!HasUnauthorizedFile) UnauthorizedReason = GetString("EFC.UnauthorizedFileMsg");
            if (!CheatTags.Contains("Sicko-Menu-Files")) CheatTags.Add("Sicko-Menu-Files");
            HasUnauthorizedFile = true;
        }

        // Check for AUM leftover files
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "settings.json")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "aum-log.txt")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "aum-prev-log.txt")))
        {
            if (!HasUnauthorizedFile) Logger.Warn("AUM files detected, disabling online play!", "EFC");
            if (!HasUnauthorizedFile) UnauthorizedReason = GetString("EFC.UnauthorizedFileMsg");
            if (!CheatTags.Contains("AUM-Menu-Files")) CheatTags.Add("AUM-Menu-Files");
            HasUnauthorizedFile = true;
        }

        // ----------- Unused Until API support is added! -----------

        // Combine Player information and Tags
        string tagsAsString = string.Join(" - ", CheatTags);
        string playerInfo = $"{ClientUserName}.{ClientFriendCode}.{ClientPUIDHash} - {tagsAsString}"; // If Detection goes off send this information to the API database!
        Logger.Test(playerInfo);

        // ----------------------------------------------------------

        return HasUnauthorizedFile;
    }

    private static void DisconnectPlayer()
    {
        AmongUsClient.Instance.ExitGame(0);
        _ = new LateTask(() =>
        {
            SceneChanger.ChangeScene("MainMenu");
            _ = new LateTask(() =>
            {
                var lines = "<color=#ebbd34>----------------------------------------------------------------------------------------------</color>";
                DisconnectPopup.Instance._textArea.enableWordWrapping = false;
                DisconnectPopup.Instance.ShowCustom($"{lines}\n\n\n<size=150%>{GetString("EFC.OnlineMsg")}</size>\n\n\n{lines}");
            }, 0.1f);
        }, 0.2f);
    }

    // Get all loaded BepInEx mods and check if one is on the ban list.
    private static bool IsBepInExModLoaded(string modName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.FullName.Contains(modName))
            {
                return true;
            }
        }
        return false;
    }
}
