using AmongUs.Data;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
class ChatControllerUpdatePatch
{
    public static int CurrentHistorySelection = -1;

    static readonly Dictionary<string, string> replaceDic = new()
            {
                { "（", " (" },
                { "）", ") " },
                { "，", ", " },
                { "：", ": " },
                { "[", "【" },
                { "]", "】" },
                { "‘", " '" },
                { "’", "' " },
                { "“", " ''" },
                { "”", "'' " },
                { "！", "! " },
                { Environment.NewLine, " " }
            };
    public static void Prefix()
    {
        if (AmongUsClient.Instance.AmHost && DataManager.Settings.Multiplayer.ChatMode == InnerNet.QuickChatModes.QuickChatOnly)
            DataManager.Settings.Multiplayer.ChatMode = InnerNet.QuickChatModes.FreeChatOrQuickChat; //コマンドを打つためにホストのみ常時フリーチャット開放
    }
    public static void Postfix(ChatController __instance)
    {
        if (Main.DarkTheme.Value)
        {
            __instance.freeChatField.background.color = new Color32(60, 60, 60, byte.MaxValue);
            __instance.freeChatField.textArea.compoText.Color(Color.white);
            __instance.freeChatField.textArea.outputText.color = Color.white;
        }

        if (!__instance.freeChatField.textArea.hasFocus) return;
        if (!GameStates.IsModHost) return;

        __instance.freeChatField.textArea.characterLimit = AmongUsClient.Instance.AmHost ? 999 : 300;

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
        {
            if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
            {
                string replacedText = GUIUtility.systemCopyBuffer;
                foreach (var pair in replaceDic)
                {
                    replacedText = replacedText.Replace(pair.Key, pair.Value);
                }

                if ((__instance.freeChatField.textArea.text + replacedText).Length < __instance.freeChatField.textArea.characterLimit)
                    __instance.freeChatField.textArea.SetText(__instance.freeChatField.textArea.text + replacedText);
                else
                {
                    int remainingLength = __instance.freeChatField.textArea.characterLimit - __instance.freeChatField.textArea.text.Length;
                    if (remainingLength > 0)
                    {
                        string text = replacedText[..remainingLength];
                        __instance.freeChatField.textArea.SetText(__instance.freeChatField.textArea.text + text);
                    }
                }
            }
        }


        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
        {
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
            __instance.freeChatField.textArea.SetText("");
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && ChatCommands.ChatHistory.Count > 0)
        {
            CurrentHistorySelection = Mathf.Clamp(--CurrentHistorySelection, 0, ChatCommands.ChatHistory.Count - 1);
            __instance.freeChatField.textArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && ChatCommands.ChatHistory.Count > 0)
        {
            CurrentHistorySelection++;
            if (CurrentHistorySelection < ChatCommands.ChatHistory.Count)
                __instance.freeChatField.textArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
            else __instance.freeChatField.textArea.SetText("");
        }
    }
}
