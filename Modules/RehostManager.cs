using InnerNet;
using AmongUs.GameOptions;
using TMPro;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Modules
{
    public static class RehostManager
    {
        public static GameObject RCountdownText;
        public static TextMeshPro RCountdownTextText;
        public static bool IsAutoRehostDone;
        public static bool ShouldPublic;
        public static void OnDisconnectInternal(DisconnectReasons reason)
        {
            if (!Main.AutoRehost.Value || !DebugModeManager.AmDebugger) return;
            if (AmongUsClient.Instance.mode != MatchMakerModes.HostAndClient) return;
            ShouldPublic = AmongUsClient.Instance.IsGamePublic;

            if (reason == DisconnectReasons.NewConnection || reason == DisconnectReasons.ConnectionLimit ||
                reason == DisconnectReasons.ExitGame) return;

            _ = new LateTask(() =>
            {
                IsAutoRehostDone = false;
                BeginAutoRehostCountdown(Options.AutoPlayAgainCountdown.GetInt());
            }, 1f, "Begin Auto Rehost Countdown");
        }
        private static void BeginAutoRehostCountdown(int seconds)
        {
            if (AmongUsClient.Instance.mode != MatchMakerModes.None || !Main.AutoRehost.Value ||
                !EOSManager.Instance.loginFlowFinished || IsAutoRehostDone)
            {
                IsAutoRehostDone = true;
                if (RCountdownTextText.isActiveAndEnabled)
                {
                    RCountdownTextText.text = string.Format(GetString("CancelStartCountDown"));
                    //Press Shift + C to cancel the countdown
                }
                Logger.Info("Auto Rehost Cancelled!", "Rehost Manager");
                return;
            }

            if (seconds == Options.AutoPlayAgainCountdown.GetInt())
            {
                RCountdownText = new GameObject("RCountdownText");
                RCountdownText.transform.position = new Vector3(0f, -2.5f, 30f);

                RCountdownTextText = RCountdownText.AddComponent<TextMeshPro>();
                RCountdownTextText.text = string.Format(GetString("CountdownText"), seconds);
                RCountdownTextText.alignment = TextAlignmentOptions.Center;
                RCountdownTextText.fontSize = 3f;
            }
            else
            {
                RCountdownTextText.text = string.Format(GetString("CountdownText"), seconds);
            }

            if (seconds == 0)
            {
                PSManager.Instance.CreateGame(GameModes.Normal);
                Logger.Info("Auto Rehost!", "Rehost Manager");
            }
            else _ = new LateTask(() =>
            {
                BeginAutoRehostCountdown(seconds - 1);
            }, 1f, "Begin Auto Rehost Countdown");
        }
    }


}
