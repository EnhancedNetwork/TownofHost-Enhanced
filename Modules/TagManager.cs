using AmongUs.Data;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using static Il2CppSystem.DateTimeParse;
using static TOHE.Translator;

namespace TOHE;

public static class TagManager
{
    private static readonly string TAGS_FILE_PATH = "./TOHE-DATA/Tags";
    public static void Init()
    {
        CreateIfNotExists();
    }
    public static void CreateIfNotExists()
    {
        try
        {
            
            if (!Directory.Exists(@"TOHE-DATA/Tags")) Directory.CreateDirectory(@"TOHE-DATA/Tags");
            var defaultTagMsg = GetResourcesTxt($"TOHE.Resources.Config.TagTemplate.txt");
            if (!File.Exists(@"./TOHE-DATA/Tag_Template.txt")) //default tag
            {
                Logger.Warn("Creating Tag_Template.txt", "TagManager");
                using FileStream fs = File.Create(@"./TOHE-DATA/Tags/Tag_Template.txt");
            }
            File.WriteAllText(@"./TOHE-DATA/Tags/Tag_Template.txt", defaultTagMsg); //overwriting default template
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "TagManager");
        }
    }
    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
    public static bool CheckFriendCode(string friendCode)
    {
        if (!File.Exists(@$"{TAGS_FILE_PATH}/{friendCode}.txt"))
        {
            Logger.Warn($"{friendCode} does not have a tag", "TagManager");
            return false;
        }
        return true;
    }
    public static string ReadTagName(string friendCode)
    {
        if (!CheckFriendCode(friendCode)) { return string.Empty; }
        string fileName = @$"{TAGS_FILE_PATH}/{friendCode}.txt";
        string temp = "";
        var searchTarget = "TagName:";
        foreach (var line in File.ReadLines(fileName))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("TagName:").Skip(1).First().Trim();
                break; 
            }
        }
        return temp;
    }
    public static string ReadTagColor(string friendCode)
    {
        if (!CheckFriendCode(friendCode)) { return string.Empty; }
        string fileName = @$"{TAGS_FILE_PATH}/{friendCode}.txt";
        string temp = "";
        var searchTarget = "TagColor:";
        foreach (var line in File.ReadLines(fileName))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("TagColor:").Skip(1).First().Trim().TrimEnd();
                break;
            }
        }
        return temp;
    }
    public static int ReadPermission(string friendCode)
    {
        if (!CheckFriendCode(friendCode)) { return -1; }
        string fileName = @$"{TAGS_FILE_PATH}/{friendCode}.txt";
        string temp = "";
        var searchTarget = "PermissionLevel:";
        foreach (var line in File.ReadLines(fileName))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("PermissionLevel:").Skip(1).First().Trim().TrimEnd();
                break;
            }
        }
        int.TryParse(temp, out int result);
        return result;
    }
    public static bool CanUseSayCommand(string friendCode)
    {
        if (!CheckFriendCode(friendCode)) { return false; }
        string fileName = @$"{TAGS_FILE_PATH}/{friendCode}.txt";
        string temp = "";
        var searchTarget = "SayCommandAccess:";
        foreach (var line in File.ReadLines(fileName))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("SayCommandAccess:").Skip(1).First().Trim().TrimEnd().ToLower();
                break;
            }
        }
        if (new[] { "yes", "y", "true", "t", "1" }.Any(c => temp.Contains(c))) return true;
        return false;
    }
}
