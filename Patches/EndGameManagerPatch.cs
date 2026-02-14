using InnerNet;
using TMPro;
using TOHE.Roles.Core.DraftAssign;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Patches;

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.ShowButtons))]
public class EndGameManagerPatch
{
    public static GameObject CountdownText;
    public static TextMeshPro CountdownTextText;
    public static bool IsRestarting;

    public static void Postfix(EndGameManager __instance)
    {
        if (!AmongUsClient.Instance.AmHost || !Options.AutoPlayAgain.GetBool()) return;
        IsRestarting = false;

        DraftAssign.Reset();

        _ = new LateTask(() =>
        {
            Logger.Msg("Beginning Auto Play Again Countdown!", "AutoPlayAgain");
            IsRestarting = true;
            BeginAutoPlayAgainCountdown(__instance, Options.AutoPlayAgainCountdown.GetInt());
        }, 0.5f, "Auto Play Again");
    }

    private static void BeginAutoPlayAgainCountdown(EndGameManager endGameManager, int seconds)
    {
        if (!IsRestarting)
        {
            try
            {
                if (CountdownTextText && CountdownTextText.isActiveAndEnabled)
                {
                    CountdownTextText.text = string.Format(GetString("CancelStartCountDown"), seconds);
                }
            }
            catch { }
            return;
        }
        if (!endGameManager) return;
        EndGameNavigation navigation = endGameManager.Navigation;
        if (!navigation) return;

        if (seconds == Options.AutoPlayAgainCountdown.GetInt())
        {
            CountdownText = new GameObject("CountdownText");
            CountdownText.transform.position = new Vector3(0f, -2.5f, 30f);
            CountdownTextText = CountdownText.AddComponent<TextMeshPro>();
            CountdownTextText.text = string.Format(GetString("CountdownText"), seconds);
            CountdownTextText.alignment = TextAlignmentOptions.Center;
            CountdownTextText.fontSize = 3f;
        }
        else
        {
            CountdownTextText.text = string.Format(GetString("CountdownText"), seconds);
        }

        if (seconds == 0) { navigation.NextGame(); CountdownText.transform.DestroyChildren(); }
        else _ = new LateTask(() =>
            {
                BeginAutoPlayAgainCountdown(endGameManager, seconds - 1);
            }, 1f, "Begin Auto Play Again Countdown");
    }
}

// Credit: EHR
[HarmonyPatch(typeof(EndGameNavigation), nameof(EndGameNavigation.NextGame))]
internal static class EndGameNavigationNextGamePatch
{
    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        _ = new LateTask(() =>
        {
            foreach (ClientData client in AmongUsClient.Instance.allClients)
            {
                if ((!client.IsDisconnected() && client.Character.Data.IsIncomplete) || client.Character.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= client.Character.Data.DefaultOutfit.ColorId)
                {
                    Logger.SendInGame(GetString("Error.InvalidColor") + $" {client.Id}/{client.PlayerName}", Color.yellow);
                    AmongUsClient.Instance.KickPlayer(client.Id, false);
                    Logger.Info($"Kicked client {client.Id}/{client.PlayerName} since its PlayerControl was not spawned in time.", "OnPlayerJoinedPatchPostfix");
                    return;
                }
            }
        }, 5f, "Kick Fortegreen Beans After Play-Again");
    }
}