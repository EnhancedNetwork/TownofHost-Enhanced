namespace TOHE.Patches;
[HarmonyPatch(typeof(DialogueBox))]
internal class DialogueBoxPatch
{
    [HarmonyPatch(nameof(DialogueBox.Show)), HarmonyPostfix]
    public static void Show_Postfix(DialogueBox __instance, [HarmonyArgument(0)]string dialouge)
    {
        if (!PlayerControl.LocalPlayer.inVent && dialouge.Contains("<size=0%>tohe</size>") && GameStates.IsInTask)
        {
            PlayerControl.LocalPlayer.ForceKillTimerContinue = true;
        }
    }

    [HarmonyPatch(nameof(DialogueBox.Hide)), HarmonyPostfix]
    public static void Hide_Postfix(DialogueBox __instance)
    {
        if (__instance.target.text.Contains("<size=0%>tohe</size>"))
        {
            PlayerControl.LocalPlayer.ForceKillTimerContinue = false;
        }
    }

    /*
     * Lets add a <size=0%>tohe</size> at where you will use the dialogue box.
     */
}
