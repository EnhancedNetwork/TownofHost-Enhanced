using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using TOHE.Modules;
using LogLevel = BepInEx.Logging.LogLevel;

namespace TOHE;

class Webhook
{
    public static void Send(string text)
    {
        if (Main.WebhookURL.Value == "none") return;
        HttpClient httpClient = new();
        Dictionary<string, string> strs = new()
        {
            { "content", text },
            { "username", "TOHE-Debugger" },
            { "avatar_url", "https://npm.elemecdn.com/hexo-static@1.0.1/img/avatar.webp" }
        };
        TaskAwaiter<HttpResponseMessage> awaiter = httpClient.PostAsync(
            Main.WebhookURL.Value, new FormUrlEncodedContent(strs)).GetAwaiter();
        awaiter.GetResult();
    }
}
class Logger
{
    public static bool IsEnable;
    public static List<string> DisableList = [];
    public static List<string> SendToGameList = [];
    private static readonly HashSet<string> NowDetailedErrorLog = [];

    public static bool isDetail = false;
    public static bool isAlsoInGame = false;
    public static void Enable() => IsEnable = true;
    public static void Disable() => IsEnable = false;
    public static void Enable(string tag, bool toGame = false)
    {
        DisableList.Remove(tag);
        if (toGame && !SendToGameList.Contains(tag)) SendToGameList.Add(tag);
        else SendToGameList.Remove(tag);
    }
    public static void Disable(string tag) { if (!DisableList.Contains(tag)) DisableList.Add(tag); }
    public static void SendInGame(string text)
    {
        if (!IsEnable) return;
        if (DestroyableSingleton<HudManager>._instance) DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(text);
    }
    private static void SendToFile(string text, LogLevel level = LogLevel.Info, string tag = "", bool escapeCRLF = true, int lineNumber = 0, string fileName = "", bool multiLine = false)
    {
        if (!IsEnable || DisableList.Contains(tag)) return;
        var logger = Main.Logger;

        if (SendToGameList.Contains(tag) || isAlsoInGame)
        {
            SendInGame($"[{tag}]{text}");
        }

        string log_text;
        if (level is LogLevel.Error or LogLevel.Fatal or LogLevel.Warning && !multiLine && !NowDetailedErrorLog.Contains(tag))
        {
            string t = DateTime.Now.ToString("HH:mm:ss");
            StackFrame stack = new(2);
            string className = stack.GetMethod()?.ReflectedType?.Name;
            string memberName = stack.GetMethod()?.Name;
            log_text = $"[{t}][{className}.{memberName}({Path.GetFileName(fileName)}:{lineNumber})][{tag}]{text}";
            NowDetailedErrorLog.Add(tag);
            _ = new LateTask(() => NowDetailedErrorLog.Remove(tag), 3f, shoudLog: false);
        }
        else
        {
            if (escapeCRLF) text = text.Replace("\r", "\\r").Replace("\n", "\\n");
            string t = DateTime.Now.ToString("HH:mm:ss");
            log_text = $"[{t}][{tag}]{text}";
        }

        switch (level)
        {
            case LogLevel.Info when !multiLine:
                logger.LogInfo(log_text);
                break;
            case LogLevel.Info:
                log_text.Split("\\n").Do(logger.LogInfo);
                break;
            case LogLevel.Warning when !multiLine:
                logger.LogWarning(log_text);
                break;
            case LogLevel.Warning:
                log_text.Split("\\n").Do(logger.LogWarning);
                break;
            case LogLevel.Error when !multiLine:
                logger.LogError(log_text);
                break;
            case LogLevel.Error:
                log_text.Split("\\n").Do(logger.LogError);
                break;
            case LogLevel.Fatal when !multiLine:
                logger.LogFatal(log_text);
                break;
            case LogLevel.Fatal:
                log_text.Split("\\n").Do(logger.LogFatal);
                break;
            case LogLevel.Message when !multiLine:
                logger.LogMessage(log_text);
                break;
            case LogLevel.Message:
                log_text.Split("\\n").Do(logger.LogMessage);
                break;
            case LogLevel.Debug when !multiLine:
                logger.LogFatal(log_text);
                break;
            case LogLevel.Debug:
                log_text.Split("\\n").Do(logger.LogFatal);
                break;
            default:
                logger.LogWarning("Error: Invalid LogLevel");
                logger.LogInfo(log_text);
                break;
        }
    }
    public static void Test(object content, string tag = "======= Test =======", bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", bool multiLine = false) =>
        SendToFile(content.ToString(), LogLevel.Debug, tag, escapeCRLF, lineNumber, fileName, multiLine);
    public static void Info(string text, string tag, bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", bool multiLine = false) =>
        SendToFile(text, LogLevel.Info, tag, escapeCRLF, lineNumber, fileName, multiLine);
    public static void Warn(string text, string tag, bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", bool multiLine = false) =>
        SendToFile(text, LogLevel.Warning, tag, escapeCRLF, lineNumber, fileName, multiLine);
    public static void Error(string text, string tag, bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", bool multiLine = false) =>
        SendToFile(text, LogLevel.Error, tag, escapeCRLF, lineNumber, fileName, multiLine);
    public static void Fatal(string text, string tag, bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", bool multiLine = false) =>
        SendToFile(text, LogLevel.Fatal, tag, escapeCRLF, lineNumber, fileName, multiLine);
    public static void Msg(string text, string tag, bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "", bool multiLine = false) =>
        SendToFile(text, LogLevel.Message, tag, escapeCRLF, lineNumber, fileName, multiLine);
    public static void Exception(Exception ex, string tag, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
        SendToFile(ex.ToString(), LogLevel.Error, tag, false, lineNumber, fileName);
    public static void CurrentMethod([CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "")
    {
        StackFrame stack = new(1);
        Msg($"\"{stack.GetMethod().ReflectedType.Name}.{stack.GetMethod().Name}\" Called in \"{Path.GetFileName(fileName)}({lineNumber})\"", "Method");
    }

    public static LogHandler Handler(string tag)
        => new(tag);
}