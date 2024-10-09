using Hazel;
using TOHE.Modules;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(NotificationPopper), nameof(NotificationPopper.Awake))]
public class NotificationPopperAwakePatch
{
    public static void Prefix(NotificationPopper __instance)
    {
        // not use ??= because exceptions may occur
        NotificationPopperPatch.Instance = __instance;
    }
}
internal class NotificationPopperPatch
{
    public static NotificationPopper Instance;

    public static void AddSettingsChangeMessage(
        int index,
        OptionItem key,
        bool playSound = false)
    {
        SendRpc(0, index, playSound: playSound);
        var haveParent = key.Parent != null;
        string str;
        if (haveParent && System.Enum.GetValues<CustomRoles>().Find(x => Translator.GetString($"{x}") == key.Parent.GetName().RemoveHtmlTags(), out var role))
        {
            str = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.LobbyChangeSettingNotification, "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.Parent.GetName() + "</font>: <font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.GetName() + "</font>", "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.GetString() + "</font>");
        }
        else if (haveParent)
        {
            str = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.LobbyChangeSettingNotification, "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.Parent.GetName() + "</font>: <font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.GetName() + "</font>", "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.GetString() + "</font>");
        }
        else
        {
            str = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.LobbyChangeSettingNotification, "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.GetName() + "</font>", "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.GetString() + "</font>");
        }
        SettingsChangeMessageLogic(key, str, playSound);
    }

    public static void AddRoleSettingsChangeMessage(
        int index,
        OptionItem key,
        CustomRoles customRole,
        bool playSound = false)
    {
        SendRpc(1, index, customRole, playSound);
        var roleColor = Utils.GetRoleColor(customRole);
        string str = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.LobbyChangeSettingNotification, "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.GetName() + "</font>", "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + key.GetString() + "</font>");
        SettingsChangeMessageLogic(key, str, playSound);
    }

    private static void SettingsChangeMessageLogic(OptionItem key, string item, bool playSound)
    {
        if (Instance.lastMessageKey == key.Id && Instance.activeMessages.Count > 0)
        {
            Instance.activeMessages[^1].UpdateMessage(item);
        }
        else
        {
            Instance.lastMessageKey = key.Id;
            LobbyNotificationMessage newMessage = Object.Instantiate(Instance.notificationMessageOrigin, Vector3.zero, Quaternion.identity, Instance.transform);
            newMessage.transform.localPosition = new Vector3(0.0f, 0.0f, -2f);
            newMessage.SetUp(item, Instance.settingsChangeSprite, Instance.settingsChangeColor, (Il2CppSystem.Action)(() => Instance.OnMessageDestroy(newMessage)));
            Instance.ShiftMessages();
            Instance.AddMessageToQueue(newMessage);
        }
        if (!playSound)
            return;
        SoundManager.Instance.PlaySoundImmediate(Instance.settingsChangeSound, false);
    }
    private static void SendRpc(byte typeId, int index, CustomRoles customRole = CustomRoles.NotAssigned, bool playSound = true)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.AllPlayerControls.Any(pc => pc.IsNonHostModdedClient())) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.NotificationPopper, SendOption.Reliable);
        writer.Write(typeId);
        writer.WritePacked(index);
        writer.WritePacked((int)customRole);
        writer.Write(playSound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}
