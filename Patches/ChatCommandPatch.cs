using Assets.CoreScripts;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;


namespace TOHE;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal class ChatCommands
{
    private static string modLogFiles = @"./TOHE-DATA/ModLogs.txt";
    private static string modTagsFiles = @"./TOHE-DATA/Tags/MOD_TAGS";
    private static string sponsorTagsFiles = @"./TOHE-DATA/Tags/SPONSOR_TAGS";
    private static string vipTagsFiles = @"./TOHE-DATA/Tags/VIP_TAGS";


    public static List<string> ChatHistory = new();

    public static bool Prefix(ChatController __instance)
    {
        if (__instance.freeChatField.textArea.text == "") return false;
        __instance.timeSinceLastMessage = 3f;
        var text = __instance.freeChatField.textArea.text;
        if (!ChatHistory.Any() || ChatHistory[^1] != text) ChatHistory.Add(text);
        ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
        string[] args = text.Split(' ');
        string subArgs = "";
        string subArgs2 = "";
        var canceled = false;
        var cancelVal = "";
        Main.isChatCommand = true;
        Logger.Info(text, "SendChat");
        if (Options.NewHideMsg.GetBool() || Blackmailer.IsEnable) // Blackmailer.ForBlackmailer.Contains(PlayerControl.LocalPlayer.PlayerId)) && PlayerControl.LocalPlayer.IsAlive())
        {
            ChatManager.SendMessage(PlayerControl.LocalPlayer, text);
        }

        //if (text.Length >= 3) if (text[..2] == "/r" && text[..3] != "/rn" && text[..3] != "/rs") args[0] = "/r";
        if (text.Length >= 4) if (text[..3] == "/up") args[0] = "/up";
        if (text.Length >= 5) if (text[..4] == "/dev") args[0] = "/dev";
        if (GuessManager.GuesserMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Judge.TrialMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (President.EndMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (ParityCop.ParityCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Pirate.DuelCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Councillor.MurderMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Mediumshiper.MsMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (MafiaRevengeManager.MafiaMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (RetributionistRevengeManager.RetributionistMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Swapper.SwapMsg(PlayerControl.LocalPlayer, text)) goto Canceled; 
        Directory.CreateDirectory(modTagsFiles);
        Directory.CreateDirectory(vipTagsFiles);
        Directory.CreateDirectory(sponsorTagsFiles);

        if (Blackmailer.ForBlackmailer.Contains(PlayerControl.LocalPlayer.PlayerId) && PlayerControl.LocalPlayer.IsAlive())
        {
            ChatManager.SendPreviousMessagesToAll();
            ChatManager.cancel = false;
            goto Canceled;
        }
        switch (args[0])
        {
            case "/dump":
                canceled = true;
                Utils.DumpLog();
                break;
            case "/v":
            case "/version":
            case "/versão":
                canceled = true;
                string version_text = "";
                foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key).ToArray())
                {
                    version_text += $"{kvp.Key}:{Main.AllPlayerNames[kvp.Key]}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                }
                if (version_text != "") HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + version_text);
                break;
            default:
                Main.isChatCommand = false;
                break;
        }
        if (AmongUsClient.Instance.AmHost)
        {
            Main.isChatCommand = true;
            switch (args[0])
            {
                case "/win":
                case "/winner":
                case "/vencedor":
                    canceled = true;
                    if (!Main.winnerNameList.Any()) Utils.SendMessage(GetString("NoInfoExists"));
                    else Utils.SendMessage("Winner: " + string.Join(", ", Main.winnerNameList));
                    break;

                case "/l":
                case "/lastresult":
                case "/fimdejogo":
                    canceled = true;
                    Utils.ShowKillLog();
                    Utils.ShowLastRoles();
                    Utils.ShowLastResult();
                    break;

                case "/gr":
                case "/gameresults":
                case "/resultados":
                    canceled = true;
                    Utils.ShowLastResult();
                    break;

                case "/kh":
                case "/killlog":
                    canceled = true;
                    Utils.ShowKillLog();
                    break;

                case "/rs":
                case "/sum":
                case "/rolesummary":
                case "/sumario":
                case "/sumário":
                case "/summary":
                    canceled = true;
                    Utils.ShowLastRoles();
                    break;



                case "/rn":
                case "/rename":
                case "/renomear":
                    canceled = true;
                    if (args.Length < 1) break;
                    if (args[1].Length is > 10 or < 1)
                        Utils.SendMessage(GetString("Message.AllowNameLength"), PlayerControl.LocalPlayer.PlayerId);
                    else Main.nickName = args[1];
                    break;

                case "/hn":
                case "/hidename":
                case "/semnome":
                    canceled = true;
                    Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
                    GameStartManagerPatch.GameStartManagerStartPatch.HideName.text =
                        ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                            ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                            : $"<color={Main.ModColor}>{Main.HideName.Value}</color>";
                    break;

                case "/level":
                case "/nível":
                case "/nivel":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    Utils.SendMessage(string.Format(GetString("Message.SetLevel"), subArgs), PlayerControl.LocalPlayer.PlayerId);
                    _ = int.TryParse(subArgs, out int input);
                    if (input is < 1 or > 999)
                    {
                        Utils.SendMessage(GetString("Message.AllowLevelRange"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    var number = Convert.ToUInt32(input);
                    PlayerControl.LocalPlayer.RpcSetLevel(number - 1);
                    break;

                case "/n":
                case "/now":
                case "/atual":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                        case "funções":
                            Utils.ShowActiveRoles();
                            break;
                        case "a":
                        case "all":
                        case "tudo":
                            Utils.ShowAllActiveSettings();
                            break;
                        default:
                            Utils.ShowActiveSettings();
                            break;
                    }
                    break;

                case "/dis":
                case "/disconnect":
                case "/desconectar":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "crew":
                        case "tripulante":
                            GameManager.Instance.enabled = false;
                            GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
                            break;

                        case "imp":
                        case "impostor":
                            GameManager.Instance.enabled = false;
                            GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                            break;

                        default:
                            __instance.AddChat(PlayerControl.LocalPlayer, "crew | imp");
                            if (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Brazilian)
                            {
                                __instance.AddChat(PlayerControl.LocalPlayer, "tripulante | impostor");
                            }
                            cancelVal = "/dis";
                            break;
                    }
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
                    break;

                case "/r":
                    canceled = true;
                    subArgs = text.Remove(0, 2);
                    SendRolesInfo(subArgs, 255, PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug);
                    break;

                case "/up":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    if (!PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsUp) break;
                    if (!Options.EnableUpMode.GetBool())
                    {
                        Utils.SendMessage(string.Format(GetString("Message.YTPlanDisabled"), GetString("EnableYTPlan")), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    SendRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId, isUp: true);
                    break;

                case "/dev" :
                    canceled = true;
                    subArgs = text.Remove(0, 4);
                    if (!PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsDev) break;
                    if (!Options.EnableDevMode.GetBool())
                    {
                        Utils.SendMessage(string.Format(GetString("Message.DevlanDisabled"), GetString("EnableDevPlan")), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    SendRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId, isDev: true);
                    break;
                    
                case "/setplayers":
                case "/maxjogadores":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    Utils.SendMessage(GetString("Message.MaxPlayers") + subArgs);
                    var numbereer = Convert.ToByte(subArgs);
                    GameOptionsManager.Instance.currentNormalGameOptions.MaxPlayers = numbereer;
                    break;

                case "/h":
                case "/help":
                case "/ajuda":
                    canceled = true;
                    Utils.ShowHelp(PlayerControl.LocalPlayer.PlayerId);
                    break;

                /*        case "/icons":
                            { 
                                Utils.SendMessage(GetString("Command.icons"), PlayerControl.LocalPlayer.PlayerId);
                                break;
                            }

                        case "/iconhelp":
                            { 
                                Utils.SendMessage(GetString("Command.icons"));
                                break;
                            }*/

                case "/kcount":
                    //canceled = true;
                    int impnum = 0;
                    int neutralnum = 0;

                    foreach (var players in Main.AllAlivePlayerControls)
                    {
                        if (Options.ShowImpRemainOnEject.GetBool())
                        {
                            if (players.GetCustomRole().IsImpostor())
                                impnum++;
                        }
                        if (Options.ShowNKRemainOnEject.GetBool())
                        {
                            if (players.GetCustomRole().IsNK())
                                neutralnum++;
                        }
                    }
                    if (!GameStates.IsLobby && Options.EnableKillerLeftCommand.GetBool())
                    {
                        Utils.SendMessage(GetString("Remaining.ImpostorCount") + impnum + "\n\r" + GetString("Remaining.NeutralCount") + neutralnum, PlayerControl.LocalPlayer.PlayerId);
                    }
                    break;


                case "/d":
                case "/death":
                case "/morto":
                    canceled = true;
                    Logger.Info($"PlayerControl.LocalPlayer.PlayerId: {PlayerControl.LocalPlayer.PlayerId}", "/death command");
                    if (GameStates.IsLobby)
                    {
                        Logger.Info("IsLobby", "/death command");
                        Utils.SendMessage(text: GetString("Message.CanNotUseInLobby"), sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (PlayerControl.LocalPlayer.IsAlive())
                    {
                        Logger.Info("IsAlive", "/death command");
                        Utils.SendMessage(text: GetString("DeathCmd.HeyPlayer") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + GetString("DeathCmd.YouAreRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>\n\n" + GetString("DeathCmd.NotDead"), sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                    {
                        Logger.Info("DeathReason.Vote", "/death command");
                        Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Ejected"), sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].deathReason == PlayerState.DeathReason.Shrouded)
                    {
                        Logger.Info("DeathReason.Shrouded", "/death command");
                        Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Shrouded"), sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].deathReason == PlayerState.DeathReason.FollowingSuicide)
                    {
                        Logger.Info("DeathReason.FollowingSuicide", "/death command");
                        Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Lovers"), sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        Logger.Info("GetRealKiller()", "/death command");
                        var killer = PlayerControl.LocalPlayer.GetRealKiller();
                        Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.DeathReason") + "<b>" + Utils.GetVitalText(PlayerControl.LocalPlayer.PlayerId) + "</b>" + "\n\r" + "</b>" + "\n\r" + GetString("DeathCmd.KillerName") + "<b>" + killer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.KillerRole") + "<b>" + $"<color={Utils.GetRoleColorCode(killer.GetCustomRole())}>{Utils.GetRoleName(killer.GetCustomRole())}</color>" + "</b>", sendTo: PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }


                case "/m":
                case "/myrole":
                case "/minhafunção":
                    canceled = true;
                    var role = PlayerControl.LocalPlayer.GetCustomRole();
                    if (GameStates.IsInGame)
                    {
                        var lp = PlayerControl.LocalPlayer;
                        var sb = new StringBuilder();
                        //sb.Append(String.Format(GetString("PlayerNameForRoleInfo"), Main.AllPlayerNames[PlayerControl.LocalPlayer.PlayerId]));
                        sb.Append(GetString(role.ToString()) + Utils.GetRoleMode(role) + lp.GetRoleInfo(true));
                        if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                            Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref sb, command: true);
                        var txt = sb.ToString();
                        sb.Clear().Append(txt.RemoveHtmlTags());
                        foreach (var subRole in Main.PlayerStates[lp.PlayerId].SubRoles.ToArray())
                            sb.Append($"\n\n" + GetString($"{subRole}") + Utils.GetRoleMode(subRole) + GetString($"{subRole}InfoLong"));
                        if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and not CustomRoles.Ntr))
                            sb.Append($"\n\n" + GetString($"Lovers") + Utils.GetRoleMode(CustomRoles.Lovers) + GetString($"LoversInfoLong"));
                        Utils.SendMessage(sb.ToString(), lp.PlayerId);
                    }
                    else
                        Utils.SendMessage((PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/t":
                case "/template":
                    canceled = true;
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                    else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{GetString("ForExample")}:\n{args[0]} test");
                    break;

                case "/mw":
                case "/messagewait":
                    canceled = true;
                    if (args.Length > 1 && int.TryParse(args[1], out int sec))
                    {
                        Main.MessageWait.Value = sec;
                        Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                    }
                    else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                    break;

                case "/say":
                case "/s":
                    canceled = true;
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")}</color>");
                    break;

                case "/mid":
                    canceled = true;
                    string msgText1 = GetString("PlayerIdList");
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc == null) continue;
                        msgText1 += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                    }
                    Utils.SendMessage(msgText1, PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/ban":
                case "/banir":
                    canceled = true;

                    string banReason = "";
                    if (args.Length < 3)
                    {
                        Utils.SendMessage(GetString("BanCommandNoReason"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        subArgs = args[1];
                        banReason = string.Join(" ", args.Skip(2));
                    }
                    //subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte banPlayerId))
                    {
                        Utils.SendMessage(GetString("BanCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (banPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("BanCommandBanHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var bannedPlayer = Utils.GetPlayerById(banPlayerId);
                    if (bannedPlayer == null)
                    {
                        Utils.SendMessage(GetString("BanCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    // Ban the specified player
                    AmongUsClient.Instance.KickPlayer(bannedPlayer.GetClientId(), true);
                    string bannedPlayerName = bannedPlayer.GetRealName();
                    string textToSend1 = $"{bannedPlayerName} {GetString("BanCommandBanned")}{PlayerControl.LocalPlayer.name} \nReason: {banReason}\n";
                    if (GameStates.IsInGame)
                    {
                        textToSend1 += $" {GetString("BanCommandBannedRole")} {GetString(bannedPlayer.GetCustomRole().ToString())}";
                    }
                    Utils.SendMessage(textToSend1);
                    //string moderatorName = PlayerControl.LocalPlayer.GetRealName().ToString();
                    //int startIndex = moderatorName.IndexOf("♥</color>") + "♥</color>".Length;
                    //moderatorName = moderatorName.Substring(startIndex);
                    //string extractedString = 
                    string moderatorFriendCode = PlayerControl.LocalPlayer.FriendCode.ToString();
                    string bannedPlayerFriendCode = bannedPlayer.FriendCode.ToString();
                    string modLogname = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n1) ? n1 : "";
                    string banlogname = Main.AllPlayerNames.TryGetValue(bannedPlayer.PlayerId, out var n11) ? n11 : "";
                    string logMessage = $"[{DateTime.Now}] {moderatorFriendCode},{modLogname} Banned: {bannedPlayerFriendCode},{banlogname} Reason: {banReason}";
                    File.AppendAllText(modLogFiles, logMessage + Environment.NewLine);
                    break;
                case "/warn":
                case "/aviso":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte warnPlayerId))
                    {
                        Utils.SendMessage(GetString("WarnCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (warnPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("WarnCommandWarnHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var warnedPlayer = Utils.GetPlayerById(warnPlayerId);
                    if (warnedPlayer == null)
                    {
                        Utils.SendMessage(GetString("WarnCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    // warn the specified player
                    string textToSend2 = "";
                    string warnReason = "Reason : Not specified\n";
                    string warnedPlayerName = warnedPlayer.GetRealName();
                    //textToSend2 = $" {warnedPlayerName} {GetString("WarnCommandWarned")} ~{player.name}";
                    if (args.Length > 2)
                    {
                        warnReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
                    }
                    else
                    {
                        Utils.SendMessage(GetString("WarnExample"), PlayerControl.LocalPlayer.PlayerId);
                    }
                    textToSend2 = $" {warnedPlayerName} {GetString("WarnCommandWarned")} {warnReason} ~{PlayerControl.LocalPlayer.name}";
                    Utils.SendMessage(textToSend2);
                    //string moderatorName1 = PlayerControl.LocalPlayer.GetRealName().ToString();
                    //int startIndex1 = moderatorName1.IndexOf("♥</color>") + "♥</color>".Length;
                    //moderatorName1 = moderatorName1.Substring(startIndex1);
                    string modLogname1 = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n2) ? n2 : "";
                    string warnlogname = Main.AllPlayerNames.TryGetValue(warnedPlayer.PlayerId, out var n12) ? n12 : "";

                    string moderatorFriendCode1 = PlayerControl.LocalPlayer.FriendCode.ToString();
                    string warnedPlayerFriendCode = warnedPlayer.FriendCode.ToString();
                    string warnedPlayerHashPuid = warnedPlayer.GetClient().GetHashedPuid();
                    string logMessage1 = $"[{DateTime.Now}] {moderatorFriendCode1},{modLogname1} Warned: {warnedPlayerFriendCode},{warnedPlayerHashPuid},{warnlogname} Reason: {warnReason}";
                    File.AppendAllText(modLogFiles, logMessage1 + Environment.NewLine);

                    break;
                case "/kick":
                case "/expulsar":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte kickPlayerId))
                    {
                        Utils.SendMessage(GetString("KickCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (kickPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("KickCommandKickHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var kickedPlayer = Utils.GetPlayerById(kickPlayerId);
                    if (kickedPlayer == null)
                    {
                        Utils.SendMessage(GetString("KickCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    // Kick the specified player
                    AmongUsClient.Instance.KickPlayer(kickedPlayer.GetClientId(), false);
                    string kickedPlayerName = kickedPlayer.GetRealName();
                    string kickReason = "Reason : Not specified\n";
                    if (args.Length > 2)
                        kickReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
                    else
                    {
                        Utils.SendMessage("Use /kick [id] [reason] in future. \nExample :-\n /kick 5 not following rules", PlayerControl.LocalPlayer.PlayerId);
                    }
                    string textToSend = $"{kickedPlayerName} {GetString("KickCommandKicked")} {PlayerControl.LocalPlayer.name} \n {kickReason}";

                    if (GameStates.IsInGame)
                    {
                        textToSend += $" {GetString("KickCommandKickedRole")} {GetString(kickedPlayer.GetCustomRole().ToString())}";
                    }
                    Utils.SendMessage(textToSend);
                    //string moderatorName2 = PlayerControl.LocalPlayer.GetRealName().ToString();
                    //int startIndex2 = moderatorName2.IndexOf("♥</color>") + "♥</color>".Length;
                    //moderatorName2 = moderatorName2.Substring(startIndex2);

                    string modLogname2 = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n3) ? n3 : "";
                    string kicklogname = Main.AllPlayerNames.TryGetValue(kickedPlayer.PlayerId, out var n13) ? n13 : "";

                    string moderatorFriendCode2 = PlayerControl.LocalPlayer.FriendCode.ToString();
                    string kickedPlayerFriendCode = kickedPlayer.FriendCode.ToString();
                    string kickedPlayerHashPuid = kickedPlayer.GetClient().GetHashedPuid();
                    string logMessage2 = $"[{DateTime.Now}] {moderatorFriendCode2},{modLogname2} Kicked: {kickedPlayerFriendCode},{kickedPlayerHashPuid},{kicklogname} Reason: {kickReason}";
                    File.AppendAllText(modLogFiles, logMessage2 + Environment.NewLine);

                    break;
                case "/tagcolor":
                case "/tagcolour":
                    canceled = true;
                    string name = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n) ? n : "";
                    if (name == "") break;
                    if (!name.Contains('\r') && PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag())
                    {
                        if (!GameStates.IsLobby)
                        {
                            Utils.SendMessage(GetString("ColorCommandNoLobby"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        subArgs = args.Length != 2 ? "" : args[1];
                        if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
                        {
                            Logger.Msg($"{subArgs}", "tagcolor");
                            Utils.SendMessage(GetString("TagColorInvalidHexCode"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        string tagColorFilePath = $"{sponsorTagsFiles}/{PlayerControl.LocalPlayer.FriendCode}.txt";
                        if (!File.Exists(tagColorFilePath))
                        {
                            Logger.Msg($"File Not exist, creating file at {tagColorFilePath}", "tagcolor");
                            File.Create(tagColorFilePath).Close();
                        }
                        File.WriteAllText(tagColorFilePath, $"{subArgs}");
                    }
                    break;

                case "/exe":
                    canceled = true;
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                    var player = Utils.GetPlayerById(id);
                    if (player != null)
                    {
                        player.Data.IsDead = true;
                        Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.etc;
                        player.RpcExileV2();
                        Main.PlayerStates[player.PlayerId].SetDead();
                        if (player.AmOwner) Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
                        else Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    }
                    break;

                case "/kill":
                case "/matar":
                    canceled = true;
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                    var target = Utils.GetPlayerById(id2);
                    if (target != null)
                    {
                        target.RpcMurderPlayerV3(target);
                        if (target.AmOwner) Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
                        else Utils.SendMessage(string.Format(GetString("Message.Executed"), target.Data.PlayerName));
                    }
                    break;

                case "/colour":
                case "/color":
                case "/cor":
                    canceled = true;
                    if (GameStates.IsInGame)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    subArgs = args.Length < 2 ? "" : args[1];
                    var color = Utils.MsgToColor(subArgs, true);
                    if (color == byte.MaxValue)
                    {
                        Utils.SendMessage(GetString("IllegalColor"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    PlayerControl.LocalPlayer.RpcSetColor(color);
                    Utils.SendMessage(string.Format(GetString("Message.SetColor"), subArgs), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/quit":
                case "/qt":
                case "/sair":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.CanNotUseByHost"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/xf":
                    canceled = true;
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.IsAlive()) continue;

                        pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                    //Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);
                    Utils.SendMessage(GetString("Message.TryFixName"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/id":
                    canceled = true;
                    string msgText = GetString("PlayerIdList");
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc == null) continue;
                        msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName(); 
                    }
                    Utils.SendMessage(msgText, PlayerControl.LocalPlayer.PlayerId);
                    break;

                /*
                case "/qq":
                    canceled = true;
                    if (Main.newLobby) Cloud.ShareLobby(true);
                    else Utils.SendMessage("很抱歉，每个房间车队姬只会发一次", PlayerControl.LocalPlayer.PlayerId);
                    break;
                */

                case "/changerole":
                case "/mudarfunção":
                    canceled = true;
                    if (!(DebugModeManager.AmDebugger && GameStates.IsInGame)) break;
                    if (GameStates.IsOnlineGame && !PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug) break;
                    subArgs = text.Remove(0, 11);
                    var setRole =  FixRoleNameInput(subArgs).ToLower().Trim().Replace(" ", string.Empty);
                    Logger.Info(setRole, "changerole Input");
                    foreach (var rl in CustomRolesHelper.AllRoles)
                    {
                        if (rl.IsVanilla()) continue;
                        var roleName = GetString(rl.ToString()).ToLower().Trim().TrimStart('*').Replace(" ", string.Empty);
                        //Logger.Info(roleName, "2");
                        if (setRole == roleName)
                        {
                            PlayerControl.LocalPlayer.RpcSetRole(rl.GetRoleTypes());
                            PlayerControl.LocalPlayer.RpcSetCustomRole(rl);
                            Utils.SendMessage(string.Format("Debug Set your role to {0}", rl.ToString()), PlayerControl.LocalPlayer.PlayerId);
                            Utils.NotifyRoles(ForceLoop: true);
                            Utils.MarkEveryoneDirtySettings();
                            break;
                        }
                    }
                    break;

                case "/end":
                case "/encerrar":
                    canceled = true;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                    GameManager.Instance.LogicFlow.CheckEndCriteria();
                    break;
                case "/cosid":
                    canceled = true;
                    var of = PlayerControl.LocalPlayer.Data.DefaultOutfit;
                    Logger.Warn($"ColorId: {of.ColorId}", "Get Cos Id");
                    Logger.Warn($"PetId: {of.PetId}", "Get Cos Id");
                    Logger.Warn($"HatId: {of.HatId}", "Get Cos Id");
                    Logger.Warn($"SkinId: {of.SkinId}", "Get Cos Id");
                    Logger.Warn($"VisorId: {of.VisorId}", "Get Cos Id");
                    Logger.Warn($"NamePlateId: {of.NamePlateId}", "Get Cos Id");
                    break;

                case "/mt":
                case "/hy":
                    canceled = true;
                    if (GameStates.IsMeeting)
                    {
                        MeetingHud.Instance.RpcClose();
                    }
                    else
                    {
                        PlayerControl.LocalPlayer.NoCheckStartMeeting(null, force: true);
                    }
                    break;

                case "/cs":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    PlayerControl.LocalPlayer.RPCPlayCustomSound(subArgs.Trim());
                    break;

                case "/sd":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    if (args.Length < 1 || !int.TryParse(args[1], out int sound1)) break;
                    RPC.PlaySoundRPC(PlayerControl.LocalPlayer.PlayerId, (Sounds)sound1);
                    break;
                case "/rps":
                    canceled = true;
                    subArgs = args.Length != 2 ? "" : args[1];

                    if (!GameStates.IsLobby && PlayerControl.LocalPlayer.IsAlive())
                    {
                        Utils.SendMessage(GetString("RpsCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    
                    if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice))
                    {
                        Utils.SendMessage(GetString("RpsCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (playerChoice < 0 || playerChoice > 2)
                    {
                        Utils.SendMessage(GetString("RpsCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        var rand = IRandom.Instance;
                        int botChoice = rand.Next(0, 3);
                        var rpsList = new List<string> { GetString("Rock"), GetString("Paper"), GetString("Scissors") };
                        if (botChoice == playerChoice)
                        {
                            Utils.SendMessage(string.Format(GetString("RpsDraw"), rpsList[botChoice]), PlayerControl.LocalPlayer.PlayerId);
                        }
                        else if ((botChoice == 0 && playerChoice == 2) ||
                                 (botChoice == 1 && playerChoice == 0) ||
                                 (botChoice == 2 && playerChoice == 1))
                        {
                            Utils.SendMessage(string.Format(GetString("RpsLose"), rpsList[botChoice]), PlayerControl.LocalPlayer.PlayerId);
                        }
                        else
                        {
                            Utils.SendMessage(string.Format(GetString("RpsWin"), rpsList[botChoice]), PlayerControl.LocalPlayer.PlayerId);
                        }
                        break;
                    }
                case "/coinflip":
                    canceled = true;

                    if (!GameStates.IsLobby && PlayerControl.LocalPlayer.IsAlive())
                    {
                        Utils.SendMessage(GetString("CoinFlipCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else  
                    {
                        var rand = IRandom.Instance;
                        int botChoice = rand.Next(1, 101);
                        var coinSide = (botChoice < 51) ? GetString("Heads") : GetString("Tails");
                        Utils.SendMessage(string.Format(GetString("CoinFlipResult"),coinSide), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                case "/gno":
                    canceled = true;
                    if (!GameStates.IsLobby && PlayerControl.LocalPlayer.IsAlive())
                    {
                        Utils.SendMessage(GetString("GNoCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (subArgs == "" || !int.TryParse(subArgs, out int guessedNo))
                    {
                        Utils.SendMessage(GetString("GNoCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else if (guessedNo < 0 || guessedNo > 99)
                    {
                        Utils.SendMessage(GetString("GNoCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        int targetNumber = Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0];
                        if (Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0] == -1)
                        {
                            var rand = IRandom.Instance;
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0] = rand.Next(0, 100);
                            targetNumber = Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0];
                        }
                        Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1]--;
                        if (Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1] == 0 && guessedNo != targetNumber)
                        {
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0] = -1;
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1] = 7;
                            //targetNumber = Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0];
                            Utils.SendMessage(string.Format(GetString("GNoLost"), targetNumber), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }                        
                        else if (guessedNo < targetNumber)
                        {
                            Utils.SendMessage(string.Format(GetString("GNoLow"), Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1]), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        else if (guessedNo > targetNumber)
                        {
                            Utils.SendMessage(string.Format(GetString("GNoHigh"), Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1]), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        else
                        {
                            Utils.SendMessage(string.Format(GetString("GNoWon"), Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1]), PlayerControl.LocalPlayer.PlayerId);
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0] = -1;
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][1] = 7;
                            break;
                        }

                    }
                    case "/rand":
                        canceled = true;
                        subArgs = args.Length != 3 ? "" : args[1];
                        subArgs2 = args.Length != 3 ? "" : args[2];

                        if (!GameStates.IsLobby && PlayerControl.LocalPlayer.IsAlive())
                        {
                            Utils.SendMessage(GetString("RandCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice1) || subArgs2 == "" || !int.TryParse(subArgs2, out int playerChoice2))
                        {
                            Utils.SendMessage(GetString("RandCommandInfo"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        else
                        {
                            var rand = IRandom.Instance;
                            int botResult = Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0];
                            Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0] = rand.Next(playerChoice1, playerChoice2);
                            botResult = Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0];
                            Utils.SendMessage(string.Format(GetString("RandResult"), Main.GuessNumber[PlayerControl.LocalPlayer.PlayerId][0]), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }

                default:
                    Main.isChatCommand = false;
                    break;
            }
        }
        goto Skip;
    Canceled:
        Main.isChatCommand = false;
        canceled = true;
    Skip:
        if (canceled)
        {
            Logger.Info("Command Canceled", "ChatCommand");
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(cancelVal);
        }
        return !canceled;
    }

    public static string FixRoleNameInput(string text)
    {
        text = text.Replace("着", "者").Trim().ToLower();
        return text switch
        {
            "管理員" or "管理" or "gm" => GetString("GM"),
            "賞金獵人" or "赏金" => GetString("BountyHunter"),
            "自爆兵" or "自爆" => GetString("Bomber"),
            "邪惡的追踪者" or "邪恶追踪者" or "追踪" => GetString("EvilTracker"),
            "煙花商人" or "烟花" => GetString("FireWorks"),
            "夢魘" or "夜魇" => GetString("Mare"),
            "詭雷" or "诡雷" => GetString("BoobyTrap"),
            "黑手黨" or "黑手" => GetString("Mafia"),
            "嗜血殺手" or "嗜血" => GetString("SerialKiller"),
            "千面鬼" or "千面" => GetString("ShapeMaster"),
            "狂妄殺手" or "狂妄" => GetString("Sans"),
            "殺戮機器" or "杀戮" or "机器" or "杀戮兵器" => GetString("Minimalism"),
            "蝕時者" or "蚀时" or "偷时" => GetString("TimeThief"),
            "狙擊手" or "狙击" => GetString("Sniper"),
            "傀儡師" or "傀儡" => GetString("Puppeteer"),
            "殭屍" or "丧尸" => GetString("Zombie"),
            "吸血鬼" or "吸血" => GetString("Vampire"),
            "術士" or "术士" => GetString("Warlock"),
            "駭客" or "黑客" => GetString("Hacker"),
            "刺客" or "忍者" => GetString("Assassin"),
            "礦工" or "矿工" => GetString("Miner"),
            "逃逸者" or "逃逸" => GetString("Escapee"),
            "女巫" or "女巫" => GetString("Witch"),
            "監視者" or "监管" => GetString("AntiAdminer"),
            "清道夫" or "清道" => GetString("Scavenger"),
            "窺視者" or "窥视" => GetString("Watcher"),
            "誘餌" or "大奖" or "头奖" => GetString("Bait"),
            "擺爛人" or "摆烂" => GetString("Needy"),
            "獨裁者" or "独裁" => GetString("Dictator"),
            "法醫" or "法医" => GetString("Doctor"),
            "偵探" or "侦探"=> GetString("Detective"),
            "幸運兒" or "幸运" => GetString("Luckey"),
            "大明星" or "明星" => GetString("SuperStar"),
            "網紅" or "网红"=> GetString("CyberStar"),
            "俠客" or "侠客"=> GetString("SwordsMan"),
            "正義賭怪" or "正义的赌怪" or "好赌" or "正义赌" => GetString("NiceGuesser"),
            "邪惡賭怪" or "邪恶的赌怪" or "坏赌" or "恶赌" or "邪恶赌" => GetString("EvilGuesser"),
            "市長" or "市长" => GetString("Mayor"),
            "被害妄想症" or "被害妄想" or "被迫害妄想症" or "被害" or "妄想" or "妄想症" => GetString("Paranoia"),
            "愚者" or "愚" => GetString("Psychic"),
            "修理大师" or "修理" or "维修" => GetString("SabotageMaster"),
            "警長" or "警长"=> GetString("Sheriff"),
            "告密者" or "告密" => GetString("Snitch"),
            "增速者" or "增速" => GetString("SpeedBooster"),
            "時間操控者" or "时间操控人" or "时间操控" => GetString("TimeManager"),
            "陷阱師" or "陷阱" or "小奖" => GetString("Trapper"),
            "傳送師" or "传送" => GetString("Transporter"),
            "縱火犯" or "纵火" => GetString("Arsonist"),
            "處刑人" or "处刑" => GetString("Executioner"),
            "小丑" or "丑皇" => GetString("Jester"),
            "投機者" or "投机" => GetString("Opportunist"),
            "馬里奧" or "马力欧" => GetString("Mario"),
            "恐怖分子" or "恐怖" => GetString("Terrorist"),
            "豺狼" or "蓝狼" or "狼" => GetString("Jackal"),
            "神" or "上帝" => GetString("God"),
            "情人" or "愛人" or "链子" or "老婆" or "老公" => GetString("Lovers"),
            "絕境者" or "绝境" => GetString("LastImpostor"),
            "閃電俠" or "闪电" => GetString("Flashman"),
            "靈媒" or "灵媒"=> GetString("Seer"),
            "破平者" or "破平" => GetString("Brakar"),
            "火炬" or "火炬"=> GetString("Torch"),
            "膽小" or "胆小" => GetString("Oblivious"),
            "迷惑者" or "迷幻" => GetString("Bewilder"),
            //"患者" or "患者"=> GetString("Sunglasses"),
            "蠢蛋" or "蠢狗" or "傻逼" => GetString("Fool"),
            "冤罪師" or "冤罪" => GetString("Innocent"),
            "資本家" or "资本主义" or "资本" => GetString("Capitalism"),
            "老兵" or "老兵"=> GetString("Veteran"),
            "加班狂" or "加班" => GetString("Workhorse"),
            "復仇者" or "复仇" => GetString("Avanger"),
            "鵜鶘" or "鹈鹕"=> GetString("Pelican"),
            "保鏢" or "保镖"=> GetString("Bodyguard"),
            "up" or "up主" => GetString("Youtuber"),
            "利己主義者" or "利己主义" or "利己" => GetString("Egoist"),
            "贗品商" or "赝品" => GetString("Counterfeiter"),
            "擲雷兵" or "掷雷" or "闪光弹" => GetString("Grenadier"),
            "竊票者" or "偷票" or "偷票者" or "窃票师" or "窃票" => GetString("TicketsStealer"),
            "教父" or "教父"=> GetString("Godfather"),
            "革命家" or "革命" => GetString("Revolutionist"),
            "fff團" or "fff" or "fff团" => GetString("FFF"),
            "清理工" or "清潔工" or "清洁工" or "清理" or "清洁" => GetString("Cleaner"),
            "醫生" or "医生"=> GetString("Medic"),
            "调查员" or "调查" => GetString("Divinator"),
            "雙重人格" or "双重" or "双人格" or "人格" => GetString("DualPersonality"),
            "玩家" or "玩家"=> GetString("Gamer"),
            "情報販子" or "情报" or "贩子" => GetString("Messenger"),
            "球狀閃電" or "球闪" or "球状" => GetString("BallLightning"),
            "潛藏者" or "潜藏" => GetString("DarkHide"),
            "貪婪者" or "贪婪" => GetString("Greedier"),
            "工作狂" or "工作" => GetString("Workaholic"),
            "呪狼" or "咒狼" => GetString("CursedWolf"),
            "寶箱怪" or "宝箱" => GetString("Mimic"),
            "集票者" or "集票" or "寄票" or "机票" => GetString("Collector"),
            "缺点者" or "缺点" => GetString("Glitch"),
            "奪魂者" or "多混" or "夺魂" => GetString("ImperiusCurse"),
            "自爆卡車" or "自爆" or "卡车" => GetString("Provocateur"),
            "快槍手" or "快枪" => GetString("QuickShooter"),
            "隱蔽者" or "隐蔽" or "小黑人" => GetString("Concealer"),
            "抹除者" or "抹除" => GetString("Eraser"),
            "肢解者" or "肢解" => GetString("OverKiller"),
            "劊子手" or "侩子手" or "柜子手" => GetString("Hangman"),
            "陽光開朗大男孩" or "阳光" or "开朗" or "大男孩" or "阳光开朗" or "开朗大男孩" or "阳光大男孩" => GetString("Sunnyboy"),
            "法官" or "审判" => GetString("Judge"),
            "入殮師" or "入检师" or "入殓" => GetString("Mortician"),
            "通靈師" or "通灵" => GetString("Mediumshiper"),
            "吟游詩人" or "诗人" => GetString("Bard"),
            "隱匿者" or "隐匿" or "隐身" or "隐身人" or "印尼" => GetString("Swooper"),
            "船鬼" or "船鬼"=> GetString("Crewpostor"),
            "嗜血騎士" or "血骑" or "骑士" or "bk" => GetString("BloodKnight"),
            "賭徒" or "赌徒"=> GetString("Totocalcio"),
            "分散机" or "分散机"=> GetString("Disperser"),
            "和平之鸽" or "和平之鴿" or "和平的鸽子" or "和平" => GetString("DovesOfNeace"),
            "持槍" or "持械" or "手长" => GetString("Reach"),
            "君主" or "君主"=> GetString("Monarch"),
            "野人" or "野人"=> GetString("Wildling"),
            "騙術師" or "骗术师"=> GetString("Trickster"),
            "衛道士" or "卫道士"=> GetString("Vindicator"),
            "寄生蟲" or "寄生虫"=> GetString("Parasite"),
            "抑鬱者" or "抑郁者" or "抑郁"=> GetString("Inhibitor"),
            "破壞者" or "破坏者"=> GetString("Saboteur"),
            "議員" or "议员"=> GetString("Councillor"),
            "眩暈者" or "眩晕者"=> GetString("Dazzler"),
            "被清洗的" or "被清洗的"=> GetString("Cleansed"),
            "死亡契約" or "死亡契约"=> GetString("Deathpact"),
            "吞噬者" or "吞噬者"=> GetString("Devourer"),
            "軍師" or "军师"=> GetString("EvilDiviner"),
            "化形者" or "化形者"=> GetString("Morphling"),
            "龍捲風" or "龙卷风"=> GetString("Twister"),
            "潜伏者" or "潜伏者"=> GetString("Lurker"),
            "罪犯" or "罪犯"=> GetString("Convict"),
            "幻想家" or "幻想家"=> GetString("Visionary"),
            "逃亡者" or "逃亡者"=> GetString("Refugee"),
            "失敗者" or "失败者"=> GetString("Underdog"),
            "速度者" or "速度者"=> GetString("Ludopath"),
            "天文學家" or "天文学家"=> GetString("Chronomancer"),
            "設陷者" or "设陷者"=> GetString("Pitfall"),
            "狂戰士" or "狂战士"=> GetString("Berserker"),
            "預言家" or "预言家"=> GetString("Farseer"),
            "驗屍官" or "验尸官"=> GetString("Bloodhound"),
            "正義追踪者" or "正义追踪者"=> GetString("Tracker"),
            "商人" or "商人"=> GetString("Merchant"),
            "懲罰者" or "惩罚者"=> GetString("Retributionist"),
            "捕快" or "捕快"=> GetString("Deputy"),
            "守護者" or "守护者"=> GetString("Guardian"),
            "癮君子" or "瘾君子"=> GetString("Addict"),
            "尋迹者" or "寻迹者"=> GetString("Tracefinder"),
            "神谕"=> GetString("Oracle"),
            "灵魂论者"=> GetString("Spiritualist"),
            "变色龙"=> GetString("Chameleon"),
            "检查员"=> GetString("ParityCop"),
            "仰慕者"=> GetString("Admirer"),
            "时间之主"=> GetString("TimeMaster"),
            "十字军"=> GetString("Crusader"),
            "遐想者"=> GetString("Reverie"),
            "瞭望员"=> GetString("Lookout"),
            "通信员" or "通讯员"=> GetString("Monitor"),
            "执灯人"=> GetString("Lighter"),
            "任务管理者"=> GetString("TaskManager"),
            "目击者"=> GetString("Witness"),
            "连环杀手"=> GetString("NSerialKiller"),
            "天启"=> GetString("Juggernaut"),
            "感染者"=> GetString("Infectious"),
            "病毒"=> GetString("Virus"),
            "追击者"=> GetString("Pursuer"),
            "幻影"=> GetString("Phantom"),
            "决斗者"=> GetString("Pirate"),
            "煽动者"=> GetString("Agitater"),
            "独行者"=> GetString("Maverick"),
            "被诅咒的灵魂"=> GetString("CursedSoul"),
            "小偷"=> GetString("Pickpocket"),
            "背叛者"=> GetString("Traitor"),
            "秃鹫"=> GetString("Vulture"),
            "美杜莎"=> GetString("Medusa"),
            "面包师"=> GetString("Baker"),
            "饥荒"=> GetString("Famine"),
            "灵魂召唤者"=> GetString("Spiritcaller"),
            "失忆者"=> GetString("Amnesiac"),
            "受虐狂"=> GetString("Masochist"),
            "末日赌怪"=> GetString("Doomsayer"),
            "裹尸布"=> GetString("Shroud"),
            "狼人"=> GetString("Werewolf"),
            "萨满"=> GetString("Shaman"),
            "探索者"=> GetString("Seeker"),
            //"神秘者"=> GetString("Occultist"),
            "遮蔽者"=> GetString("Shade"),
            "灵魂收集者"=> GetString("SoulCollector"),
            "浪漫者"=> GetString("Romantic"),
            "复仇浪漫者"=> GetString("VengefulRomantic"),
            "无情浪漫者"=> GetString("RuthlessRomantic"),
            "投毒者"=> GetString("Poisoner"),
            "巫师"=> GetString("HexMaster"),
            "魅影"=> GetString("Wraith"),
            "扫把星"=> GetString("Jinx"),
            "药剂师"=> GetString("PotionMaster"),
            "祭祀者"=> GetString("Ritualist"),
            "亡灵巫师"=> GetString("Necromancer"),
            "好迷你船员" => GetString("NiceMini"),
            "坏迷你船员" => GetString("EvilMini"),
            _ => text,
        };
    }

    public static bool GetRoleByName(string name, out CustomRoles role)
    {
        role = new();
        if (name == "" || name == string.Empty) return false;

        if ((TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.SChinese) == SupportedLangs.SChinese)
        {
            Regex r = new("[\u4e00-\u9fa5]+$");
            MatchCollection mc = r.Matches(name);
            string result = string.Empty;
            for (int i = 0; i < mc.Count; i++)
            {
                if (mc[i].ToString() == "是") continue;
                result += mc[i]; //匹配结果是完整的数字，此处可以不做拼接的
            }
            name = FixRoleNameInput(result.Replace("是", string.Empty).Trim());
        }
        else name = name.Trim().ToLower();

        foreach (var rl in CustomRolesHelper.AllRoles)
        {
            if (rl.IsVanilla()) continue;
            var roleName = GetString(rl.ToString()).ToLower().Trim().Replace(" ", "");
            string nameWithoutId = Regex.Replace(name.Replace(" ", ""), @"^\d+", "");
            if (nameWithoutId == roleName)
            {          
                role = rl;
                return true;
            }
        }
        return false;
    }
    public static void SendRolesInfo(string role, byte playerId, bool isDev = false, bool isUp = false)
    {
        role = role.Trim().ToLower();
        if (role.StartsWith("/r")) role.Replace("/r", string.Empty);
        if (role.StartsWith("/up")) role.Replace("/up", string.Empty);
        if (role.StartsWith("/dev")) role.Replace("/dev", string.Empty);
        if (role.EndsWith("\r\n")) role.Replace("\r\n", string.Empty);
        if (role.EndsWith("\n")) role.Replace("\n", string.Empty);

        if (role == "" || role == string.Empty)
        {
            Utils.ShowActiveRoles(playerId);
            return;
        }

        role = FixRoleNameInput(role).ToLower().Trim().Replace(" ", string.Empty);

        foreach (var rl in CustomRolesHelper.AllRoles)
        {
            if (rl.IsVanilla()) continue;
            if (rl == CustomRoles.Mini) continue;
            var roleName = GetString(rl.ToString());
            if (role == roleName.ToLower().Trim().TrimStart('*').Replace(" ", string.Empty))
            {
                string devMark = "";
                if ((isDev || isUp) && GameStates.IsLobby)
                {
                    devMark = "▲";
                    if (CustomRolesHelper.IsAdditionRole(rl) || rl is CustomRoles.GM) devMark = "";
                    if (rl.GetCount() < 1 || rl.GetMode() == 0) devMark = "";
                    if (isUp)
                    {
                        if (devMark == "▲") Utils.SendMessage(string.Format(GetString("Message.YTPlanSelected"), roleName), playerId);
                        else Utils.SendMessage(string.Format(GetString("Message.YTPlanSelectFailed"), roleName), playerId);
                    }
                    if (isDev)
                    {
                        if (devMark == "▲") Utils.SendMessage(string.Format(GetString("Message.DevlanSelected"), roleName), playerId);
                        else Utils.SendMessage(string.Format(GetString("Message.DevlanSelectFailed"), roleName), playerId);
                    }
                    if (devMark == "▲")
                    {
                        byte pid = playerId == 255 ? (byte)0 : playerId;
                        Main.DevRole.Remove(pid);
                        Main.DevRole.Add(pid, rl);
                    }
                    if (isUp) return;
                    if (isDev) return;
                }
                var sb = new StringBuilder();
                sb.Append(devMark + roleName + Utils.GetRoleMode(rl) + GetString($"{rl}InfoLong"));
                if (Options.CustomRoleSpawnChances.ContainsKey(rl))
                {
                    Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[rl], ref sb, command: true);
                    var txt = sb.ToString();
                    sb.Clear().Append(txt.RemoveHtmlTags());
                }
                Utils.SendMessage(sb.ToString(), playerId);
                return;
            }
        }
        if (isUp) Utils.SendMessage(GetString("Message.YTPlanCanNotFindRoleThePlayerEnter"), playerId);
        else Utils.SendMessage(GetString("Message.CanNotFindRoleThePlayerEnter"), playerId);

        if (isDev) Utils.SendMessage(GetString("Message.DevlanCanNotFindRoleThePlayerEnter"), playerId);
        else Utils.SendMessage(GetString("Message.CanNotFindRoleThePlayerEnter"), playerId);
        return;
    }
    public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
    {
        canceled = false;
        if (!AmongUsClient.Instance.AmHost) return;
        if ((Options.NewHideMsg.GetBool() || Blackmailer.IsEnable) && player.PlayerId != 0) // Blackmailer.ForBlackmailer.Contains(player.PlayerId)) && PlayerControl.LocalPlayer.IsAlive() && player.PlayerId != 0)
        {
            ChatManager.SendMessage(player, text);
        }

        if (text.StartsWith("\n")) text = text[1..];
        //if (!text.StartsWith("/")) return;
        string[] args = text.Split(' ');
        string subArgs = "";
        string subArgs2 = "";
        //if (text.Length >= 3) if (text[..2] == "/r" && text[..3] != "/rn") args[0] = "/r";
        //   if (SpamManager.CheckSpam(player, text)) return;
        if (GuessManager.GuesserMsg(player, text)) { canceled = true; return; }
        if (Judge.TrialMsg(player, text)) { canceled = true; return; }
        if (President.EndMsg(player, text)) { canceled = true; return; }
        if (ParityCop.ParityCheckMsg(player, text)) { canceled = true; return; }
        if (Pirate.DuelCheckMsg(player, text)) { canceled = true; return; }
        if (Councillor.MurderMsg(player, text)) { canceled = true; return; }
        if (Swapper.SwapMsg(player, text)) { canceled = true; return; }
        if (Mediumshiper.MsMsg(player, text)) return;
        if (MafiaRevengeManager.MafiaMsgCheck(player, text)) return;
        if (RetributionistRevengeManager.RetributionistMsgCheck(player, text)) return;
        Directory.CreateDirectory(modTagsFiles);
        Directory.CreateDirectory(vipTagsFiles);
        Directory.CreateDirectory(sponsorTagsFiles);

        if (Blackmailer.ForBlackmailer.Contains(player.PlayerId) && player.IsAlive() && player.PlayerId != 0)
        {
            ChatManager.SendPreviousMessagesToAll();
            ChatManager.cancel = false;
            canceled = true; 
            return; 
        }
        
        switch (args[0])
        {
            case "/l":
            case "/lastresult":
            case "/fimdejogo":
                Utils.ShowKillLog(player.PlayerId);
                Utils.ShowLastRoles(player.PlayerId);
                Utils.ShowLastResult(player.PlayerId);
                break;

            case "/gr":
            case "/gameresults":
            case "/resultados":
                Utils.ShowLastResult(player.PlayerId);
                break;

            case "/kh":
            case "/killlog":
                Utils.ShowKillLog(player.PlayerId);
                break;

            case "/rs":
            case "/sum":
            case "/rolesummary":
            case "/sumario":
            case "/sumário":
            case "/summary":
                Utils.ShowLastRoles(player.PlayerId);
                break;


            case "/n":
            case "/now":
            case "/atual":
                subArgs = args.Length < 2 ? "" : args[1];
                switch (subArgs)
                {
                    case "r":
                    case "roles":
                    case "funções":
                        Utils.ShowActiveRoles(player.PlayerId);
                        break;
                    case "a":
                    case "all":
                    case "tudo":
                        Utils.ShowAllActiveSettings(player.PlayerId);
                        break;
                    default:
                        Utils.ShowActiveSettings(player.PlayerId);
                        break;
                }
                break;

            case "/r":
                subArgs = text.Remove(0, 2);
                SendRolesInfo(subArgs, player.PlayerId, player.FriendCode.GetDevUser().DeBug);
                break;

            case "/h":
            case "/help":
            case "/ajuda":
                Utils.ShowHelpToClient(player.PlayerId);
                break;

            case "/m":
            case "/myrole":
            case "/minhafunção":
                var role = player.GetCustomRole();
                if (GameStates.IsInGame)
                {
                    var sb = new StringBuilder();
                    //sb.Append(String.Format(GetString("PlayerNameForRoleInfo"), Main.AllPlayerNames[player.PlayerId]));
                    sb.Append(GetString(role.ToString()) + Utils.GetRoleMode(role) + player.GetRoleInfo(true));
                    if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                        Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref sb, command: true);
                    var txt = sb.ToString();
                    sb.Clear().Append(txt.RemoveHtmlTags());
                    foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles.ToArray())
                        sb.Append($"\n\n" + GetString($"{subRole}") + Utils.GetRoleMode(subRole) + GetString($"{subRole}InfoLong"));
                    if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and not CustomRoles.Ntr))
                        sb.Append($"\n\n" + GetString($"Lovers") + Utils.GetRoleMode(CustomRoles.Lovers) + GetString($"LoversInfoLong"));
                    Utils.SendMessage(sb.ToString(), player.PlayerId);
                }
                else
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                break;

            case "/up":
                subArgs = text.Remove(0, 3);
                if (!Options.EnableUpMode.GetBool())
                {
                    Utils.SendMessage(string.Format(GetString("Message.YTPlanDisabled"), GetString("EnableYTPlan")), player.PlayerId);
                    break;
                }
                else
                {
                    Utils.SendMessage(GetString("Message.OnlyCanBeUsedByHost"), player.PlayerId);
                    break;
                }

            case "/dev":
                subArgs = text.Remove(0, 4);
                if (!Options.EnableDevMode.GetBool())
                {
                    Utils.SendMessage(string.Format(GetString("Message.DevlanDisabled"), GetString("EnableDevPlan")), player.PlayerId);
                    break;
                }
                else
                {
                    Utils.SendMessage(GetString("Message.OnlyCanBeUsedByDev"), player.PlayerId);
                    break;
                }

            case "/win":
            case "/winner":
            case "/vencedor":
                if (!Main.winnerNameList.Any()) Utils.SendMessage(GetString("NoInfoExists"), player.PlayerId);
                else Utils.SendMessage("Winner: " + string.Join(", ", Main.winnerNameList), player.PlayerId);
                break;

            /*        case "/icons":
                        { 
                            Utils.SendMessage(GetString("Command.icons"), player.PlayerId);
                            break;
                        } */

            case "/kcount":
                int impnum = 0;
                int neutralnum = 0;

                foreach (var players in Main.AllAlivePlayerControls)
                {
                    if (Options.ShowImpRemainOnEject.GetBool())
                    {
                        if (players.GetCustomRole().IsImpostor())
                            impnum++;
                    }
                    if (Options.ShowNKRemainOnEject.GetBool())
                    {
                        if (players.GetCustomRole().IsNK())
                            neutralnum++;
                    }
                }
                if (!GameStates.IsLobby && Options.EnableKillerLeftCommand.GetBool())
                {
                    Utils.SendMessage(GetString("Remaining.ImpostorCount") + impnum + "\n\r" + GetString("Remaining.NeutralCount") + neutralnum, player.PlayerId);
                }
                break;

            case "/d":
            case "/death":
            case "/morto":

                if (GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }
                else if (player.IsAlive())
                {
                    Utils.SendMessage(GetString("DeathCmd.HeyPlayer") + "<b>" + player.GetRealName() + "</b>" + GetString("DeathCmd.YouAreRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>\n\n" + GetString("DeathCmd.NotDead"), player.PlayerId);
                    break;
                }
                else if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Vote)
                {
                    Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Ejected"), player.PlayerId);
                    break;
                }
                else if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.Shrouded)
                {
                    Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Shrouded"), player.PlayerId);
                    break;
                }
                else if (Main.PlayerStates[player.PlayerId].deathReason == PlayerState.DeathReason.FollowingSuicide)
                {
                    Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Lovers"), player.PlayerId);
                    break;
                }
                else
                {
                    var killer = player.GetRealKiller();
                    Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.DeathReason") + "<b>" + Utils.GetVitalText(player.PlayerId) + "</b>" + "\n\r" + "</b>" + "\n\r" + GetString("DeathCmd.KillerName") + "<b>" + killer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.KillerRole") + "<b>" + $"<color={Utils.GetRoleColorCode(killer.GetCustomRole())}>{Utils.GetRoleName(killer.GetCustomRole())}</color>" + "</b>", player.PlayerId);
                    break;
                }

            case "/t":
            case "/template":
                if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                break;

            case "/colour":
            case "/color":
            case "/cor":    
                if (Options.PlayerCanSetColor.GetBool() || player.FriendCode.GetDevUser().IsDev || player.FriendCode.GetDevUser().ColorCmd || Utils.IsPlayerVIP(player.FriendCode))
                {
                    if (GameStates.IsInGame)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                        break;
                    }
                    subArgs = args.Length < 2 ? "" : args[1];
                    var color = Utils.MsgToColor(subArgs);
                    if (color == byte.MaxValue)
                    {
                        Utils.SendMessage(GetString("IllegalColor"), player.PlayerId);
                        break;
                    }
                    player.RpcSetColor(color);
                    Utils.SendMessage(string.Format(GetString("Message.SetColor"), subArgs), player.PlayerId);
                }
                else
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                }
                break;

            case "/quit":
            case "/qt":
            case "/sair":
                subArgs = args.Length < 2 ? "" : args[1];
                var cid = player.PlayerId.ToString();
                cid = cid.Length != 1 ? cid.Substring(1, 1) : cid;
                if (subArgs.Equals(cid))
                {
                    string name = player.GetRealName();
                    Utils.SendMessage(string.Format(GetString("Message.PlayerQuitForever"), name));
                    AmongUsClient.Instance.KickPlayer(player.GetClientId(), true);
                }
                else
                {
                    Utils.SendMessage(string.Format(GetString("SureUse.quit"), cid), player.PlayerId);
                }
                break;
            case "/id":
                if (Options.ApplyModeratorList.GetValue() == 0 || !Utils.IsPlayerModerator(player.FriendCode)) break;

                string msgText = GetString("PlayerIdList");
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc == null) continue;
                    msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                }
                Utils.SendMessage(msgText, player.PlayerId);
                break;
            case "/mid":
                //canceled = true;
                //checking if modlist on or not
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("midCommandDisabled"), player.PlayerId);
                    break;
                }
                //checking if player is has necessary privellege or not
                if (!Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("midCommandNoAccess"), player.PlayerId);
                    break;
                }
                string msgText1 = GetString("PlayerIdList");
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc == null) continue;
                    msgText1 += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                }
                Utils.SendMessage(msgText1, player.PlayerId);
                break;
            case "/ban":
            case "/banir":
                //canceled = true;
                // Check if the ban command is enabled in the settings
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("BanCommandDisabled"), player.PlayerId);
                    break;
                }

                // Check if the player has the necessary privileges to use the command
                if (!Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("BanCommandNoAccess"), player.PlayerId);
                    break;
                }
                string banReason = "";
                if (args.Length < 3)
                {
                    Utils.SendMessage(GetString("BanCommandNoReason"), player.PlayerId);
                    break;
                }
                else
                {
                    subArgs = args[1];
                    banReason = string.Join(" ", args.Skip(2));
                }
                //subArgs = args.Length < 2 ? "" : args[1];
                if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte banPlayerId))
                {
                    Utils.SendMessage(GetString("BanCommandInvalidID"), player.PlayerId);
                    break;
                }

                if (banPlayerId == 0)
                {
                    Utils.SendMessage(GetString("BanCommandBanHost"), player.PlayerId);
                    break;
                }

                var bannedPlayer = Utils.GetPlayerById(banPlayerId);
                if (bannedPlayer == null)
                {
                    Utils.SendMessage(GetString("BanCommandInvalidID"), player.PlayerId);
                    break;
                }

                // Prevent moderators from baning other moderators
                if (Utils.IsPlayerModerator(bannedPlayer.FriendCode))
                {
                    Utils.SendMessage(GetString("BanCommandBanMod"), player.PlayerId);
                    break;
                }

                // Ban the specified player
                AmongUsClient.Instance.KickPlayer(bannedPlayer.GetClientId(), true);
                string bannedPlayerName = bannedPlayer.GetRealName();
                string textToSend1 = $"{bannedPlayerName} {GetString("BanCommandBanned")}{player.name} \nReason: {banReason}\n";
                if (GameStates.IsInGame)
                {
                    textToSend1 += $" {GetString("BanCommandBannedRole")} {GetString(bannedPlayer.GetCustomRole().ToString())}";
                }
                Utils.SendMessage(textToSend1);
                //string moderatorName = player.GetRealName().ToString();
                //int startIndex = moderatorName.IndexOf("♥</color>") + "♥</color>".Length;
                //moderatorName = moderatorName.Substring(startIndex);
                //string extractedString = 
                string modLogname = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n1) ? n1 : "";
                string banlogname = Main.AllPlayerNames.TryGetValue(bannedPlayer.PlayerId, out var n11) ? n11 : "";
                string moderatorFriendCode = player.FriendCode.ToString();
                string bannedPlayerFriendCode = bannedPlayer.FriendCode.ToString();
                string bannedPlayerHashPuid = bannedPlayer.GetClient().GetHashedPuid();
                string logMessage = $"[{DateTime.Now}] {moderatorFriendCode},{modLogname} Banned: {bannedPlayerFriendCode},{bannedPlayerHashPuid},{banlogname} Reason: {banReason}";
                File.AppendAllText(modLogFiles, logMessage + Environment.NewLine);
                break;
            case "/warn":
            case "/aviso":
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("WarnCommandDisabled"), player.PlayerId);
                    break;
                }
                if (!Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("WarnCommandNoAccess"), player.PlayerId);
                    break;
                }
                subArgs = args.Length < 2 ? "" : args[1];
                if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte warnPlayerId))
                {
                    Utils.SendMessage(GetString("WarnCommandInvalidID"), player.PlayerId);
                    break;
                }
                if (warnPlayerId == 0)
                {
                    Utils.SendMessage(GetString("WarnCommandWarnHost"), player.PlayerId);
                    break;
                }

                var warnedPlayer = Utils.GetPlayerById(warnPlayerId);
                if (warnedPlayer == null)
                {
                    Utils.SendMessage(GetString("WarnCommandInvalidID"), player.PlayerId);
                    break;
                }

                // Prevent moderators from warning other moderators
                if (Utils.IsPlayerModerator(warnedPlayer.FriendCode))
                {
                    Utils.SendMessage(GetString("WarnCommandWarnMod"), player.PlayerId);
                    break;
                }
                // warn the specified player
                string warnReason = "Reason : Not specified\n";
                string warnedPlayerName = warnedPlayer.GetRealName();
                //textToSend2 = $" {warnedPlayerName} {GetString("WarnCommandWarned")} ~{player.name}";
                if (args.Length > 2)
                {
                    warnReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
                }
                else
                {
                    Utils.SendMessage("Use /warn [id] [reason] in future. \nExample :-\n /warn 5 lava chatting", player.PlayerId);
                }
                Utils.SendMessage($" {warnedPlayerName} {GetString("WarnCommandWarned")} {warnReason} ~{player.name}");
                //string moderatorName1 = player.GetRealName().ToString();
                //int startIndex1 = moderatorName1.IndexOf("♥</color>") + "♥</color>".Length;
                //moderatorName1 = moderatorName1.Substring(startIndex1);
                string modLogname1 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n2) ? n2 : "";
                string warnlogname = Main.AllPlayerNames.TryGetValue(warnedPlayer.PlayerId, out var n12) ? n12 : "";
                string moderatorFriendCode1 = player.FriendCode.ToString();
                string warnedPlayerFriendCode = warnedPlayer.FriendCode.ToString();
                string warnedPlayerHashPuid = warnedPlayer.GetClient().GetHashedPuid();
                string logMessage1 = $"[{DateTime.Now}] {moderatorFriendCode1},{modLogname1} Warned: {warnedPlayerFriendCode},{warnedPlayerHashPuid},{warnlogname} Reason: {warnReason}";
                File.AppendAllText(modLogFiles, logMessage1 + Environment.NewLine);

                break;
            case "/kick":
            case "/expulsar":
                // Check if the kick command is enabled in the settings
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("KickCommandDisabled"), player.PlayerId);
                    break;
                }

                // Check if the player has the necessary privileges to use the command
                if (!Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("KickCommandNoAccess"), player.PlayerId);
                    break;
                }

                subArgs = args.Length < 2 ? "" : args[1];
                if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte kickPlayerId))
                {
                    Utils.SendMessage(GetString("KickCommandInvalidID"), player.PlayerId);
                    break;
                }

                if (kickPlayerId == 0)
                {
                    Utils.SendMessage(GetString("KickCommandKickHost"), player.PlayerId);
                    break;
                }

                var kickedPlayer = Utils.GetPlayerById(kickPlayerId);
                if (kickedPlayer == null)
                {
                    Utils.SendMessage(GetString("KickCommandInvalidID"), player.PlayerId);
                    break;
                }

                // Prevent moderators from kicking other moderators
                if (Utils.IsPlayerModerator(kickedPlayer.FriendCode))
                {
                    Utils.SendMessage(GetString("KickCommandKickMod"), player.PlayerId);
                    break;
                }

                // Kick the specified player
                AmongUsClient.Instance.KickPlayer(kickedPlayer.GetClientId(), false);
                string kickedPlayerName = kickedPlayer.GetRealName();
                string kickReason = "Reason : Not specified\n";
                if (args.Length > 2)
                    kickReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
                else
                {
                    Utils.SendMessage("Use /kick [id] [reason] in future. \nExample :-\n /kick 5 not following rules", player.PlayerId);
                }
                string textToSend = $"{kickedPlayerName} {GetString("KickCommandKicked")} {player.name} \n {kickReason}";

                if (GameStates.IsInGame)
                {
                    textToSend += $" {GetString("KickCommandKickedRole")} {GetString(kickedPlayer.GetCustomRole().ToString())}";
                }
                Utils.SendMessage(textToSend);
                //string moderatorName2 = player.GetRealName().ToString();
                //int startIndex2 = moderatorName2.IndexOf("♥</color>") + "♥</color>".Length;
                //moderatorName2 = moderatorName2.Substring(startIndex2);
                string modLogname2 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n3) ? n3 : "";
                string kicklogname = Main.AllPlayerNames.TryGetValue(kickedPlayer.PlayerId, out var n13) ? n13 : "";

                string moderatorFriendCode2 = player.FriendCode.ToString();
                string kickedPlayerFriendCode = kickedPlayer.FriendCode.ToString();
                string kickedPlayerHashPuid = kickedPlayer.GetClient().GetHashedPuid();
                string logMessage2 = $"[{DateTime.Now}] {moderatorFriendCode2},{modLogname2} Kicked: {kickedPlayerFriendCode},{kickedPlayerHashPuid},{kicklogname} Reason: {kickReason}";
                File.AppendAllText(modLogFiles, logMessage2 + Environment.NewLine);

                break;
            case "/modcolor":
            case "/modcolour":
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("ColorCommandDisabled"), player.PlayerId);
                    break;
                }
                if (!Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("ColorCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("ColorCommandNoLobby"), player.PlayerId);
                    break;
                }
                if (!Options.GradientTagsOpt.GetBool()) 
                {
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "modcolor");
                        Utils.SendMessage(GetString("ColorInvalidHexCode"), player.PlayerId);
                        break;
                    }
                    string colorFilePath = $"{modTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(colorFilePath))
                    {
                        Logger.Warn($"File Not exist, creating file at {modTagsFiles}/{player.FriendCode}.txt", "modcolor");
                        File.Create(colorFilePath).Close();
                    }

                    File.WriteAllText(colorFilePath, $"{subArgs}");
                    break;
                }
                else
                {
                    subArgs = args.Length < 3 ? "" : args[1] + " " + args[2];
                    Regex regex = new Regex(@"^[0-9A-Fa-f]{6}\s[0-9A-Fa-f]{6}$");
                    if (string.IsNullOrEmpty(subArgs) || !regex.IsMatch(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "modcolor");
                        Utils.SendMessage(GetString("ColorInvalidGradientCode"), player.PlayerId);
                        break;
                    }
                    string colorFilePath = $"{modTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(colorFilePath))
                    {
                        Logger.Msg($"File Not exist, creating file at {modTagsFiles}/{player.FriendCode}.txt", "modcolor");
                        File.Create(colorFilePath).Close();
                    }
                    //Logger.Msg($"File exists, creating file at {modTagsFiles}/{player.FriendCode}.txt", "modcolor");
                    //Logger.Msg($"{subArgs}","modcolor");
                    File.WriteAllText(colorFilePath, $"{subArgs}");
                    break;
                }
            case "/vipcolor":
            case "/vipcolour":
                if (Options.ApplyVipList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("VipColorCommandDisabled"), player.PlayerId);
                    break;
                }
                if (!Utils.IsPlayerVIP(player.FriendCode))
                {
                    Utils.SendMessage(GetString("VipColorCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("VipColorCommandNoLobby"), player.PlayerId);
                    break;
                }
                if (!Options.GradientTagsOpt.GetBool()) 
                { 
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "vipcolor");
                        Utils.SendMessage(GetString("VipColorInvalidHexCode"), player.PlayerId);
                        break;
                    }
                    string colorFilePathh = $"{vipTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(colorFilePathh))
                    {
                        Logger.Warn($"File Not exist, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                        File.Create(colorFilePathh).Close();
                    }
        
                    File.WriteAllText(colorFilePathh, $"{subArgs}");
                    break;
                }
                else
                {
                    subArgs = args.Length < 3 ? "" : args[1] + " " + args[2];
                    Regex regexx = new Regex(@"^[0-9A-Fa-f]{6}\s[0-9A-Fa-f]{6}$");
                    if (string.IsNullOrEmpty(subArgs) || !regexx.IsMatch(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "vipcolor");
                        Utils.SendMessage(GetString("VipColorInvalidGradientCode"), player.PlayerId);
                        break;
                    }
                    string colorFilePathh = $"{vipTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(colorFilePathh))
                    {
                        Logger.Msg($"File Not exist, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                        File.Create(colorFilePathh).Close();
                    }
                    //Logger.Msg($"File exists, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                    //Logger.Msg($"{subArgs}","modcolor");
                    File.WriteAllText(colorFilePathh, $"{subArgs}");
                    break;
                }
            case "/tagcolor":
            case "/tagcolour":
                string name1 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n) ? n : "";
                if (name1 == "") break;
                if (!name1.Contains('\r') && player.FriendCode.GetDevUser().HasTag())
                {
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("ColorCommandNoLobby"), player.PlayerId);
                        break;
                    }
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
                    {
                        Logger.Msg($"{subArgs}", "tagcolor");
                        Utils.SendMessage(GetString("TagColorInvalidHexCode"), player.PlayerId);
                        break;
                    }
                    string tagColorFilePath = $"{sponsorTagsFiles}/{player.FriendCode}.txt";
                    if (!File.Exists(tagColorFilePath))
                    {
                        Logger.Msg($"File Not exist, creating file at {tagColorFilePath}", "tagcolor");
                        File.Create(tagColorFilePath).Close();
                    }

                    File.WriteAllText(tagColorFilePath, $"{subArgs}");
                }
                break;

            case "/xf":
                if (GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.IsAlive()) continue;

                    pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                }
                ChatUpdatePatch.DoBlockChat = false;
                //Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);
                Utils.SendMessage(GetString("Message.TryFixName"), player.PlayerId);
                break;

            case "/say":
            case "/s":
                if (player.FriendCode.GetDevUser().IsDev)
                {
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color={Main.ModColor}>{GetString("MessageFromDev")}</color>");
                }
                else if (player.FriendCode.IsDevUser())
                {
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#4bc9b0>{GetString("MessageFromSponsor")}</color>");
                }
                else if (Utils.IsPlayerModerator(player.FriendCode))
                {
                    if (Options.ApplyModeratorList.GetValue() == 0 || Options.AllowSayCommand.GetBool() == false)
                    {
                        Utils.SendMessage(GetString("SayCommandDisabled"), player.PlayerId);
                        break;
                    }
                    else
                    {
                        if (args.Length > 1)
                            Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#8bbee0>{GetString("MessageFromModerator")}<size=1.25>{player.GetRealName()}</size></color>");
                        //string moderatorName3 = player.GetRealName().ToString();
                        //int startIndex3 = moderatorName3.IndexOf("♥</color>") + "♥</color>".Length;
                        //moderatorName3 = moderatorName3.Substring(startIndex3);
                        string modLogname3 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n4) ? n4 : "";

                        string moderatorFriendCode3 = player.FriendCode.ToString();
                        string logMessage3 = $"[{DateTime.Now}] {moderatorFriendCode3},{modLogname3} used /s: {args.Skip(1).Join(delimiter: " ")}";
                        File.AppendAllText(modLogFiles, logMessage3 + Environment.NewLine);

                    }
                }
                break;
            case "/rps":
                //canceled = true;
                subArgs = args.Length != 2 ? "" : args[1];

                if (!GameStates.IsLobby && player.IsAlive())
                {
                    Utils.SendMessage(GetString("RpsCommandInfo"), player.PlayerId);
                    break;
                }

                if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice))
                {
                    Utils.SendMessage(GetString("RpsCommandInfo"), player.PlayerId);
                    break;
                }
                else if (playerChoice < 0 || playerChoice > 2)
                {
                    Utils.SendMessage(GetString("RpsCommandInfo"), player.PlayerId);
                    break;
                }
                else
                {
                    var rand = IRandom.Instance;
                    int botChoice = rand.Next(0, 3);
                    var rpsList = new List<string> { GetString("Rock"), GetString("Paper"), GetString("Scissors") };
                    if (botChoice == playerChoice)
                    {
                        Utils.SendMessage(string.Format(GetString("RpsDraw"), rpsList[botChoice]), player.PlayerId);
                    }
                    else if ((botChoice == 0 && playerChoice == 2) ||
                             (botChoice == 1 && playerChoice == 0) ||
                             (botChoice == 2 && playerChoice == 1))
                    {
                        Utils.SendMessage(string.Format(GetString("RpsLose"), rpsList[botChoice]), player.PlayerId);
                    }
                    else
                    {
                        Utils.SendMessage(string.Format(GetString("RpsWin"), rpsList[botChoice]), player.PlayerId);
                    }
                    break;
                }
            case "/coinflip":
                //canceled = true;

                if (!GameStates.IsLobby && player.IsAlive())
                {
                    Utils.SendMessage(GetString("CoinflipCommandInfo"), player.PlayerId);
                    break;
                }
                else
                {
                    var rand = IRandom.Instance;
                    int botChoice = rand.Next(1,101);
                    var coinSide = (botChoice < 51) ? GetString("Heads") : GetString("Tails");
                    Utils.SendMessage(string.Format(GetString("CoinFlipResult"), coinSide), player.PlayerId);
                    break;
                }
            case "/gno":
                //canceled = true;
                if (!GameStates.IsLobby && player.IsAlive())
                {
                    Utils.SendMessage(GetString("GNoCommandInfo"), player.PlayerId);
                    break;
                }
                subArgs = args.Length != 2 ? "" : args[1];
                if (subArgs == "" || !int.TryParse(subArgs, out int guessedNo))
                {
                    Utils.SendMessage(GetString("GNoCommandInfo"), player.PlayerId);
                    break;
                }
                else if (guessedNo < 0 || guessedNo > 99)
                {
                    Utils.SendMessage(GetString("GNoCommandInfo"), player.PlayerId);
                    break;
                }
                else
                {
                    int targetNumber = Main.GuessNumber[player.PlayerId][0];
                    if (Main.GuessNumber[player.PlayerId][0] == -1)
                    {
                        var rand = IRandom.Instance;
                        Main.GuessNumber[player.PlayerId][0] = rand.Next(0, 100);
                        targetNumber = Main.GuessNumber[player.PlayerId][0];
                    }
                    Main.GuessNumber[player.PlayerId][1]--;
                    if (Main.GuessNumber[player.PlayerId][1] == 0 && guessedNo != targetNumber)
                    {
                        Main.GuessNumber[player.PlayerId][0] = -1;
                        Main.GuessNumber[player.PlayerId][1] = 7;
                        //targetNumber = Main.GuessNumber[player.PlayerId][0];
                        Utils.SendMessage(string.Format(GetString("GNoLost"), targetNumber), player.PlayerId);
                        break;
                    }
                    else if (guessedNo < targetNumber)
                    {
                        Utils.SendMessage(string.Format(GetString("GNoLow"), Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                        break;
                    }
                    else if (guessedNo > targetNumber)
                    {
                        Utils.SendMessage(string.Format(GetString("GNoHigh"), Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                        break;
                    }
                    else
                    {
                        Utils.SendMessage(string.Format(GetString("GNoWon"), 7-Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                        Main.GuessNumber[player.PlayerId][0] = -1;
                        Main.GuessNumber[player.PlayerId][1] = 7;
                        break;
                    }
                }
            case "/rand":
                subArgs = args.Length != 3 ? "" : args[1];
                subArgs2 = args.Length != 3 ? "" : args[2];

                if (!GameStates.IsLobby && player.IsAlive())
                {
                    Utils.SendMessage(GetString("RandCommandInfo"), player.PlayerId);
                    break;
                }
                if (subArgs == "" || !int.TryParse(subArgs, out int playerChoice1) || subArgs2 == "" || !int.TryParse(subArgs2, out int playerChoice2))
                {
                    Utils.SendMessage(GetString("RandCommandInfo"), player.PlayerId);
                    break;
                }
                else
                {
                    var rand = IRandom.Instance;
                    int botResult = Main.GuessNumber[player.PlayerId][0];
                    Main.GuessNumber[player.PlayerId][0] = rand.Next(playerChoice1, playerChoice2);
                    botResult = Main.GuessNumber[player.PlayerId][0];
                    Utils.SendMessage(string.Format(GetString("RandResult"), Main.GuessNumber[player.PlayerId][0]), player.PlayerId);
                    break;
                }


            default:
                if (SpamManager.CheckSpam(player, text)) return;
                break;
        }
    }
}
[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
class ChatUpdatePatch
{
    public static bool DoBlockChat = false;
    public static void Postfix(ChatController __instance)
    {
        if (!AmongUsClient.Instance.AmHost || !Main.MessagesToSend.Any() || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.timeSinceLastMessage)) return;
        if (DoBlockChat) return;
        var player = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
        if (player == null) return;
        (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
        Main.MessagesToSend.RemoveAt(0);
        int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
        var name = player.Data.PlayerName;
        if (clientId == -1)
        {
            player.SetName(title);
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            player.SetName(name);
        }
        var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
        writer.StartMessage(clientId);
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(title)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
            .Write(msg)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(player.Data.PlayerName)
            .EndRpc();
        writer.EndMessage();
        writer.SendMessage();
        __instance.timeSinceLastMessage = 0f;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
internal class AddChatPatch
{
    public static void Postfix(string chatText)
    {
        switch (chatText)
        {
            default:
                break;
        }
        if (!AmongUsClient.Instance.AmHost) return;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
class RpcSendChatPatch
{
    public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
    {
        if (string.IsNullOrWhiteSpace(chatText))
        {
            __result = false;
            return false;
        }
        int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
        chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
        if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
        if (chatText.Contains("who", StringComparison.OrdinalIgnoreCase))
            DestroyableSingleton<UnityTelemetry>.Instance.SendWho();
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
        messageWriter.Write(chatText);
        messageWriter.EndMessage();
        __result = true;
        return false;
    }
}
