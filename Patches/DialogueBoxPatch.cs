namespace TOHE.Patches;

[HarmonyPatch(typeof(DialogueBox))]
internal class DialogueBoxPatch
{
    [HarmonyPatch(nameof(DialogueBox.Show)), HarmonyPrefix]
    public static bool Show_Prefix(DialogueBox __instance, string dialogue)
    {
        __instance.target.text = dialogue;
        if (Minigame.Instance != null)
            Minigame.Instance.Close();
        if (Minigame.Instance != null)
            Minigame.Instance.Close();
        __instance.gameObject.SetActive(true);
        return false;
    }
    [HarmonyPatch(nameof(DialogueBox.Show)), HarmonyPostfix]
    public static void Show_Postfix(DialogueBox __instance, string dialogue)
    {
        if (!PlayerControl.LocalPlayer.inVent && dialogue.Contains("<size=0%>tohe</size>") && GameStates.IsInTask)
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
