using AmongUs.InnerNet.GameDataMessages;
using Assets.CoreScripts;
using Hazel;
using System;
using System.Collections;
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


namespace TOHE.Patches;

// Credit: EHR
internal class Command(string commandKey, string arguments, string description, Command.UsageLevels usageLevel, Command.UsageTimes usageTime, Action<PlayerControl, string, string, string[]> action, bool isCanceled, bool alwaysHidden, string[] argsDescriptions = null, CustomRoles[] requiredRole = null)
{
    public enum UsageLevels
    {
        Everyone,
        Modded,
        Host,
        HostOrVIP,
        HostOrModerator,
        Developer,
        HostOrExePerm,
        HostOrEndPerm,
        HostOrSayPerm,
        HostOrBanPerm,
        HostOrWarnPerm,
        HostOrStartPerm,
        RoleSpecific,
        MiniGames,
        Debug
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

    public static Dictionary<string, Command> AllCommands = [];
    public static HashSet<string> AllAliases = [];

    public string CommandKey => commandKey;
    public HashSet<string> CommandForms;
    public string Arguments => arguments;
    public string Description => description;
    public string[] ArgsDescriptions => argsDescriptions ?? [];
    public UsageLevels UsageLevel => usageLevel;
    public UsageTimes UsageTime => usageTime;
    public Action<PlayerControl, string, string, string[]> Action => action;
    public bool IsCanceled => isCanceled;
    public bool AlwaysHidden => alwaysHidden;
    private CustomRoles[] RequiredRole => requiredRole;

    public bool IsThisCommand(string text)
    {
        if (!text.StartsWith('/')) return false;

        text = text.ToLower().Trim().TrimStart('/');
        return CommandForms.Any(text.Split(' ')[0].Equals);
    }

    public bool CanUseCommand(PlayerControl pc, bool checkTime = true, bool sendErrorMessage = false)
    {
        if (UsageLevel == UsageLevels.Everyone && UsageTime == UsageTimes.Always) return true;

        if (UsageLevel == UsageLevels.RoleSpecific && Main.Daybreak)
        {
            Utils.SendMessage("\n", pc.PlayerId, GetString($"Daybreak.BlockCommands"));
            return false;
        }

        // if (Lovers.PrivateChat.GetBool() && GameStates.IsInTask && pc.IsAlive()) return false;

        var permLvl = TagManager.ReadPermission(pc.FriendCode);

        switch (UsageLevel)
        {
            case UsageLevels.Host when !pc.IsHost():

            case UsageLevels.Developer when !pc.FriendCode.GetDevUser().IsDev:

            case UsageLevels.Modded when !pc.IsModded():

            case UsageLevels.HostOrVIP when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerVIP(pc.FriendCode) 
                && !Utils.IsPlayerModerator(pc.FriendCode) && !pc.FriendCode.GetDevUser().IsDev:

            case UsageLevels.HostOrModerator when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(pc.FriendCode) 
                && !pc.FriendCode.GetDevUser().IsDev:

            case UsageLevels.HostOrExePerm when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(pc.FriendCode) 
                && !pc.FriendCode.GetDevUser().IsDev && !TagManager.CanUseExecuteCommand(pc.FriendCode):

            case UsageLevels.HostOrEndPerm when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(pc.FriendCode) 
                && !pc.FriendCode.GetDevUser().IsDev && !TagManager.CanUseEndCommand(pc.FriendCode):

            case UsageLevels.HostOrSayPerm when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(pc.FriendCode) 
                && !pc.FriendCode.GetDevUser().IsDev && !TagManager.CanUseSayCommand(pc.FriendCode):

            case UsageLevels.HostOrWarnPerm when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(pc.FriendCode) 
                && !pc.FriendCode.GetDevUser().IsDev && permLvl < 2:

            case UsageLevels.HostOrStartPerm when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(pc.FriendCode) 
                && !pc.FriendCode.GetDevUser().IsDev && permLvl < 3:

            case UsageLevels.HostOrBanPerm when !pc.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(pc.FriendCode) 
                && !pc.FriendCode.GetDevUser().IsDev && permLvl < 4:

            case UsageLevels.RoleSpecific when RequiredRole == null || !RequiredRole.Contains(pc.GetCustomRole()) && !RequiredRole.Any(x => pc.GetCustomSubRoles().Contains(x)):

            case UsageLevels.MiniGames when !Options.CanPlayMiniGames.GetBool():

            case UsageLevels.Debug when !DebugModeManager.AmDebugger || GameStates.IsOnlineGame && !pc.FriendCode.GetDevUser().DeBug:

                if (sendErrorMessage) Utils.SendMessage("\n", pc.PlayerId, GetString($"Commands.NoAccess.Level.{UsageLevel}"));
                return false;
        }

        if (!checkTime) return true;

        switch (UsageTime)
        {
            case UsageTimes.InLobby when !GameStates.IsLobby:
            case UsageTimes.InGame when !GameStates.InGame:
            case UsageTimes.InMeeting when !GameStates.IsMeeting || GameStates.IsExilling:
            case UsageTimes.AfterDeath when pc.IsAlive():
            case UsageTimes.AfterDeathOrLobby when pc.IsAlive() && !GameStates.IsLobby:
                if (sendErrorMessage) Utils.SendMessage("\n", pc.PlayerId, GetString($"Commands.NoAccess.Time.{UsageTime}"));
                return false;
        }

        return true;
    }

    private HashSet<string> GetCrossLangAliases()
    {
        CommandForms = [];
        foreach (var lang in EnumHelper.GetAllValues<SupportedLangs>())
        {

            var names = GetString(CommandKey, lang).Split("|");
            // Logger.Info($"Found aliases: {string.Join(", ", names)} for {CommandKey} in {lang}", "Command.GetCrossLangAliases");
            foreach (var n in names)
            {
                var name = n.ToLower().Trim();
                if (!CommandForms.Contains(name))
                {
                    CommandForms.Add(name);
                }
            }
        }
        // Logger.Info($"Found {CommandForms.Count} aliases for {commandKey}", "Command.GetCrossLangAliases");
        return CommandForms;
    }

    public void ProcessTranslations()
    {
        if (!commandKey.StartsWith("Command."))
            commandKey = "Command." + commandKey;

        if (description.StartsWith("CommandDescription."))
            description = GetString(description);
        
        if (argsDescriptions != null)
            for (int i = 0; i < argsDescriptions.Length; i++)
            {
                if (argsDescriptions[i].StartsWith("CommandArgs."))
                    argsDescriptions[i] = GetString(argsDescriptions[i]);
            }
    }

    public string ToHelpString()
    {
        var names = GetString(CommandKey).Split("|");
        return $"\n  ○ /{names[0]} {Description}";
    }

    public static Command Create(string commandKey, string arguments, string description, UsageLevels usageLevel, UsageTimes usageTime, Action<PlayerControl, string, string, string[]> action, bool isCanceled, bool alwaysHidden, string[] argsDescriptions = null, CustomRoles[] requiredRole = null)
    {
        // Logger.Info($"Creating Command {commandKey}", "Command.Create");
        var command = new Command(commandKey, arguments, description, usageLevel, usageTime, action, isCanceled, alwaysHidden, argsDescriptions, requiredRole);
        // Logger.Info($"Processing Command {commandKey}", "Command.Create");
        command.ProcessTranslations();
        // Logger.Info($"Translating Command {commandKey}", "Command.Create");
        command.GetCrossLangAliases();
        if (!AllCommands.TryAdd(commandKey, command))
        {
            throw new Exception($"Command with key {commandKey} already exists.");
        }
        // Logger.Info($"Created Command {commandKey}", "Command.Create");
        return command;
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
    
    private static bool WaitingToSend;

    public static void LoadCommands()
    {
        HashSet<Command> _ =
        [
            Command.Create("Command.Dump", "", GetString("CommandDescription.Dump"), Command.UsageLevels.Modded, Command.UsageTimes.Always, DumpCommand, false, false), // ["dump", "дамп", "лог", "导出日志", "日志", "导出"]
            Command.Create("Command.Version", "", GetString("CommandDescription.Version"), Command.UsageLevels.Modded, Command.UsageTimes.Always, VersionCommand, false, false), // ["v", "version", "в", "версия", "检查版本", "versão", "版本"]
            Command.Create("Command.Save", "[filePath]", GetString("CommandDescription.Save"), Command.UsageLevels.Modded, Command.UsageTimes.InLobby, SaveCommand, false, false, [GetString("CommandArgs.Save.Path")]), // ["save", "savepreset"]
            Command.Create("Command.Load", "[filePath]", GetString("CommandDescription.Load"), Command.UsageLevels.Host, Command.UsageTimes.InLobby, LoadCommand, false, false, [GetString("CommandArgs.Load.Path")]), // ["load", "loadpreset"]
            Command.Create("Command.Docs", "{role}", GetString("CommandDescription.Docs"), Command.UsageLevels.Developer, Command.UsageTimes.InLobby, DocsCommand, false, false, [GetString("CommandArgs.Docs.Role")]), // ["docs"]
            Command.Create("Command.Winner", "", GetString("CommandDescription.Winner"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, WinnerCommand, true, false), // ["win", "winner", "победители", "获胜者", "vencedor", "胜利", "获胜", "赢", "获胜的人", "赢家"]
            Command.Create("Command.LastResult", "", GetString("CommandDescription.LastResult"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, LastResultCommand, true, false), // ["l", "lastresult", "л", "对局职业信息", "resultados", "ultimoresultado", "fimdejogo", "上局信息", "信息", "情况"]
            Command.Create("Command.GameResult", "", GetString("CommandDescription.GameResult"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, GameResultCommand, true, false), // ["gr", "gameresults", "resultados", "对局结果", "上局结果", "结果"]
            Command.Create("Command.KillLog", "", GetString("CommandDescription.KillLog"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, KillLogCommand, true, false), // ["kh", "killlog", "击杀日志", "击杀情况"]
            Command.Create("Command.RoleSummary", "", GetString("CommandDescription.RoleSummary"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, RoleSummaryCommand, true, false), // ["rs", "sum", "rolesummary", "sumario", "sumário", "summary", "результат", "上局职业", "职业信息", "对局职业"]
            Command.Create("Command.GhostInfo", "", GetString("CommandDescription.GhostInfo"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, GhostInfoCommand, true, false), // ["ghostinfo", "幽灵职业介绍", "鬼魂职业介绍", "幽灵职业", "鬼魂职业"]
            Command.Create("Command.ApocInfo", "", GetString("CommandDescription.ApocInfo"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, ApocInfoCommand, true, false), // ["apocinfo", "apocalypseinfo", "末日中立职业介绍", "末日中立介绍", "末日类中立职业介绍", "末日类中立介绍"]
            Command.Create("Command.CovenInfo", "", GetString("CommandDescription.CovenInfo"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, CovenInfoCommand, true, false), // ["coveninfo", "covinfo", "巫师阵营职业介绍", "巫师阵营介绍", "巫师介绍"]
            Command.Create("Command.Rename", "{name}", GetString("CommandDescription.Rename"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, RenameCommand, true, false, [GetString("CommandArgs.Rename.Name")]), // ["rn", "rename", "name", "рн", "ренейм", "переименовать", "修改名称", "renomear", "重命名", "命名为"] 
            Command.Create("Command.HideName", "", GetString("CommandDescription.HideName"), Command.UsageLevels.Host, Command.UsageTimes.InLobby, HideNameCommand, true, false), // ["hn", "hidename", "хн", "спрник", "隐藏姓名", "semnome", "escondernome", "隐藏名字", "藏名"]
            Command.Create("Command.Level", "{level}", GetString("CommandDescription.Level"), Command.UsageLevels.Host, Command.UsageTimes.InLobby, LevelCommand, true, false, [GetString("CommandArgs.Level.Level")]), // ["level", "лвл", "уровень", "修改等级", "nível", "nivel", "等级", "等级设置为"] 
            Command.Create("Command.Now", "", GetString("CommandDescription.Now"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, NowCommand, true, false), // ["n", "now", "н", "当前设置", "atual", "设置", "系统设置", "模组设置"] 
            Command.Create("Command.Disconnect", "{team}", GetString("CommandDescription.Disconnect"), Command.UsageLevels.Host, Command.UsageTimes.InGame, DisconnectCommand, true, false, [GetString("CommandArgs.Disconnect.Team")]), // ["dis", "disconnect", "дис", "断连", "desconectar", "断连"]
            Command.Create("Command.R", "[role]", GetString("CommandDescription.R"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, RCommand, true, false, [GetString("CommandArgs.R.Role")]), // ["r", "р", "função", "role", "роль"] 
            Command.Create("Command.Factions", "", GetString("CommandDescription.Factions"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, FactionsCommand, true, false), // ["f", "factions", "faction"]
            Command.Create("Command.MyRole", "", GetString("CommandDescription.MyRole"), Command.UsageLevels.Everyone, Command.UsageTimes.InGame, MyRoleCommand, true, false), // ["m", "myrole", "м", "мояроль", "我的职业", "minhafunção", "м", "身份", "我", "我的身份"]
            Command.Create("Command.Help", "", GetString("CommandDescription.Help"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, HelpCommand, true, false), // ["h", "help", "хэлп", "хелп", "помощь", "帮助", "ajuda", "教程"] 
            Command.Create("Command.SetRole", "{id} {role}", GetString("CommandDescription.SetRole"), Command.UsageLevels.Host, Command.UsageTimes.InLobby, SetRoleCommand, true, false, [GetString("CommandArgs.SetRole.Id"), GetString("CommandArgs.SetRole.Role")]), // ["setrole", "setaddon", "сетроль", "预设职业", "definir-função"]
            Command.Create("Command.Fix", "{id}", "CommandDescription.Fix", Command.UsageLevels.HostOrModerator, Command.UsageTimes.InGame, FixCommand, true, false, ["CommandArgs.Fix.Id"]), // ["fix|blackscreenfix|fixblackscreen|фикс|исправить"]
            Command.Create("Command.AFKExempt", "{id}", GetString("CommandDescription.AFKExempt"), Command.UsageLevels.HostOrModerator, Command.UsageTimes.Always, AFKExemptCommand, true, false, [GetString("CommandArgs.AFKExempt.Id")]), // ["afkexempt", "освафк", "афкосв", "挂机检测器不会检测", "afk-isentar"]
            Command.Create("Command.TPOut", "", GetString("CommandDescription.TPOut"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, TPOutCommand, true, false), // ["tpout", "тпаут", "传送出", "传出"]
            Command.Create("Command.TPIn", "", GetString("CommandDescription.TPIn"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, TPInCommand, true, false), // ["tpin", "тпин", "传送进", "传进"]
            Command.Create("Command.KCount", "", GetString("CommandDescription.KCount"), Command.UsageLevels.Everyone, Command.UsageTimes.InGame, KCountCommand, true, false), // ["gamestate", "gstate", "gs", "kcount", "kc", "кубийц", "гс", "статигры", "对局状态", "estadojogo", "status", "количество", "убийцы", "存活阵营", "阵营", "存货阵营信息", "阵营信息"] 
            Command.Create("Command.Template", "{tag}", GetString("CommandDescription.Template"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, TemplateCommand, true, false, [GetString("CommandArgs.Template.Tag")]), // ["t", "template", "т", "темплейт", "模板", "шаблон", "пример", "模板信息"] 
            Command.Create("Command.MessageWait", "{duration}", GetString("CommandDescription.MessageWait"), Command.UsageLevels.Host, Command.UsageTimes.Always, MessageWaitCommand, true, false, [GetString("CommandArgs.MessageWait.Duration")]), // ["mw", "messagewait", "мв", "медленныйрежим", "消息冷却", "espera-mensagens", "消息等待时间"]
            Command.Create("Command.Death", "[id]", GetString("CommandDescription.Death"), Command.UsageLevels.Everyone, Command.UsageTimes.AfterDeath, DeathCommand, true, false, [GetString("CommandArgs.Death.Id")]), // ["death", "d", "д", "смерть", "死亡原因", "abate", "morto", "умер", "причина", "死亡"]
            Command.Create("Command.Say", "{message}", GetString("CommandDescription.Say"), Command.UsageLevels.HostOrSayPerm, Command.UsageTimes.Always, SayCommand, true, false, [GetString("CommandArgs.Say.Message")]), // ["say", "s", "сказать", "с", "说", "falar", "dizer"]
            Command.Create("Command.Vote", "{id}", GetString("CommandDescription.Vote"), Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, VoteCommand, true, true, [GetString("CommandArgs.Vote.Id")]),// ["vote", "голос", "投票给", "votar", "投票", "票"]
            Command.Create("Command.Ban", "{id} [reason]", GetString("CommandDescription.Ban"), Command.UsageLevels.HostOrBanPerm, Command.UsageTimes.Always, BanKickCommand, true, false, [GetString("CommandArgs.Ban.Id"), GetString("CommandArgs.Ban.Reason")]), // ["ban", "kick", "бан", "кик", "забанить", "кикнуть", "封禁", "踢出", "banir", "expulsar", "выгнать", "踢"] 
            Command.Create("Command.Warn", "{id} [reason]", GetString("CommandDescription.Warn"), Command.UsageLevels.HostOrWarnPerm, Command.UsageTimes.Always, WarnCommand, true, false, [GetString("CommandArgs.Warn.Id"), GetString("CommandArgs.Warn.Reason")]), // ["warn", "aviso", "варн", "пред", "предупредить", "警告", "提醒"]
            Command.Create("Command.Exe", "{id}", GetString("CommandDescription.Exe"), Command.UsageLevels.HostOrExePerm, Command.UsageTimes.InGame, ExeCommand, true, false, [GetString("CommandArgs.Exe.Id")]), // ["exe", "выкинуть", "驱逐", "executar", "уничтожить", "повесить", "казнить", "казнь", "мут", "驱赶"]
            Command.Create("Command.Color", "{color}", GetString("CommandDescription.Color"), Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, ColorCommand, true, false, [GetString("CommandArgs.Color.Color")]), // ["colour", "color", "cor", "цвет", "颜色", "更改颜色", "修改颜色", "换颜色"]
            Command.Create("Command.Start", "[duration]", GetString("CommandDescription.Start"), Command.UsageLevels.HostOrStartPerm, Command.UsageTimes.InLobby, StartCommand, true, false, [GetString("CommandArgs.Start.Time")]), // ["start", "开始", "старт"]
            Command.Create("Command.End", "", GetString("CommandDescription.End"), Command.UsageLevels.HostOrEndPerm, Command.UsageTimes.InGame, EndCommand, true, false), // ["end", "encerrar", "завершить", "结束", "结束游戏"]
            Command.Create("Command.ID", "", GetString("CommandDesciption.ID"), Command.UsageLevels.Everyone, Command.UsageTimes.Always, IDCommand, true, true), // ["id", "айди", "编号", "玩家编号", "mid", "玩家列表", "玩家信息", "玩家编号列表", "guesslist", "gl编号", "玩家id", "id列表", "列表", "所有id", "全部id", "編號", "玩家編號"]
            Command.Create("Command.SetPlayers", "{number}", "CommandDescription.SetPlayers", Command.UsageLevels.Host, Command.UsageTimes.InLobby, SetPlayersCommand, true, false, ["CommandArgs.SetPlayers.Num"]), // ["setplayers", "maxjogadores", "设置最大玩家数", "设置最大玩家数量", "设置玩家数", "设置玩家数量", "玩家数", "玩家数量", "玩家"]
            Command.Create("Command.Icons", "", "CommandDescription.Icons", Command.UsageLevels.Everyone, Command.UsageTimes.Always, IconsCommand, true, false), // ["icon", "icons", "符号", "标志"]
            // "Command.IconHelp": ["iconhelp", "符号帮助", "标志帮助"]
            Command.Create("Command.Me", "[id]", "CommandDescription.Me", Command.UsageLevels.Everyone, Command.UsageTimes.Always, MeCommand, true, false, ["CommandArgs.Me.Id"]), // ["me", "我的权限", "权限"]
            Command.Create("Command.TagColor", "{color}", "CommandDescription.TagColor", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, TagColorCommand, true, false, ["CommandArgs.TagColor.Color"]), // ["tagcolor", "tagcolour", "标签颜色", "附加名称颜色"]
            Command.Create("Command.Kill", "{id}", "CommandDescription.Kill", Command.UsageLevels.Host, Command.UsageTimes.InGame, KillCommand, true, false, ["CommandArgs.Kill.Id"]), // ["kill", "matar", "убить", "击杀", "杀死"]
            Command.Create("Command.Quit", "{?}", "CommandDescription.Quit", Command.UsageLevels.Everyone, Command.UsageTimes.Always, QuitCommand, true, false, ["CommandArgs.Quit.Arg1"]), // ["quit", "qt", "sair", "退出", "退"]
            Command.Create("Command.FixNames", "", "CommandDescription.FixNames", Command.UsageLevels.Everyone, Command.UsageTimes.InGame, FixNamesCommand, true, false), // ["xf", "修复", "修"]
            Command.Create("Command.ChangeRole", "{role}", "CommandDescription.ChangeRole", Command.UsageLevels.Debug, Command.UsageTimes.InGame, ChangeRoleCommand, true, true, ["CommandArgs.ChangeRole.Role"]), // ["changerole", "mudarfunção", "改变职业", "修改职业"]
            Command.Create("Command.Deck", "", "CommandDescription.Deck", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, DeckCommand, true, false), // ["deck"]
            Command.Create("Command.Draft", "[start|desc|add|reset|number]", "CommandDescription.Draft", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, DraftCommand, true, false, ["CommandArgs.Draft.Arg1"]), // ["draft"]
            Command.Create("Command.DraftDesc", "{number}", "CommandDescription.DraftDesc", Command.UsageLevels.Everyone, Command.UsageTimes.InLobby, DraftDescCommand, false, false, ["CommandArgs.DraftDesc.Number"]), //["dd", "draftdescription"]

            // Commands with methods in other classes (mostly role commands)
            Command.Create("Command.Guess", "{id} {role}", GetString("CommandDescription.Guess"), Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, GuessManager.GuessCommand, false, false, [GetString("CommandArgs.Guess.Id"), GetString("CommandArgs.Guess.Role")]), // ["shoot", "guess", "bet", "st", "gs", "bt", "猜", "赌", "賭"]
            Command.Create("Command.Trial", "{id}", GetString("CommandDescription.Trial"), Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, Judge.TrialCommand, false, false, [GetString("CommandArgs.Trial.Id")], [CustomRoles.Judge, CustomRoles.Councillor]), // ["sp", "jj","tl", "trial", "审判", "判", "审", "審判", "審", "Murder"]
            Command.Create("Command.Finish", "", GetString("CommandDescription.Finish"), Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, President.FinishCommand, false, false, requiredRole: [CustomRoles.President]), // ["finish", "结束", "结束会议", "結束", "結束會議"]
            Command.Create("Command.Reveal", "", GetString("CommandDescription.Reveal"), Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, President.RevealCommand, false, false, requiredRole: [CustomRoles.President]), // ["reveal","展示"]
            Command.Create("Command.Inspect", "{id1} {id2}", GetString("CommandDescription.Inspect"), Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, Inspector.InspectCommand, false, false, [GetString("CommandArgs.Inspect.Id1"), GetString("CommandArgs.Inspect.Id2")], [CustomRoles.Inspector]), // ["compare", "cmp", "比较", "比較"]
            Command.Create("Command.Duel", "{number}", GetString("CommandDescription.Duel"), Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, Pirate.DuelCommand, false, false, [GetString("CommandArgs.Duel.Num")]), // ["duel"]
            Command.Create("Command.Revenge", "{id}", GetString("CommandDescription.Revenge"), Command.UsageLevels.RoleSpecific, Command.UsageTimes.InGame, Nemesis.RevengeCommand, false, false, [GetString("CommandArgs.Revenge.Id")], [CustomRoles.Nemesis]), // ["rv"]
            Command.Create("Command.Retribution", "{id}", GetString("CommandDescription.Retribution"), Command.UsageLevels.RoleSpecific, Command.UsageTimes.InGame, Retributionist.RetributionCommand, false, false, [GetString("CommandArgs.Retribution.Id")], [CustomRoles.Retributionist]), // ["ret"]
            Command.Create("Command.Exorcism", "", GetString("CommandDescription.Exorcism"), Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, Exorcist.ExorcismCommand, false, false, [GetString("CommandArgs.Exorcism")], [CustomRoles.Exorcist]), // ["exorcise", "exorcism", "ex"]
            Command.Create("Command.Ritual", "{id} {role}", GetString("CommandDescription.Ritual"), Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, Ritualist.RitualCommand, false, false, [GetString("CommandArgs.Ritual.Id"), GetString("CommandArgs.Ritual.Role")], [CustomRoles.Ritualist]), // ["rt", "rit", "ritual", "bloodritual", "鲜血仪式", "仪式", "献祭", "举行", "附魔"]
            Command.Create("Command.Medium", "{letter}", "CommandDescription.Medium", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, Medium.MediumCommand, false, false, ["CommandArgs.Medium.YN"]), // ["通灵", "ms", "mediumship", "medium"]
            Command.Create("Command.Summon", "{id}", "CommandDescription.Summon", Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, Summoner.SummonCommand, false, false, ["CommandArgs.Summon.Id"], [CustomRoles.Summoner]), // ["summon", "sm"]
            Command.Create("Command.Swap", "{id}", "CommandDescription.Swap", Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, Swapper.SwapCommand, false, false, ["CommandArgs.Swap.Id"], [CustomRoles.Swapper]), // ["sw", "换票", "换", "換票", "換", "swap", "st"]
            Command.Create("Command.Expel", "{id}", "CommandDescription.Expel", Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, Dictator.ExpelCommand, false, false, ["CommandArgs.Expel.Id"], [CustomRoles.Dictator]), // ["exp", "expel", "独裁", "獨裁"]
            Command.Create("Command.Daybreak", "", "CommandDescription.Daybreak", Command.UsageLevels.RoleSpecific, Command.UsageTimes.InMeeting, Starspawn.DaybreakCommand, false, false, requiredRole: [CustomRoles.Starspawn]), // ["db", "daybreak"]
            Command.Create("Command.Answer", "{letter}", "CommandDescription.Answer", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, Quizmaster.AnswerCommand, false, false, ["CommandArgs.Answer.ABC"]), // ["ans", "asw", "answer", "回答"]
            Command.Create("Command.ShowQuestion", "", "CommandDescription.ShowQuestion", Command.UsageLevels.Everyone, Command.UsageTimes.InMeeting, Quizmaster.ShowQuestionCommand, false, false), // ["qmquiz", "提问"]
            

            /*
            /cosid
            /mt
            /cs
            /sd
            
            /rps
            /coinflip
            /gno
            /rand
            /8ball

            /pv
            /poll

            /modcolor
            /vipcolor
            */
        ];
    }

    public static bool Prefix(ChatController __instance)
    {
        if (__instance.quickChatField.visible) return true;

        __instance.freeChatField.textArea.text = __instance.freeChatField.textArea.text.Replace("\b", string.Empty).Replace("\r", string.Empty);
        
        __instance.timeSinceLastMessage = 3f;

        string text = __instance.freeChatField.textArea.text.Trim();
        if (text.StartsWith("/cmd ")) 
        {
            text = text[5..];
            if (!text.StartsWith("/")) text = "/" + text;
        }
        var cancelVal = string.Empty;

        if (Blackmailer.CheckBlackmaile(PlayerControl.LocalPlayer) && PlayerControl.LocalPlayer.IsAlive())
        {
            goto Canceled;
        }
        if (Exorcist.IsExorcismCurrentlyActive() && PlayerControl.LocalPlayer.IsAlive())
        {
            Exorcist.ExorcisePlayer(PlayerControl.LocalPlayer);
            goto Canceled;
        }

        if (ChatHistory.Count == 0 || ChatHistory[^1] != text)
            ChatHistory.Add(text);

        ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;

        string[] args = text.Split(' ');
        var canceled = false;
        Main.isChatCommand = true;

        Directory.CreateDirectory(modTagsFiles);
        Directory.CreateDirectory(vipTagsFiles);
        Directory.CreateDirectory(sponsorTagsFiles);

        Logger.Info(text, "SendChat");

        Main.isChatCommand = false;

        if (text.StartsWith('/'))
        {
            foreach ((string key, Command command) in Command.AllCommands)
            {
                if (!command.IsThisCommand(text)) continue;

                Main.isChatCommand = true;

                if (!command.CanUseCommand(PlayerControl.LocalPlayer, sendErrorMessage: true))
                    goto Canceled;

                command.Action(PlayerControl.LocalPlayer, key, text, args);
                if (command.IsCanceled) goto Canceled;

                break;
            }
        }

        if (string.IsNullOrWhiteSpace(text))
            goto Canceled;

        goto Skip;
        Canceled:
        Main.isChatCommand = false;
        canceled = true;
        Skip:

        if (ExileController.Instance)
            canceled = true;

        if (canceled)
        {
            Logger.Info("Command Canceled", "ChatCommand");
            __instance.freeChatField.textArea.Clear();
            __instance.freeChatField.textArea.SetText(cancelVal);
        }
        else
        {
            if (GameStates.IsLobby && AmongUsClient.Instance.AmHost)
            {
                string name = PlayerControl.LocalPlayer.GetRealName();

                Utils.SendMessage(text.Insert(0, new('\n', name.Count(x => x == '\n'))), title: name, addToHistory: false, noSplit: true);

                canceled = true;
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(string.Empty);
            }

            ChatManager.SendMessage(PlayerControl.LocalPlayer, text);
        }

        if (!canceled && AmongUsClient.Instance.AmHost && Utils.TempReviveHostRunning)
        {
            if (!WaitingToSend) Main.Instance.StartCoroutine(Wait());
            return false;
            
            IEnumerator Wait()
            {
                WaitingToSend = true;
                while (Utils.TempReviveHostRunning && AmongUsClient.Instance.AmHost) yield return null;
                yield return new WaitForSecondsRealtime(0.5f);
                if (GameStates.IsEnded || GameStates.IsLobby) yield break;
                WaitingToSend = false;
                if (HudManager.InstanceExists) HudManager.Instance.Chat.SendChat();
            }
        }

        return !canceled;
    }

    public static bool Old_Prefix(ChatController __instance)
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
        if (Blackmailer.HasEnabled && AmongUsClient.Instance.AmHost) // Blackmailer.ForBlackmailer.Contains(PlayerControl.LocalPlayer.PlayerId)) && PlayerControl.LocalPlayer.IsAlive())
        {
            ChatManager.SendMessage(PlayerControl.LocalPlayer, text);
        }

        if (Main.Daybreak) goto Canceled;

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
                    subArgs = args.Length < 2 ? string.Empty : string.Join(" ", args[1..]);
                    PlayerControl.LocalPlayer.RPCPlayCustomSound(subArgs.Trim());
                    break;

                case "/sd":
                case "/播放音效给":
                case "/播放声音给":
                    canceled = true;
                    subArgs = args.Length < 2 ? string.Empty : string.Join(" ", args[1..]);
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
                
                // case "/spam":
                //     canceled = true;
                //     ChatManager.SendQuickChatSpam();
                //     ChatManager.SendPreviousMessagesToAll();
                //     break;
                
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

    public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
    {
        canceled = false;
        if (!AmongUsClient.Instance.AmHost || player.AmOwner) return;

        long now = Utils.TimeStamp;

        if (LastSentCommand.TryGetValue(player.PlayerId, out long ts) && ts + 2 >= now && !player.IsModded())
        {
            Logger.Warn("Chat message ignored, it was sent too soon after their last message", "ReceiveChat");
            return;
        }

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
        
        if (text.StartsWith("/cmd ")) 
        {
            text = text[5..];
            if (!text.StartsWith("/")) text = "/" + text;
        }

        if (text.StartsWith("\n")) text = text[1..];

        string[] args = text.Split(' ');

        var commandEntered = false;

        if (text.StartsWith('/') && !player.IsModded() && (!GameStates.IsMeeting || MeetingHud.Instance.state is not MeetingHud.VoteStates.Results and not MeetingHud.VoteStates.Proceeding))
        {
            foreach ((string key, Command command) in Command.AllCommands)
            {
                if (!command.IsThisCommand(text)) continue;

                Logger.Info($" Recognized command: {text}", "ReceiveChat");
                commandEntered = true;

                if (!command.CanUseCommand(player, sendErrorMessage: true))
                {
                    canceled = true;
                    break;
                }

                // if (command.AlwaysHidden) ChatManager.SendPreviousMessagesToAll();
                command.Action(player, key, text, args);
                if (command.IsCanceled) canceled = command.AlwaysHidden /*|| !Options.HostSeesCommandsEnteredByOthers.GetBool()*/;
                break;
            }
        }

        if (ExileController.Instance)
        {
            canceled = true;
            // HasMessageDuringEjectionScreen = true;
        }

        if (!canceled) ChatManager.SendMessage(player, text);

        switch (commandEntered)
        {
            case true:
                LastSentCommand[player.PlayerId] = now;
                break;
            case false:
                SpamManager.CheckSpam(player, text);
                break;
        }
    }

    public static string FixRoleNameInput(string text)
    {
        text = text.Replace("着", "者").Trim().ToLower();
        return text;
    }

    public static bool GetRoleByName(string name, out CustomRoles role)
    {
        role = new();

        if (name.IsNullOrWhiteSpace()) return false;

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

        // Logger.Info($"found role name {nameWithoutId}", "GetRoleByName");

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
        Utils.SendMessage(Des, playerId, title, addToHistory: false);

        // Show role settings
        Utils.SendMessage("", playerId, Conf.ToString(), addToHistory: false);
        return;
    }
    public static void Old_OnReceiveChat(PlayerControl player, string text, out bool canceled)
    {
        canceled = false;
        if (!AmongUsClient.Instance.AmHost) return;

        if (Main.Daybreak) return;

        if (!Blackmailer.CheckBlackmaile(player)) ChatManager.SendMessage(player, text);

        if (text.StartsWith("\n")) text = text[1..];
        string[] args = text.Split(' ');
        string subArgs = "";
        string subArgs2 = "";

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

            case "/modcolor":
            case "/modcolour":
            case "/模组端颜色":
            case "/模组颜色":
                if (Options.ApplyModeratorList.GetBool())
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
                    //Logger.Msg($"File exists, creating file at {vipTagsFiles}/{player.FriendCode}.txt", "vipcolor");
                    //Logger.Msg($"{subArgs}","modcolor");
                    File.WriteAllText(colorFilePathh, $"{subArgs}");
                    break;
                }
            
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

            default:
                if (SpamManager.CheckSpam(player, text)) return;
                break;
        }
    }

    public static void RequestCommandProcessingFromHost(string text, string commandKey)
    {
        if (!Command.AllCommands.TryGetValue(commandKey, out Command command))
        {
            Logger.Error($"Invalid Command {commandKey}.", "RequestCommandProcessingFromHost");
            return;
        }

        PlayerControl pc = PlayerControl.LocalPlayer;
        MessageWriter w = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)CustomRPC.RequestCommandProcessing, SendOption.Reliable, AmongUsClient.Instance.HostId);
        w.Write(commandKey);
        w.Write(pc.PlayerId);
        w.Write(text);
        AmongUsClient.Instance.FinishRpcImmediately(w);
    }

    private static bool CheckArg(string lookupKey, string arg)
    {
        var values = GetString(lookupKey).Split("|").Select(x => x.Trim());
        return values.Contains(arg);
    }

#region Command Handlers
    
    private static void FixCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (args.Length < 2 || !byte.TryParse(args[1], out byte id)) return;

        var pc = id.GetPlayer();
        if (pc == null) return;

        pc.FixBlackScreen();

        if (Main.AllPlayerControls.All(x => x.IsAlive()))
            Logger.SendInGame(GetString("FixBlackScreenWaitForDead"));
    }

    private static void AFKExemptCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (args.Length < 2 || !byte.TryParse(args[1], out byte afkId)) return;

        AFKDetector.ExemptedPlayers.Add(afkId);
        Utils.SendMessage("\n", player.PlayerId, string.Format(GetString("PlayerExemptedFromAFK"), afkId.GetPlayerName()));
    }

    private static void DumpCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        Utils.DumpLog();
    }

    private static void VersionCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        string versionText = Main.playerVersion.OrderBy(pair => pair.Key).Aggregate(string.Empty, (current, kvp) => current + $"{kvp.Key}: ({Main.AllPlayerNames[(byte)kvp.Key]}) {kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n");
        if (versionText != string.Empty && HudManager.InstanceExists) HudManager.Instance.Chat.AddChat(player, (player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + versionText);
    }

    private static void SaveCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        string saveFileName = "template";
        if (args.Length >= 2)
            saveFileName = string.Join(" ", args[1..]);

        string saveFile = OptionCopier.Save(fileName: saveFileName);
        Utils.SendMessage(string.Format(GetString("PresetSaved"), saveFile), player.PlayerId);
    }

    private static void LoadCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        string loadFileName = "template";
        if (args.Length >= 2)
            loadFileName = string.Join(" ", args[1..]);

        string loadFile = OptionCopier.Load(fileName: loadFileName);
        Utils.SendMessage(string.Format(GetString("PresetLoaded"), loadFile), player.PlayerId);
    }

    private static void DocsCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        int roleId = 500;
        if (args.Length >= 2)
            roleId = int.Parse(args[1]);

        if (roleId != 500)
            ((CustomRoles)roleId).GenerateDocs();
    }

    private static void WinnerCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (Main.winnerNameList.Count == 0)
            Utils.SendMessage(GetString("NoInfoExists"));
        else Utils.SendMessage("Winner: " + string.Join(", ", Main.winnerNameList));
    }

    private static void LastResultCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        Utils.ShowKillLog(player.PlayerId);
        Utils.ShowLastRoles(player.PlayerId);
        Utils.ShowLastResult(player.PlayerId);
    }

    private static void GameResultCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        Utils.ShowLastResult(player.PlayerId);
    }

    private static void KillLogCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        Utils.ShowKillLog(player.PlayerId);
    }

    private static void RoleSummaryCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }
        Utils.ShowLastRoles(player.PlayerId);
    }

    private static void GhostInfoCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }
        Utils.SendMessage(GetString("Message.GhostRoleInfo"), player.PlayerId);
    }

    private static void ApocInfoCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }
        Utils.SendMessage(GetString("Message.ApocalypseInfo"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Apocalypse), GetString("ApocalypseInfoTitle")));
    }

    private static void CovenInfoCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }
        Utils.SendMessage(GetString("Message.CovenInfo"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Coven), GetString("CovenInfoTitle")));
    }

    private static void RenameCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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

    private static void HideNameCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        Main.HideName.Value = args.Length > 1 ? string.Join(' ', args[1..]) : Main.HideName.DefaultValue.ToString();

        GameStartManagerPatch.GameStartManagerStartPatch.HideName.text =
            ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                : $"<color={Main.ModColor}>{Main.HideName.Value}</color>";
    }

    private static void LevelCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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

    private static void NowCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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

    private static void DisconnectCommand(PlayerControl player, string commandKey, string text, string[] args)
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

    private static void RCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        string subArgs = args.Length > 1 ? string.Join(' ', args[1..]) : string.Empty;
        byte to = player.AmOwner && Input.GetKeyDown(KeyCode.LeftShift) ? byte.MaxValue : player.PlayerId;
        SendRolesInfo(subArgs, to);
    }

    private static void FactionsCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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

    private static void MyRoleCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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
            var ACleared = Sub.ToString()[2..];
            ACleared = ACleared.Length > 1200 ? $"<size={Asize}>" + ACleared.RemoveHtmlTags() + "</size>" : ACleared;
            Sub.Clear().Append(ACleared);
        }

        Utils.SendMessage(Des, player.PlayerId, title, addToHistory: false);
        Utils.SendMessage("", player.PlayerId, Conf.ToString(), addToHistory: false);
        if (Sub.ToString() != string.Empty) Utils.SendMessage(Sub.ToString(), player.PlayerId, SubTitle, addToHistory: false);

        Logger.Info($"Command '/m' should be send message", "OnReceiveChat");
    }

    private static void HelpCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        Utils.ShowHelp(player.PlayerId);
    }

    private static void SetRoleCommand(PlayerControl player, string commandKey, string text, string[] args)
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

    private static void TPOutCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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

    private static void TPInCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (!Options.PlayerCanUseTP.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
            return;
        }
        player.RpcTeleport(new Vector2(-0.2f, 1.3f));
    }
    private static void KCountCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (!Options.EnableKillerLeftCommand.GetBool())
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
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

        Utils.SendMessage(sub.ToString(), player.PlayerId);
    }

    private static void TemplateCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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

    private static void MessageWaitCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (args.Length > 1 && int.TryParse(args[1], out int sec))
        {
            Main.MessageWait.Value = sec;
            Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
        }
        else
            Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
    }

    private static void DeathCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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

    private static void SayCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (args.Length > 1)
        {
            if (player.IsHost()) 
            {
                Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")} ~ <size=1.25>{player.GetRealName(clientData: true)}</size></color>");
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

    private static void VoteCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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

    private static void BanKickCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
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
        if (!player.IsHost() && (Utils.IsPlayerModerator(kickedPlayer.FriendCode) || TagManager.ReadPermission(kickedPlayer.FriendCode) >= (isBan ? 5 : 4)))
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

    private static void ExeCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (args.Length < 2 || !int.TryParse(args[1], out int id)) return;
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
    }

    private static void WarnCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        var subArgs = args.Length < 2 ? "" : args[1];
        if (string.IsNullOrEmpty(subArgs) || !byte.TryParse(subArgs, out byte warnPlayerId))
        {
            Utils.SendMessage(GetString("WarnCommandInvalidID"), player.PlayerId);
            return;
        }
        if (warnPlayerId == 0)
        {
            Utils.SendMessage(GetString("WarnCommandWarnHost"), player.PlayerId);
            return;
        }

        var warnedPlayer = Utils.GetPlayerById(warnPlayerId);
        if (warnedPlayer == null)
        {
            Utils.SendMessage(GetString("WarnCommandInvalidID"), player.PlayerId);
            return;
        }

        // Prevent moderators from warning other moderators
        if ((Utils.IsPlayerModerator(warnedPlayer.FriendCode) || TagManager.ReadPermission(warnedPlayer.FriendCode) >= 2) && !player.IsHost())
        {
            Utils.SendMessage(GetString("WarnCommandWarnMod"), player.PlayerId);
            return;
        }
        // warn the specified player
        string warnReason = "Reason : Not specified\n";
        string warnedPlayerName = warnedPlayer.GetRealName();
        //textToSend2 = $" {warnedPlayerName} {GetString("WarnCommandWarned")} ~{player.name}";
        if (args.Length > 2)
        {
            warnReason = "Reason : " + string.Join(" ", args.Skip(2)) + "\n";
        }
        Utils.SendMessage($" {warnedPlayerName} {GetString("WarnCommandWarned")} {warnReason} ~{player.name}");
        string modLogname1 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n2) ? n2 : "";
        string warnlogname = Main.AllPlayerNames.TryGetValue(warnedPlayer.PlayerId, out var n12) ? n12 : "";
        string moderatorFriendCode1 = player.FriendCode.ToString();
        string warnedPlayerFriendCode = warnedPlayer.FriendCode.ToString();
        string warnedPlayerHashPuid = warnedPlayer.GetClient().GetHashedPuid();
        string logMessage1 = $"[{DateTime.Now}] {moderatorFriendCode1},{modLogname1} Warned: {warnedPlayerFriendCode},{warnedPlayerHashPuid},{warnlogname} Reason: {warnReason}";
        File.AppendAllText(modLogFiles, logMessage1 + Environment.NewLine);
    }

    private static void ColorCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (player.IsHost() || Options.PlayerCanSetColor.GetBool() || player.FriendCode.GetDevUser().IsDev || player.FriendCode.GetDevUser().ColorCmd || Utils.IsPlayerVIP(player.FriendCode) || TagManager.ReadPermission(player.FriendCode) >= 1)
        {
            var subArgs = args.Length < 2 ? "" : args[1];
            var color = Utils.MsgToColor(subArgs);
            if (color == byte.MaxValue)
            {
                Utils.SendMessage(GetString("IllegalColor"), player.PlayerId);
                return;
            }
            player.RpcSetColor(color);
            Utils.SendMessage(string.Format(GetString("Message.SetColor"), subArgs), player.PlayerId);
        }
        else
        {
            Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
        }
    }

    private static void StartCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        var tagCanStart = TagManager.ReadPermission(player.FriendCode) >= 3;
        if (!player.IsHost() && !tagCanStart && !Utils.IsPlayerModerator(player.FriendCode))
        {
            Utils.SendMessage(GetString("StartCommandNoAccess"), player.PlayerId);
            return;
        }
        if (!player.IsHost() && !tagCanStart && (!Options.ApplyModeratorList.GetBool() || !Options.AllowStartCommand.GetBool()))
        {
            Utils.SendMessage(GetString("StartCommandDisabled"), player.PlayerId);
            return;
        }
        if (GameStates.IsCountDown)
        {
            Utils.SendMessage(GetString("StartCommandCountdown"), player.PlayerId);
            return;
        }
        var subArgs = args.Length < 2 ? string.Empty : args[1];
        if (string.IsNullOrEmpty(subArgs) || !int.TryParse(subArgs, out int countdown))
        {
            countdown = 5;
        }
        if (countdown < Options.StartCommandMinCountdown.GetInt() || countdown > Options.StartCommandMaxCountdown.GetInt())
        {
            Utils.SendMessage(string.Format(GetString("StartCommandInvalidCountdown"), Options.StartCommandMinCountdown.GetInt(), Options.StartCommandMaxCountdown.GetInt()), player.PlayerId);
            return;
        }
        GameStartManager.Instance.BeginGame();
        GameStartManager.Instance.countDownTimer = countdown;
        Utils.SendMessage(string.Format(GetString("StartCommandStarted"), player.name));
    }

    private static void EndCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        Utils.SendMessage(string.Format(GetString("EndCommandEnded"), player.name));
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
        GameManager.Instance.LogicFlow.CheckEndCriteria();
    }

    private static void IDCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (!player.IsHost() && TagManager.ReadPermission(player.FriendCode) < 2 && (!Options.ApplyModeratorList.GetBool() || !Utils.IsPlayerModerator(player.FriendCode))
            && !Options.EnableVoteCommand.GetBool()) return;

        string msgText = GetString("PlayerIdList");
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc == null) continue;
            msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName().RemoveHtmlTags();
        }
        Utils.SendMessage(msgText, player.PlayerId);
    }

    private static void SetPlayersCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        var subArgs = args.Length < 2 ? "" : args[1];
        var numbereer = Convert.ToByte(subArgs);
        if (numbereer > 15 && GameStates.IsVanillaServer)
        {
            Utils.SendMessage(GetString("Message.MaxPlayersFailByRegion"));
            return;
        }
        Utils.SendMessage(GetString("Message.MaxPlayers") + numbereer);
        if (GameStates.IsNormalGame)
            GameOptionsManager.Instance.currentNormalGameOptions.MaxPlayers = numbereer;

        else if (GameStates.IsHideNSeek)
            GameOptionsManager.Instance.currentHideNSeekGameOptions.MaxPlayers = numbereer;
    }

    private static void IconsCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (player.IsHost())
        {
            var iconhelp = GetString("Command.IconHelp").Split("|");
            foreach (var ih in iconhelp)
            {
                var i = "/" + ih;
                if (args[0] == i)
                {
                    Utils.SendMessage(GetString("Command.icons"), title: GetString("IconsTitle"));
                    return;
                }
            }
        }
        Utils.SendMessage(GetString("Command.icons"), player.PlayerId, GetString("IconsTitle"));
    }

    private static void MeCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        const string yes = "<#10e341><b>✓</b></color>";
        const string no = "<#e31010><b>〤</b></color>";

        var subArgs = text.Length == 3 ? string.Empty : text[3..];
        string Devbox = player.FriendCode.GetDevUser().DeBug ? yes : no;
        string ColorBox = player.FriendCode.GetDevUser().ColorCmd ? yes : no;

        if (string.IsNullOrEmpty(subArgs))
        {
            HudManager.Instance.Chat.AddChat(player, (player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), player.PlayerId, player.GetRealName(clientData: true), player.GetClient().FriendCode, player.GetClient().GetHashedPuid(), player.FriendCode.GetDevUser().GetUserType(), Devbox, "", ColorBox)}");
        }
        else
        {
            if (byte.TryParse(subArgs, out byte meid))
            {
                if (meid != player.PlayerId)
                {
                    var targetplayer = Utils.GetPlayerById(meid);
                    if (targetplayer != null && targetplayer.GetClient() != null)
                    {
                        HudManager.Instance.Chat.AddChat(player, (player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandTargetInfo"), targetplayer.PlayerId, targetplayer.GetRealName(clientData: true), targetplayer.GetClient().FriendCode, targetplayer.GetClient().GetHashedPuid(), targetplayer.FriendCode.GetDevUser().GetUserType())}");
                    }
                    else
                    {
                        HudManager.Instance.Chat.AddChat(player, (player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{GetString("Message.MeCommandInvalidID")}");
                    }
                }
                else
                {
                    HudManager.Instance.Chat.AddChat(player, (player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{string.Format(GetString("Message.MeCommandInfo"), player.PlayerId, player.GetRealName(clientData: true), player.GetClient().FriendCode, player.GetClient().GetHashedPuid(), player.FriendCode.GetDevUser().GetUserType(), Devbox, "", ColorBox)}");
                }
            }
            else
            {
                HudManager.Instance.Chat.AddChat(player, (player.FriendCode.GetDevUser().HasTag() ? "\n" : string.Empty) + $"{GetString("Message.MeCommandInvalidID")}");
            }
        }
    }

    private static void TagColorCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        string name1 = Main.AllPlayerNames.TryGetValue(player.PlayerId, out var n) ? n : "";
        if (name1 == "") return;
        if (!name1.Contains('\r') && player.FriendCode.GetDevUser().HasTag())
        {
            if (!GameStates.IsLobby)
            {
                Utils.SendMessage(GetString("ColorCommandNoLobby"), player.PlayerId);
                return;
            }
            var subArgs = args.Length != 2 ? "" : args[1];
            if (string.IsNullOrEmpty(subArgs) || !Utils.CheckColorHex(subArgs))
            {
                Logger.Msg($"{subArgs}", "tagcolor");
                Utils.SendMessage(GetString("TagColorInvalidHexCode"), player.PlayerId);
                return;
            }
            string tagColorFilePath = $"{sponsorTagsFiles}/{player.FriendCode}.txt";
            if (!File.Exists(tagColorFilePath))
            {
                Logger.Msg($"File Not exist, creating file at {tagColorFilePath}", "tagcolor");
                File.Create(tagColorFilePath).Close();
            }

            File.WriteAllText(tagColorFilePath, $"{subArgs}");
        }
    }

    private static void KillCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) return;
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
    }

    private static void QuitCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (player.IsHost())
        {
            Utils.SendMessage(GetString("Message.CanNotUseByHost"), player.PlayerId);
            return;
        }

        if (Options.PlayerCanUseQuitCommand.GetBool())
        {
            var subArgs = args.Length < 2 ? "" : args[1];
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
    }

    private static void FixNamesCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.IsAlive()) continue;

            pc.RpcSetNamePrivate(pc.GetRealName(isMeeting: true), player, true);
        }
        ChatUpdatePatch.DoBlockChat = false;
        //Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);
        Utils.SendMessage(GetString("Message.TryFixName"), player.PlayerId);
    }

    private static void ChangeRoleCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (GameStates.IsHideNSeek) return;
        if (!GameStates.IsInGame) return;

        var subArgs = string.Join(" ", args[1..]);
        var setRole = FixRoleNameInput(subArgs).ToLower().Trim().Replace(" ", string.Empty);
        Logger.Info(setRole, "changerole Input");
        foreach (var rl in CustomRolesHelper.AllRoles)
        {
            if (rl.IsVanilla()) continue;
            var roleName = GetString(rl.ToString()).ToLower().Trim().TrimStart('*').Replace(" ", string.Empty);
            //Logger.Info(roleName, "2");
            if (setRole == roleName)
            {
                player.GetRoleClass()?.OnRemove(player.PlayerId);
                player.RpcChangeRoleBasis(rl);
                player.RpcSetCustomRole(rl);
                player.GetRoleClass().OnAdd(player.PlayerId);
                Utils.SendMessage(string.Format("Debug Set your role to {0}", rl.ToString()), player.PlayerId);
                Utils.NotifyRoles(SpecifyTarget: player, NoCache: true);
                Utils.MarkEveryoneDirtySettings();
                break;
            }
        }
    }

    private static void DeckCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        return;
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        player.SendDeckList();
    }

    private static void DraftCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        return;
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (args.Length < 2 || args[1] == "start")
        {
            if (!player.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(player.FriendCode)&& !player.FriendCode.GetDevUser().IsDev) return;

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
            DraftDescCommand(player, "Command.DraftDesc", text, args);
            return;
        }
        else if (args[1] == "add")
        {
            if (!player.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(player.FriendCode)&& !player.FriendCode.GetDevUser().IsDev) return;

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
            if (!player.IsHost() && !AmongUsClient.Instance.AmHost && !Utils.IsPlayerModerator(player.FriendCode)&& !player.FriendCode.GetDevUser().IsDev) return;

            DraftAssign.Reset();
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
                return;
            }

            if (cmdResult == DraftAssign.DraftCmdResult.NoCurrentDraft)
            {
                Utils.SendMessage(GetString("NoCurrentDraft"), player.PlayerId);
                return;
            }
            else if (cmdResult == DraftAssign.DraftCmdResult.DraftRemoved)
            {
                Utils.SendMessage(GetString("DraftSelectionCleared"), player.PlayerId);
                return;
            }
            else
            {
                Utils.SendMessage(string.Format(GetString("DraftSelection"), draftedRole.ToColoredString()), player.PlayerId);
            }
        }
    }

    private static void DraftDescCommand(PlayerControl player, string commandKey, string text, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            RequestCommandProcessingFromHost(text, commandKey);
            return;
        }

        if (int.TryParse(args[1], out int index))
        {
            player.SendDraftDescription(index);
        }
        else
        {
            Utils.SendMessage(GetString("InvalidDraftSelection"), player.PlayerId);
        }
    }

#endregion
}
[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
class ChatUpdatePatch
{
    public static readonly List<(string Text, byte SendTo, string Title, long SendTimeStamp)> LastMessages = [];
    public static bool DoBlockChat = false;
    // public static ChatController Instance;

    public static void Postfix(ChatController __instance)
    {
        var chatBubble = __instance.chatBubblePool.Prefab.CastFast<ChatBubble>();
        chatBubble.TextArea.overrideColorTags = false;

        if (Main.DarkTheme.Value)
        {
            chatBubble.TextArea.color = Color.white;
            chatBubble.Background.color = new(0.1f, 0.1f, 0.1f, 1f);
        }

        LastMessages.RemoveAll(x => Utils.TimeStamp - x.SendTimeStamp > 10);
    }

    internal static bool SendLastMessages(ref CustomRpcSender sender)
    {
        PlayerControl player = GameStates.IsVanillaServer ? PlayerControl.LocalPlayer : GameStates.IsLobby ? Main.AllPlayerControls.Without(PlayerControl.LocalPlayer).RandomElement() : Main.AllAlivePlayerControls.MinBy(x => x.PlayerId) ?? Main.AllPlayerControls.MinBy(x => x.PlayerId) ?? PlayerControl.LocalPlayer;
        if (player == null) return false;

        bool wasCleared = false;

        foreach ((string msg, byte sendTo, string title, _) in LastMessages)
            wasCleared = SendMessage(player, msg, sendTo, title, ref sender);

        return LastMessages.Count > 0 && !wasCleared;
    }

    private static bool SendMessage(PlayerControl player, string msg, byte sendTo, string title, ref CustomRpcSender sender)
    {
        int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).OwnerId;

        string name = player.Data.PlayerName;

        if (clientId == -1 && HudManager.InstanceExists)
        {
            player.SetName(title);
            HudManager.Instance.Chat.AddChat(player, msg);
            player.SetName(name);
        }

        sender.AutoStartRpc(player.NetId, RpcCalls.SetName, clientId)
            .Write(player.Data.NetId)
            .Write(title)
            .EndRpc();

        sender.AutoStartRpc(player.NetId, RpcCalls.SendChat, clientId)
            .Write(msg)
            .EndRpc();

        sender.AutoStartRpc(player.NetId, RpcCalls.SetName, clientId)
            .Write(player.Data.NetId)
            .Write(player.Data.PlayerName)
            .EndRpc();

        if (sender.stream.Length > 500)
        {
            sender.SendMessage();
            sender = CustomRpcSender.Create(sender.name, sender.sendOption);
            return true;
        }

        return false;
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
