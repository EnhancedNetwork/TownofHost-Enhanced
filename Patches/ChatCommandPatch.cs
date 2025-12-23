using AmongUs.InnerNet.GameDataMessages;
using Assets.CoreScripts;
using Hazel;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.Core.AssignManager;
using TOHE.Roles.Core.DraftAssign;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;


namespace TOHE;

// Credit: EHR
internal class Command(string commandKey, string arguments, string description, Command.UsageLevels usageLevel, Command.UsageTimes usageTime, Action<PlayerControl, string, string[]> action, bool isCanceled, bool alwaysHidden, string[] argsDescriptions = null)
{
    public enum UsageLevels
    {
        Everyone,
        Modded,
        Host,
        HostOrVIP,
        HostOrModerator,
        Developer
    }

    public enum UsageTimes
    {
        Always,
        InLobby,
        InGame,
        InMeeting,
        AfterDeath,
        AfterDeathOrLobby
    }

    public string CommandKey => commandKey;
    private string[] commandForms;
    public string[] CommandForms => commandForms ?? GetCrossLangAliases();
    public string Arguments => arguments;
    public string Description => description;
    public string[] ArgsDescriptions => argsDescriptions ?? [];
    public UsageLevels UsageLevel => usageLevel;
    public UsageTimes UsageTime => usageTime;
    public Action<PlayerControl, string, string[]> Action => action;
    public bool IsCanceled => isCanceled;
    public bool AlwaysHidden => alwaysHidden;

    public bool IsThisCommand(string text)
    {
        if (!text.StartsWith('/')) return false;

        text = text.ToLower().Trim().TrimStart('/');
        return CommandForms.Any(text.Split(' ')[0].Equals);
    }

    public bool CanUseCommand(PlayerControl pc, bool checkTime = true, bool sendErrorMessage = false)
    {
        if (UsageLevel == UsageLevels.Everyone && UsageTime == UsageTimes.Always) return true;

        // if (Lovers.PrivateChat.GetBool() && GameStates.IsInTask && pc.IsAlive()) return false;

        switch (UsageLevel)
        {
            case UsageLevels.Host when !pc.IsHost():
            case UsageLevels.Developer when !pc.FriendCode.GetDevUser().IsDev:
            case UsageLevels.Modded when !pc.IsModded():
            case UsageLevels.HostOrVIP when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerVIP(pc.FriendCode) 
                && !Utils.IsPlayerModerator(pc.FriendCode) && !pc.FriendCode.GetDevUser().IsDev:
            case UsageLevels.HostOrModerator when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(pc.FriendCode) 
                && !pc.FriendCode.GetDevUser().IsDev:
                if (sendErrorMessage) Utils.SendMessage("\n", pc.PlayerId, GetString($"Commands.NoAccess.Level.{UsageLevel}"));
                return false;
        }

        if (!checkTime) return true;

        switch (UsageTime)
        {
            case UsageTimes.InLobby when !GameStates.IsLobby:
            case UsageTimes.InGame when !GameStates.InGame:
            case UsageTimes.InMeeting when !GameStates.IsMeeting:
            case UsageTimes.AfterDeath when pc.IsAlive():
            case UsageTimes.AfterDeathOrLobby when pc.IsAlive() && !GameStates.IsLobby:
                if (sendErrorMessage) Utils.SendMessage("\n", pc.PlayerId, GetString($"Commands.NoAccess.Time.{UsageTime}"));
                return false;
        }

        return true;
    }

    private string[] GetCrossLangAliases()
    {
        commandForms = [];
        foreach (var lang in EnumHelper.GetAllValues<SupportedLangs>())
        {
            var names = GetString($"{CommandKey}", lang).Split("|");
            foreach (var n in names)
            {
                var name = n.ToLower().Trim();
                if (!commandForms.Contains(name))
                {
                    commandForms.AddItem(name);
                }
            }
        }
        return commandForms;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal class ChatCommands
{
#if !ANDROID
    private static readonly string modLogFiles = @"./TOHE-DATA/ModLogs.txt";
    private static readonly string modTagsFiles = @"./TOHE-DATA/Tags/MOD_TAGS";
    private static readonly string sponsorTagsFiles = @"./TOHE-DATA/Tags/SPONSOR_TAGS";
    private static readonly string vipTagsFiles = @"./TOHE-DATA/Tags/VIP_TAGS";
#else
    private static readonly string modLogFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TOHE-DATA", "ModLogs.txt");
    private static readonly string modTagsFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TOHE-DATA", "MOD_TAGS");
    private static readonly string sponsorTagsFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TOHE-DATA", "SPONSOR_TAGS");
    private static readonly string vipTagsFiles = Path.Combine(UnityEngine.Application.persistentDataPath, "TOHE-DATA", "VIP_TAGS");
#endif

    private static readonly Dictionary<char, int> Pollvotes = [];
    private static readonly Dictionary<char, string> PollQuestions = [];
    private static readonly List<byte> PollVoted = [];
    private static float Polltimer = 120f;
    private static string PollMSG = "";

    public const string Csize = "85%"; // CustomRole Settings Font-Size
    public const string Asize = "75%"; // All Appended Addons Font-Size

    public static readonly List<string> ChatHistory = [];
    public static readonly Dictionary<byte, long> LastSentCommand = [];
    public static HashSet<Command> AllCommands = [];

    public static void LoadCommands()
    {
        AllCommands =
        [
            new("Command.Dump", "", GetString("CommandDescription.Dump"), Command.UsageLevels.Modded, Command.UsageTimes.Always, DumpCommand, false, false), // ["dump", "дамп", "лог", "导出日志", "日志", "导出"]
            new("Command.Version", "", GetString("CommandDescription.Version"), Command.UsageLevels.Modded, Command.UsageTimes.Always, VersionCommand, false, false), // ["v", "version", "в", "версия", "检查版本", "versão", "版本"]
            new("Command.Save", "[?]", GetString("CommandDescription.Save"), Command.UsageLevels.Modded, Command.UsageTimes.InLobby, SaveCommand, false, false, [GetString("CommandArgs.Save.Path")]), // ["save", "savepreset"]
            new("Command.Docs", "{role}", GetString("CommandDescription.Docs"), Command.UsageLevels.Developer, Command.UsageTimes.InLobby, DocsCommand, false, false, [GetString("CommandArgs.Docs.Role")]), // ["docs"]
            new("Command.Winner", "", GetString("CommandDescription.Winner"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, WinnerCommand, true, false), // ["win", "winner", "победители", "获胜者", "vencedor", "胜利", "获胜", "赢", "获胜的人", "赢家"]
            new("Command.LastResult", "", GetString("CommandDescription.LastResult"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, LastResultCommand, true, false), // ["l", "lastresult", "л", "对局职业信息", "resultados", "ultimoresultado", "fimdejogo", "上局信息", "信息", "情况"]
            new("Command.GameResult", "", GetString("CommandDescription.GameResult"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, GameResultCommand, true, false), // ["gr", "gameresults", "resultados", "对局结果", "上局结果", "结果"]
            new("Command.KillLog", "", GetString("CommandDescription.KillLog"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, KillLogCommand, true, false), // ["kh", "killlog", "击杀日志", "击杀情况"]
            new("Command.RoleSummary", "", GetString("CommandDescription.RoleSummary"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, RoleSummaryCommand, true, false), // ["rs", "sum", "rolesummary", "sumario", "sumário", "summary", "результат", "上局职业", "职业信息", "对局职业"]
            new("Command.GhostInfo", "", GetString("CommandDescription.GhostInfo"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, GhostInfoCommand, true, false), // ["ghostinfo", "幽灵职业介绍", "鬼魂职业介绍", "幽灵职业", "鬼魂职业"]
            new("Command.ApocInfo", "", GetString("CommandDescription.ApocInfo"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, ApocInfoCommand, true, false), // ["apocinfo", "apocalypseinfo", "末日中立职业介绍", "末日中立介绍", "末日类中立职业介绍", "末日类中立介绍"]
            new("Command.CovenInfo", "", GetString("CommandDescription.CovenInfo"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, CovenInfoCommand, true, false), // ["coveninfo", "covinfo", "巫师阵营职业介绍", "巫师阵营介绍", "巫师介绍"]
            new("Command.Rename", "{name}", GetString("CommandDescription.Rename"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, RenameCommand, true, false, [GetString("CommandArgs.Rename.Name")]), // ["rn", "rename", "name", "рн", "ренейм", "переименовать", "修改名称", "renomear", "重命名", "命名为"] 
            new("Command.HideName", "", GetString("CommandDescription.HideName"), Command.UsageLevels.Host, Command.UsageTimes.InLobby, HideNameCommand, true, false), // ["hn", "hidename", "хн", "спрник", "隐藏姓名", "semnome", "escondernome", "隐藏名字", "藏名"]
            new("Command.Level", "{level}", GetString("CommandDescription.Level"), Command.UsageLevels.Host, Command.UsageTimes.InLobby, LevelCommand, true, false, [GetString("CommandArgs.Level.Level")]), // ["level", "лвл", "уровень", "修改等级", "nível", "nivel", "等级", "等级设置为"] 
            new("Command.Now", "", GetString("CommandDescription.Now"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, NowCommand, true, false), // ["n", "now", "н", "当前设置", "atual", "设置", "系统设置", "模组设置"] 
            new("Command.Disconnect", "{team}", GetString("CommandDescription.Disconnect"), Command.UsageLevels.Host, Command.UsageTimes.InGame, DisconnectCommand, true, false, [GetString("CommandArgs.Disconnect.Team")]), // ["dis", "disconnect", "дис", "断连", "desconectar", "断连"]
            new("Command.R", "[role]", GetString("CommandDescription.R"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, RCommand, true, false, [GetString("CommandArgs.R.Role")]), // ["r", "р", "função", "role", "роль"] 
            new("Command.Factions", "", GetString("CommandDescription.Factions"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, FactionsCommand, true, false), // ["f", "factions", "faction"]
            new("Command.MyRole", "", GetString("CommandDescription.MyRole"), Command.UsageLevels.Everyone, Command.UsageTimes.InGame, MyRoleCommand, true, false), // ["m", "myrole", "м", "мояроль", "我的职业", "minhafunção", "м", "身份", "我", "我的身份"]
            new("Command.Help", "", GetString("CommandDescription.Help"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, HelpCommand, true, false), // ["h", "help", "хэлп", "хелп", "помощь", "帮助", "ajuda", "教程"] 
            new("Command.SetRole", "{id} {role}", GetString("CommandDescription.SetRole"), Command.UsageLevels.Host, Command.UsageTimes.InLobby, SetRoleCommand, true, false, [GetString("CommandArgs.SetRole.Id"), GetString("CommandArgs.SetRole.Role")]), // ["setrole", "setaddon", "сетроль", "预设职业", "definir-função"]
            new("Command.AFKExempt", "{id}", GetString("CommandDescription.AFKExempt"), Command.UsageLevels.HostOrModerator, Command.UsageTimes.Always, AFKExemptCommand, true, false, [GetString("CommandArgs.AFKExempt.Id")]), // ["afkexempt", "освафк", "афкосв", "挂机检测器不会检测", "afk-isentar"]
            new("Command.TPOut", "", GetString("CommandDescription.TPOut"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, TPOutCommand, true, false), // ["tpout", "тпаут", "传送出", "传出"]
            new("Command.TPIn", "", GetString("CommandDescription.TPIn"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, TPInCommand, true, false), // ["tpin", "тпин", "传送进", "传进"]
            new("Command.KCount", "", GetString("CommandDescription.KCount"), Command.UsageLevels.Everyone, Command.UsageTimes.InGame, KCountCommand, true, false), // ["gamestate", "gstate", "gs", "kcount", "kc", "кубийц", "гс", "статигры", "对局状态", "estadojogo", "status", "количество", "убийцы", "存活阵营", "阵营", "存货阵营信息", "阵营信息"] 
            new("Command.Template", "{tag}", GetString("CommandDescription.Template"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, TemplateCommand, true, false, [GetString("CommandArgs.Template.Tag")]), // ["t", "template", "т", "темплейт", "模板", "шаблон", "пример", "模板信息"] 
            new("Command.MessageWait", "{duration}", GetString("CommandDescription.MessageWait"), Command.UsageLevels.Host, Command.UsageTimes.Always, MessageWaitCommand, true, false, [GetString("CommandArgs.MessageWait.Duration")]), // ["mw", "messagewait", "мв", "медленныйрежим", "消息冷却", "espera-mensagens", "消息等待时间"]
            new("Command.Death", "[id]", GetString("CommandDescription.Death"), Command.UsageLevels.Everyone, Command.UsageTimes.AfterDeath, DeathCommand, true, false, [GetString("CommandArgs.Death.Id")]), // ["death", "d", "д", "смерть", "死亡原因", "abate", "morto", "умер", "причина", "死亡"]
            new("Command.Say", "{message}", GetString("CommandDescription.Say"), Command.UsageLevels.HostOrModerator, Command.UsageTimes.Always, SayCommand, true, false, [GetString("CommandArgs.Say.Message")]), // ["say", "s", "сказать", "с", "说", "falar", "dizer"]
            new("Command.Vote", "{id}", GetString("CommandDescription.Vote"), Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, VoteCommand, true, true, [GetString("CommandArgs.Vote.Id")]),// ["vote", "голос", "投票给", "votar", "投票", "票"]
            new("Command.Ban", "{id} [reason]", GetString("CommandDescription.Ban"), Command.UsageLevels.HostOrModerator, Command.UsageTimes.Always, BanKickCommand, true, false, [GetString("CommandArgs.Ban.Id"), GetString("CommandArgs.Ban.Reason")]), // ["ban", "kick", "бан", "кик", "забанить", "кикнуть", "封禁", "踢出", "banir", "expulsar", "выгнать", "踢"] 
            
        ];
    }

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

        if (Main.Daybreak) goto Canceled;
        if (GuessManager.GuesserMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Judge.TrialMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (President.EndMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Inspector.InspectCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Pirate.DuelCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Councillor cl && cl.MurderMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Nemesis.NemesisMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Retributionist.RetributionistMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Exorcist ex && ex.CheckCommand(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Ritualist.RitualistMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Medium.MsMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (Summoner.SummonerCheckMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Swapper sw && sw.SwapMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Dictator dt && dt.ExilePlayer(PlayerControl.LocalPlayer, text)) goto Canceled;
        if (PlayerControl.LocalPlayer.GetRoleClass() is Starspawn st && st.DaybreakMessage(PlayerControl.LocalPlayer, text)) goto Canceled;
        Directory.CreateDirectory(modTagsFiles);
        Directory.CreateDirectory(vipTagsFiles);
        Directory.CreateDirectory(sponsorTagsFiles);

        if (Blackmailer.CheckBlackmaile(PlayerControl.LocalPlayer) && PlayerControl.LocalPlayer.IsAlive())
        {
            goto Canceled;
        }
        if (Exorcist.IsExorcismCurrentlyActive() && PlayerControl.LocalPlayer.IsAlive())
        {
            Exorcist.ExorcisePlayer(PlayerControl.LocalPlayer);
            goto Canceled;
        }
        Main.isChatCommand = false;
        if (AmongUsClient.Instance.AmHost)
        {
            Main.isChatCommand = true;
            switch (args[0])
            {
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
                
                case "/me":
                case "/我的权限":
                case "/权限":
                    canceled = true;
                    subArgs = text.Length == 3 ? string.Empty : text.Remove(0, 3);
                    string Devbox = PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                    // string UpBox = PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsUp ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                    string ColorBox = PlayerControl.LocalPlayer.FriendCode.GetDevUser().ColorCmd ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";

                    if (string.IsNullOrEmpty(subArgs))
                    {
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.GetRealName(clientData: true), PlayerControl.LocalPlayer.GetClient().FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid(), PlayerControl.LocalPlayer.FriendCode.GetDevUser().GetUserType(), Devbox, "", ColorBox)}");
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
                                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandTargetInfo"), targetplayer.PlayerId, targetplayer.GetRealName(clientData: true), targetplayer.GetClient().FriendCode, targetplayer.GetClient().GetHashedPuid(), targetplayer.FriendCode.GetDevUser().GetUserType())}");
                                }
                                else
                                {
                                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{(GetString("Message.MeCommandInvalidID"))}");
                                }
                            }
                            else
                            {
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.GetRealName(clientData: true), PlayerControl.LocalPlayer.GetClient().FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid(), PlayerControl.LocalPlayer.FriendCode.GetDevUser().GetUserType(), Devbox, "", ColorBox)}");
                            }
                        }
                        else
                        {
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (PlayerControl.LocalPlayer.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{(GetString("Message.MeCommandInvalidID"))}");
                        }
                    }
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
                    string modLogname1 = Main.AllPlayerNames.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var n2) ? n2 : "";
                    string warnlogname = Main.AllPlayerNames.TryGetValue(warnedPlayer.PlayerId, out var n12) ? n12 : "";

                    string moderatorFriendCode1 = PlayerControl.LocalPlayer.FriendCode.ToString();
                    string warnedPlayerFriendCode = warnedPlayer.FriendCode.ToString();
                    string warnedPlayerHashPuid = warnedPlayer.GetClient().GetHashedPuid();
                    string logMessage1 = $"[{DateTime.Now}] {moderatorFriendCode1},{modLogname1} Warned: {warnedPlayerFriendCode},{warnedPlayerHashPuid},{warnlogname} Reason: {warnReason}";
                    File.AppendAllText(modLogFiles, logMessage1 + Environment.NewLine);

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
                    //Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);
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
                    subArgs = text[8..];
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
                        //Logger.Info(roleName, "2");
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
                        if (MeetingHud.Instance)
                        {
                            MeetingHud.Instance.RpcClose();
                        }
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
                    RPC.PlaySoundRPC((Sounds)sound1, PlayerControl.LocalPlayer.PlayerId);
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
                        int botChoice = rand.Next(100);
                        var coinSide = (botChoice < 50) ? GetString("Heads") : GetString("Tails");
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
                case "/deck":
                    if (!PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsDev && !Options.devEnableDraft)
                    {
                        break;
                    }
                    canceled = true;
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    PlayerControl.LocalPlayer.SendDeckList();

                    break;
                case "/draft":
                    if (!PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsDev && !Options.devEnableDraft)
                    {
                        break;
                    }
                    canceled = true;
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }

                    if (args.Length < 2 || args[1] == "start")
                    {
                        var startResult = DraftAssign.StartDraft();

                        if (startResult == DraftAssign.DraftCmdResult.NoCurrentDraft)
                        {
                            Utils.SendMessage(GetString("StartDraftWrongGameMode"), PlayerControl.LocalPlayer.PlayerId);
                        }
                        else
                        {
                            foreach (var pc in Main.AllPlayerControls)
                            {
                                Utils.SendMessage(string.Format(GetString("DraftPoolMessage"), pc.GetFormattedDraftPool()), pc.PlayerId);
                            }
                        }
                    }
                    else if (args[1] == "desc" || args[1] == "description")
                    {
                        if (args.Length > 2) args[1] = args[2];
                        goto case "/dd";
                    }
                    else if (args[1] == "add")
                    {
                        var addResult = DraftAssign.DraftActive;

                        if (!addResult)
                        {
                            Utils.SendMessage(GetString("NoCurrentDraft"), PlayerControl.LocalPlayer.PlayerId);
                        }
                        else
                        {
                            foreach (var pc in Main.AllPlayerControls)
                            {
                                Utils.SendMessage(string.Format(GetString("DraftPoolMessage"), pc.GetFormattedDraftPool()), pc.PlayerId);
                            }
                        }
                    }
                    else if (args[1] == "reset")
                    {
                        DraftAssign.Reset();
                    }
                    else if (args[1] == "enable" && PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsDev)
                    {
                        Options.devEnableDraft = true;
                        Options.DraftHeader.SetHidden(false);
                        Options.DraftMode.SetHidden(false);
                        Utils.SendMessage($"{PlayerControl.LocalPlayer.GetRealName()} has enabled draft for you. (No leaks yet please)", PlayerControl.LocalPlayer.PlayerId);
                    }
                    else
                    {
                        CustomRoles draftedRole;
                        DraftAssign.DraftCmdResult cmdResult;
                        if (int.TryParse(args[1], out int index))
                        {
                            (cmdResult, draftedRole) = PlayerControl.LocalPlayer.DraftRole(index);
                            PlayerControl.LocalPlayer.SendDraftDescription(index);
                        }
                        else
                        {
                            Utils.SendMessage(GetString("InvalidDraftSelection"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }

                        if (cmdResult == DraftAssign.DraftCmdResult.NoCurrentDraft)
                        {
                            Utils.SendMessage(GetString("NoCurrentDraft"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        else if (cmdResult == DraftAssign.DraftCmdResult.DraftRemoved)
                        {
                            Utils.SendMessage(GetString("DraftSelectionCleared"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        else
                        {
                            Utils.SendMessage(string.Format(GetString("DraftSelection"), draftedRole.ToColoredString()), PlayerControl.LocalPlayer.PlayerId);
                        }
                    }
                    break;
                case "/dd":
                case "/draftdescription":
                    if (int.TryParse(args[1], out int index2))
                    {
                        PlayerControl.LocalPlayer.SendDraftDescription(index2);
                    }
                    else
                    {
                        Utils.SendMessage(GetString("InvalidDraftSelection"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    break;
                case "/load":
                case "/loadpreset":
                    canceled = true;
                    string loadFileName = "template";
                    if (args.Length >= 2)
                        loadFileName = string.Join(" ", args[1..]);

                    string loadFile = OptionCopier.Load(fileName: loadFileName);
                    Utils.SendMessage(string.Format(GetString("PresetLoaded"), loadFile), PlayerControl.LocalPlayer.PlayerId);
                    break;
                case "/spam":
                    canceled = true;
                    ChatManager.SendQuickChatSpam();
                    ChatManager.SendPreviousMessagesToAll();
                    break;

                case "/fix" 
                or "/blackscreenfix" 
                or "/fixblackscreen":
                    FixCommand(PlayerControl.LocalPlayer, text, args);
                    break;

                case "afkexempt":
                    AFKExemptCommand(PlayerControl.LocalPlayer, text, args);
                    break;
                
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

            __instance.quickChatMenu.Clear();
            __instance.quickChatField.Clear();
        }
        return !canceled;
    }

    public static string FixRoleNameInput(string text)
    {
        text = text.Replace("着", "者").Trim().ToLower();
        return text;
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

        string nameWithoutId = Regex.Replace(name.Replace(" ", ""), @"^\d+", "");

        if (Options.CrossLanguageGetRole.GetBool())
        {
            foreach (var rl in CustomRolesHelper.AllRoles)
            {
                if (!CrossLangRoleNames.ContainsKey(rl))
                    continue;
                else
                {
                    if (!CrossLangRoleNames[rl].Contains(nameWithoutId))
                        continue;
                    else
                    {
                        role = rl;
                        return true;
                    }
                }
            }
        }
        else
        {
            foreach (var rl in CustomRolesHelper.AllRoles)
            {
                if (rl.IsVanilla()) continue;
                var roleName = GetString(rl.ToString()).ToLower().Trim().Replace(" ", "");
                if (nameWithoutId == roleName)
                {
                    role = rl;
                    return true;
                }
            }
        }
        return false;
    }
    public static CustomRoles ParseRole(string role)
    {
        role = FixRoleNameInput(role).ToLower().Trim().Replace(" ", string.Empty);
        var result = CustomRoles.NotAssigned;

        foreach (var rl in CustomRolesHelper.AllRoles)
        {
            if (rl.IsVanilla()) continue;

            if (Options.CrossLanguageGetRole.GetBool())
            {
                if (!CrossLangRoleNames.ContainsKey(rl))
                    continue;
                else
                {
                    if (!CrossLangRoleNames[rl].Contains(role))
                        continue;
                    else
                    {
                        result = rl;
                        break;
                    }
                }
            }
            else
            {
                var roleName = GetString(rl.ToString());
                if (role == roleName.ToLower().Trim().TrimStart('*').Replace(" ", string.Empty))
                {
                    result = rl;
                    break;
                }
            }
        }

        return result;
    }

    public static void SendRolesInfo(string role, byte playerId, bool isDev = false, bool isUp = false)
    {
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
                {
                    Utils.SendMessage(GetString("ModeDescribe.FFA"), playerId);
                    return;
                }
            case CustomGameMode.SpeedRun:
                {
                    Utils.SendMessage(GetString("ModeDescribe.SpeedRun"), playerId);
                    return;
                }
        }
        role = role.Trim().ToLower();
        if (role.StartsWith("/r")) _ = role.Replace("/r", string.Empty);
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

        var result = ParseRole(role);

        if (result == CustomRoles.NotAssigned)
        {
            Utils.SendMessage(GetString("Message.CanNotFindRoleThePlayerEnter"), playerId);
            return;
        }

        bool shouldDevAssign = isDev || isUp;

        if (CustomRolesHelper.IsAdditionRole(result) || result is CustomRoles.GM or CustomRoles.Mini || result.IsGhostRole() && !isDev
            || result.GetCount() < 1 || result.GetMode() == 0)
        {
            shouldDevAssign = false;
        }

        byte pid = playerId == 255 ? (byte)0 : playerId;

        if (isUp)
        {
            if (result.IsGhostRole() || !shouldDevAssign)
            {
                Utils.SendMessage(string.Format(GetString("Message.YTPlanSelectFailed"), Translator.GetActualRoleName(result)), playerId);
                return;
            }

            GhostRoleAssign.forceRole.Remove(pid);
            RoleAssign.SetRoles[pid] = result;

            Utils.SendMessage(string.Format(GetString("Message.YTPlanSelected"), Translator.GetActualRoleName(result)), playerId);
            return;
        }

        if (isDev && shouldDevAssign)
        {
            if (result.IsGhostRole() && !result.IsAdditionRole())
            {
                CustomRoles setrole = result.GetCustomRoleTeam() switch
                {
                    Custom_Team.Impostor => CustomRoles.ImpostorTOHE,
                    _ => CustomRoles.CrewmateTOHE

                };
                RoleAssign.SetRoles[pid] = setrole;
                GhostRoleAssign.forceRole[pid] = result;
            }
            else
            {
                GhostRoleAssign.forceRole.Remove(pid);
                RoleAssign.SetRoles[pid] = result;
            }
        }


        var Des = result.GetInfoLong();
        var title = "▲" + $"<color=#ffffff>" + result.GetRoleTitle() + "</color>\n";
        var Conf = new StringBuilder();
        string rlHex = Utils.GetRoleColorCode(result);
        if (Options.CustomRoleSpawnChances.ContainsKey(result))
        {
            Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[result], ref Conf);
            var cleared = Conf.ToString();
            var Setting = $"<color={rlHex}>{GetString(result.ToString())} {GetString("Settings:")}</color>\n";
            Conf.Clear().Append($"<color=#ffffff>" + $"<size={Csize}>" + Setting + cleared + "</size>" + "</color>");

        }
        // Show role info
        Utils.SendMessage(Des, playerId, title, noReplay: true);

        // Show role settings
        Utils.SendMessage("", playerId, Conf.ToString(), noReplay: true);
        return;
    }
    public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
    {
        canceled = false;
        if (!AmongUsClient.Instance.AmHost) return;

        if (Main.Daybreak) return;

        if (!Blackmailer.CheckBlackmaile(player)) ChatManager.SendMessage(player, text);

        if (text.StartsWith("\n")) text = text[1..];
        //if (!text.StartsWith("/")) return;
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
        if (player.GetRoleClass() is Exorcist ex && ex.CheckCommand(player, text)) { canceled = true; Logger.Info($"Is Exorcist command", "OnReceiveChat"); return; }
        if (player.GetRoleClass() is Dictator dt && dt.ExilePlayer(player, text)) { canceled = true; Logger.Info($"Is Dictator command", "OnReceiveChat"); return; }
        if (Ritualist.RitualistMsgCheck(player, text)) { canceled = true; Logger.Info($"Is Ritualist command", "OnReceiveChat"); return; }
        if (Summoner.SummonerCheckMsg(player, text)) { canceled = true; Logger.Info($"Is Summoner command", "OnReceiveChat"); return; }
        if (player.GetRoleClass() is Starspawn st && st.DaybreakMessage(player, text)) { canceled = true; Logger.Info($"Is Starspawn command", "OnReceiveChat"); return; }

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
        if (Exorcist.IsExorcismCurrentlyActive() && player.IsAlive() && !player.IsHost())
        {
            Logger.Info($"This player (id {player.PlayerId}) was Exorcised", "OnReceiveChat");
            Exorcist.ExorcisePlayer(player);
            canceled = true;
            return;
        }

        switch (args[0])
        {
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
            
            case "/up":
            case "/指定":
            case "/成为":
                _ = text[3..];
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

            case "/colour":
            case "/color":
            case "/cor":
            case "/цвет":
            case "/颜色":
            case "/更改颜色":
            case "/修改颜色":
            case "/换颜色":
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
                if (TagManager.ReadPermission(player.FriendCode) < 2 && (Options.ApplyModeratorList.GetValue() == 0 || !Utils.IsPlayerModerator(player.FriendCode))
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
                //canceled = true;
                var tagCanUse = TagManager.ReadPermission(player.FriendCode) >= 2;
                //checking if modlist on or not
                //checking if player is has necessary privellege or not
                if (!tagCanUse && !Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("midCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (!tagCanUse && Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("midCommandDisabled"), player.PlayerId);
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

            case "/warn":
            case "/aviso":
            case "/варн":
            case "/пред":
            case "/предупредить":
            case "/警告":
            case "/提醒":
                var tagCanWarn = TagManager.ReadPermission(player.FriendCode) >= 2;
                if (!tagCanWarn && Options.ApplyModeratorList.GetValue() == 0)
                {
                    Utils.SendMessage(GetString("WarnCommandDisabled"), player.PlayerId);
                    break;
                }
                if (!tagCanWarn && !Utils.IsPlayerModerator(player.FriendCode))
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
                if (Utils.IsPlayerModerator(warnedPlayer.FriendCode) || TagManager.ReadPermission(warnedPlayer.FriendCode) >= 2)
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
                    //Logger.Msg($"File exists, creating file at {modTagsFiles}/{player.FriendCode}.txt", "modcolor");
                    //Logger.Msg($"{subArgs}","modcolor");
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
                    //Logger.Msg($"File exists, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                    //Logger.Msg($"{subArgs}","modcolor");
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
                //Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);
                Utils.SendMessage(GetString("Message.TryFixName"), player.PlayerId);
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
                    int botChoice = rand.Next(100);
                    var coinSide = (botChoice < 50) ? GetString("Heads") : GetString("Tails");
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
                        str = GetString("Yes");
                        break;
                    case 1:
                        str = GetString("No");
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
                // string UpBox = player.FriendCode.GetDevUser().IsUp ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";
                string ColorBox = player.FriendCode.GetDevUser().ColorCmd ? "<#10e341><b>✓</b></color>" : "<#e31010><b>〤</b></color>";

                subArgs = text.Length == 3 ? string.Empty : text.Remove(0, 3);
                if (string.IsNullOrEmpty(subArgs))
                {
                    Utils.SendMessage((player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), player.PlayerId, player.GetRealName(clientData: true), player.GetClient().FriendCode, player.GetClient().GetHashedPuid(), player.FriendCode.GetDevUser().GetUserType(), Devbox, "", ColorBox)}", player.PlayerId);
                }
                else
                {
                    var tagCanMe = TagManager.ReadPermission(player.FriendCode) >= 2;
                    if ((Options.ApplyModeratorList.GetValue() == 0 || !Utils.IsPlayerModerator(player.FriendCode)) && !tagCanMe)
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
                                Utils.SendMessage($"{string.Format(GetString("Message.MeCommandTargetInfo"), targetplayer.PlayerId, targetplayer.GetRealName(clientData: true), targetplayer.GetClient().FriendCode, targetplayer.GetClient().GetHashedPuid(), targetplayer.FriendCode.GetDevUser().GetUserType())}", player.PlayerId);
                            }
                            else
                            {
                                Utils.SendMessage($"{(GetString("Message.MeCommandInvalidID"))}", player.PlayerId);
                            }
                        }
                        else
                        {
                            Utils.SendMessage($"{string.Format(GetString("Message.MeCommandInfo"), PlayerControl.LocalPlayer.PlayerId, PlayerControl.LocalPlayer.GetRealName(clientData: true), PlayerControl.LocalPlayer.GetClient().FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid(), PlayerControl.LocalPlayer.FriendCode.GetDevUser().GetUserType(), Devbox, "", ColorBox)}", player.PlayerId);
                        }
                    }
                    else
                    {
                        Utils.SendMessage($"{(GetString("Message.MeCommandInvalidID"))}", player.PlayerId);
                    }
                }
                break;

            case "/start":
            case "/开始":
            case "/старт":
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                    break;
                }
                var tagCanStart = TagManager.ReadPermission(player.FriendCode) >= 3;
                if (!tagCanStart && !Utils.IsPlayerModerator(player.FriendCode))
                {
                    Utils.SendMessage(GetString("StartCommandNoAccess"), player.PlayerId);
                    break;
                }
                if (!tagCanStart && (Options.ApplyModeratorList.GetValue() == 0 || Options.AllowStartCommand.GetBool() == false))
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
            case "/deck":
                if (!PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsDev && !Options.devEnableDraft && !player.FriendCode.GetDevUser().IsDev)
                {
                    break;
                }
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                    break;
                }

                player.SendDeckList();

                break;
            case "/draft":
                if (!PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsDev && !Options.devEnableDraft && !player.FriendCode.GetDevUser().IsDev)
                {
                    break;
                }
                if (!GameStates.IsLobby)
                {
                    Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"), player.PlayerId);
                    break;
                }

                var tagCanStartDraft = TagManager.ReadPermission(player.FriendCode) >= 3;
                if (args.Length < 2 || args[1] == "start")
                {
                    if (!tagCanStartDraft && !Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.GetDevUser().IsDev)
                    {
                        Utils.SendMessage(GetString("StartDraftNoAccess"), player.PlayerId);
                        break;
                    }

                    var startResult = DraftAssign.StartDraft();

                    if (startResult == DraftAssign.DraftCmdResult.NoCurrentDraft)
                    {
                        Utils.SendMessage(GetString("StartDraftWrongGameMode"), player.PlayerId);
                    }
                    else
                    {
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            Utils.SendMessage(string.Format(GetString("DraftPoolMessage"), pc.GetFormattedDraftPool()), pc.PlayerId);
                        }
                    }
                }
                else if (args[1] == "desc" || args[1] == "description")
                {
                    if (args.Length > 2) args[1] = args[2];
                    goto case "/dd";
                }
                else if (args[1] == "add")
                {
                    if (!tagCanStartDraft && !Utils.IsPlayerModerator(player.FriendCode))
                    {
                        Utils.SendMessage(GetString("StartDraftNoAccess"), player.PlayerId);
                        break;
                    }
                    var addResult = DraftAssign.DraftActive;

                    if (!addResult)
                    {
                        Utils.SendMessage(GetString("NoCurrentDraft"), player.PlayerId);
                    }
                    else
                    {
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            Utils.SendMessage(string.Format(GetString("DraftPoolMessage"), pc.GetFormattedDraftPool()), pc.PlayerId);
                        }
                    }
                }
                else if (args[1] == "reset")
                {
                    if (!tagCanStartDraft && !Utils.IsPlayerModerator(player.FriendCode))
                    {
                        Utils.SendMessage(GetString("StartDraftNoAccess"), player.PlayerId);
                        break;
                    }
                    DraftAssign.Reset();
                }
                else if (args[1] == "enable" && player.FriendCode.GetDevUser().IsDev)
                {
                    Options.devEnableDraft = true;
                    Options.DraftHeader.SetHidden(false);
                    Options.DraftMode.SetHidden(false);
                    foreach (var pc in Main.AllPlayerControls)
                        Utils.SendMessage($"Developer {player.GetRealName()} has enabled draft for you. (No leaks yet please)", pc.PlayerId);
                }
                else
                {
                    CustomRoles draftedRole;
                    DraftAssign.DraftCmdResult cmdResult;
                    if (int.TryParse(args[1], out int index))
                    {
                        (cmdResult, draftedRole) = player.DraftRole(index);
                        player.SendDraftDescription(index);
                    }
                    else
                    {
                        Utils.SendMessage(GetString("InvalidDraftSelection"), player.PlayerId);
                        break;
                    }

                    if (cmdResult == DraftAssign.DraftCmdResult.NoCurrentDraft)
                    {
                        Utils.SendMessage(GetString("NoCurrentDraft"), player.PlayerId);
                        break;
                    }
                    else if (cmdResult == DraftAssign.DraftCmdResult.DraftRemoved)
                    {
                        Utils.SendMessage(GetString("DraftSelectionCleared"), player.PlayerId);
                        break;
                    }
                    else
                    {
                        Utils.SendMessage(string.Format(GetString("DraftSelection"), draftedRole.ToColoredString()), player.PlayerId);
                        SendRolesInfo(draftedRole.ToString(), player.PlayerId, isDev: player.FriendCode.GetDevUser().DeBug);
                    }
                }
                break;
            case "/dd":
            case "/draftdescription":
                if (int.TryParse(args[1], out int index2))
                {
                    player.SendDraftDescription(index2);
                }
                else
                {
                    Utils.SendMessage(GetString("InvalidDraftSelection"), player.PlayerId);
                    break;
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

            case "/fix" 
            or "/blackscreenfix" 
            or "/fixblackscreen":
                FixCommand(player, text, args);
                break;

            case "afkexempt":
                AFKExemptCommand(player, text, args);
                break;

            default:
                if (SpamManager.CheckSpam(player, text)) return;
                break;
        }
    }

    private static void RequestCommandProcessingFromHost(string text, [CallerMemberName] string methodName = "Unknown", bool modCommand = false, bool adminCommand = false)
    {
        PlayerControl pc = PlayerControl.LocalPlayer;
        MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)CustomRPC.RequestCommandProcessing, SendOption.Reliable, AmongUsClient.Instance.HostId);
        w.Write(methodName);
        w.Write(pc.PlayerId);
        w.Write(text);
        w.Write(modCommand);
        w.Write(adminCommand);
        AmongUsClient.Instance.FinishRpcImmediately(w);
    }

    private static bool CheckArg(string lookupKey, string arg)
    {
        var values = GetString(lookupKey).Split("|").Select(x => x.Trim());
        return values.Contains(arg);
    }

#region Command Handlers
    
    private static void FixCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            if (!Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.GetDevUser().IsDev) return;
        }

        if (args.Length < 2 || !byte.TryParse(args[1], out byte id)) return;

        var pc = id.GetPlayer();
        if (pc == null) return;

        pc.FixBlackScreen();

        if (Main.AllPlayerControls.All(x => x.IsAlive()))
            Logger.SendInGame(GetString("FixBlackScreenWaitForDead"));
    }

    private static void AFKExemptCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            if (!Utils.IsPlayerModerator(player.FriendCode) && !player.FriendCode.GetDevUser().IsDev) return;
        }

        if (args.Length < 2 || !byte.TryParse(args[1], out byte afkId)) return;

        AFKDetector.ExemptedPlayers.Add(afkId);
        Utils.SendMessage("\n", player.PlayerId, string.Format(GetString("PlayerExemptedFromAFK"), afkId.GetPlayerName()));
    }

    private static void DumpCommand(PlayerControl player, string text, string[] args)
    {
        Utils.DumpLog();
    }

    private static void VersionCommand(PlayerControl player, string text, string[] args)
    {
        string versionText = Main.playerVersion.OrderBy(pair => pair.Key).Aggregate(string.Empty, (current, kvp) => current + $"{kvp.Key}: ({Main.AllPlayerNames[(byte)kvp.Key]}) {kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n");
        if (versionText != string.Empty && HudManager.InstanceExists) HudManager.Instance.Chat.AddChat(player, (player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + versionText);
    }

    private static void SaveCommand(PlayerControl player, string text, string[] args)
    {
        string saveFileName = "template";
        if (args.Length >= 2)
            saveFileName = string.Join(" ", args[1..]);

        string saveFile = OptionCopier.Save(fileName: saveFileName);
        Utils.SendMessage(string.Format(GetString("PresetSaved"), saveFile), PlayerControl.LocalPlayer.PlayerId);
    }

    private static void DocsCommand(PlayerControl player, string text, string[] args)
    {
        int roleId = 500;
        if (args.Length >= 2)
            roleId = int.Parse(args[1]);

        if (roleId != 500)
            ((CustomRoles)roleId).GenerateDocs();
    }

    private static void WinnerCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        if (Main.winnerNameList.Count == 0)
            Utils.SendMessage(GetString("NoInfoExists"));
        else Utils.SendMessage("Winner: " + string.Join(", ", Main.winnerNameList));
    }

    private static void LastResultCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        Utils.ShowKillLog(player.PlayerId);
        Utils.ShowLastRoles(player.PlayerId);
        Utils.ShowLastResult(player.PlayerId);
    }

    private static void GameResultCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        Utils.ShowLastResult(player.PlayerId);
    }

    private static void KillLogCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        Utils.ShowKillLog(player.PlayerId);
    }

    private static void RoleSummaryCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }
        Utils.ShowLastRoles(player.PlayerId);
    }

    private static void GhostInfoCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }
        Utils.SendMessage(GetString("Message.GhostRoleInfo"), PlayerControl.LocalPlayer.PlayerId);
    }

    private static void ApocInfoCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }
        Utils.SendMessage(GetString("Message.ApocalypseInfo"), PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Apocalypse), GetString("ApocalypseInfoTitle")));
    }

    private static void CovenInfoCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }
        Utils.SendMessage(GetString("Message.CovenInfo"), PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Coven), GetString("CovenInfoTitle")));
    }

    private static void RenameCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        if (args.Length < 2) return;

        string name = Regex.Replace(string.Join(' ', args[1..]), "<size=[^>]*>", string.Empty).Trim();

        if (name.RemoveHtmlTags().Length is > 15 or < 1)
            Utils.SendMessage(GetString("Message.AllowNameLength"), player.PlayerId);
        else
        {
            if (!Options.PlayerCanSetName.GetBool() && !Utils.IsPlayerVIP(player.FriendCode) && !player.FriendCode.GetDevUser().IsDev && !player.FriendCode.GetDevUser().NameCmd && !(TagManager.ReadPermission(player.FriendCode) >= 1))
            {
                Utils.SendMessage(GetString("Message.OnlyVIPCanUse"), player.PlayerId);
                return;
            }

            Main.AllPlayerNames[player.PlayerId] = name;
            player.RpcSetName(name);
        }
    }

    private static void HideNameCommand(PlayerControl player, string text, string[] args)
    {
        Main.HideName.Value = args.Length > 1 ? string.Join(' ', args[1..]) : Main.HideName.DefaultValue.ToString();

        GameStartManagerPatch.GameStartManagerStartPatch.HideName.text =
            ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                : $"<color={Main.ModColor}>{Main.HideName.Value}</color>";
    }

    private static void LevelCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        string subArgs = args.Length < 2 ? string.Empty : args[1];
        Utils.SendMessage(string.Format(GetString("Message.SetLevel"), subArgs), player.PlayerId);
        _ = int.TryParse(subArgs, out int input);

        if (input is < 1 or > 999)
        {
            Utils.SendMessage(GetString("Message.AllowLevelRange"), player.PlayerId);
            return;
        }

        var number = Convert.ToUInt32(input);
        player.RpcSetLevel(number - 1);
    }

    private static void NowCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        string subArgs = args.Length < 2 ? string.Empty : args[1].ToLower();

        switch (subArgs)
        {
            case var _ when CheckArg("CommandArgs.Now.Roles", subArgs): // ["r", "role"]
                Utils.ShowActiveRoles(player.PlayerId);
                break;
            case var _ when CheckArg("CommandArgs.Now.All", subArgs): // ["a", "all"]
                Utils.ShowAllActiveSettings(player.PlayerId);
                break;
            default:
                Utils.ShowActiveSettings(player.PlayerId);
                break;
        }
    }

    private static void DisconnectCommand(PlayerControl player, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? string.Empty : args[1];
        switch (subArgs)
        {
            case var _ when CheckArg("CommandArgs.Disconnect.Crew", subArgs): // ["crew", "tripulante", "船员"]
                GameManager.Instance.enabled = false;
                Utils.NotifyGameEnding();
                GameManager.Instance.RpcEndGame(GameOverReason.CrewmateDisconnect, false);
                break;
            
            case var _ when CheckArg("CommandArgs.Disconnect.Imp", subArgs): // ["imp", "impostor", "内鬼", "伪装者"]
                GameManager.Instance.enabled = false;
                Utils.NotifyGameEnding();
                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                break;

            default:
                if (!HudManager.InstanceExists) break;
                    HudManager.Instance.Chat.AddChat(player, GetString("CommandArgs.Disconnect.Team")); // ["crew | imp"]
                break;
        }
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
    }

    private static void RCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        string subArgs = args.Length > 1 ? string.Join(' ', args[1..]) : string.Empty;
        byte to = player.AmOwner && Input.GetKeyDown(KeyCode.LeftShift) ? byte.MaxValue : player.PlayerId;
        SendRolesInfo(subArgs, to);
    }

    private static void FactionsCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        var impCount = $"{GetString("NumberOfImpostors")}: {GameOptionsManager.Instance.GameHostOptions.NumImpostors}";
        var nnkCount = $"{GetString("NonNeutralKillingRolesMinPlayer")}: {Options.NonNeutralKillingRolesMinPlayer.GetInt()}\n{GetString("NonNeutralKillingRolesMaxPlayer")}: {Options.NonNeutralKillingRolesMaxPlayer.GetInt()}";
        var nkCount = $"{GetString("NeutralKillingRolesMinPlayer")}: {Options.NeutralKillingRolesMinPlayer.GetInt()}\n{GetString("NeutralKillingRolesMaxPlayer")}: {Options.NeutralKillingRolesMaxPlayer.GetInt()}";
        var apocCount = $"{GetString("NeutralApocalypseRolesMinPlayer")}: {Options.NeutralApocalypseRolesMinPlayer.GetInt()}\n{GetString("NeutralApocalypseRolesMaxPlayer")}: {Options.NeutralApocalypseRolesMaxPlayer.GetInt()}";
        var covCount = $"{GetString("CovenRolesMinPlayer")}: {Options.CovenRolesMinPlayer.GetInt()}\n{GetString("CovenRolesMaxPlayer")}: {Options.CovenRolesMaxPlayer.GetInt()}";
        var addonCount = $"{GetString("NoLimitAddonsNumMax")}: {Options.NoLimitAddonsNumMax.GetInt()}";

        Utils.SendMessage($"{impCount}\n{nnkCount}\n{nkCount}\n{apocCount}\n{covCount}\n{addonCount}", player.PlayerId, $"<color={Main.ModColor}>{GetString("FactionSettingsTitle")}</color>");
    }

    private static void MyRoleCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        var role = player.GetCustomRole();
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

    private static void HelpCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        Utils.ShowHelp(player.PlayerId);
    }

    private static void SetRoleCommand(PlayerControl player, string text, string[] args)
    {
        string subArgs = string.Join(' ', args[1..]);

        if (!GuessManager.MsgToPlayerAndRole(subArgs, out byte resultId, out CustomRoles roleToSet, out _))
        {
            Utils.SendMessage(GetString("InvalidArguments"), player.PlayerId);
            return;
        }

        if (resultId != 0 && !player.FriendCode.GetDevUser().IsUp && !GameStates.IsLocalGame)
        {
            Utils.SendMessage(GetString("Message.NoPermissionSetRoleOthers"), player.PlayerId);
            return;
        }

        PlayerControl targetPc = Utils.GetPlayerById(resultId);
        if (targetPc == null) return;

        if (roleToSet.IsAdditionRole())
        {
            if (!AddonAssign.SetAddOns.ContainsKey(resultId)) AddonAssign.SetAddOns[resultId] = [];

            if (AddonAssign.SetAddOns[resultId].Contains(roleToSet))
                AddonAssign.SetAddOns[resultId].Remove(roleToSet);
            else
                AddonAssign.SetAddOns[resultId].Add(roleToSet);
        }
        else
            RoleAssign.SetRoles[targetPc.PlayerId] = roleToSet;

        Utils.SendMessage("\n", player.PlayerId, string.Format(GetString("RoleSelected"), resultId.GetPlayerName(), roleToSet.ToColoredString()));
    }

    private static void TPOutCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        if (!GameStates.IsLobby) return;

        if (!!Options.PlayerCanUseTP.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
            return;
        }

        player.RpcTeleport(new Vector2(0.1f, 3.8f));
    }

    private static void TPInCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        if (!Options.PlayerCanUseTP.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
            return;
        }
        PlayerControl.LocalPlayer.RpcTeleport(new Vector2(-0.2f, 1.3f));
    }
    private static void KCountCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        if (!Options.EnableKillerLeftCommand.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), PlayerControl.LocalPlayer.PlayerId);
            return;
        }

        var sub = new StringBuilder();

        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.Standard:
                var allAlivePlayers = Main.AllAlivePlayerControls;
                int impnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Impostor) && !Main.PlayerStates[pc.PlayerId].IsRandomizer && !pc.Is(CustomRoles.Narc));
                int madnum = allAlivePlayers.Count(pc => ((pc.GetCustomRole().IsMadmate() && !pc.Is(CustomRoles.Narc)) || pc.Is(CustomRoles.Madmate)) && !Main.PlayerStates[pc.PlayerId].IsRandomizer);
                int neutralnum = allAlivePlayers.Count(pc => pc.GetCustomRole().IsNK());
                int apocnum = allAlivePlayers.Count(pc => (pc.IsNeutralApocalypse() || pc.IsTransformedNeutralApocalypse()) && !Main.PlayerStates[pc.PlayerId].IsRandomizer);
                int covnum = allAlivePlayers.Count(pc => pc.Is(Custom_Team.Coven) && !Main.PlayerStates[pc.PlayerId].IsRandomizer);

                sub.Append(string.Format(GetString("Remaining.ImpostorCount"), impnum));

                if (Options.ShowMadmatesInLeftCommand.GetBool())
                    sub.Append(string.Format("\n\r" + GetString("Remaining.MadmateCount"), madnum));

                if (Options.ShowApocalypseInLeftCommand.GetBool())
                    sub.Append(string.Format("\n\r" + GetString("Remaining.ApocalypseCount"), apocnum));

                if (Options.ShowCovenInLeftCommand.GetBool())
                    sub.Append(string.Format("\n\r" + GetString("Remaining.CovenCount"), covnum));

                sub.Append(string.Format("\n\r" + GetString("Remaining.NeutralCount"), neutralnum));
                break;

            case CustomGameMode.FFA:
                FFAManager.AppendFFAKcount(sub);
                break;

            case CustomGameMode.SpeedRun:
                SpeedRun.AppendSpeedRunKcount(sub);
                break;
        }

        Utils.SendMessage(sub.ToString(), PlayerControl.LocalPlayer.PlayerId);
    }

    private static void TemplateCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        if (player.AmOwner)
        {
            if (args.Length > 1)
                TemplateManager.SendTemplate(args[1]);
            else
                HudManager.Instance.Chat.AddChat(player, (player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{GetString("ForExample")}:\n{args[0]} test");
        }
        else
        {
            if (args.Length > 1)
                TemplateManager.SendTemplate(args[1], player.PlayerId);
            else
                Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
        }
    }

    private static void MessageWaitCommand(PlayerControl player, string text, string[] args)
    {
        if (args.Length > 1 && int.TryParse(args[1], out int sec))
        {
            Main.MessageWait.Value = sec;
            Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
        }
        else
            Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
    }

    private static void DeathCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        if (!GameStates.IsInGame) return;

        if (Main.DeadPassedMeetingPlayers.Contains(player.PlayerId) && Utils.IsRevivingRoleAlive()) return;

        PlayerControl target = args.Length < 2 || !byte.TryParse(args[1], out byte targetId) ? player : targetId.GetPlayer();
        if (target == null) return;

        if (target.IsAlive())
        {
            Utils.SendMessage(string.Format(GetString("DeathCmd.NotDead"), target.GetRealName(), target.GetCustomRole().ToColoredString()), player.PlayerId);
        }
        else if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Vote)
        {
            Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + target.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(target.GetCustomRole())}>{Utils.GetRoleName(target.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Ejected"), player.PlayerId);
        }
        else if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Shrouded)
        {
            Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + target.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(target.GetCustomRole())}>{Utils.GetRoleName(target.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Shrouded"), player.PlayerId);
        }
        else if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.FollowingSuicide)
        {
            Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + target.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(target.GetCustomRole())}>{Utils.GetRoleName(target.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.Lovers"), player.PlayerId);
        }
        else
        {
            var killer = target.GetRealKiller(out var MurderRole);
            string killerName = killer == null ? "N/A" : killer.GetRealName(clientData: true);
            string killerRole = killer == null ? "N/A" : Utils.GetRoleName(MurderRole);
            Utils.SendMessage(GetString("DeathCmd.YourName") + "<b>" + target.GetRealName() + "</b>" + "\n\r" + GetString("DeathCmd.YourRole") + "<b>" + $"<color={Utils.GetRoleColorCode(target.GetCustomRole())}>{Utils.GetRoleName(target.GetCustomRole())}</color>" + "</b>" + "\n\r" + GetString("DeathCmd.DeathReason") + "<b>" + Utils.GetVitalText(target.PlayerId) + "</b>" + "\n\r" + "</b>" + "\n\r" + GetString("DeathCmd.KillerName") + "<b>" + killerName + "</b>" + "\n\r" + GetString("DeathCmd.KillerRole") + "<b>" + $"<color={Utils.GetRoleColorCode(killer.GetCustomRole())}>{killerRole}</color>" + "</b>", player.PlayerId);
        }
    }

    private static void SayCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, modCommand: true);
            return;
        }

        if (args.Length > 1)
        {
            if (player.IsHost()) 
            {
                Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")} ~ <size=1.25>{PlayerControl.LocalPlayer.GetRealName(clientData: true)}</size></color>");
            }
            else if (player.FriendCode.GetDevUser().IsDev)
            {
                Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color={Main.ModColor}>{GetString("MessageFromDev")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
            }
            else if (player.FriendCode.IsDevUser() && !dbConnect.IsBooster(player.FriendCode))
            {
                Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#4bc9b0>{GetString("MessageFromSponsor")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
            }
            else if (Utils.IsPlayerModerator(player.FriendCode) || TagManager.CanUseSayCommand(player.FriendCode))
            {
                if (!TagManager.CanUseSayCommand(player.FriendCode) && (Options.ApplyModeratorList.GetValue() == 0 || Options.AllowSayCommand.GetBool() == false))
                {
                    Utils.SendMessage(GetString("SayCommandDisabled"), player.PlayerId);
                }
                else
                {
                    var modTitle = (Utils.IsPlayerModerator(player.FriendCode) || TagManager.ReadPermission(player.FriendCode) >= 2) ? $"<color=#8bbee0>{GetString("MessageFromModerator")}" : $"<color=#ffff00>{GetString("MessageFromVIP")}";
                    Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"{modTitle} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
                    
                    string modLogname3 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n4) ? n4 : "";

                    string moderatorFriendCode3 = player.FriendCode.ToString();
                    string logMessage3 = $"[{DateTime.Now}] {moderatorFriendCode3},{modLogname3} used /s: {args.Skip(1).Join(delimiter: " ")}";
                    File.AppendAllText(modLogFiles, logMessage3 + Environment.NewLine);

                }
            }
        }
    }

    private static void VoteCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text);
            return;
        }

        var subArgs = args.Length != 2 ? "" : args[1];
        if (subArgs == "" || !int.TryParse(subArgs, out int arg))
            return;
        var plr = Utils.GetPlayerById(arg);

        if (GameStates.IsLobby)
        {
            Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
            return;
        }


        if (!Options.EnableVoteCommand.GetBool())
        {
            Utils.SendMessage(GetString("VoteDisabled"), player.PlayerId);
            return;
        }

        if (arg != 253) // skip
        {
            if (plr == null || !plr.IsAlive())
            {
                Utils.SendMessage(GetString("VoteDead"), player.PlayerId);
                return;
            }
        }
        if (!player.IsAlive())
        {
            Utils.SendMessage(GetString("CannotVoteWhenDead"), player.PlayerId);
            return;
        }
        if (GameStates.IsMeeting)
        {
            player.RpcCastVote((byte)arg);
        }
    }

    private static void BanKickCommand(PlayerControl player, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, modCommand: true);
            return;
        }

        bool isBan = GetString("Command.Ban.Ban").Split("|").Contains(args[0][1..]); // ["ban", "banir", "бан", "забанить", "封禁"]

        var permLvl = TagManager.ReadPermission(player.FriendCode);

        var tagHasPerms = permLvl >= (isBan ? 5 : 4);

        // Check if the Kick command is enabled in the settings
        if (!Options.ApplyModeratorList.GetBool() && !player.IsHost() && !tagHasPerms)
        {
            Utils.SendMessage(GetString(isBan ? "BanCommandDisabled" : "KickCommandDisabled"), player.PlayerId);
            return;
        }

        // Check if the Player has the necessary privileges to use the command
        if (!Utils.IsPlayerModerator(player.FriendCode) && !player.IsHost() && !tagHasPerms)
        {
            Utils.SendMessage(GetString(isBan ? "BanCommandNoAccess" : "KickCommandNoAccess"), player.PlayerId);
            return;
        }

        var subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte kickPlayerId))
        {
            Utils.SendMessage(GetString(isBan ? "BanCommandInvalidID" : "KickCommandInvalidID"), player.PlayerId);
            return;
        }

        if (kickPlayerId.IsHost())
        {
            Utils.SendMessage(GetString(isBan ? "BanCommandBanHost" : "KickCommandKickHost"), player.PlayerId);
            return;
        }

        var kickedPlayer = Utils.GetPlayerById(kickPlayerId);
        if (kickedPlayer == null)
        {
            Utils.SendMessage(GetString(isBan ? "BanCommandInvalidID" : "KickCommandInvalidID"), player.PlayerId);
            return;
        }

        // Prevent moderators from kicking other moderators
        if (Utils.IsPlayerModerator(kickedPlayer.FriendCode) || TagManager.ReadPermission(kickedPlayer.FriendCode) >= (isBan ? 5 : 4))
        {
            Utils.SendMessage(GetString(isBan ? "BanCommandBanMod" : "KickCommandKickMod"), player.PlayerId);
            return;
        }

        // Kick the specified player
        AmongUsClient.Instance.KickPlayer(kickedPlayer.GetClientId(), isBan);
        string kickedPlayerName = kickedPlayer.GetRealName();
        string textToSend = $"{kickedPlayerName} {GetString("KickCommandKicked")} {player.name} \n";
        string kickReason = "Not specified";
        if (args.Length > 2)
        {
            kickReason = string.Join(" ", args[2..]);
            textToSend += "Reason : " + kickReason + "\n";
        }
        else
            textToSend += "Reason : Not specified\n";
        
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
    }

#endregion
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
            var chatBubble = __instance.chatBubblePool.Prefab.CastFast<ChatBubble>();
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
        //Logger.Info($"player is null? {player == null}", "ChatUpdatePatch");
        if (player == null) return;

        (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
        //Logger.Info($"MessagesToSend - sendTo: {sendTo} - title: {title}", "ChatUpdatePatch");

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
                // green beans color id is -1
            }
            // It is impossible to get null player here unless it quits
        }
        Main.MessagesToSend.RemoveAt(0);

        int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
        var name = player.Data.PlayerName;

        //__instance.freeChatField.textArea.characterLimit = 999;

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
        __instance.charCountText.SetText(length <= 0 ? GetString("ThankYouForUsingTOHE") : $"{length}/{__instance.textArea.characterLimit}");
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
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        */

        var message = new RpcSendChatMessage(__instance.NetId, chatText);
        RpcUtils.LateBroadcastReliableMessage(message);
        __result = true;
        return false;
    }
}
