using Assets.CoreScripts;
using Hazel;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.Core;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;
using AmongUs.InnerNet.GameDataMessages;
using static TOHE.RPC;


namespace TOHE;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal class ChatCommands
{
    private static readonly string modLogFiles = @"./TOHO-DATA/ModLogs.txt";
    private static readonly string modTagsFiles = @"./TOHO-DATA/Tags/MOD_TAGS";
    private static readonly string sponsorTagsFiles = @"./TOHO-DATA/Tags/SPONSOR_TAGS";
    private static readonly string vipTagsFiles = @"./TOHO-DATA/Tags/VIP_TAGS";
    private static readonly string VIPListPath = @"./TOHO-DATA/VIP-List.txt";
    private static readonly string ModListPath = @"./TOHO-DATA/Moderators.txt";

    private static readonly Dictionary<char, int> Pollvotes = [];
    private static readonly Dictionary<char, string> PollQuestions = [];
    private static readonly List<byte> PollVoted = [];
    private static float Polltimer = 120f;
    private static string PollMSG = "";

    public const string Csize = "85%"; // CustomRole Settings Font-Size
    public const string Asize = "75%"; // All Appended Addons Font-Size

    public static List<string> ChatHistory = [];

    public static readonly HashSet<byte> Spectators = [];
    public static readonly HashSet<byte> LastSpectators = [];

    public static bool Prefix(ChatController __instance)
    {
        if (__instance.quickChatField.visible == false && __instance.freeChatField.textArea.text == "") return false;
        if (!GameStates.IsModHost && !AmongUsClient.Instance.AmHost) return true;
        __instance.timeSinceLastMessage = 3f;
        var text = __instance.freeChatField.textArea.text;
        if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
        ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
        string[] args = text.Trim().Split(' ');
        string subArgs = "";
        string subArgs2 = "";
        var canceled = false;
        var cancelVal = "";
        Main.isChatCommand = true;
        Logger.Info(text, "SendChat");
        if ((Options.NewHideMsg.GetBool() || Blackmailer.HasEnabled) && AmongUsClient.Instance.AmHost) // Blackmailer.ForBlackmailer.Contains(PlayerControl.LocalPlayer.PlayerId)) && PlayerControl.LocalPlayer.IsAlive())
        {
            ChatManager.SendMessage(PlayerControl.LocalPlayer, text);
        }
        if (text.Length >= 4) if (text[..3] == "/up") args[0] = "/up";

        if (GuessManager.GuesserMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Judge.TrialMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (President.EndMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Inspector.InspectCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Pirate.DuelCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Councillor cl && cl.MurderMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Nemesis.NemesisMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Retributionist.RetributionistMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Ritualist.RitualistMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Medium.MsMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Gunslinger.GunslingerDuelCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Swapper sw && sw.SwapMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Dictator dt && dt.ExilePlayer(PlayerControl.LocalPlayer, text)) goto Canceled;
        Directory.CreateDirectory(modTagsFiles);
        Directory.CreateDirectory(vipTagsFiles);
        Directory.CreateDirectory(sponsorTagsFiles);

        if (Blackmailer.CheckBlackmaile(PlayerControl.LocalPlayer) && PlayerControl.LocalPlayer.IsAlive())
        {
            goto Canceled;
        }
        switch (args[0])
        {
            case "/dump":
            case "/导出日志":
            case "/日志":
            case "/导出":
                Utils.DumpLog();
                break;
            case "/v":
            case "/version":
            case "/versão":
            case "/版本":
                canceled = true;
                string version_text = "";
                var player = PlayerControl.LocalPlayer;
                var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
                var name = player?.Data?.PlayerName;
                try
                {
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key).ToArray())
                    {
                        var pc = Utils.GetClientById(kvp.Key)?.Character;
                        version_text += $"{kvp.Key}/{(pc?.PlayerId != null ? pc.PlayerId.ToString() : "null")}:{pc?.GetRealName(clientData: true) ?? "null"}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "")
                    {
                        player.SetName(title);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, version_text);
                        player.SetName(name);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message, "/version");
                    version_text = "Error while getting version : " + e.Message;
                    if (version_text != "")
                    {
                        player.SetName(title);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, version_text);
                        player.SetName(name);
                    }
                }
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
                /* WILLS - v1.6.0
                case "/wa":
                    var willrole = PlayerControl.LocalPlayer.GetCustomRole();
                    if (willrole != CustomRoles.Author && Options.EnableWills.GetBool() == false) break;
                    var subArgs3 = text.Remove(0, 4);
                    if (subArgs3 == "" || subArgs3 == string.Empty)
                    {
                        break;
                    }
                    if (WillManager.Notes.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                    {
                        WillManager.Notes[PlayerControl.LocalPlayer.PlayerId] += "\n";
                        WillManager.Notes[PlayerControl.LocalPlayer.PlayerId] += subArgs3;
                    }
                    else
                    {
                        WillManager.Notes[PlayerControl.LocalPlayer.PlayerId] = subArgs3;
                    }
                    break;
                case "/wc":
                    var wcrole = PlayerControl.LocalPlayer.GetCustomRole();
                    if (wcrole != CustomRoles.Author && Options.EnableWills.GetBool() == false) break;
                    WillManager.Notes.Remove(PlayerControl.LocalPlayer.PlayerId);
                    break;
                case "/wv":
                    var wvrole = PlayerControl.LocalPlayer.GetCustomRole();
                    if (wvrole != CustomRoles.Author && Options.EnableWills.GetBool() == false) break;
                    Utils.SendMessage(WillManager.Notes[PlayerControl.LocalPlayer.PlayerId], PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Author), Translator.GetString("WillNotesTitle")));
                    break;
                */

                case "/ans":
                case "/asw":
                case "/answer":
                case "/回答":
                    Quizmaster.AnswerByChat(PlayerControl.LocalPlayer, args);
                    break;

                case "/qmquiz":
                case "/提问":
                    Quizmaster.ShowQuestion(PlayerControl.LocalPlayer);
                    break;

                case "/win":
                case "/winner":
                case "/vencedor":
                case "/胜利":
                case "/获胜":
                case "/赢":
                case "/胜利者":
                case "/获胜的人":
                case "/赢家":
                    canceled = true;
                    if (Main.winnerNameList.Count == 0) Utils.SendMessage(GetString("NoInfoExists"));
                    else Utils.SendMessage("Winner: " + string.Join(", ", Main.winnerNameList));
                    break;

                case "/l":
                case "/lastresult":
                case "/fimdejogo":
                case "/上局信息":
                case "/信息":
                case "/情况":
                    canceled = true;
                    Utils.ShowKillLog();
                    Utils.ShowLastRoles();
                    Utils.ShowLastResult();
                    break;

                case "/gr":
                case "/gameresults":
                case "/resultados":
                case "/对局结果":
                case "/上局结果":
                case "/结果":
                    canceled = true;
                    Utils.ShowLastResult();
                    break;

                case "/kh":
                case "/killlog":
                case "/击杀日志":
                case "/击杀情况":
                    canceled = true;
                    Utils.ShowKillLog();
                    break;

                case "/rs":
                case "/sum":
                case "/rolesummary":
                case "/sumario":
                case "/sumário":
                case "/summary":
                case "/результат":
                case "/上局职业":
                case "/职业信息":
                case "/对局职业":
                    canceled = true;
                    Utils.ShowLastRoles();
                    break;

                case "/ghostinfo":
                case "/幽灵职业介绍":
                case "/鬼魂职业介绍":
                case "/幽灵职业":
                case "/鬼魂职业":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.GhostRoleInfo"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/apocinfo":
                case "/apocalypseinfo":
                case "/末日中立职业介绍":
                case "/末日中立介绍":
                case "/末日类中立职业介绍":
                case "/末日类中立介绍":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.ApocalypseInfo"), PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Apocalypse), GetString("ApocalypseInfoTitle")));
                    break;

                case "/coveninfo":
                case "/covinfo":
                case "/巫师阵营职业介绍":
                case "/巫师阵营介绍":
                case "/巫师介绍":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.CovenInfo"), PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Coven), GetString("CovenInfoTitle")));
                    break;

                case "/anomalyinfo":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.AnomalyInfo"), PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jester), GetString("AnomalyInfoTitle")));
                    break;

                case "/rn":
                case "/rename":
                case "/renomear":
                case "/переименовать":
                case "/重命名":
                case "/命名为":
                    canceled = true;
                    if (args.Length < 1) break;
                    if (args.Skip(1).Join(delimiter: " ").Length is > 10 or < 1)
                    {
                        Utils.SendMessage(GetString("Message.AllowNameLength"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    else
                    {
                        var temp = args.Skip(1).Join(delimiter: " ");
                        Main.HostRealName = temp;
                        Main.AllPlayerNames[PlayerControl.LocalPlayer.PlayerId] = temp;
                        Utils.SendMessage(string.Format(GetString("Message.SetName"), temp), PlayerControl.LocalPlayer.PlayerId);
                    }
                    break;

                case "/hn":
                case "/hidename":
                case "/semnome":
                case "/隐藏名字":
                case "/藏名":
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
                case "/等级":
                case "/等级设置为":
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
                case "/设置":
                case "/系统设置":
                case "/模组设置":
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
                case "/断连":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "crew":
                        case "tripulante":
                        case "船员":
                            GameManager.Instance.enabled = false;
                            Utils.NotifyGameEnding();
                            GameManager.Instance.RpcEndGame(GameOverReason.CrewmateDisconnect, false);
                            break;

                        case "imp":
                        case "impostor":
                        case "内鬼":
                        case "伪装者":
                            GameManager.Instance.enabled = false;
                            Utils.NotifyGameEnding();
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
                case "/role":
                case "/р":
                case "/роль":
                    canceled = true;
                    if (text.Contains("/role") || text.Contains("/роль"))
                        subArgs = text.Remove(0, 5);
                    else
                        subArgs = text.Remove(0, 2);
                    SendRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId);
                    break;    

                case "/up":
                case "/指定":
                case "/成为":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    if (!PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsUp)
                    {
                        Utils.SendMessage($"{GetString("InvalidPermissionCMD")}", PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
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

                case "/setplayers":
                case "/maxjogadores":
                case "/设置最大玩家数":
                case "/设置最大玩家数量":
                case "/设置玩家数":
                case "/设置玩家数量":
                case "/玩家数":
                case "/玩家数量":
                case "/玩家":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    var numbereer = Convert.ToByte(subArgs);
                    if (numbereer > 15 && GameStates.IsVanillaServer)
                    {
                        Utils.SendMessage(GetString("Message.MaxPlayersFailByRegion"));
                        break;
                    }
                    Utils.SendMessage(GetString("Message.MaxPlayers") + numbereer);
                    if (GameStates.IsNormalGame)
                        GameOptionsManager.Instance.currentNormalGameOptions.MaxPlayers = numbereer;

                    else if (GameStates.IsHideNSeek)
                        GameOptionsManager.Instance.currentHideNSeekGameOptions.MaxPlayers = numbereer;
                    break;

                case "/h":
                case "/help":
                case "/ajuda":
                case "/хелп":
                case "/хэлп":
                case "/помощь":
                case "/帮助":
                case "/教程":
                    canceled = true;
                    Utils.ShowHelp(PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/icon":
                case "/icons":
                case "/符号":
                case "/标志":
                    {
                        Utils.SendMessage(GetString("Command.icons"), PlayerControl.LocalPlayer.PlayerId, GetString("IconsTitle"));
                        break;
                    }

                case "/iconhelp":
                case "/符号帮助":
                case "/标志帮助":
                    {
                        Utils.SendMessage(GetString("Command.icons"), title: GetString("IconsTitle"));
                        break;
                    }

                case "/kc":
                case "/kcount":
                case "/количество":
                case "/убийцы":
                case "/存活阵营":
                case "/阵营":
                case "/存货阵营信息":
                case "/阵营信息":
                    if (GameStates.IsLobby) break;

                    if (!Options.EnableKillerLeftCommand.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    var allAlivePlayers = Main.AllAlivePlayerControls;
                    int impnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Impostor));
                    int madnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsMadmate() || pc.Is(CustomRoles.Madmate));
                    int neutralnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsNK());
                    int apocnum = allAlivePlayers.Count(pc => pc.IsNeutralApocalypse() || pc.IsTransformedNeutralApocalypse());
                    int covnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Coven));

                    var sub = new StringBuilder();
                    sub.Append(string.Format(GetString("Remaining.ImpostorCount"), impnum));

                    if (Options.ShowMadmatesInLeftCommand.GetBool())
                        sub.Append(string.Format("\n\r" + GetString("Remaining.MadmateCount"), madnum));

                    if (Options.ShowApocalypseInLeftCommand.GetBool())
                        sub.Append(string.Format("\n\r" + GetString("Remaining.ApocalypseCount"), apocnum));

                    if (Options.ShowCovenInLeftCommand.GetBool())
                        sub.Append(string.Format("\n\r" + GetString("Remaining.CovenCount"), covnum));

                    sub.Append(string.Format("\n\r" + GetString("Remaining.NeutralCount"), neutralnum));

                    Utils.SendMessage(sub.ToString(), PlayerControl.LocalPlayer.PlayerId);
                    break;
                case "/vote":
                case "/投票":
                case "/票":
                    subArgs = args.Length != 2 ? "" : args[1];
                    if (subArgs == "" || !int.TryParse(subArgs, out int arg))
                        break;
                    var plr = Utils.GetPlayerById(arg);

                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (!Options.EnableVoteCommand.GetBool())
                    {
                        Utils.SendMessage(GetString("VoteDisabled"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (Options.ShouldVoteCmdsSpamChat.GetBool())
                    {
                        canceled = true;
                    }

                    if (arg != 253) // skip
                    {
                        if (plr == null || !plr.IsAlive())
                        {
                            Utils.SendMessage(GetString("VoteDead"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                    }
                    if (!PlayerControl.LocalPlayer.IsAlive())
                    {
                        Utils.SendMessage(GetString("CannotVoteWhenDead"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (GameStates.IsMeeting)
                    {
                        PlayerControl.LocalPlayer.RpcCastVote((byte)arg);
                    }
                    break;

                case "/d":
                case "/death":
                case "/morto":
                case "/умер":
                case "/причина":
                case "/死亡原因":
                case "/死亡":
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
                        var killer = PlayerControl.LocalPlayer.GetRealKiller(out var MurderRole);
                        string killerName = killer == null ? "N/A" : killer.GetRealName(clientData: true);
                        string killerRole = killer == null ? "N/A" : Utils.GetRoleName(MurderRole);
                        Utils.SendMessage(text: GetString("DeathCmd.YourName") + "<b>" + PlayerControl.LocalPlayer.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(PlayerControl.LocalPlayer.GetCustomRole())}>{Utils.GetRoleName(PlayerControl.LocalPlayer.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.DeathReason") + "<b>" + Utils.GetVitalText(PlayerControl.LocalPlayer.PlayerId) + "</b>" + "\n\r" + "</b>" + "\n\r" + GetString("DeathCmd.KillerName") + "<b>" + killerName + "</b>" + "\n\r" + GetString("DeathCmd.KillerRole") + "<b>" + $"<color={Utils.GetRoleColorCode(killer.GetCustomRole())}>{killerRole}</color>" + "</b>", sendTo: PlayerControl.LocalPlayer.PlayerId);

                        break;
                    }


                case "/m":
                case "/myrole":
                case "/minhafunção":
                case "/м":
                case "/мояроль":
                case "/身份":
                case "/我":
                case "/我的身份":
                case "/我的职业":
                    canceled = true;
                    var role = PlayerControl.LocalPlayer.GetCustomRole();
                    if (GameStates.IsInGame)
                    {
                        var lp = PlayerControl.LocalPlayer;
                        var Des = lp.GetRoleInfo(true);
                        var title = $"<color=#ffffff>" + role.GetRoleTitle() + "</color>\n";
                        var Conf = new StringBuilder();
                        var Sub = new StringBuilder();
                        var rlHex = Utils.GetRoleColorCode(role);
                        var SubTitle = $"<color={rlHex}>" + GetString("YourAddon") + "</color>\n";

                        if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                            Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref Conf);
                        var cleared = Conf.ToString();
                        var Setting = $"<color={rlHex}>{GetString(role.ToString())} {GetString("Settings:")}</color>\n";
                        Conf.Clear().Append($"<color=#ffffff>" + $"<size={Csize}>" + Setting + cleared + "</size>" + "</color>");


                        foreach (var subRole in Main.PlayerStates[lp.PlayerId].SubRoles.ToArray())
                            Sub.Append($"\n\n" + $"<size={Asize}>" + Utils.GetRoleTitle(subRole) + Utils.GetInfoLong(subRole) + "</size>");

                        if (Sub.ToString() != string.Empty)
                        {
                            var ACleared = Sub.ToString().Remove(0, 2);
                            ACleared = ACleared.Length > 1200 ? $"<size={Asize}>" + ACleared.RemoveHtmlTags() + "</size>" : ACleared;
                            Sub.Clear().Append(ACleared);
                        }

                        Utils.SendMessage(Des, lp.PlayerId, title, noReplay: true);
                        Utils.SendMessage("", lp.PlayerId, Conf.ToString(), noReplay: true);
                        if (Sub.ToString() != string.Empty) Utils.SendMessage(Sub.ToString(), lp.PlayerId, SubTitle, noReplay: true);
                    }
                    else
                        Utils.SendMessage((PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/me":
                case "/我的权限":
                case "/权限":
                    canceled = true;
                    subArgs = text.Length == 3 ? string.Empty : text.Remove(0, 3);
                    string Devbox = PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                    string UpBox = PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsUp ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                    string ColorBox = PlayerControl.LocalPlayer.FriendCode.GetDevUser().ColorCmd ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";

                    if (string.IsNullOrEmpty(subArgs))
                    {
                       HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.GetRealName(clientData: true), PlayerControl.LocalPlayer.GetClient().FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid(), PlayerControl.LocalPlayer.FriendCode.GetDevUser(), Devbox, UpBox, ColorBox)}");                   
                    }
                    else
                    {
                        if (byte.TryParse(subArgs, out byte meid))
                        {
                            if (meid != PlayerControl.LocalPlayer.PlayerId)
                            {
                                var targetplayer = Utils.GetPlayerById(meid);
                                if (targetplayer != null && targetplayer.GetClient() != null)
                                {
                                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandTargetInfo"), targetplayer.PlayerId, targetplayer.GetRealName(clientData: true), targetplayer.GetClient().FriendCode, targetplayer.GetClient().GetHashedPuid(), targetplayer.FriendCode.GetDevUser())}");                              
                                }
                                else
                                {
                                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{(GetString("Message.MeCommandInvalidID"))}");
                                }
                            }
                            else
                            {
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.GetRealName(clientData: true), PlayerControl.LocalPlayer.GetClient().FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid(), PlayerControl.LocalPlayer.FriendCode.GetDevUser(), Devbox, UpBox, ColorBox)}");
                            }
                        }
                        else
                        {
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{(GetString("Message.MeCommandInvalidID"))}");
                        }
                    }
                    break;

                case "/t":
                case "/template":
                case "/шаблон":
                case "/пример":
                case "/模板":
                case "/模板信息":
                    canceled = true;
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                    else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/mw":
                case "/messagewait":
                case "/消息等待时间":
                case "/消息冷却":
                    canceled = true;
                    if (args.Length > 1 && int.TryParse(args[1], out int sec))
                    {
                        Main.MessageWait.Value = sec;
                        Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                    }
                    else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                    break;

                case "/tpout":
                case "/传送出":
                case "/传出":
                    canceled = true;
                    if (!GameStates.IsLobby) break;
                    if (!Options.PlayerCanUseTP.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    PlayerControl.LocalPlayer.RpcTeleport(new Vector2(0.1f, 3.8f));
                    break;
                case "/tpin":
                case "/传进":
                case "/传送进":
                    canceled = true;
                    if (!GameStates.IsLobby) break;
                    if (!Options.PlayerCanUseTP.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    PlayerControl.LocalPlayer.RpcTeleport(new Vector2(-0.2f, 1.3f));
                    break;

                case "/say":
                case "/s":
                case "/с":
                case "/сказать":
                case "/说":
                    canceled = true;
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")} ~ <size=1.25>{PlayerControl.LocalPlayer.GetRealName(clientData: true)}</size></color>");
                    break;

                case "/mid":
                case "/玩家列表":
                case "/玩家信息":
                case "/玩家编号列表":
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
                case "/бан":
                case "/забанить":
                case "/封禁":
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

                    // Ban the specified Player
                    AmongUsClient.Instance.KickPlayer(bannedPlayer.GetClientId(), true);
                    string bannedPlayerName = bannedPlayer.GetRealName();
                    string textToSend1 = $"{bannedPlayerName} {GetString("BanCommandBanned")}{PlayerControl.LocalPlayer.name} \nReason: {banReason}\n";
                    if (GameStates.IsInGame)
                    {
                        textToSend1 += $" {GetString("BanCommandBannedRole")} {GetString(bannedPlayer.GetCustomRole().ToString())}";
                    }
                    Utils.SendMessage(textToSend1);
                    string moderatorFriendCode = PlayerControl.LocalPlayer.FriendCode.ToString();
                    string bannedPlayerFriendCode = bannedPlayer.FriendCode.ToString();
                    string modLogname = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n1) ? n1 : "";
                    string banlogname = Main.AllPlayerNames.TryGetValue(bannedPlayer.PlayerId, out var n11) ? n11 : "";
                    string logMessage = $"[{DateTime.Now}] {moderatorFriendCode},{modLogname} Banned: {bannedPlayerFriendCode},{banlogname} Reason: {banReason}";
                    File.AppendAllText(modLogFiles, logMessage + Environment.NewLine);
                    break;

                case "/vip":
                    canceled = true;
                    subArgs = args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte vipPlayerId))
                    {
                        Utils.SendMessage(GetString("vipCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (vipPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("vipCommandBanHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var vipPlayer = Utils.GetPlayerById(vipPlayerId);
                    if (vipPlayer == null)
                    {
                        Utils.SendMessage(GetString("vipCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    Utils.SendMessage(GetString("vipAdded"), PlayerControl.LocalPlayer.PlayerId);
                    
                    File.AppendAllText(VIPListPath, $"{vipPlayer?.FriendCode}" + Environment.NewLine);
                    break;

                case "/mod":
                    canceled = true;
                    subArgs = args[1];
                    if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte modPlayerId))
                    {
                        Utils.SendMessage(GetString("modCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (modPlayerId == 0)
                    {
                        Utils.SendMessage(GetString("modCommandBanHost"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    var modPlayer = Utils.GetPlayerById(modPlayerId);
                    if (modPlayer == null)
                    {
                        Utils.SendMessage(GetString("modCommandInvalidID"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    Utils.SendMessage(GetString("modAdded"), PlayerControl.LocalPlayer.PlayerId);

                    File.AppendAllText(ModListPath, $"{modPlayer?.FriendCode}" + Environment.NewLine);
                    break;

                case "/warn":
                case "/aviso":
                case "/варн":
                case "/пред":
                case "/предупредить":
                case "/警告":
                case "/提醒":
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

                    // Warn the specified Player
                    string textToSend2 = "";
                    string warnReason = "Reason : Not specified\n";
                    string warnedPlayerName = warnedPlayer.GetRealName();
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
                case "/кик":
                case "/кикнуть":
                case "/выгнать":
                case "/踢出":
                case "/踢":
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

                    // Kick the specified Player
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
                case "/标签颜色":
                case "/附加名称颜色":
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
                case "/уничтожить":
                case "/повесить":
                case "/казнить":
                case "/казнь":
                case "/мут":
                case "/驱逐":
                case "/驱赶":
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
                        player.SetDeathReason(PlayerState.DeathReason.etc);
                        player.SetRealKiller(PlayerControl.LocalPlayer);
                        Main.PlayerStates[player.PlayerId].SetDead();
                        player.RpcExileV2();
                        MurderPlayerPatch.AfterPlayerDeathTasks(PlayerControl.LocalPlayer, player, GameStates.IsMeeting);

                        if (player.IsHost()) Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
                        else Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    }
                    break;

                case "/kill":
                case "/matar":
                case "/убить":
                case "/击杀":
                case "/杀死":
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
                        target.RpcMurderPlayer(target);
                        if (target.IsHost()) Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
                        else Utils.SendMessage(string.Format(GetString("Message.Executed"), target.Data.PlayerName));

                        _ = new LateTask(() =>
                        {
                            Utils.NotifyRoles(ForceLoop: false, NoCache: true);

                        }, 0.2f, "Update NotifyRoles players after /kill");
                    }
                    break;

                case "/colour":
                case "/color":
                case "/cor":
                case "/цвет":
                case "/颜色":
                case "/更改颜色":
                case "/修改颜色":
                case "/换颜色":
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
                case "/退出":
                case "/退":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.CanNotUseByHost"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/xf":
                case "/修复":
                case "/修":
                    canceled = true;
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.IsAlive()) continue;
                        pc.SetName(pc.GetRealName(isMeeting: true));
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                    Utils.SendMessage(GetString("Message.TryFixName"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/id":
                case "/айди":
                case "/编号":
                case "/玩家编号":
                    canceled = true;
                    string msgText = GetString("PlayerIdList");
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc == null) continue;
                        msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                    }
                    Utils.SendMessage(msgText, PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/setrole":
                case "/设置的职业":
                case "/指定的职业":
                    canceled = true;
                    subArgs = text.Remove(0, 8);
                    SendRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug);
                    break;

                case "/changerole":
                case "/mudarfunção":
                case "/改变职业":
                case "/修改职业":
                    canceled = true;
                    if (GameStates.IsHideNSeek) break;
                    if (!(DebugModeManager.AmDebugger && GameStates.IsInGame)) break;
                    if (GameStates.IsOnlineGame && !PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug) break;
                    subArgs = text.Remove(0, 11);
                    var setRole = FixRoleNameInput(subArgs).ToLower().Trim().Replace(" ", string.Empty);
                    Logger.Info(setRole, "changerole Input");
                    foreach (var rl in CustomRolesHelper.AllRoles)
                    {
                        if (rl.IsVanilla()) continue;
                        var roleName = GetString(rl.ToString()).ToLower().Trim().TrimStart('*').Replace(" ", string.Empty);
                        if (setRole == roleName)
                        {
                            PlayerControl.LocalPlayer.GetRoleClass()?.OnRemove(PlayerControl.LocalPlayer.PlayerId);
                            PlayerControl.LocalPlayer.RpcChangeRoleBasis(rl);
                            PlayerControl.LocalPlayer.RpcSetCustomRole(rl);
                            PlayerControl.LocalPlayer.GetRoleClass().OnAdd(PlayerControl.LocalPlayer.PlayerId);
                            Utils.SendMessage(string.Format("Debug Set your role to {0}", rl.ToString()), PlayerControl.LocalPlayer.PlayerId);
                            Utils.NotifyRoles(SpecifyTarget: PlayerControl.LocalPlayer, NoCache: true);
                            Utils.MarkEveryoneDirtySettings();
                            break;
                        }
                    }
                    break;

                case "/end":
                case "/encerrar":
                case "/завершить":
                case "/结束":
                case "/结束游戏":
                    canceled = true;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                    GameManager.Instance.LogicFlow.CheckEndCriteria();
                    break;
                case "/cosid":
                case "/装扮编号":
                case "/衣服编号":
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
                case "/强制过会议":
                case "/强制跳过会议":
                case "/过会议":
                case "/结束会议":
                case "/强制结束会议":
                case "/跳过会议":
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
                case "/播放声音":
                case "/播放音效":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    PlayerControl.LocalPlayer.RPCPlayCustomSound(subArgs.Trim());
                    break;

                case "/sd":
                case "/播放音效给":
                case "/播放声音给":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    if (args.Length < 1 || !int.TryParse(args[1], out int sound1)) break;
                    RPC.PlaySoundRPC(PlayerControl.LocalPlayer.PlayerId, (Sounds)sound1);
                    break;

                case "/poll":
                case "/发起投票":
                case "/执行投票":
                    canceled = true;


                    if (args.Length == 2 && args[1] == GetString("Replay") && Pollvotes.Any() && PollMSG != string.Empty)
                    {
                        Utils.SendMessage(PollMSG);
                        break;
                    }

                    PollMSG = string.Empty;
                    Pollvotes.Clear();
                    PollQuestions.Clear();
                    PollVoted.Clear();
                    Polltimer = 120f;

                    static System.Collections.IEnumerator StartPollCountdown()
                    {
                        if (!Pollvotes.Any() || !GameStates.IsLobby)
                        {
                            Pollvotes.Clear();
                            PollQuestions.Clear();
                            PollVoted.Clear();

                            yield break;
                        }
                        bool playervoted = (Main.AllPlayerControls.Length - 1) > Pollvotes.Values.Sum();


                        while (playervoted && Polltimer > 0f)
                        {
                            if (!Pollvotes.Any() || !GameStates.IsLobby)
                            {
                                Pollvotes.Clear();
                                PollQuestions.Clear();
                                PollVoted.Clear();

                                yield break;
                            }
                            playervoted = (Main.AllPlayerControls.Length - 1) > Pollvotes.Values.Sum();
                            Polltimer -= Time.deltaTime;
                            yield return null;
                        }

                        if (!Pollvotes.Any() || !GameStates.IsLobby)
                        {
                            Pollvotes.Clear();
                            PollQuestions.Clear();
                            PollVoted.Clear();

                            yield break;
                        }

                        Logger.Info($"FINNISHED!! playervote?: {!playervoted} polltime?: {Polltimer <= 0}", "/poll - StartPollCountdown");

                        DetermineResults();
                    }

                    static void DetermineResults()
                    {
                        int basenum = Pollvotes.Values.Max();
                        var winners = Pollvotes.Where(x => x.Value == basenum);

                        string msg = "";

                        Color32 clr = new(47, 234, 45, 255); //Main.PlayerColors.First(x => x.Key == PlayerControl.LocalPlayer.PlayerId).Value;
                        var tytul = Utils.ColorString(clr, GetString("PollResultTitle"));

                        if (winners.Count() == 1)
                        {
                            var losers = Pollvotes.Where(x => x.Key != winners.First().Key);
                            msg = string.Format(GetString("Poll.Result"), $"{winners.First().Key}{PollQuestions[winners.First().Key]}", winners.First().Value);

                            for (int i = 0; i < losers.Count(); i++)
                            {
                                msg += $"\n{losers.ElementAt(i).Key} / {losers.ElementAt(i).Value} {PollQuestions[losers.ElementAt(i).Key]}";

                            }
                            msg += "</size>";


                            Utils.SendMessage(msg, title: tytul);
                        }
                        else
                        {
                            var tienum = Pollvotes.Values.Max();
                            var tied = Pollvotes.Where(x => x.Value == tienum);

                            for (int i = 0; i < (tied.Count() - 1); i++)
                            {
                                msg += "\n" + tied.ElementAt(i).Key + PollQuestions[tied.ElementAt(i).Key] + " & ";
                            }
                            msg += "\n" + tied.Last().Key + PollQuestions[tied.Last().Key];

                            Utils.SendMessage(string.Format(GetString("Poll.Tied"), msg, tienum), title: tytul);
                        }

                        Pollvotes.Clear();
                        PollQuestions.Clear();
                        PollVoted.Clear();
                    }


                    if (Main.AllPlayerControls.Length < 3)
                    {
                        Utils.SendMessage(GetString("Poll.MissingPlayers"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Poll.OnlyInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (args.SkipWhile(x => !x.Contains('?')).ToArray().Length < 3 || !args.Any(x => x.Contains('?')))
                    {
                        Utils.SendMessage(GetString("PollUsage"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    var resultat = args.TakeWhile(x => !x.Contains('?')).Concat(args.SkipWhile(x => !x.Contains('?')).Take(1));

                    string tytul = string.Join(" ", resultat.Skip(1));
                    bool Longtitle = tytul.Length > 30;
                    tytul = Utils.ColorString(Palette.PlayerColors[PlayerControl.LocalPlayer.Data.DefaultOutfit.ColorId], tytul);
                    var altTitle = Utils.ColorString(new Color32(151, 198, 230, 255), GetString("PollTitle"));

                    var ClearTIT = args.ToList();
                    ClearTIT.RemoveRange(0, resultat.ToArray().Length);

                    var Questions = ClearTIT.ToArray();
                    string msg = "";


                    if (Longtitle) msg += "<voffset=-0.5em>" + tytul + "</voffset>\n\n";
                    for (int i = 0; i < Math.Clamp(Questions.Length, 2, 5); i++)
                    {
                        msg += Utils.ColorString(RndCLR(), $"{char.ToUpper((char)(i + 65))}) {Questions[i]}\n");
                        Pollvotes[char.ToUpper((char)(i + 65))] = 0;
                        PollQuestions[char.ToUpper((char)(i + 65))] = $"<size=45%>〖 {Questions[i]} 〗</size>";
                    }
                    msg += $"\n{GetString("Poll.Begin")}";
                    msg += $"\n<size=55%><i>{GetString("Poll.TimeInfo")}</i></size>";
                    PollMSG = !Longtitle ? "<voffset=-0.5em>" + tytul + "</voffset>\n\n" + msg : msg;

                    Logger.Info($"Poll message: {msg}", "MEssapoll");

                    Utils.SendMessage(msg, title: !Longtitle ? tytul : altTitle);

                    Main.Instance.StartCoroutine(StartPollCountdown());


                    static Color32 RndCLR()
                    {
                        byte r, g, b;

                        r = (byte)IRandom.Instance.Next(45, 185);
                        g = (byte)IRandom.Instance.Next(45, 185);
                        b = (byte)IRandom.Instance.Next(45, 185);

                        return new Color32(r, g, b, 255);
                    }

                    break;

                case "/rps":
                case "/剪刀石头布":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
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
                case "/抛硬币":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
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
                        Utils.SendMessage(string.Format(GetString("CoinFlipResult"), coinSide), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                case "/gno":
                case "/猜数字":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
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
                case "/XY数字":
                case "/范围游戏":
                case "/猜范围":
                case "/范围":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
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
                        int botResult = rand.Next(playerChoice1, playerChoice2 + 1);
                        Utils.SendMessage(string.Format(GetString("RandResult"), botResult), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                case "/8ball":
                case "/8号球":
                case "/幸运球":
                    if (!Options.CanPlayMiniGames.GetBool())
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    canceled = true;
                    var rando = IRandom.Instance;
                    int result = rando.Next(0, 16);
                    string str = "";
                    switch (result)
                    {
                        case 0:
                            str = GetString("8BallYes");
                            break;
                        case 1:
                            str = GetString("8BallNo");
                            break;
                        case 2:
                            str = GetString("8BallMaybe");
                            break;
                        case 3:
                            str = GetString("8BallTryAgainLater");
                            break;
                        case 4:
                            str = GetString("8BallCertain");
                            break;
                        case 5:
                            str = GetString("8BallNotLikely");
                            break;
                        case 6:
                            str = GetString("8BallLikely");
                            break;
                        case 7:
                            str = GetString("8BallDontCount");
                            break;
                        case 8:
                            str = GetString("8BallStop");
                            break;
                        case 9:
                            str = GetString("8BallPossibly");
                            break;
                        case 10:
                            str = GetString("8BallProbably");
                            break;
                        case 11:
                            str = GetString("8BallProbablyNot");
                            break;
                        case 12:
                            str = GetString("8BallBetterNotTell");
                            break;
                        case 13:
                            str = GetString("8BallCantPredict");
                            break;
                        case 14:
                            str = GetString("8BallWithoutDoubt");
                            break;
                        case 15:
                            str = GetString("8BallWithDoubt");
                            break;
                    }
                    Utils.SendMessage("<align=\"center\"><size=150%>" + str + "</align></size>", PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medium), GetString("8BallTitle")));
                    break;
                case "/start":
                case "/开始":
                case "/старт":
                    canceled = true;
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (GameStates.IsCountDown)
                    {
                        Utils.SendMessage(GetString("StartCommandCountdown"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (string.IsNullOrEmpty(subArgs) || !int.TryParse(subArgs, out int countdown))
                    {
                        countdown = 5;
                    }
                    else
                    {
                        countdown = int.Parse(subArgs);
                    }
                    if (countdown < 0 || countdown > 99)
                    {
                        Utils.SendMessage(string.Format(GetString("StartCommandInvalidCountdown"), 0, 99), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    GameStartManager.Instance.BeginGame();
                    GameStartManager.Instance.countDownTimer = countdown;
                    Utils.SendMessage(string.Format(GetString("StartCommandStarted"), PlayerControl.LocalPlayer.name));
                    Logger.Info("Game Starting", "ChatCommand");
                    break;

                default:
                    Main.isChatCommand = false;
                    break;
                case "/spectate":
                case "/спектейт":
                case "/观战":
                case "/espectar":
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (Options.DisableSpectateCommand.GetBool())
                    {
                        Utils.SendMessage("\n", PlayerControl.LocalPlayer.PlayerId, GetString("SpectateDisabled"));
                        break;
                    }

                    if (LastSpectators.Contains(PlayerControl.LocalPlayer.PlayerId))
                    {
                        Utils.SendMessage("\n", PlayerControl.LocalPlayer.PlayerId, GetString("SpectateCommand.WasSpectatingLastRound"));
                        break;
                    }

                    if (Spectators.Remove(PlayerControl.LocalPlayer.PlayerId))
                    {
                        Utils.SendMessage("\n", PlayerControl.LocalPlayer.PlayerId, GetString("SpectateCommand.Removed"));
                        break;
                    }

                    if (Spectators.Add(PlayerControl.LocalPlayer.PlayerId))
                        Utils.SendMessage("\n", PlayerControl.LocalPlayer.PlayerId, GetString("SpectateCommand.Success"));
                    break;
                case "/spam":
                    canceled = true;
                    ChatManager.SendQuickChatSpam();
                    ChatManager.SendPreviousMessagesToAll();
                    subArgs = args.Length < 2 ? "" : args[1];
                    CopsAndRobbersManager.AbilityDescription(subArgs);
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

            __instance.quickChatMenu.Clear();
            __instance.quickChatField.Clear();
        }
        return !canceled;
    }

    public static string FixRoleNameInput(string text)
    {
        text = text.Replace("着", "者").Trim().ToLower();
        return text switch
        {
            // Because of partial translation conflicts (zh-cn and zh-tw)
            // Need to wait for follow-up finishing

            /*
            // GM
            "GM(遊戲大師)" or "管理员" or "管理" or "gm" or "GM" => GetString("GM"),
            
            // 原版职业
            "船員" or "船员" or "白板" or "天选之子" => GetString("CrewmateTOHO"),
            "工程師" or "工程师" => GetString("EngineerTOHO"),
            "科學家" or "科学家" => GetString("ScientistTOHO"),
            "守護天使" or "守护天使" => GetString("GuardianAngelTOHO"),
            "偽裝者" or "内鬼" => GetString("ImpostorTOHO"),
            "變形者" or "变形者" => GetString("ShapeshifterTOHO"),

            // 隱藏職業 and 隐藏职业
            "陽光開朗大男孩" or "阳光开朗大男孩" => GetString("Sunnyboy"),
            "吟遊詩人" or "吟游诗人" => GetString("Bard"),
            "核爆者" or "核武器" => GetString("Nuker"),

            // 偽裝者陣營職業 and 内鬼阵营职业
            "賞金獵人" or "赏金猎人" or "赏金" => GetString("BountyHunter"),
            "煙火工匠" or "烟花商人" or "烟花爆破者" or "烟花" => GetString("Fireworker"),
            "嗜血殺手" or "嗜血杀手" or "嗜血" => GetString("Mercenary"),
            "百变怪" or "千面鬼" or "千面" => GetString("ShapeMaster"),
            "吸血鬼" or "吸血" => GetString("Vampire"),
            "吸血鬼之王" or "吸血鬼女王"  => GetString("Vampiress"),
            "術士" or "术士" => GetString("Warlock"),
            "刺客" or "忍者" => GetString("Ninja"),
            "僵屍" or "僵尸" or"殭屍" or "丧尸" => GetString("Zombie"),
            "駭客" or "骇客" or "黑客" => GetString("Anonymous"),
            "礦工" or "矿工" => GetString("Miner"),
            "殺人機器" or "杀戮机器" or "杀戮" or "机器" or "杀戮兵器" => GetString("KillingMachine"),
            "通緝犯" or "逃逸者" or "逃逸" => GetString("Escapist"),
            "女巫" => GetString("Witch"),
            "傀儡師" or "傀儡师" or "傀儡" => GetString("Puppeteer"),
            "主謀" or "策划者" => GetString("Mastermind"),
            "時間竊賊" or "蚀时者" or "蚀时" or "偷时" => GetString("TimeThief"),
            "狙擊手" or "狙击手" or "狙击" => GetString("Sniper"),
            "送葬者" or "暗杀者" => GetString("Undertaker"),
            "裂縫製造者" or "裂缝制造者" => GetString("RiftMaker"),
            "邪惡的追踪者" or "邪恶追踪者" or "邪恶的追踪者" => GetString("EvilTracker"),
            "邪惡賭怪" or "邪恶赌怪" or "坏赌" or "恶赌" or "邪恶赌怪" => GetString("EvilGuesser"),
            "監管者" or "监管者" or "监管" => GetString("AntiAdminer"),
            "狂妄殺手" or "狂妄杀手" => GetString("Arrogance"),
            "自爆兵" or "自爆" => GetString("Bomber"),
            "清道夫" or "清道" => GetString("Scavenger"),
            "陷阱師" or "诡雷" => GetString("Trapster"),
            "歹徒" => GetString("Gangster"),
            "清潔工" or "清理工" or "清洁工" => GetString("Cleaner"),
            "球狀閃電" or "球状闪电" => GetString("Lightning"),
            "貪婪者" or "贪婪者" or "贪婪" => GetString("Greedy"),
            "被詛咒的狼" or "呪狼" => GetString("CursedWolf"),
            "換魂師" or "夺魂者" or "夺魂" => GetString("SoulCatcher"),
            "快槍手" or "快枪手" or "快枪" => GetString("QuickShooter"),
            "隱蔽者" or "隐蔽者" or "小黑人" => GetString("Camouflager"),
            "抹除者" or "抹除" => GetString("Eraser"),
            "肢解者" or "肢解" => GetString("Butcher"),
            "劊子手" or "刽子手" => GetString("Hangman"),
            "隱身人" or "隐匿者" or "隐匿" or "隐身" => GetString("Swooper"),
            "船鬼" => GetString("Crewpostor"),
            "野人" => GetString("Wildling"),
            "騙術師" or "骗术师" => GetString("Trickster"),
            "衛道士" or "卫道士" or "内鬼市长" => GetString("Vindicator"),
            "寄生蟲" or "寄生虫" => GetString("Parasite"),
            "分散者" or "分散" => GetString("Disperser"),
            "抑鬱者" or "抑郁者" or "抑郁" => GetString("Inhibitor"),
            "破壞者" or "破坏者" or "破坏" => GetString("Saboteur"),
            "議員" or "邪恶法官" or "议员" or "邪恶审判" => GetString("Councillor"),
            "眩暈者" or "眩晕者" or "眩晕" => GetString("Dazzler"),
            "簽約人" or "死亡契约" or "死亡" or "锲约" => GetString("Deathpact"),
            "吞噬者" or "吞噬" => GetString("Devourer"),
            "軍師" or "军师" => GetString("Consigliere"),
            "化型者" or "化形者" => GetString("Morphling"),
            "躁動者" or "龙卷风" => GetString("Twister"),
            "策畫者" or "潜伏者" or "潜伏" => GetString("Lurker"),
            "罪犯" => GetString("Convict"),
            "幻想家" or "幻想" => GetString("Visionary"),
            "逃亡者" or "逃亡" => GetString("Refugee"),
            "潛伏者" or "失败者" or "失败的man" or "失败" => GetString("Underdog"),
            "賭博者" or "速度者" or "速度" => GetString("Ludopath"),
            "懸賞者" or "教父" => GetString("Godfather"),
            "天文學家" or "天文学家" or "天文家" or "天文学" => GetString("Chronomancer"),
            "設陷者" or "设陷者" or "设陷" => GetString("Pitfall"),
            "狂戰士" or "狂战士" or "升级者" or "狂战士" => GetString("Berserker"),
            "壞迷你船員" or "坏迷你船员" or "坏小孩" or "坏迷你" => GetString("EvilMini"),
            "勒索者" or "勒索" => GetString("Blackmailer"),
            "教唆者" or "教唆" => GetString("Instigator"),

            // 船員陣營職業 and 船员阵营职业
            "擺爛人" or "摆烂人" or "摆烂" => GetString("Needy"),
            "大明星" or "明星" => GetString("SuperStar"),
            "網紅" or "网红" => GetString("Celebrity"),
            "清洗者" or "清洗" => GetString("Cleanser"),
            "守衛者" or "守卫者" => GetString("Keeper"),
            "俠客" or "侠客" or "正义使者" => GetString("Knight"),
            "市長" or "市长" => GetString("Mayor"),
            "被害妄想症" or "被害妄想" or "被迫害妄想症" or "被害" or "妄想" or "妄想症" => GetString("Paranoia"),
            "愚者" => GetString("Psychic"),
            "修理工" or "修理" or "修理大师" => GetString("Mechanic"),
            "警長" or "警长" => GetString("Sheriff"),
            "義警" or "义务警员" or "警员" => GetString("Vigilante"),
            "監禁者" or "狱警" or "狱卒" => GetString("Jailer"),
            "模仿者" or "模仿猫" or "模仿" => GetString("CopyCat"),
            "告密者" => GetString("Snitch"),
            "展現者" or "展现者" or "展现" => GetString("Marshall"),
            "增速師" or "增速者" or "增速" => GetString("SpeedBooster"),
            "法醫" or "法医" => GetString("Doctor"),
            "獨裁主義者" or "独裁者" or "独裁" => GetString("Dictator"),
            "偵探" or "侦探" => GetString("Analyst"),
            "正義賭怪" or "正义赌怪" or "好赌" or "正义的赌怪" => GetString("NiceGuesser"),
            "賭場管理員" or "竞猜大师" or "竞猜" => GetString("GuessMaster"),
            "傳送師" or "传送师" => GetString("Transporter"),
            "時間大師" or "时间操控者" or "时间操控" => GetString("TimeManager"),
            "老兵" => GetString("Veteran"),
            "埋雷兵" => GetString("Bastion"),
            "保鑣" or "保镖" => GetString("Bodyguard"),
            "贗品商" or "赝品商" => GetString("Deceiver"),
            "擲彈兵" or "掷雷兵" => GetString("Grenadier"),
            "軍醫" or "医生" => GetString("Medic"),
            "占卜師" or "调查员" or "占卜师" => GetString("FortuneTeller"),
            "法官" or "正义法官" or "正义审判" => GetString("Judge"),
            "殯葬師" or "入殓师" => GetString("Mortician"),
            "通靈師" or "通灵师" => GetString("Mediumshiper"),
            "和平之鴿" or "和平之鸽" => GetString("Pacifist"),
            "窺視者" or "观察者" or "观察" => GetString("Observer"),
            "君主" => GetString("Monarch"),
            "預言家" or "预言家" or "预言" => GetString("Overseer"),
            "驗屍官" or "验尸官" or "验尸" => GetString("Coroner"),
            "正義的追蹤者" or "正义追踪者" or "正义的追踪者" => GetString("Tracker"),
            "商人" => GetString("Merchant"),
            "總統" or "总统" => GetString("President"),
            "獵鷹" or "猎鹰" => GetString("Hawk"),
            "捕快" or "下属" => GetString("Deputy"),
            "算命師" or "研究者" => GetString("Investigator"),
            "守護者" or "守护者" or "守护" => GetString("Guardian"),
            "賢者" or "瘾君子" or "醉酒" => GetString("Addict"),
            "鼹鼠" => GetString("Mole"),
            "藥劑師" or "炼金术士" or "药剂" => GetString("Alchemist"),
            "尋跡者" or "寻迹者" or "寻迹" or "寻找鸡腿" => GetString("Tracefinder"),
            "先知" or "神谕" or "神谕者" => GetString("Oracle"),
            "靈魂論者" or "灵魂论者" => GetString("Spiritualist"),
            "變色龍" or "变色龙" or "变色" => GetString("Chameleon"),
            "檢查員" or "检查员" or "检查" => GetString("Inspector"),
            "仰慕者" or "仰慕" => GetString("Admirer"),
            "時間之主" or "时间之主" or "回溯时间" => GetString("TimeMaster"),
            "十字軍" or "十字军" => GetString("Crusader"),
            "遐想者" or "遐想" => GetString("Reverie"),
            "瞭望者" or "瞭望员" => GetString("Lookout"),
            "通訊員" or "通信员" => GetString("Telecommunication"),
            "執燈人" or "执灯人" or "执灯" or "灯人" or "小灯人" => GetString("Lighter"),
            "任務管理員" or "任务管理者" => GetString("TaskManager"),
            "目擊者" or "目击者" or "目击" => GetString("Witness"),
            "換票師" or "换票师" => GetString("Swapper"),
            "警察局長" or "警察局长" => GetString("ChiefOfPolice"),
            "好迷你船員" or "好迷你船员" or "好迷你" or "好小孩" => GetString("NiceMini"),
            "間諜" or "间谍" => GetString("Spy"),
            "隨機者" or "萧暮" or "暮" or "萧暮不姓萧" => GetString("Randomizer"),
            "猜想者" or "猜想" or "谜团" => GetString("Enigma"),
            "船長" or "舰长" or "船长" => GetString("Captain"),
            "慈善家" or "恩人" => GetString("Benefactor"),

            // 中立陣營職業 and 中立阵营职业
            "小丑" or "丑皇" => GetString("Jester"),
            "縱火犯" or "纵火犯" or "纵火者" or "纵火" => GetString("Arsonist"),
            "焚燒狂" or "焚烧狂" or "焚烧" => GetString("Pyromaniac"),
            "神風特攻隊" or "神风特攻队" => GetString("Kamikaze"),
            "獵人" or "猎人" => GetString("Huntsman"),
            "恐怖分子" => GetString("Terrorist"),
            "暴民" or "处刑人" or "处刑" or "处刑者" => GetString("Executioner"),
            "律師" or "律师" => GetString("Lawyer"),
            "投機主義者" or "投机者" or "投机" => GetString("Opportunist"),
            "瑪利歐" or "马里奥" => GetString("Vector"),
            "豺狼" or "蓝狼" => GetString("Jackal"),
            "神" or "上帝" => GetString("God"),
            "冤罪師" or "冤罪师" or "冤罪" => GetString("Innocent"),
            "暗殺者" or "隐形者" =>GetString("Stealth"),
            "企鵝" or "企鹅" =>GetString("Penguin"),
            "鵜鶘" or "鹈鹕" => GetString("Pelican"),
            "疫醫" or "瘟疫学家" => GetString("PlagueDoctor"),
            "革命家" or "革命者" => GetString("Revolutionist"),
            "單身狗" => GetString("Hater"),
            "柯南" => GetString("Konan"),
            "玩家" => GetString("Demon"),
            "潛藏者" or "潜藏" => GetString("Stalker"),
            "工作狂" => GetString("Workaholic"),
            "至日者" or "至日" => GetString("Solsticer"),
            "集票者" or "集票" => GetString("Collector"),
            "挑釁者" or "自爆卡车" => GetString("Provocateur"),
            "嗜血騎士" or "嗜血骑士" => GetString("BloodKnight"),
            "瘟疫之源" or "瘟疫使者" => GetString("PlagueBearer"),
            "萬疫之神" or "瘟疫" => GetString("Pestilence"),
            "故障者" or "缺点者" or "缺点" => GetString("Glitch"),
            "跟班" or "跟班小弟" => GetString("Sidekick"),
            "追隨者" or "赌徒" or "下注" => GetString("Follower"),
            "魅魔" => GetString("Cultist"),
            "連環殺手" or "连环杀手" => GetString("SerialKiller"),
            "劍聖" or "天启" => GetString("Juggernaut"),
            "感染者" or "感染" => GetString("Infectious"),
            "病原體" or "病毒" => GetString("Virus"),
            "起訴人" or "起诉人" => GetString("Pursuer"),
            "怨靈" or "幽灵" => GetString("Phantom"),
            "挑戰者" or "决斗者" or "挑战者" => GetString("Pirate"),
            "炸彈王" or "炸弹狂" or "煽动者" => GetString("Agitater"),
            "獨行者" or "独行者" => GetString("Maverick"),
            "被詛咒的靈魂" or "诅咒之人" => GetString("CursedSoul"),
            "竊賊" or "小偷" => GetString("Pickpocket"),
            "背叛者" or "背叛" => GetString("Traitor"),
            "禿鷲" or "秃鹫" => GetString("Vulture"),
            "搗蛋鬼" or "任务执行者" => GetString("Taskinator"),
            "麵包師" or "面包师" => GetString("Baker"),
            "飢荒" or "饥荒" => GetString("Famine"),
            "靈魂召喚者" or "灵魂召唤者" => GetString("Spiritcaller"),
            "失憶者" or "失忆者" or "失忆" => GetString("Amnesiac"),
            "模仿家" or "效仿者" => GetString("Imitator"),
            "強盜" => GetString("Bandit"),
            "分身者" => GetString("Doppelganger"),
            "受虐狂" => GetString("PunchingBag"),
            "賭神" or "末日赌怪" => GetString("Doomsayer"),
            "裹屍布" or "裹尸布" => GetString("Shroud"),
            "月下狼人" or "狼人" => GetString("Werewolf"),
            "薩滿" or "萨满" => GetString("Shaman"),
            "冒險家" or "探索者" => GetString("Seeker"),
            "精靈" or "小精灵" or "精灵" => GetString("Pixie"),
            "咒魔" or "神秘者" => GetString("Occultist"),
            "靈魂收割者" or "灵魂收集者" or "灵魂收集" or "收集灵魂" => GetString("SoulCollector"),
            "薛丁格的貓" or "薛定谔的猫" => GetString("SchrodingersCat"),
            "暗戀者" or "浪漫者" => GetString("Romantic"),
            "報復者" or "复仇浪漫者" => GetString("VengefulRomantic"),
            "絕情者" or "无情浪漫者" => GetString("RuthlessRomantic"),
            "毒醫" or "投毒者" => GetString("Poisoner"),
            "代碼工程師" or "巫师" => GetString("HexMaster"),
            "幻影" or "魅影" => GetString("Wraith"),
            "掃把星" or "扫把星" => GetString("Jinx"),
            "魔藥師" or "药剂师" => GetString("PotionMaster"),
            "死靈法師" or "亡灵巫师" => GetString("Necromancer"),
            "測驗者" or "测验长" => GetString("Quizmaster"),

            // 附加職業 and 附加职业
            "絕境者" or "绝境者" => GetString("LastImpostor"),
            "超頻" or "超频波" or "超频" => GetString("Overclocked"),
            "戀人" or "恋人" => GetString("Lovers"),
            "叛徒" => GetString("Madmate"),
            "觀察者" or "窥视者" or "觀察" or "窥视" => GetString("Watcher"),
            "閃電俠" or "闪电侠" or "閃電" or "闪电" => GetString("Flash"),
            "持燈人" or "火炬" or "持燈" => GetString("Torch"),
            "靈媒" or "灵媒" or "靈媒" => GetString("Seer"),
            "破平者" or "破平" => GetString("Tiebreaker"),
            "膽小鬼" or "胆小鬼" or "膽小" or "胆小" => GetString("Oblivious"),
            "視障" or "迷幻者" or "視障" or "迷幻" => GetString("Bewilder"),
            "墨鏡" or "患者" => GetString("Sunglasses"),
            "加班狂" => GetString("Workhorse"),
            "蠢蛋" => GetString("Fool"),
            "復仇者" or "复仇者" or "復仇" or "复仇" => GetString("Avanger"),
            "Youtuber" or "UP主" or "YT" => GetString("Youtuber"),
            "利己主義者" or "利己主义者" or "利己主義" or "利己主义" => GetString("Egoist"),
            "竊票者" or "窃票者" or "竊票" or "窃票" => GetString("TicketsStealer"),
            //"雙重人格" or "双重人格" => GetString("Schizophrenic"),
            "保險箱" or "宝箱怪" => GetString("Mimic"),
            "賭怪" or "赌怪" => GetString("Guesser"),
            "死神" => GetString("Necroview"),
            "長槍" or "持枪" => GetString("Reach"),
            "魅魔小弟" => GetString("Charmed"),
            "乾淨" or "干净" => GetString("Cleansed"),
            "誘餌" or "诱饵" => GetString("Bait"),
            "陷阱師" or "陷阱师" => GetString("Trapper"),
            "被感染" or "感染" => GetString("Infected"),
            "防賭" or "不可被赌" => GetString("Onbound"),
            "反擊者" or "回弹者" or "回弹" => GetString("Rebound"),
            "平凡者" or "平凡" => GetString("Mundane"),
            "騎士" or "骑士" => GetString("Knighted"),
            "漠視" or "不受重视" or "被漠視的" => GetString("Unreportable"),
            "被傳染" or "传染性" => GetString("Contagious"),
            "幸運" or "幸运加持" => GetString("Lucky"),
            "倒霉" or "倒霉蛋" => GetString("Unlucky"),
            "虛無" or "无效投票" => GetString("VoidBallot"),
            "敏感" or "意识者" or "意识" => GetString("Aware"),
            "嬌嫩" or "脆弱" or "脆弱者" => GetString("Fragile"),
            "專業" or "双重猜测" => GetString("DoubleShot"),
            "流氓" => GetString("Rascal"),
            "無魂" or "没有灵魂" => GetString("Soulless"),
            "墓碑" => GetString("Gravestone"),
            "懶人" or "懒人" => GetString("Lazy"),
            "驗屍" or "尸检" => GetString("Autopsy"),
            "忠誠" or "忠诚" => GetString("Loyal"),
            "惡靈" or "恶灵" => GetString("EvilSpirit"),
            "狼化" or "招募" or "狼化的" or "被招募的" => GetString("Recruit"),
            "被仰慕" or "仰慕" => GetString("Admired"),
            "發光" or "光辉" => GetString("Glow"),
            "病態" or "患病者" or "患病的" or "患病" => GetString("Diseased"),
            "健康" or "健康的" or "健康者" => GetString("Antidote"),
            "固執者" or "固执者" or "固執" or "固执" => GetString("Stubborn"),
            "無影" or "迅捷" => GetString("Swift"),
            "反噬" or "食尸鬼" => GetString("Ghoul"),
            "嗜血者" => GetString("Bloodthirst"),
            "獵夢者" or "梦魇" or "獵夢"=> GetString("Mare"),
            "地雷" or "爆破者" or "爆破" => GetString("Burst"),
            "偵察員" or "侦察员" or "偵察" or "侦察" => GetString("Sleuth"),
            "笨拙" or "笨蛋" => GetString("Clumsy"),
            "敏捷" => GetString("Nimble"),
            "規避者" or "规避者" or "规避" => GetString("Circumvent"),
            "名人" or "网络员" or "网络" => GetString("Cyber"),
            "焦急者" or "焦急的" or "焦急" => GetString("Hurried"),
            "OIIAI" => GetString("Oiiai"),
            "順從者" or "影响者" or "順從" or "影响" => GetString("Influenced"),
            "沉默者" or "沉默" => GetString("Silent"),
            "易感者" or "易感" => GetString("Susceptible"),
            "狡猾" or "棘手者" or "棘手" => GetString("Tricky"),
            "彩虹" => GetString("Rainbow"),
            "疲勞者" or "疲劳者" or "疲勞" or "疲劳" => GetString("Tired"),
            "雕像" => GetString("Statue"),
            "没有搜集的繁体中文" or "雷达" => GetString("Radar"),

            // 幽靈職業 and 幽灵职业
            // 偽裝者 and 内鬼
            "爪牙" => GetString("Minion"),
            "黑手黨" or "黑手党" or "黑手" => GetString("Nemesis"),
            "嗜血之魂" or "血液伯爵" => GetString("Bloodmoon"),
            // 船員 and 船员
            "没有搜集的繁体中文" or "鬼怪" => GetString("Ghastly"),
            "冤魂" or "典狱长" => GetString("Warden"),
            "報應者" or "惩罚者" or "惩罚" or "报仇者" => GetString("Retributionist"),

            // 随机阵营职业
            "迷你船員" or "迷你船员" or "迷你" or "小孩" or "Mini" => GetString("Mini"),*/
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
        if (role.StartsWith("/r")) _ = role.Replace("/r", string.Empty);
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                Utils.SendMessage(GetString("ModeDescribe.FFA"), playerId);
                return;
            case CustomGameMode.UltimateTeam:
                Utils.SendMessage(GetString("ModeDescribe.UltimateTeam"), playerId);
                return;
            case CustomGameMode.TrickorTreat:
                Utils.SendMessage(GetString("ModeDescribe.TrickorTreat"), playerId);
                return;
            case CustomGameMode.CandR:
                var copName = GetString(CustomRoles.Cop.ToString()).ToLower().Trim().TrimStart('*').Replace(" ", string.Empty);
                var robberName = GetString(CustomRoles.Robber.ToString()).ToLower().Trim().TrimStart('*').Replace(" ", string.Empty);
                var Conf1 = new StringBuilder();

                CustomRoles rl1;
                if (role == copName) rl1 = CustomRoles.Cop;
                else if (role == robberName) rl1 = CustomRoles.Robber;
                else
                {
                    Utils.SendMessage(GetString("ModeDescribe.C&R"), playerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cop), GetString("ModeC&R")));
                    return;
                }

                var description = rl1.GetInfoLong();
                var title1 = Utils.ColorString(Utils.GetRoleColor(rl1), GetString($"{rl1}"));
                string rlHex1 = Utils.GetRoleColorCode(rl1);
                var Setting = $"<size={Csize}><color={rlHex1}>{GetString(rl1.ToString())} {GetString("Settings:")}</color>\n";
                Conf1.Clear().Append(Setting);

                foreach (OptionItem opt in CopsAndRobbersManager.roleSettings[rl1])
                {
                    Conf1.Append($"{opt.GetName(true)}: {opt.GetString()}\n");
                    int deep = 0;
                    if (opt.Children.Any()) deep = 1;
                    Utils.ShowChildrenSettings(opt, ref Conf1, deep: deep);
                    var cleared = Conf1.ToString();
                    Conf1.Clear().Append($"<color=#ffffff>{cleared}</color>");
                }
                Conf1.Append("</size>");

                // Show Role Info
                Utils.SendMessage(description, playerId, title1, noReplay: true);

                // Show Role Settings
                Utils.SendMessage("", playerId, Conf1.ToString(), noReplay: true);
                return;
        }
        if (role.StartsWith("/up")) _ = role.Replace("/up", string.Empty);
        if (role.EndsWith("\r\n")) _ = role.Replace("\r\n", string.Empty);
        if (role.EndsWith("\n")) _ = role.Replace("\n", string.Empty);
        if (role.StartsWith("/bt")) _ = role.Replace("/bt", string.Empty);
        if (role.StartsWith("/rt")) _ = role.Replace("/rt", string.Empty);

        if (role == "" || role == string.Empty)
        {
            Utils.ShowActiveRoles(playerId);
            return;
        }

        role = FixRoleNameInput(role).ToLower().Trim().Replace(" ", string.Empty);

        foreach (var rl in CustomRolesHelper.AllRoles)
        {
            if (rl.IsVanilla()) continue;
            var roleName = GetString(rl.ToString());
            if (role == roleName.ToLower().Trim().TrimStart('*').Replace(" ", string.Empty))
            {
                string devMark = "";
                if ((isDev || isUp) && GameStates.IsLobby)
                {
                    devMark = "▲";
                    if (CustomRolesHelper.IsAdditionRole(rl) || rl is CustomRoles.GM or CustomRoles.Mini || rl.IsGhostRole()) devMark = "";
                    if (rl.GetCount() < 1 || rl.GetMode() == 0) devMark = "";
                    if (isUp)
                    {
                        if (devMark == "▲") Utils.SendMessage(string.Format(GetString("Message.YTPlanSelected"), roleName), playerId);
                        else Utils.SendMessage(string.Format(GetString("Message.YTPlanSelectFailed"), roleName), playerId);
                    }
                    if (devMark == "▲")
                    {
                        byte pid = playerId == 255 ? (byte)0 : playerId;
                        GhostRoleAssign.forceRole.Remove(pid);
                        RoleAssign.SetRoles.Remove(pid);
                        RoleAssign.SetRoles.Add(pid, rl);
                    }
                    if (rl.IsGhostRole() && !rl.IsAdditionRole() && isDev && (rl.GetCount() >= 1 && rl.GetMode() > 0))
                    {
                        byte pid = playerId == 255 ? (byte)0 : playerId;
                        CustomRoles setrole = rl.GetCustomRoleTeam() switch
                        {
                            Custom_Team.Impostor => CustomRoles.ImpostorTOHO,
                            _ => CustomRoles.CrewmateTOHO

                        };
                        RoleAssign.SetRoles.Remove(pid);
                        RoleAssign.SetRoles.Add(pid, setrole);
                        GhostRoleAssign.forceRole[pid] = rl;

                        devMark = "▲";
                    }

                    if (isUp) return;
                }
                var Des = rl.GetInfoLong();
                var title = devMark + $"<color=#ffffff>" + rl.GetRoleTitle() + "</color>\n";
                var Conf = new StringBuilder();
                string rlHex = Utils.GetRoleColorCode(rl);
                if (Options.CustomRoleSpawnChances.TryGetValue(rl, out var spawnRate))
                {
                    Utils.ShowChildrenSettings(spawnRate, ref Conf);
                    var cleared = Conf.ToString();
                    var Setting = $"<color={rlHex}>{GetString(rl.ToString())} {GetString("Settings:")}</color>\n";
                    Conf.Clear().Append($"<color=#ffffff>" + $"<size={Csize}>" + Setting + cleared + "</size>" + "</color>");

                }
                // Show Role Info
                Utils.SendMessage(Des, playerId, title, noReplay: true);

                // Show Role Settings
                Utils.SendMessage("", playerId, Conf.ToString(), noReplay: true);
                return;
            }
        }
        if (isUp) Utils.SendMessage(GetString("Message.YTPlanCanNotFindRoleThePlayerEnter"), playerId);
        else Utils.SendMessage(GetString("Message.CanNotFindRoleThePlayerEnter"), playerId);
        return;
    }
    public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
    {
        canceled = false;
        if (!AmongUsClient.Instance.AmHost) return;

        if (!Blackmailer.CheckBlackmaile(player)) ChatManager.SendMessage(player, text);

        if (text.StartsWith("\n")) text = text[1..];
        string[] args = text.Split(' ');
        string subArgs = "";
        string subArgs2 = "";

        if (GuessManager.GuesserMsg(player, text)) { canceled = true; Logger.Info($"Is Guesser command", "OnReceiveChat"); return; }
        if (Judge.TrialMsg(player, text)) { canceled = true; Logger.Info($"Is Judge command", "OnReceiveChat"); return; }
        if (President.EndMsg(player, text)) { canceled = true; Logger.Info($"Is President command", "OnReceiveChat"); return; }
        if (Inspector.InspectCheckMsg(player, text)) { canceled = true; Logger.Info($"Is Inspector command", "OnReceiveChat"); return; }
        if (Pirate.DuelCheckMsg(player, text)) { canceled = true; Logger.Info($"Is Pirate command", "OnReceiveChat"); return; }
        if (player.GetRoleClass() is Councillor cl && cl.MurderMsg(player, text)) { canceled = true; Logger.Info($"Is Councillor command", "OnReceiveChat"); return; }
        if (player.GetRoleClass() is Swapper sw && sw.SwapMsg(player, text)) { canceled = true; Logger.Info($"Is Swapper command", "OnReceiveChat"); return; }
        if (Medium.MsMsg(player, text)) { Logger.Info($"Is Medium command", "OnReceiveChat"); return; }
        if (Nemesis.NemesisMsgCheck(player, text)) { Logger.Info($"Is Nemesis Revenge command", "OnReceiveChat"); return; }
        if (Retributionist.RetributionistMsgCheck(player, text)) { Logger.Info($"Is Retributionist Revenge command", "OnReceiveChat"); return; }
        if (player.GetRoleClass() is Dictator dt && dt.ExilePlayer(player, text)) { canceled = true; Logger.Info($"Is Dictator command", "OnReceiveChat"); return; }
        if (Ritualist.RitualistMsgCheck(player, text)) { canceled = true; Logger.Info($"Is Ritualist command", "OnReceiveChat"); return; }
        if (Gunslinger.GunslingerDuelCheckMsg(player, text)) { canceled = true; Logger.Info($"Is Gunslinger command", "OnReceiveChat"); return; }

        Directory.CreateDirectory(modTagsFiles);
        Directory.CreateDirectory(vipTagsFiles);
        Directory.CreateDirectory(sponsorTagsFiles);

        if (Blackmailer.CheckBlackmaile(player) && player.IsAlive() && !player.IsHost())
        {
            Logger.Info($"This player (id {player.PlayerId}) was Blackmailed", "OnReceiveChat");
            ChatManager.SendPreviousMessagesToAll();
            ChatManager.cancel = false;
            canceled = true;
            return;
        }

        switch (args[0])
        {
            case "/r":
            case "/role":
            case "/р":
            case "/роль":
                Logger.Info($"Command '/r' was activated", "OnReceiveChat");
                if (text.Contains("/role") || text.Contains("/роль"))
                    subArgs = text.Remove(0, 5);
                else
                    subArgs = text.Remove(0, 2);
                SendRolesInfo(subArgs, player.PlayerId, isDev: player.FriendCode.GetDevUser().DeBug);
                break;

            case "/m":
            case "/myrole":
            case "/minhafunção":
            case "/м":
            case "/мояроль":
            case "/身份":
            case "/我":
            case "/我的身份":
            case "/我的职业":
                Logger.Info($"Command '/m' was activated", "OnReceiveChat");
                var role = player.GetCustomRole();
                if (GameStates.IsInGame)
                {
                    var Des = player.GetRoleInfo(true);
                    var title = $"<color=#ffffff>" + role.GetRoleTitle() + "</color>\n";
                    var Conf = new StringBuilder();
                    var Sub = new StringBuilder();
                    var rlHex = Utils.GetRoleColorCode(role);
                    var SubTitle = $"<color={rlHex}>" + GetString("YourAddon") + "</color>\n";

                    if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                        Utils.ShowChildrenSettings(opt, ref Conf);
                    var cleared = Conf.ToString();
                    var Setting = $"<color={rlHex}>{GetString(role.ToString())} {GetString("Settings:")}</color>\n";
                    Conf.Clear().Append($"<color=#ffffff>" + $"<size={Csize}>" + Setting + cleared + "</size>" + "</color>");

                    foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles.ToArray())
                    {
                        Sub.Append($"\n\n" + $"<size={Asize}>" + Utils.GetRoleTitle(subRole) + Utils.GetInfoLong(subRole) + "</size>");

                    }
                    if (Sub.ToString() != string.Empty)
                    {
                        var ACleared = Sub.ToString().Remove(0, 2);
                        ACleared = ACleared.Length > 1200 ? $"<size={Asize}>" + ACleared.RemoveHtmlTags() + "</size>" : ACleared;
                        Sub.Clear().Append(ACleared);
                    }

                    Utils.SendMessage(Des, player.PlayerId, title, noReplay: true);
                    Utils.SendMessage("", player.PlayerId, Conf.ToString(), noReplay: true);
                    if (Sub.ToString() != string.Empty) Utils.SendMessage(Sub.ToString(), player.PlayerId, SubTitle, noReplay: true);

                    Logger.Info($"Command '/m' should be send message", "OnReceiveChat");
                }
                else
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                break;

            case "/h":
            case "/help":
            case "/ajuda":
            case "/хелп":
            case "/хэлп":
            case "/помощь":
            case "/帮助":
            case "/教程":
                Utils.ShowHelpToClient(player.PlayerId);
                break;

            case "/ans":
            case "/asw":
            case "/answer":
            case "/回答":
                Quizmaster.AnswerByChat(player, args);
                break;

            case "/qmquiz":
            case "/提问":
                Quizmaster.ShowQuestion(player);
                break;

            case "/l":
            case "/lastresult":
            case "/fimdejogo":
            case "/上局信息":
            case "/信息":
            case "/情况":
                Utils.ShowKillLog(player.PlayerId);
                Utils.ShowLastRoles(player.PlayerId);
                Utils.ShowLastResult(player.PlayerId);
                break;

            case "/gr":
            case "/gameresults":
            case "/resultados":
            case "/对局结果":
            case "/上局结果":
            case "/结果":
                Utils.ShowLastResult(player.PlayerId);
                break;

            case "/kh":
            case "/killlog":
            case "/击杀日志":
            case "/击杀情况":
                Utils.ShowKillLog(player.PlayerId);
                break;

            case "/rs":
            case "/sum":
            case "/rolesummary":
            case "/sumario":
            case "/sumário":
            case "/summary":
            case "/результат":
            case "/上局职业":
            case "/职业信息":
            case "/对局职业":
                Utils.ShowLastRoles(player.PlayerId);
                break;

            case "/ghostinfo":
            case "/幽灵职业介绍":
            case "/鬼魂职业介绍":
            case "/幽灵职业":
            case "/鬼魂职业":
                if (GameStates.IsInGame)
                {
                    Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                    break;
                }
                Utils.SendMessage(GetString("Message.GhostRoleInfo"), player.PlayerId);
                break;

            case "/apocinfo":
            case "/apocalypseinfo":
            case "/末日中立职业介绍":
            case "/末日中立介绍":
            case "/末日类中立职业介绍":
            case "/末日类中立介绍":
                Utils.SendMessage(GetString("Message.ApocalypseInfo"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Apocalypse), GetString("ApocalypseInfoTitle")));
                break;

            case "/coveninfo":
            case "/covinfo":
                Utils.SendMessage(GetString("Message.CovenInfo"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Coven), GetString("CovenInfoTitle")));
                break;

            case "/anomalyinfo":
                canceled = true;
                Utils.SendMessage(GetString("Message.AnomalyInfo"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jester), GetString("AnomalyInfoTitle")));
                break;

            case "/rn":
            case "/rename":
            case "/renomear":
            case "/переименовать":
            case "/重命名":
            case "/命名为":
                if (Options.PlayerCanSetName.GetBool() || player.FriendCode.GetDevUser().IsDev || player.FriendCode.GetDevUser().NameCmd || TagManager.ReadPermission(player.FriendCode) >= 1)
                {
                    if (GameStates.IsInGame)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                        break;
                    }
                    if (args.Length < 1) break;
                    if (args.Skip(1).Join(delimiter: " ").Length is > 10 or < 1)
                    {
                        Utils.SendMessage(GetString("Message.AllowNameLength"), player.PlayerId);
                        break;
                    }
                    Main.AllPlayerNames[player.PlayerId] = args.Skip(1).Join(delimiter: " ");
                    Utils.SendMessage(string.Format(GetString("Message.SetName"), args.Skip(1).Join(delimiter: " ")), player.PlayerId);
                    break;
                }
                else
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                }
                break;

            case "/n":
            case "/now":
            case "/atual":
            case "/设置":
            case "/系统设置":
            case "/模组设置":
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

            case "/up":
            case "/指定":
            case "/成为":
                _ = text.Remove(0, 3);
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

            case "/win":
            case "/winner":
            case "/vencedor":
            case "/胜利":
            case "/获胜":
            case "/赢":
            case "/胜利者":
            case "/获胜的人":
            case "/赢家":
                if (Main.winnerNameList.Count == 0) Utils.SendMessage(GetString("NoInfoExists"), player.PlayerId);
                else Utils.SendMessage("Winner: " + string.Join(", ", Main.winnerNameList), player.PlayerId);
                break;


            case "/pv":
                canceled = true;
                if (!Pollvotes.Any())
                {
                    Utils.SendMessage(GetString("Poll.Inactive"), player.PlayerId);
                    break;
                }
                if (PollVoted.Contains(player.PlayerId))
                {
                    Utils.SendMessage(GetString("Poll.AlreadyVoted"), player.PlayerId);
                    break;
                }

                subArgs = args.Length != 2 ? "" : args[1];
                char vote = ' ';

                if (int.TryParse(subArgs, out int integer) && (Pollvotes.Count - 1) >= integer)
                {
                    vote = char.ToUpper((char)(integer + 65));
                }
                else if (!(char.TryParse(subArgs, out vote) && Pollvotes.ContainsKey(char.ToUpper(vote))))
                {
                    Utils.SendMessage(GetString("Poll.VotingInfo"), player.PlayerId);
                    break;
                }
                vote = char.ToUpper(vote);

                PollVoted.Add(player.PlayerId);
                Pollvotes[vote]++;
                Utils.SendMessage(string.Format(GetString("Poll.YouVoted"), vote, Pollvotes[vote]), player.PlayerId);
                Logger.Info($"The new value of {vote} is {Pollvotes[vote]}", "TestPV_CHAR");

                break;

            case "/icon":
            case "/icons":
            case "/符号":
            case "/标志":
                {
                    Utils.SendMessage(GetString("Command.icons"), player.PlayerId, GetString("IconsTitle"));
                    break;
                }

            case "/kc":
            case "/kcount":
            case "/количество":
            case "/убийцы":
            case "/存活阵营":
            case "/阵营":
            case "/存货阵营信息":
            case "/阵营信息":
                if (GameStates.IsLobby) break;

                if (!Options.EnableKillerLeftCommand.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }

                var allAlivePlayers = Main.AllAlivePlayerControls;
                int impnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Impostor));
                int madnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsMadmate() || pc.Is(CustomRoles.Madmate));
                int apocnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsNA());
                int neutralnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsNK());
                int covnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Coven));

                var sub = new StringBuilder();
                sub.Append(string.Format(GetString("Remaining.ImpostorCount"), impnum));

                if (Options.ShowMadmatesInLeftCommand.GetBool())
                    sub.Append(string.Format("\n\r" + GetString("Remaining.MadmateCount"), madnum));

                if (Options.ShowApocalypseInLeftCommand.GetBool())
                    sub.Append(string.Format("\n\r" + GetString("Remaining.ApocalypseCount"), apocnum));

                if (Options.ShowCovenInLeftCommand.GetBool())
                    sub.Append(string.Format("\n\r" + GetString("Remaining.CovenCount"), covnum));

                sub.Append(string.Format("\n\r" + GetString("Remaining.NeutralCount"), neutralnum));

                Utils.SendMessage(sub.ToString(), player.PlayerId);
                break;

            case "/d":
            case "/death":
            case "/morto":
            case "/умер":
            case "/причина":
            case "/死亡原因":
            case "/死亡":
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
                    var killer = player.GetRealKiller(out var MurderRole);
                    string killerName = killer == null ? "N/A" : killer.GetRealName(clientData: true);
                    string killerRole = killer == null ? "N/A" : Utils.GetRoleName(MurderRole);
                    Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + player.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(player.GetCustomRole())}>{Utils.GetRoleName(player.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.DeathReason") + "<b>" + Utils.GetVitalText(player.PlayerId) + "</b>" + "\n\r" + "</b>" + "\n\r" + GetString("DeathCmd.KillerName") + "<b>" + killerName + "</b>" + "\n\r" + GetString("DeathCmd.KillerRole") + "<b>" + $"<color={Utils.GetRoleColorCode(killer.GetCustomRole())}>{killerRole}</color>" + "</b>", player.PlayerId);
                    break;
                }

            case "/t":
            case "/template":
            case "/шаблон":
            case "/пример":
            case "/模板":
            case "/模板信息":
                if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                break;

            case "/colour":
            case "/color":
            case "/cor":
            case "/цвет":
            case "/颜色":
            case "/更改颜色":
            case "/修改颜色":
            case "/换颜色":
                if (Options.PlayerCanSetColor.GetBool() || player.FriendCode.GetDevUser().IsDev || player.FriendCode.GetDevUser().ColorCmd || (Utils.IsPlayerVIP(player.FriendCode) && Options.ApplyVipList.GetBool()))
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
            case "/退出":
            case "/退":
                if (Options.PlayerCanUseQuitCommand.GetBool())
                {
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
                }
                else
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                }
                break;

            case "/id":
            case "/айди":
            case "/编号":
            case "/玩家编号":
                if ((Options.ApplyModeratorList.GetValue() == 0 || (!Utils.IsPlayerModerator(player.FriendCode) && TagManager.ReadPermission(player.FriendCode) < 2))
                    && !Options.EnableVoteCommand.GetBool()) break;

                string msgText = GetString("PlayerIdList");
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc == null) continue;
                    msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                }
                Utils.SendMessage(msgText, player.PlayerId);
                break;

            case "/mid":
            case "/玩家列表":
            case "/玩家信息":
            case "/玩家编号列表":
                //Checking if modlist on or not
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("midCommandDisabled"), player.PlayerId);
                    break;
                }
                //Checking if Player is has necessary privellege or not
                if (!Utils.IsPlayerModerator(player.FriendCode) && TagManager.ReadPermission(player.FriendCode) < 2)
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
            case "/бан":
            case "/забанить":
            case "/封禁":
                // Check if the Ban command is enabled in the settings
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("BanCommandDisabled"), player.PlayerId);
                    break;
                }

                // Check if the Player has the necessary privileges to use the command
                if (!Utils.IsPlayerModerator(player.FriendCode) && TagManager.ReadPermission(player.FriendCode) < 5)
                {
                    Utils.SendMessage(GetString("BanCommandNoAccess"), player.PlayerId);
                    break;
                }
                string banReason;
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

                // Prevent Moderators from baning other Moderators
                if (Utils.IsPlayerModerator(bannedPlayer.FriendCode) || TagManager.ReadPermission(bannedPlayer.FriendCode) >= 2)
                {
                    Utils.SendMessage(GetString("BanCommandBanMod"), player.PlayerId);
                    break;
                }

                // Ban the specified Player
                AmongUsClient.Instance.KickPlayer(bannedPlayer.GetClientId(), true);
                string bannedPlayerName = bannedPlayer.GetRealName();
                string textToSend1 = $"{bannedPlayerName} {GetString("BanCommandBanned")}{player.name} \nReason: {banReason}\n";
                if (GameStates.IsInGame)
                {
                    textToSend1 += $" {GetString("BanCommandBannedRole")} {GetString(bannedPlayer.GetCustomRole().ToString())}";
                }
                Utils.SendMessage(textToSend1);
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
            case "/варн":
            case "/пред":
            case "/предупредить":
            case "/警告":
            case "/提醒":
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("WarnCommandDisabled"), player.PlayerId);
                    break;
                }
                if (!Utils.IsPlayerModerator(player.FriendCode) && TagManager.ReadPermission(player.FriendCode) < 2)
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

                // Prevent Moderators from warning other Moderators
                if (Utils.IsPlayerModerator(warnedPlayer.FriendCode) || TagManager.ReadPermission(warnedPlayer.FriendCode) >= 2)
                {
                    Utils.SendMessage(GetString("WarnCommandWarnMod"), player.PlayerId);
                    break;
                }
                // Warn the specified Player
                string warnReason = "Reason : Not specified\n";
                string warnedPlayerName = warnedPlayer.GetRealName();
                if (args.Length > 2)
                {
                    warnReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
                }
                else
                {
                    Utils.SendMessage("Use /warn [id] [reason] in future. \nExample :-\n /warn 5 lava chatting", player.PlayerId);
                }
                Utils.SendMessage($" {warnedPlayerName} {GetString("WarnCommandWarned")} {warnReason} ~{player.name}");
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
            case "/кик":
            case "/кикнуть":
            case "/выгнать":
            case "/踢出":
            case "/踢":
                // Check if the Kick command is enabled in the settings
                if (Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("KickCommandDisabled"), player.PlayerId);
                    break;
                }

                // Check if the Player has the necessary privileges to use the command
                if (!Utils.IsPlayerModerator(player.FriendCode) && TagManager.ReadPermission(player.FriendCode) < 4)
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

                // Prevent Moderators from kicking other Moderators
                if (Utils.IsPlayerModerator(kickedPlayer.FriendCode) || TagManager.ReadPermission(kickedPlayer.FriendCode) >= 4)
                {
                    Utils.SendMessage(GetString("KickCommandKickMod"), player.PlayerId);
                    break;
                }

                // Kick the specified Player
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
            case "/模组端颜色":
            case "/模组颜色":
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
                    Regex regex = new(@"^[0-9A-Fa-f]{6}\s[0-9A-Fa-f]{6}$");
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
                    File.WriteAllText(colorFilePath, $"{subArgs}");
                    break;
                }
            case "/vipcolor":
            case "/vipcolour":
            case "/VIP玩家颜色":
            case "/VIP颜色":
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
                    Regex regexx = new(@"^[0-9A-Fa-f]{6}\s[0-9A-Fa-f]{6}$");
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
                    File.WriteAllText(colorFilePathh, $"{subArgs}");
                    break;
                }
            case "/tagcolor":
            case "/tagcolour":
            case "/标签颜色":
            case "/附加名称颜色":
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
            case "/修复":
            case "/修":
                if (GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.IsAlive()) continue;

                    pc.RpcSetNamePrivate(pc.GetRealName(isMeeting: true), player, true);
                }
                ChatUpdatePatch.DoBlockChat = false;
                Utils.SendMessage(GetString("Message.TryFixName"), player.PlayerId);
                break;

            case "/tpout":
            case "/传送出":
            case "/传出":
                if (!GameStates.IsLobby) break;
                if (!Options.PlayerCanUseTP.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
                player.RpcTeleport(new Vector2(0.1f, 3.8f));
                break;
            case "/tpin":
            case "/传进":
            case "/传送进":
                if (!GameStates.IsLobby) break;
                if (!Options.PlayerCanUseTP.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }

                player.RpcTeleport(new Vector2(-0.2f, 1.3f));
                break;

            case "/vote":
            case "/投票":
            case "/票":
                subArgs = args.Length != 2 ? "" : args[1];
                if (subArgs == "" || !int.TryParse(subArgs, out int arg))
                    break;
                var plr = Utils.GetPlayerById(arg);

                if (GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }


                if (!Options.EnableVoteCommand.GetBool())
                {
                    Utils.SendMessage(GetString("VoteDisabled"), player.PlayerId);
                    break;
                }
                if (Options.ShouldVoteCmdsSpamChat.GetBool())
                {
                    canceled = true;
                    ChatManager.SendPreviousMessagesToAll();
                }

                if (arg != 253)
                {
                    if (plr == null || !plr.IsAlive())
                    {
                        Utils.SendMessage(GetString("VoteDead"), player.PlayerId);
                        break;
                    }
                }
                if (!player.IsAlive())
                {
                    Utils.SendMessage(GetString("CannotVoteWhenDead"), player.PlayerId);
                    break;
                }
                if (GameStates.IsMeeting)
                {
                    player.RpcCastVote((byte)arg);
                }
                break;

            case "/say":
            case "/s":
            case "/с":
            case "/сказать":
            case "/说":
                if (player.FriendCode.GetDevUser().IsDev)
                {
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color={Main.ModColor}>{GetString("MessageFromDev")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                }
                else if (Utils.IsPlayerModerator(player.FriendCode))
                {
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#8bbee0>{GetString("MessageFromSponsor")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                }
                else if (Utils.IsPlayerModerator(player.FriendCode) || TagManager.CanUseSayCommand(player.FriendCode))
                {
                    if (Options.ApplyModeratorList.GetValue() == 0 || Options.AllowSayCommand.GetBool() == false)
                    {
                        Utils.SendMessage(GetString("SayCommandDisabled"), player.PlayerId);
                        break;
                    }
                    else
                    {
                        var modTitle = (Utils.IsPlayerModerator(player.FriendCode) || TagManager.ReadPermission(player.FriendCode) >= 2) ? $"<color=#8bbee0>{GetString("MessageFromModerator")}" : $"<color=#ffff00>{GetString("MessageFromVIP")}";
                        if (args.Length > 1)
                            Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"{modTitle} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                        string modLogname3 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n4) ? n4 : "";

                        string moderatorFriendCode3 = player.FriendCode.ToString();
                        string logMessage3 = $"[{DateTime.Now}] {moderatorFriendCode3},{modLogname3} used /s: {args.Skip(1).Join(delimiter: " ")}";
                        File.AppendAllText(modLogFiles, logMessage3 + Environment.NewLine);

                    }
                }
                break;
            case "/rps":
            case "/剪刀石头布":
                //canceled = true;
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
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
            case "/抛硬币":
                //canceled = true;
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }

                if (!GameStates.IsLobby && player.IsAlive())
                {
                    Utils.SendMessage(GetString("CoinflipCommandInfo"), player.PlayerId);
                    break;
                }
                else
                {
                    var rand = IRandom.Instance;
                    int botChoice = rand.Next(1, 101);
                    var coinSide = (botChoice < 51) ? GetString("Heads") : GetString("Tails");
                    Utils.SendMessage(string.Format(GetString("CoinFlipResult"), coinSide), player.PlayerId);
                    break;
                }
            case "/gno":
            case "/猜数字":
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
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
                        Utils.SendMessage(string.Format(GetString("GNoWon"), Main.GuessNumber[player.PlayerId][1]), player.PlayerId);
                        Main.GuessNumber[player.PlayerId][0] = -1;
                        Main.GuessNumber[player.PlayerId][1] = 7;
                        break;
                    }
                }
            case "/rand":
            case "/XY数字":
            case "/范围游戏":
            case "/猜范围":
            case "/范围":
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
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
                    int botResult = rand.Next(playerChoice1, playerChoice2 + 1);
                    Utils.SendMessage(string.Format(GetString("RandResult"), botResult), player.PlayerId);
                    break;
                }
            case "/8ball":
            case "/8号球":
            case "/幸运球":
                if (!Options.CanPlayMiniGames.GetBool())
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    break;
                }
                canceled = true;
                var rando = IRandom.Instance;
                int result = rando.Next(0, 16);
                string str = "";
                switch (result)
                {
                    case 0:
                        str = GetString("8BallYes");
                        break;
                    case 1:
                        str = GetString("8BallNo");
                        break;
                    case 2:
                        str = GetString("8BallMaybe");
                        break;
                    case 3:
                        str = GetString("8BallTryAgainLater");
                        break;
                    case 4:
                        str = GetString("8BallCertain");
                        break;
                    case 5:
                        str = GetString("8BallNotLikely");
                        break;
                    case 6:
                        str = GetString("8BallLikely");
                        break;
                    case 7:
                        str = GetString("8BallDontCount");
                        break;
                    case 8:
                        str = GetString("8BallStop");
                        break;
                    case 9:
                        str = GetString("8BallPossibly");
                        break;
                    case 10:
                        str = GetString("8BallProbably");
                        break;
                    case 11:
                        str = GetString("8BallProbablyNot");
                        break;
                    case 12:
                        str = GetString("8BallBetterNotTell");
                        break;
                    case 13:
                        str = GetString("8BallCantPredict");
                        break;
                    case 14:
                        str = GetString("8BallWithoutDoubt");
                        break;
                    case 15:
                        str = GetString("8BallWithDoubt");
                        break;
                }
                Utils.SendMessage("<align=\"center\"><size=150%>" + str + "</align></size>", player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medium), GetString("8BallTitle")));
                break;
            case "/me":
            case "/我的权限":
            case "/权限":

                string Devbox = player.FriendCode.GetDevUser().DeBug ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                string UpBox = player.FriendCode.GetDevUser().IsUp ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                string ColorBox = player.FriendCode.GetDevUser().ColorCmd ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";

                subArgs = text.Length == 3 ? string.Empty : text.Remove(0, 3);
                if (string.IsNullOrEmpty(subArgs))
                {
                    Utils.SendMessage((player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), player.PlayerId, player.GetRealName(clientData: true), player.GetClient().FriendCode, player.GetClient().GetHashedPuid(), player.FriendCode.GetDevUser(), Devbox, UpBox, ColorBox)}", player.PlayerId);
                }
                else
                {
                    if (Options.ApplyModeratorList.GetValue() == 0 || (!Utils.IsPlayerModerator(player.FriendCode) && TagManager.ReadPermission(player.FriendCode) < 2))
                    {
                        Utils.SendMessage(GetString("Message.MeCommandNoPermission"), player.PlayerId);
                        break;
                    }
                    if (byte.TryParse(subArgs, out byte meid))
                    {
                        if (meid != player.PlayerId)
                        {
                            var targetplayer = Utils.GetPlayerById(meid);
                            if (targetplayer != null && targetplayer.GetClient() != null)
                            {
                                Utils.SendMessage($"{string.Format(GetString("Message.MeCommandTargetInfo"), targetplayer.PlayerId, targetplayer.GetRealName(clientData: true), targetplayer.GetClient().FriendCode, targetplayer.GetClient().GetHashedPuid(), targetplayer.FriendCode.GetDevUser())}", player.PlayerId);
                            }
                            else
                            {
                                Utils.SendMessage($"{(GetString("Message.MeCommandInvalidID"))}", player.PlayerId);
                            }
                        }
                        else
                        {
                            Utils.SendMessage($"{string.Format(GetString("Message.MeCommandInfo"), PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.GetRealName(clientData: true), PlayerControl.LocalPlayer.GetClient().FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid(), PlayerControl.LocalPlayer.FriendCode.GetDevUser(), Devbox, UpBox, ColorBox)}", player.PlayerId);
                        }
                    }
                    else
                    {
                        Utils.SendMessage($"{(GetString("Message.MeCommandInvalidID"))}", player.PlayerId);
                    }
                }
                break;
                
            case "/spectate":
            case "/спектейт":
            case "/观战":
            case "/espectar":
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                    break;
                }

                if (Options.DisableSpectateCommand.GetBool())
                {
                    Utils.SendMessage("\n", player.PlayerId, GetString("SpectateDisabled"));
                    break;
                }

                if (LastSpectators.Contains(player.PlayerId))
                {
                    Utils.SendMessage("\n", player.PlayerId, GetString("SpectateCommand.WasSpectatingLastRound"));
                    break;
                }

                if (Spectators.Remove(player.PlayerId))
                {
                    Utils.SendMessage("\n", player.PlayerId, GetString("SpectateCommand.Removed"));
                    break;
                }

                if (Spectators.Add(player.PlayerId))
                    Utils.SendMessage("\n", player.PlayerId, GetString("SpectateCommand.Success"));
                break;

            case "/start":
            case "/开始":
            case "/старт":
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                    break;
                }

                if (!Utils.IsPlayerModerator(player.FriendCode) && TagManager.ReadPermission(player.FriendCode) < 3)
                {
                    Utils.SendMessage(GetString("StartCommandNoAccess"), player.PlayerId);
                    break;

                }
                if (Options.ApplyModeratorList.GetValue() == 0 || Options.AllowStartCommand.GetBool() == false)
                {
                    Utils.SendMessage(GetString("StartCommandDisabled"), player.PlayerId);
                    break;
                }
                if (GameStates.IsCountDown)
                {
                    Utils.SendMessage(GetString("StartCommandCountdown"), player.PlayerId);
                    break;
                }
                subArgs = args.Length < 2 ? "" : args[1];
                if (string.IsNullOrEmpty(subArgs) || !int.TryParse(subArgs, out int countdown))
                {
                    countdown = 5;
                }
                else
                {
                    countdown = int.Parse(subArgs);
                }
                if (countdown < Options.StartCommandMinCountdown.CurrentValue || countdown > Options.StartCommandMaxCountdown.CurrentValue)
                {
                    Utils.SendMessage(string.Format(GetString("StartCommandInvalidCountdown"), Options.StartCommandMinCountdown.CurrentValue, Options.StartCommandMaxCountdown.CurrentValue), player.PlayerId);
                    break;
                }
                GameStartManager.Instance.BeginGame();
                GameStartManager.Instance.countDownTimer = countdown;
                Utils.SendMessage(string.Format(GetString("StartCommandStarted"), player.name));
                break;
            case "/end":
            case "/encerrar":
            case "/завершить":
            case "/结束":
            case "/结束游戏":
                if (!TagManager.CanUseEndCommand(player.FriendCode))
                {
                    Utils.SendMessage(GetString("EndCommandNoAccess"), player.PlayerId);
                    break;

                }
                Utils.SendMessage(string.Format(GetString("EndCommandEnded"), player.name));
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                GameManager.Instance.LogicFlow.CheckEndCriteria();
                break;
            case "/exe":
            case "/уничтожить":
            case "/повесить":
            case "/казнить":
            case "/казнь":
            case "/мут":
            case "/驱逐":
            case "/驱赶":
                if (!TagManager.CanUseExecuteCommand(player.FriendCode))
                {
                    Utils.SendMessage(GetString("ExecuteCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }
                if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                var target = Utils.GetPlayerById(id);
                if (target != null)
                {
                    target.Data.IsDead = true;
                    target.SetDeathReason(PlayerState.DeathReason.etc);
                    target.SetRealKiller(player);
                    Main.PlayerStates[target.PlayerId].SetDead();
                    target.RpcExileV2();
                    MurderPlayerPatch.AfterPlayerDeathTasks(target, target, GameStates.IsMeeting);
                    Utils.SendMessage(string.Format(GetString("Message.ExecutedNonHost"), target.Data.PlayerName, player.Data.PlayerName));
                }
                break;
                          case "/ability":
                subArgs = args.Length < 2 ? "" : args[1];
                CopsAndRobbersManager.AbilityDescription(subArgs, player.PlayerId);
                break;

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
    public static ChatController Instance;
    public static void Postfix(ChatController __instance)
    {
        if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count == 0 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.timeSinceLastMessage)) return;
        if (DoBlockChat) return;

        Instance ??= __instance;

        if (Main.DarkTheme.Value)
        {
            var chatBubble = __instance.chatBubblePool.Prefab.Cast<ChatBubble>();
            chatBubble.TextArea.overrideColorTags = false;
            chatBubble.TextArea.color = Color.white;
            chatBubble.Background.color = Color.black;
        }

        var player = PlayerControl.LocalPlayer;
        if (GameStates.IsInGame || player.Data.IsDead)
        {
            player = Main.AllAlivePlayerControls.ToArray().OrderBy(x => x.PlayerId).FirstOrDefault()
                     ?? Main.AllPlayerControls.ToArray().OrderBy(x => x.PlayerId).FirstOrDefault()
                     ?? player;
        }
        if (player == null) return;

        (string msg, byte sendTo, string title) = Main.MessagesToSend[0];

        if (sendTo != byte.MaxValue && GameStates.IsLobby)
        {
            var networkedPlayerInfo = Utils.GetPlayerInfoById(sendTo);
            if (networkedPlayerInfo != null)
            {
                if (networkedPlayerInfo.DefaultOutfit.ColorId == -1)
                {
                    var delaymessage = Main.MessagesToSend[0];
                    Main.MessagesToSend.RemoveAt(0);
                    Main.MessagesToSend.Add(delaymessage);
                    return;
                }
            }
        }
        Main.MessagesToSend.RemoveAt(0);

        int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
        var name = player.Data.PlayerName;

        if (clientId == -1)
        {
            player.SetName(title);
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg, false);
            player.SetName(name);
        }

        if (clientId == AmongUsClient.Instance.ClientId || sendTo == PlayerControl.LocalPlayer.PlayerId)
        {
            player.SetName(title);
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg, false);
            player.SetName(name);
            return;
        }

        var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
        writer.StartMessage(clientId);
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(player.Data.NetId)
            .Write(title)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
            .Write(msg)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(player.Data.NetId)
            .Write(player.Data.PlayerName)
            .EndRpc();
        writer.EndMessage();
        writer.SendMessage();

        __instance.timeSinceLastMessage = 0f;
    }
}

[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
internal class UpdateCharCountPatch
{
    public static void Postfix(FreeChatInputField __instance)
    {
        int length = __instance.textArea.text.Length;
        __instance.charCountText.SetText(length <= 0 ? GetString("ThankYouForUsingTOHO") : $"{length}/{__instance.textArea.characterLimit}");
        __instance.charCountText.enableWordWrapping = false;
        if (length < (AmongUsClient.Instance.AmHost ? 888 : 444))
            __instance.charCountText.color = Color.black;
        else if (length < (AmongUsClient.Instance.AmHost ? 1111 : 777))
            __instance.charCountText.color = new Color(1f, 1f, 0f, 1f);
        else
            __instance.charCountText.color = Color.red;
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
        if (!GameStates.IsModHost)
        {
            __result = false;
            return true;
        }
        int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
        chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
        if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
        if (chatText.Contains("who", StringComparison.OrdinalIgnoreCase))
            DestroyableSingleton<UnityTelemetry>.Instance.SendWho();
        /*
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
        messageWriter.Write(chatText);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        */
        var message = new RpcSendChatMessage(__instance.NetId, chatText);
        RpcUtils.LateBroadcastReliableMessage(message);
        __result = true;
        return false;
    }
}
