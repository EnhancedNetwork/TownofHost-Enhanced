using HarmonyLib;
using InnerNet;
using System.Collections.Generic;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
internal class MakePublicPatch
{
    public static bool Prefix(GameStartManager __instance)
    {
        // 定数設定による公開ルームブロック
        if (!Main.AllowPublicRoom)
        {
            var message = GetString("DisabledByProgram");
            Logger.Info(message, "MakePublicPatch");
            Logger.SendInGame(message);
            return false;
        }
        if (ModUpdater.isBroken || (ModUpdater.hasUpdate && ModUpdater.forceUpdate) || !VersionChecker.IsSupported)
        {
            var message = "";
            if (!VersionChecker.IsSupported) message = GetString("UnsupportedVersion");
            if (ModUpdater.isBroken) message = GetString("ModBrokenMessage");
            if (ModUpdater.hasUpdate) message = GetString("CanNotJoinPublicRoomNoLatest");
            Logger.Info(message, "MakePublicPatch");
            Logger.SendInGame(message);
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(MMOnlineManager), nameof(MMOnlineManager.Start))]
internal class MMOnlineManagerStartPatch
{
    public static void Postfix(MMOnlineManager __instance)
    {
        if (!((ModUpdater.hasUpdate && ModUpdater.forceUpdate) || ModUpdater.isBroken || !VersionChecker.IsSupported)) return;
        var obj = GameObject.Find("FindGameButton");
        if (obj)
        {
            obj?.SetActive(false);
            var parentObj = obj.transform.parent.gameObject;
            var textObj = Object.Instantiate(obj.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>());
            textObj.transform.position = new Vector3(1f, -0.3f, 0);
            textObj.name = "CanNotJoinPublic";

            string message = "";
            if (!VersionChecker.IsSupported)
            {
                message = GetString("UnsupportedVersion");
            }
            else if (ModUpdater.isBroken)
            {
                message = GetString("ModBrokenMessage");
            }
            else if (ModUpdater.hasUpdate)
            {
                message = GetString("CanNotJoinPublicRoomNoLatest");
            }
            _ = new LateTask(() => { textObj.text = $"<size=2>{Utils.ColorString(Color.red, message)}</size>"; }, 0.01f, "Can Not Join Public");
        }
    }
}
[HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
internal class SplashLogoAnimatorPatch
{
    public static void Prefix(SplashManager __instance)
    {
        if (DebugModeManager.AmDebugger)
        {
            __instance.sceneChanger.AllowFinishLoadingScene();
            __instance.startedSceneLoad = true;
        }
    }
}
[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
internal class RunLoginPatch
{
    public static int ClickCount = 0;
    public static int checkCount = 0;
    public static bool isAllowedOnline = false;
    public static void Prefix(ref bool canOnline)
    {
#if DEBUG
        if (ClickCount < 10) canOnline = true;
        if (ClickCount >= 10) ModUpdater.forceUpdate = false;
#endif
    }

    public static void Postfix(ref bool canOnline)
    {
        isAllowedOnline = canOnline;
        var friendcode = EOSManager.Instance.friendCode;
        if (friendcode == null || friendcode == "")
        {
            Logger.Info("friendcode not found", "EOSManager");
            canOnline = false;
        }

        else if (Main.devRelease && !dbConnect.CanAccessDev(friendcode))
        {
            Main.hasAccess = false;
            Logger.Warn("Banned because no access to dev", "dbConnect");
        }
    }
}
[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
internal class BanMenuSetVisiblePatch
{
    public static bool Prefix(BanMenu __instance, bool show)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;
        __instance.BanButton.gameObject.SetActive(AmongUsClient.Instance.CanBan());
        __instance.KickButton.gameObject.SetActive(AmongUsClient.Instance.CanKick());
        __instance.MenuButton.gameObject.SetActive(show);
        return false;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNet.InnerNetClient.CanBan))]
internal class InnerNetClientCanBanPatch
{
    public static bool Prefix(InnerNet.InnerNetClient __instance, ref bool __result)
    {
        __result = __instance.AmHost;
        return false;
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.KickPlayer))]
internal class KickPlayerPatch
{
    public static Dictionary<string, int> AttemptedKickPlayerList = new();
    public static bool Prefix(InnerNet.InnerNetClient __instance, int clientId, bool ban)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        var HashedPuid = AmongUsClient.Instance.GetClient(clientId).GetHashedPuid();
        if (!AttemptedKickPlayerList.ContainsKey(HashedPuid))
            AttemptedKickPlayerList.Add(HashedPuid, 0);
        else if (AttemptedKickPlayerList[HashedPuid] < 15)
        {
            Logger.Fatal($"Kick player Request too fast! Canceled.", "KickPlayerPatch");
            AttemptedKickPlayerList[HashedPuid] = 0;
            return false;
        }
        if (ban) BanManager.AddBanPlayer(AmongUsClient.Instance.GetRecentClient(clientId));

        return true;
    }
}
[HarmonyPatch(typeof(ResolutionManager), nameof(ResolutionManager.SetResolution))]
internal class SetResolutionManager
{
    public static void Postfix()
    {
        //if (MainMenuManagerPatch.qqButton != null)
        //    MainMenuManagerPatch.qqButton.transform.localPosition = Vector3.Reflect(MainMenuManagerPatch.template.transform.localPosition, Vector3.left);
        //if (MainMenuManagerPatch.discordButton != null)
        //    MainMenuManagerPatch.discordButton.transform.localPosition = Vector3.Reflect(MainMenuManagerPatch.template.transform.localPosition, Vector3.left);
        //if (MainMenuManagerPatch.updateButton != null)
        //    MainMenuManagerPatch.updateButton.transform.localPosition = MainMenuManagerPatch.template.transform.localPosition + new Vector3(0.25f, 0.75f);
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendAllStreamedObjects))]
internal class InnerNetObjectSerializePatch
{
    public static void Prefix()
    {
        if (AmongUsClient.Instance.AmHost)
            GameOptionsSender.SendAllGameOptions();
    }
}