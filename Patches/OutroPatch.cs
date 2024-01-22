using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
class EndGamePatch
{
    public static Dictionary<byte, string> SummaryText = [];
    public static string KillLog = "";
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        GameStates.InGame = false;

        Logger.Info("-----------Game over-----------", "Phase");
        if (!GameStates.IsModHost) return;
        if (GameStates.IsHideNSeek) return;
        SummaryText = [];
        foreach (var id in Main.PlayerStates.Keys.ToArray())
        {
            if (Doppelganger.IsEnable)
            {
                if (Doppelganger.DoppelVictim.ContainsKey(id))
                {
                    var dpc = Utils.GetPlayerById(id);
                    if (dpc != null) 
                    {
                        //if (id == PlayerControl.LocalPlayer.PlayerId) Main.nickName = Doppelganger.DoppelVictim[id];
                        //else
                        //{ 
                        dpc.RpcSetName(Doppelganger.DoppelVictim[id]);
                        //}
                        Main.AllPlayerNames[id] = Doppelganger.DoppelVictim[id];
                    }
                }
            }
            if (Main.EnableRoleSummary.Value)
            {
                SummaryText[id] = Utils.SummaryTexts(id, disableColor: false);
            }
            if (!Main.EnableRoleSummary.Value)
            {
                SummaryText[id] = Utils.NewSummaryTexts(id, disableColor: false);
            }
        }

        var sb = new StringBuilder(GetString("KillLog") + ":");
        foreach (var kvp in Main.PlayerStates.OrderBy(x => x.Value.RealKiller.Item1.Ticks))
        {
            var date = kvp.Value.RealKiller.Item1;
            if (date == DateTime.MinValue) continue;
            var killerId = kvp.Value.GetRealKiller();
            var targetId = kvp.Key;
            sb.Append($"\n{date:T} {Main.AllPlayerNames[targetId]}({(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetDisplayRoleName(targetId, true))}{(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetSubRolesText(targetId, summary: true))}) [{Utils.GetVitalText(kvp.Key)}]");
            if (killerId != byte.MaxValue && killerId != targetId)
                sb.Append($"\n\t⇐ {Main.AllPlayerNames[killerId]}({(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetDisplayRoleName(killerId, true))}{(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetSubRolesText(killerId, summary: true))})");
        }
        KillLog = sb.ToString();
        if (!KillLog.Contains('\n')) KillLog = "";

        if (GameStates.IsNormalGame)
            Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
        
        //winnerListリセット
        TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
        var winner = new List<PlayerControl>();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) winner.Add(pc);
        }
        foreach (var team in CustomWinnerHolder.WinnerRoles.ToArray())
        {
            winner.AddRange(Main.AllPlayerControls.Where(p => p.Is(team) && !winner.Contains(p)));
        }

        Main.winnerNameList = [];
        Main.winnerList = [];
        foreach (var pc in winner.ToArray())
        {
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;

            TempData.winners.Add(new WinningPlayerData(pc.Data));
            Main.winnerList.Add(pc.PlayerId);
            Main.winnerNameList.Add(pc.GetRealName());
        }

        BountyHunter.ChangeTimer = [];
        Main.isDoused = [];
        Main.isDraw = [];
        Main.isRevealed = [];
        Main.PlayerQuitTimes = [];
        ChatManager.ChatSentBySystem = [];

        Main.VisibleTasksCount = false;
        if (AmongUsClient.Instance.AmHost)
        {
            Main.RealOptionsData.Restore(GameOptionsManager.Instance.CurrentGameOptions);
            GameOptionsSender.AllSenders.Clear();
            GameOptionsSender.AllSenders.Add(new NormalGameOptionsSender());
            /* Send SyncSettings RPC */
        }
    }
}
[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
class SetEverythingUpPatch
{
    public static string LastWinsText = "";
    public static string LastWinsReason = "";

    public static void Postfix(EndGameManager __instance)
    {
        if (GameStates.IsHideNSeek) return;
        if (!Main.playerVersion.ContainsKey(AmongUsClient.Instance.HostId)) return;
        //#######################################
        //          ==勝利陣営表示==
        //#######################################
        if (Main.EnableRoleSummary.Value)
        {        
        __instance.WinText.alignment = TMPro.TextAlignmentOptions.Right;
        }
        if (!Main.EnableRoleSummary.Value)
        {        
        __instance.WinText.alignment = TMPro.TextAlignmentOptions.Center;
        }
        var WinnerTextObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
        WinnerTextObject.transform.localScale = new(0.6f, 0.6f, 0.6f);

        if (Main.EnableRoleSummary.Value)
        {
            WinnerTextObject.transform.position = new(__instance.WinText.transform.position.x + 2.4f, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
        }
        if (!Main.EnableRoleSummary.Value)
        {
            WinnerTextObject.transform.position = new(__instance.WinText.transform.position.x, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
        }
        var WinnerText = WinnerTextObject.GetComponent<TMPro.TextMeshPro>(); //WinTextと同じ型のコンポーネントを取得
        WinnerText.fontSizeMin = 3f;
        WinnerText.text = "";

        string CustomWinnerText = "";
        string AdditionalWinnerText = "";
        string CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Crewmate);

        if (Options.CurrentGameMode == CustomGameMode.FFA)
        {
            var winnerId = CustomWinnerHolder.WinnerIds.FirstOrDefault();
            __instance.BackgroundBar.material.color = new Color32(0, 255, 255, 255);
            WinnerText.text = Main.AllPlayerNames[winnerId] + " wins!";
            WinnerText.color = Main.PlayerColors[winnerId];
            goto EndOfText;
        }

        var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
        if (winnerRole >= 0)
        {
            CustomWinnerText = GetWinnerRoleName(winnerRole);
            CustomWinnerColor = Utils.GetRoleColorCode(winnerRole);
       //     __instance.WinText.color = Utils.GetRoleColor(winnerRole);
            __instance.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
            if (winnerRole.IsNeutral())
            {
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
            }
        }
        if (AmongUsClient.Instance.AmHost && Main.PlayerStates[0].MainRole == CustomRoles.GM)
        {
            __instance.WinText.text = GetString("GameOver");
            __instance.WinText.color = Utils.GetRoleColor(CustomRoles.GM);
           __instance.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
        }
        switch (CustomWinnerHolder.WinnerTeam)
        {
            //通常勝利
            case CustomWinner.Crewmate:
                CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Engineer);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Engineer);
                break;
            case CustomWinner.Impostor:
                CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Impostor);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Impostor);
                break;
            case CustomWinner.Rogue:
                CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Rogue);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Rogue);
                break;
            case CustomWinner.Egoist:
                CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Egoist);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Egoist);
                break;
            //特殊勝利
            case CustomWinner.Terrorist:
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Terrorist);
                break;
            case CustomWinner.Lovers:
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Lovers);
                break;
            //引き分け処理
            case CustomWinner.Draw:
                __instance.WinText.text = GetString("ForceEnd");
                __instance.WinText.color = Color.white;
                __instance.BackgroundBar.material.color = Color.gray;
                WinnerText.text = GetString("ForceEndText");
                WinnerText.color = Color.gray;
                break;
            case CustomWinner.NiceMini:
            //    __instance.WinText.color = Utils.GetRoleColor(CustomRoles.Mini);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Mini);
            //    WinnerText.text = GetString("NiceMiniDied");
                WinnerText.color = Utils.GetRoleColor(CustomRoles.Mini);
                break;
            case CustomWinner.Neutrals:
                __instance.WinText.text = GetString("DefeatText");
                __instance.WinText.color = Utils.GetRoleColor(CustomRoles.Impostor);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Executioner);
                WinnerText.text = GetString("NeutralsLeftText");
                WinnerText.color = Utils.GetRoleColor(CustomRoles.Executioner);
                break;
            //全滅
            case CustomWinner.None:
                __instance.WinText.text = "";
                __instance.WinText.color = Color.black;
                __instance.BackgroundBar.material.color = Color.gray;
                WinnerText.text = GetString("EveryoneDied");
                WinnerText.color = Color.gray;
                break;
            case CustomWinner.Error:
                __instance.WinText.text = GetString("ErrorEndText");
                __instance.WinText.color = Color.red;
                __instance.BackgroundBar.material.color = Color.red;
                WinnerText.text = GetString("ErrorEndTextDescription");
                WinnerText.color = Color.white;
                break;
        }

        foreach (var additionalWinners in CustomWinnerHolder.AdditionalWinnerTeams)
        {
            var addWinnerRole = (CustomRoles)additionalWinners;
            AdditionalWinnerText += "+" + Utils.ColorString(Utils.GetRoleColor(addWinnerRole), GetAdditionalWinnerRoleName(addWinnerRole));
        }
        if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None and not CustomWinner.Error)
        {
            if (AdditionalWinnerText == "") WinnerText.text = $"<size=100%><color={CustomWinnerColor}>{CustomWinnerText}</color></size>";
            else WinnerText.text = $"<size=100%><color={CustomWinnerColor}>{CustomWinnerText}</color></size>\n<size=75%>{AdditionalWinnerText}</size>";
        }

        static string GetWinnerRoleName(CustomRoles role)
        {
            var name = GetString($"WinnerRoleText.{Enum.GetName(typeof(CustomRoles), role)}");
            if (name == "" || name.StartsWith("*") || name.StartsWith("<INVALID")) name = Utils.GetRoleName(role);
            return name;
        }
        static string GetAdditionalWinnerRoleName(CustomRoles role)
        {
            var name = GetString($"AdditionalWinnerRoleText.{Enum.GetName(typeof(CustomRoles), role)}");
            if (name == "" || name.StartsWith("*") || name.StartsWith("<INVALID")) name = Utils.GetRoleName(role);
            return name;
        }

    EndOfText:

        LastWinsText = WinnerText.text.RemoveHtmlTags();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //#######################################
        //           ==最終結果表示==
        //#######################################

        var Pos = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));
        var RoleSummaryObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
        RoleSummaryObject.transform.position = new Vector3(__instance.Navigation.ExitButton.transform.position.x + 0.1f, Pos.y - 0.1f, -15f);
        RoleSummaryObject.transform.localScale = new Vector3(1f, 1f, 1f);

        StringBuilder sb = new($"{GetString("RoleSummaryText")}");
        List<byte> cloneRoles = new(Main.PlayerStates.Keys);
        foreach (var id in Main.winnerList.ToArray())
        {
            if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>")) continue;
            sb.Append($"\n<color={CustomWinnerColor}>★</color> ").Append(EndGamePatch.SummaryText[id]);
            cloneRoles.Remove(id);
        }
        if (Options.CurrentGameMode == CustomGameMode.FFA)
        {
            List<(int, byte)> list = [];
            foreach (byte id in cloneRoles.ToArray())
            {
                list.Add((FFAManager.GetRankOfScore(id), id));
            }

            list.Sort();
            foreach (var id in list.Where(x => EndGamePatch.SummaryText.ContainsKey(x.Item2)))
                sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id.Item2]);
        }
        else
        { 
            foreach (var id in cloneRoles.ToArray())
            {
                if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>")) continue;
                sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id]);
            }
        }
        var RoleSummary = RoleSummaryObject.GetComponent<TMPro.TextMeshPro>();
        {
            RoleSummary.alignment = TMPro.TextAlignmentOptions.TopLeft;
            RoleSummary.color = Color.white;
            RoleSummary.outlineWidth *= 1.2f;
            if (Main.EnableRoleSummary.Value)
            {
                RoleSummary.fontSizeMin = RoleSummary.fontSizeMax = RoleSummary.fontSize = 1.25f;
            }
            if (!Main.EnableRoleSummary.Value)
            {
                RoleSummary.fontSizeMin = RoleSummary.fontSizeMax = RoleSummary.fontSize = 1.25f;
            }
        }
        var RoleSummaryRectTransform = RoleSummary.GetComponent<RectTransform>();
        RoleSummaryRectTransform.anchoredPosition = new Vector2(Pos.x + 3.5f, Pos.y - 0.1f);
        RoleSummary.text = sb.ToString();

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Utils.ApplySuffix();
    }
}