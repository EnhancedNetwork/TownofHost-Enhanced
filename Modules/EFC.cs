using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

// Enhanced File Checker.
internal class EFC
{
    private static bool EFCEnabled = true;
    private static readonly ReadOnlyCollection<string> BannedBepInExMods = new(new List<string> { "MalumMenu", "AUnlocker" }); // Put BepInEx BepInPlugin name, not dll name here lol.
    private static readonly ReadOnlyCollection<string> KeyWordsInVersionInfo = new(new List<string> { "Malum", "Sicko" }); // Banned words for version text
    public static string UnauthorizedReason = string.Empty;
    public static List<string> CheatTags = []; // For API report
    public static bool HasTrySpoofFriendCode = false;
    public static bool HasUnauthorizedFile = false;
    public static bool HasShownPopUp = false;

    // Set up if unauthorized files have been found.
    public static void UpdateUnauthorizedFiles()
    {
        if (EFCEnabled == false) return;

        GameObject PlayButton = GameObject.Find("Main Buttons/PlayButton");

        // Disable play button until EAC information is gathered from API
        if (PlayButton != null)
        {
            if (BanManager.EACDict.Count < 1 || HasTrySpoofFriendCode)
            {
                PlayButton.GetComponent<UnityEngine.BoxCollider2D>().enabled = false;
                GameObject.Find("Main Buttons/PlayButton/Inactive").GetComponent<SpriteRenderer>().color = Color.gray;
            }
            else
            {
                PlayButton.GetComponent<UnityEngine.BoxCollider2D>().enabled = true;
                GameObject.Find("Main Buttons/PlayButton/Inactive").GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f);
            }
        }

        // Skip check if player is a dev for testing... I promise (- ‿◦ )
        if (DevManager.GetDevUser(EOSManager.Instance?.friendCode).IsDev 
            || DevManager.GetDevUser(PlayerControl.LocalPlayer?.FriendCode).IsDev) return;

        // Unauthorized file or ban detected.
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

            GameObject SignInStatus = GameObject.Find("SignInStatus");

            if (SignInStatus != null)
            {
                SignInStatusComponent SignInStatusCom = SignInStatus.GetComponent<SignInStatusComponent>();
                SignInStatusCom?.SetOffline();

                GameObject.Find("Account_CTA")?.SetActive(false);
                GameObject.Find("AccountTab/GameHeader/LeftSide/FriendCode")?.SetActive(false);
                if (GameObject.Find("Stats_CTA") != null) GameObject.Find("Stats_CTA").transform.position = new Vector2(1.7741f, -0.2442f);
            }

            if (GameStates.IsOnlineGame && AmongUsClient.Instance.mode != InnerNet.MatchMakerModes.None)
            {
                DisconnectPlayer();
            }

            GameObject.Find("SplashArt")?.SetActive(false);
            SoundManager.instance?.ChangeMusicVolume(0);
            return;
        }

        if (EOSManager.Instance.editAccountUsername.gameObject.active || EOSManager.Instance.askToMergeAccount.gameObject.active)
        {
            HasTrySpoofFriendCode = true;
        }

        if (GameStates.IsOnlineGame && AmongUsClient.Instance.mode != InnerNet.MatchMakerModes.None)
        {
            if (BanManager.CheckEACList(PlayerControl.LocalPlayer?.FriendCode, PlayerControl.LocalPlayer?.GetClient().GetHashedPuid()) || HasTrySpoofFriendCode)
            {
                DisconnectPlayer();
            }
        }
    }

    // Check if there's any unauthorized files.
    public static bool CheckIfUnauthorizedFiles()
    {
        if (EFCEnabled == false) return false;

        // Skip check if player is a dev for testing... I promise (- ‿◦ )
        if (DevManager.GetDevUser(EOSManager.Instance?.friendCode).IsDev
            || DevManager.GetDevUser(PlayerControl.LocalPlayer?.FriendCode).IsDev)
        {
            EFCEnabled = false;
            return false;
        }

        // Get user info for later use with API.
        string ClientUserName = GameObject.Find("AccountTab")?.GetComponent<AccountTab>()?.userName.text;
        string ClientFriendCode = EOSManager.Instance.friendCode;
        string ClientPUIDHash = GetHashedPuidFromPuid(EOSManager.Instance.ProductUserId);

        // Check EAC list
        foreach (var user in BanManager.EACDict)
        {
            if ((user["friendcode"].ToString().ToLower().Trim() == ClientFriendCode.ToLower().Trim())
                || (user["hashPUID"].ToString().ToLower().Trim() == ClientPUIDHash.ToLower().Trim()))
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
            if (UnityEngine.Object.FindFirstObjectByType<VersionShower>().text.text.ToLower().Contains(WordInVersionInfo.ToLower()))
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
            File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-prev-log.txt")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-config")))
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

    // Get hashed puid from puid.
    public static string GetHashedPuidFromPuid(string puid)
    {
        using SHA256 sha256 = SHA256.Create();

        // get sha-256 hash
        byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(puid));
        string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();

        // pick front 5 and last 4
        return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
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
