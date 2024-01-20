using System;

namespace TOHE.Modules;

class LogHandler(string tag) : ILogHandler
{
    public string Tag { get; } = tag;

    public void Info(string text)
        => Logger.Info(text, Tag, true);
    public void Warn(string text)
        => Logger.Warn(text, Tag, true);
    public void Error(string text)
        => Logger.Error(text, Tag, true);
    public void Fatal(string text)
        => Logger.Fatal(text, Tag, true);
    public void Msg(string text)
        => Logger.Msg(text, Tag, true);
    public void Exception(Exception ex)
        => Logger.Exception(ex, Tag);
}