using AmongUs.GameOptions;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetRight))]
class ChatBubbleSetRightPatch
{
    public static void Postfix(ChatBubble __instance)
    {
        if (Main.isChatCommand) __instance.SetLeft();
    }
}
[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
class ChatBubbleSetNamePatch
{
    public static void Postfix(ChatBubble __instance, [HarmonyArgument(1)] bool isDead, [HarmonyArgument(2)] bool voted)
    {
        var seer = PlayerControl.LocalPlayer;
        var target = __instance.playerInfo.Object;

        if (GameStates.IsInGame && !voted && seer.PlayerId == target.PlayerId)
            __instance.NameText.color = seer.GetRoleColor();

        var seerRoleClass = seer.GetRoleClass();

        // if based role is Shapeshifter and is Desync Shapeshifter
        if (seerRoleClass?.ThisRoleBase.GetRoleTypes() == RoleTypes.Shapeshifter && seer.HasDesyncRole())
        {
            // When target is impostor, set name color as white
            __instance.NameText.color = Color.white;
        }

        if (Main.DarkTheme.Value)
        {
            if (isDead)
                __instance.Background.color = new(0.1f, 0.1f, 0.1f, 0.6f);
            else
                __instance.Background.color = new(0.1f, 0.1f, 0.1f, 1f);

            __instance.TextArea.color = Color.white;
        }
    }
}

