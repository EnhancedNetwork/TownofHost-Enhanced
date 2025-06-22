using System;
using System.Text;
using TMPro;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.Crewmate;
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
    public static string MainRoleLog = "";
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        GameStates.InGame = false;

        // if game is H&S or Host no have mod
        if (!GameStates.IsModHost || GameStates.IsHideNSeek) return;

        Logger.Info("-----------Game over-----------", "Phase");

        try
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (GhostRoleAssign.GhostGetPreviousRole != null)
                {
                    foreach (var pvc in GhostRoleAssign.GhostGetPreviousRole.Keys) // Sets role back to original so it shows up in /l results.
                    {
                        if (!Main.PlayerStates.TryGetValue(pvc, out var state) || !state.MainRole.IsGhostRole()) continue;
                        if (!GhostRoleAssign.GhostGetPreviousRole.TryGetValue(pvc, out CustomRoles prevrole)) continue;

                        Main.PlayerStates[pvc].MainRole = prevrole;

                        var message = new RpcSyncPlayerSetting(PlayerControl.LocalPlayer.NetId, pvc, prevrole);
                        RpcUtils.LateBroadcastReliableMessage(message);
                    }

                    if (GhostRoleAssign.GhostGetPreviousRole.Any()) Logger.Info(string.Join(", ", GhostRoleAssign.GhostGetPreviousRole.Select(x => $"{Utils.GetPlayerInfoById(x.Key).PlayerName}/{x.Value}")), "OutroPatch.GhostGetPreviousRole");
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error($"{e} at EndGamePatch", "GhostGetPreviousRole");
        }

        SummaryText = [];

        GhostRoleAssign.GhostGetPreviousRole = [];

        foreach (var id in Main.PlayerStates.Keys.ToArray())
        {
            if (AmongUsClient.Instance.AmHost && Main.OvverideOutfit.TryGetValue(id, out var RealOutfit))
            {
                var dpc = Utils.GetPlayerById(id);
                if (dpc != null)
                {
                    dpc.RpcSetName(RealOutfit.name);
                    Main.AllPlayerNames[id] = RealOutfit.name;
                }
            }

            SummaryText[id] = Utils.SummaryTexts(id, disableColor: false);
        }

        CustomRoleManager.RoleClass.Values.Where(x => x.IsEnable).Do(x => x.IsEnable = false);
        CustomNetObject.Reset();

        var sb = new StringBuilder(GetString("KillLog") + ":");
        if (Options.OldKillLog.GetBool())
            foreach (var kvp in Main.PlayerStates.OrderBy(x => x.Value.RealKiller.Item1.Ticks))
            {
                var date = kvp.Value.RealKiller.Item1;
                if (date == DateTime.MinValue) continue;
                var killerId = kvp.Value.GetRealKiller();
                var targetId = kvp.Key;
                sb.Append($"\n{date:T} {Main.AllPlayerNames[targetId]}({(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetDisplayRoleAndSubName(targetId, targetId, false, true))}{(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetSubRolesText(targetId, summary: true))}) [{Utils.GetVitalText(kvp.Key)}]");
                if (killerId != byte.MaxValue && killerId != targetId)
                    sb.Append($"\n\t⇐ {Main.AllPlayerNames[killerId]}({(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetDisplayRoleAndSubName(killerId, killerId, false, true))}{(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetSubRolesText(killerId, summary: true))})");
            }
        else
        {
            sb.Clear();
            foreach (var kvp in Main.PlayerStates.OrderBy(x => x.Value.RealKiller.Item1.Ticks))
            {
                var date = kvp.Value.RealKiller.Item1;
                if (date == DateTime.MinValue) continue;
                var killerId = kvp.Value.GetRealKiller();
                var targetId = kvp.Key;

                sb.Append($"\n<line-height=85%><size=85%><voffset=-1em><color=#9c9c9c>{date:T}</color> {Main.AllPlayerNames[targetId]}({(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetDisplayRoleAndSubName(targetId, targetId, false, true))}{(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetSubRolesText(targetId, summary: true))}) 『{Utils.GetVitalText(kvp.Key, true)}』</voffset></size></line-height>");
                if (killerId != byte.MaxValue && killerId != targetId)
                    sb.Append($"<br>\t⇐ {Main.AllPlayerNames[killerId]}({(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetDisplayRoleAndSubName(killerId, killerId, false, true))}{(Options.CurrentGameMode == CustomGameMode.FFA ? string.Empty : Utils.GetSubRolesText(killerId, summary: true))})");
            }

        }
        KillLog = sb.ToString();
        if (!KillLog.Contains('\n')) KillLog = "";

        var sb2 = new StringBuilder(GetString("MainRoleLog") + ":");
        foreach (var kvp in Main.PlayerStates.OrderBy(x => x.Key))
        {
            if (Options.CurrentGameMode != CustomGameMode.Standard) break;
            if (kvp.Value.MainRoleLogs.Where(x => !x.Item2.IsVanilla()).ToList().Count <= 1) continue;
            sb2.Append($"\n[{kvp.Key}] {Main.AllPlayerNames[kvp.Key]} {Utils.GetDisplayRoleAndSubName(kvp.Key, kvp.Key, false, false)}");
            foreach (var item in kvp.Value.MainRoleLogs.OrderBy(x => x.Item1.Ticks))
            {
                if (item.Item2.IsVanilla()) continue;
                item.Item2.GetActualRoleName(out var rolename);
                sb2.Append($"\n => {Utils.ColorString(Utils.GetRoleColor(item.Item2), rolename)} [{item.Item1:T}]");
            }
        }
        MainRoleLog = sb2.ToString();
        if (!MainRoleLog.Contains('\n')) MainRoleLog = "";

        if (GameStates.IsNormalGame)
            Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;

        //winnerListÒâ¬Òé╗ÒââÒâê
        EndGameResult.CachedWinners = new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();
        var winner = new List<PlayerControl>();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) winner.Add(pc);
        }
        foreach (var team in CustomWinnerHolder.WinnerRoles.ToArray())
        {
            winner.AddRange(Main.AllPlayerControls.Where(p => p.Is(team) && !winner.Contains(p)));
        }

        Main.winnerNameList.Clear();
        Main.winnerList.Clear();

        // Remove duplicates
        winner = winner.Distinct().ToList();

        foreach (var pc in winner.ToArray())
        {
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;
            // Check "Contains" to avoid adding players twice
            if (Main.winnerList.Contains(pc.PlayerId)) continue;

            EndGameResult.CachedWinners.Add(new CachedPlayerData(pc.Data));
            Main.winnerList.Add(pc.PlayerId);
            Main.winnerNameList.Add(pc.GetRealName());
        }

        BountyHunter.ChangeTimer.Clear();
        Revolutionist.IsDraw.Clear();
        Overseer.IsRevealed.Clear();
        Main.PlayerQuitTimes.Clear();
        ChatManager.ChatSentBySystem = [];

        Main.VisibleTasksCount = false;
        Main.GameIsLoaded = false;
        Main.IntroDestroyed = false;

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
        //      ==Victory faction display==
        //#######################################

        __instance.WinText.alignment = TextAlignmentOptions.Right;

        var WinnerTextObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
        WinnerTextObject.transform.localScale = new(0.6f, 0.6f, 0.6f);
        WinnerTextObject.transform.position = new(__instance.WinText.transform.position.x + 2.4f, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);

        var WinnerText = WinnerTextObject.GetComponent<TextMeshPro>(); //Get components of the same type as WinText
        WinnerText.fontSizeMin = 3f;
        WinnerText.text = "";

        string CustomWinnerText = "";
        string AdditionalWinnerText = "";
        string CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Crewmate);

        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                {
                    var winnerId = CustomWinnerHolder.WinnerIds.FirstOrDefault();
                    __instance.BackgroundBar.material.color = new Color32(0, 255, 255, 255);
                    WinnerText.text = Main.AllPlayerNames[winnerId] + " Wins!";
                    WinnerText.color = Main.PlayerColors[winnerId];
                    goto EndOfText;
                }
            case CustomGameMode.SpeedRun:
                {
                    var winnerId = CustomWinnerHolder.WinnerIds.FirstOrDefault();
                    __instance.BackgroundBar.material.color = new Color32(255, 251, 0, 255);
                    WinnerText.text = Main.AllPlayerNames[winnerId] + " Wins!";
                    WinnerText.color = Main.PlayerColors[winnerId];
                    goto EndOfText;
                }
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
        if (Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].MainRole == CustomRoles.GM)
        {
            __instance.WinText.text = GetString("GameOver");
            __instance.WinText.color = Utils.GetRoleColor(CustomRoles.GM);
            __instance.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
        }
        switch (CustomWinnerHolder.WinnerTeam)
        {
            case CustomWinner.Crewmate:
                CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Crewmate);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Crewmate);
                break;
            case CustomWinner.Impostor:
                CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Impostor);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Impostor);
                break;
            case CustomWinner.Egoist:
                CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Egoist);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Egoist);
                break;
            case CustomWinner.Coven:
                CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Coven);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Coven);
                break;
            case CustomWinner.Terrorist:
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Terrorist);
                break;
            case CustomWinner.Lovers:
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Lovers);
                break;
            case CustomWinner.Draw:
                __instance.WinText.text = GetString("ForceEnd");
                __instance.WinText.color = Color.white;
                __instance.BackgroundBar.material.color = Color.gray;
                WinnerText.text = GetString("ForceEndText");
                WinnerText.color = Color.gray;
                break;
            case CustomWinner.NiceMini:
                //    __instance.WinText.color = Utils.GetRoleColor(CustomRoles.Mini);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.NiceMini);
                //    WinnerText.text = GetString("NiceMiniDied");
                WinnerText.color = Utils.GetRoleColor(CustomRoles.NiceMini);
                break;
            case CustomWinner.Neutrals:
                __instance.WinText.text = GetString("DefeatText");
                __instance.WinText.color = Utils.GetRoleColor(CustomRoles.Impostor);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Executioner);
                WinnerText.text = GetString("NeutralsLeftText");
                WinnerText.color = Utils.GetRoleColor(CustomRoles.Executioner);
                break;
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

        LastWinsText = WinnerText.text/*.RemoveHtmlTags()*/;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //########################################
        //     ==The final result indicates==
        //########################################

        var Pos = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));
        var RoleSummaryObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
        RoleSummaryObject.transform.position = new Vector3(__instance.Navigation.ExitButton.transform.position.x + 0.1f, Pos.y - 0.1f, -15f);
        RoleSummaryObject.transform.localScale = new Vector3(1f, 1f, 1f);

        StringBuilder sb = new($"{GetString("RoleSummaryText")}\n<b>");
        List<byte> cloneRoles = new(Main.PlayerStates.Keys);
        foreach (byte id in Main.winnerList.ToArray())
        {
            if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>")) continue;
            sb.Append('\n').Append(EndGamePatch.SummaryText[id]);
            cloneRoles.Remove(id);
        }
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                {
                    List<(int, byte)> listFFA = [];
                    foreach (byte id in cloneRoles.ToArray())
                    {
                        listFFA.Add((FFAManager.GetRankOfScore(id), id));
                    }

                    listFFA.Sort();
                    foreach (var id in listFFA.Where(x => EndGamePatch.SummaryText.ContainsKey(x.Item2)))
                        sb.Append($"\n  ").Append(EndGamePatch.SummaryText[id.Item2]);
                    break;
                }
            case CustomGameMode.SpeedRun:
                {
                    sb.Clear();
                    sb.Append(SpeedRun.GetGameState(forGameEnd: true));
                    break;
                }
            default: // Normal game
                {
                    sb.Append($"</b>\n");
                    foreach (byte id in cloneRoles.ToArray())
                    {
                        if (EndGamePatch.SummaryText[id].Contains("<INVALID:NotAssigned>")) continue;
                        sb.Append('\n').Append(EndGamePatch.SummaryText[id]);
                    }

                    break;
                }
        }
        var RoleSummary = RoleSummaryObject.GetComponent<TextMeshPro>();
        RoleSummary.alignment = TextAlignmentOptions.TopLeft;
        RoleSummary.color = Color.white;
        RoleSummary.outlineWidth *= 1.2f;
        RoleSummary.fontSizeMin = RoleSummary.fontSizeMax = RoleSummary.fontSize = 1.25f;

        var RoleSummaryRectTransform = RoleSummary.GetComponent<RectTransform>();
        RoleSummaryRectTransform.anchoredPosition = new Vector2(Pos.x + 3.5f, Pos.y - 0.1f);
        RoleSummary.text = sb.ToString();

        Logger.Info($"{CustomWinnerHolder.WinnerTeam}", "Winner Team");
        Logger.Info($"{LastWinsReason}", "Wins Reason");
        Logger.Info($"{RoleSummary.text.RemoveHtmlTags()}", "Role Summary");

        Utils.ShowLastRoles(sendMessage: false);

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Utils.ApplySuffix();
    }
}
