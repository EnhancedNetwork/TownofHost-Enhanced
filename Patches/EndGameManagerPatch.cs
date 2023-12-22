using HarmonyLib;
using TMPro;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Patches;

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.ShowButtons))]
public class EndGameManagerPatch
{
    public static GameObject CountdownText;
    public static bool IsRestarting { get; private set; }

    public static void Postfix(EndGameManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost || !Options.AutoPlayAgain.GetBool()) return;
        IsRestarting = false;

        _ = new LateTask(() =>
        {
            Logger.Msg("Beginning Auto Play Again Countdown!", "AutoPlayAgain");
            IsRestarting = true;
            BeginAutoPlayAgainCountdown(__instance, Options.AutoPlayAgainCountdown.GetInt());
        }, 0.5f, "Auto Play Again");
    }

    public static void CancelPlayAgain()
    {
        IsRestarting = false;
    }

    private static void BeginAutoPlayAgainCountdown(EndGameManager endGameManager, int seconds)
    {
        if (!IsRestarting) return;
        if (endGameManager == null) return;
        EndGameNavigation navigation = endGameManager.Navigation;
        if (navigation == null) return;

        if (seconds == Options.AutoPlayAgainCountdown.GetInt())
        {
            CountdownText = new GameObject("CountdownText");
            CountdownText.transform.position = new Vector3(0f, -2.5f, 30f);
            var CountdownTextText = CountdownText.AddComponent<TextMeshPro>();
            CountdownTextText.text = string.Format(GetString("CountdownText"), seconds);
            CountdownTextText.alignment = TextAlignmentOptions.Center;
            CountdownTextText.fontSize = 3f;
        }
        else
        {
            var CountdownTextText = CountdownText.GetComponent<TextMeshPro>();
            CountdownTextText.text = string.Format(GetString("CountdownText"), seconds);
        }

        if (seconds == 0) { navigation.NextGame(); CountdownText.transform.DestroyChildren(); }
        else _ = new LateTask(() => 
            { 
                BeginAutoPlayAgainCountdown(endGameManager, seconds - 1);
            }, 1f, "Begin Auto Play Again Countdown");
    }
}