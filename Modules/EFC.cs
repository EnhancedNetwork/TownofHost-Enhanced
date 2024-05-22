using System;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;

namespace TOHE
{
    // Enhanced File Checker.
    internal class EFC
    {
        private static readonly ReadOnlyCollection<string> BannedBepInExMods = new(new List<string> { "MalumMenu", "AUnlocker" });
        public static string UnauthorizedReason = string.Empty;
        public static List<string> CheatTags = []; // For API report
        public static bool HasUnauthorizedFile = false;
        public static bool HasShownPopUp = false;

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

                GameObject SignInStatus = GameObject.Find("SignInStatus");

                if (SignInStatus != null)
                {
                    SignInStatusComponent SignInStatusCom = SignInStatus.GetComponent<SignInStatusComponent>();
                    SignInStatusCom?.SetOffline();

                    GameObject.Find("Account_CTA")?.SetActive(false);
                    GameObject.Find("AccountTab/GameHeader/LeftSide/FriendCode")?.SetActive(false);
                    if (GameObject.Find("Stats_CTA") != null) GameObject.Find("Stats_CTA").transform.position = new Vector2(1.7741f, -0.2442f);
                }

                GameObject.Find("SplashArt")?.SetActive(false);
                SoundManager.instance.ChangeMusicVolume(0);
            }
        }

        // Check if there's any unauthorized files.
        public static bool CheckIfUnauthorizedFiles()
        {
            // Skip check if player is a dev for testing... I promise (- ‿◦ )
            if (DevManager.GetDevUser(EOSManager.Instance?.friendCode).IsDev) return false;

            // Get user info for later use with API.
            string ClientName = GameObject.Find("AccountTab")?.GetComponent<AccountTab>()?.userName.text;
            string ClientFriendCode = EOSManager.Instance.friendCode;
            string ClientPUID = EOSManager.Instance.ProductUserId;

            // Check for Banned BepInEx Mods
            foreach (var bannedMod in BannedBepInExMods)
            {
                if (IsBepInExModLoaded(bannedMod))
                {
                    if (!HasUnauthorizedFile) Logger.Warn($"{bannedMod} BepInEx Mod detected, disabling online play!", "UFC");
                    if (!HasUnauthorizedFile) UnauthorizedReason = "Unauthorized BepInEx Mod Detected";
                    if (!CheatTags.Contains($"{bannedMod}-BepInEx")) CheatTags.Add($"{bannedMod}-BepInEx");
                    HasUnauthorizedFile = true;
                }
            }

            // Check for version.dll
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "version.dll")))
            {
                if (!HasUnauthorizedFile) Logger.Warn("version.dll detected, disabling online play!", "UFC");
                if (!HasUnauthorizedFile) UnauthorizedReason = "Unauthorized <color=#ffffff>'</color><color=#ffca2b>version.dll</color><color=#ffffff>'</color> Detected";
                if (!CheatTags.Contains("version.dll")) CheatTags.Add("version.dll");
                HasUnauthorizedFile = true;
            }

            // Check for Sicko
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-settings.json")) ||
                File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-log.txt")) ||
                File.Exists(Path.Combine(Environment.CurrentDirectory, "sicko-prev-log.txt")))
            {
                if (!HasUnauthorizedFile) Logger.Warn("Sicko files detected, disabling online play!", "UFC");
                if (!HasUnauthorizedFile) UnauthorizedReason = "Unauthorized Game Files Detected";
                if (!CheatTags.Contains("Sicko-Menu")) CheatTags.Add("Sicko-Menu");
                HasUnauthorizedFile = true;
            }

            // Check for AUM
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "settings.json")) ||
                File.Exists(Path.Combine(Environment.CurrentDirectory, "aum-log.txt")) ||
                File.Exists(Path.Combine(Environment.CurrentDirectory, "aum-prev-log.txt")))
            {
                if (!HasUnauthorizedFile) Logger.Warn("AUM files detected, disabling online play!", "UFC");
                if (!HasUnauthorizedFile) UnauthorizedReason = "Unauthorized Game Files Detected";
                if (!CheatTags.Contains("AUM-Menu")) CheatTags.Add("AUM-Menu");
                HasUnauthorizedFile = true;
            }

            // Obfuscate player info
            string tagsAsString = string.Join(" - ", CheatTags);
            string ObfuscatedInfo = Obfuscator($"{ClientName}.{ClientFriendCode}.{ClientPUID} - {tagsAsString}");

            Logger.Test(ObfuscatedInfo);

            return HasUnauthorizedFile;
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

        // Obfuscate info that gets set to the API.
        public static string Obfuscator(string i)
        {
            if (string.IsNullOrEmpty(i)) return i;
            var f = i.ToCharArray();
            Array.Reverse(f);
            var s = new char[f.Length];
            for (int j = 0; j < f.Length; j += 2)
            {
                s[j] = f[Math.Min(j + 1, f.Length - 1)];
                if (j + 1 < f.Length) s[j + 1] = f[j];
            }
            for (int j = 0; j < s.Length; j++) s[j] = (char)(s[j] + 5 + 5 + 5 + 55 + 504);
            return new string(s);
        }
    }
}
