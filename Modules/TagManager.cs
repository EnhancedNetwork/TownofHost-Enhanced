using System;
using System.IO;
using System.Text;

namespace TOHE.Modules;

public static class TagManager
{
#if ANDROID
    private static readonly string TAGS_FILE_PATH = Path.Combine(UnityEngine.Application.persistentDataPath, "TOHE-DATA", "Tags");
#else
    private static readonly string TAGS_FILE_PATH = "./TOHE-DATA/Tags";
#endif

    public static void Init()
    {
        CreateIfNotExists();
    }

    public static void CreateIfNotExists()
    {
        try
        {
#if ANDROID
            string dataDirectory = Path.Combine(UnityEngine.Application.persistentDataPath, "TOHE-DATA", "Tags");
            string templateFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "TOHE-DATA", "Tags", "Tag_Template.txt");
#else
            string dataDirectory = @"TOHE-DATA/Tags";
            string templateFilePath = @"./TOHE-DATA/Tags/Tag_Template.txt";
#endif

            if (!Directory.Exists(dataDirectory)) Directory.CreateDirectory(dataDirectory);
            var defaultTagMsg = GetResourcesTxt($"TOHE.Resources.Config.TagTemplate.txt");
            if (!File.Exists(templateFilePath)) // Default tag
            {
                using FileStream fs = File.Create(templateFilePath);
            }
            File.WriteAllText(templateFilePath, defaultTagMsg); // Overwriting default template
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

    private static readonly string[] TagFolders = ["MOD_TAGS", "VIP_TAGS", "SPONSOR_TAGS"];

    // Check for the friend's tag file in any folder (not just in the root directory)
    public static bool CheckFriendCode(string friendCode, bool log = false)
    {
        var folderPaths = Directory.GetDirectories(TAGS_FILE_PATH)
            .Where(folder => !TagFolders.Contains(Path.GetFileName(folder)))
            .Concat([TAGS_FILE_PATH])
            .ToList();

        var filePath = folderPaths
            .Select(folder => Path.Combine(folder, $"{friendCode}.txt"))
            .FirstOrDefault(File.Exists);

        if (filePath == null)
        {
            if (log)
                Logger.Info($"{friendCode} does not have a tag", "TagManager");
            return false;
        }

        return true;
    }

    public static string ReadTagName(string friendCode)
    {
        var folderPaths = Directory.GetDirectories(TAGS_FILE_PATH)
            .Where(folder => !TagFolders.Contains(Path.GetFileName(folder)))
            .Concat([TAGS_FILE_PATH])
            .ToList();

        var filePath = folderPaths
            .Select(folder => Path.Combine(folder, $"{friendCode}.txt"))
            .FirstOrDefault(File.Exists);

        if (filePath == null) return string.Empty;

        string temp = "";
        var searchTarget = "TagName:";
        foreach (var line in File.ReadLines(filePath))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("TagName:").Skip(1).First().TrimStart();
                break;
            }
        }
        return temp;
    }

    public static string ReadTagColor(string friendCode)
    {
        var folderPaths = Directory.GetDirectories(TAGS_FILE_PATH)
            .Where(folder => !TagFolders.Contains(Path.GetFileName(folder)))
            .Concat([TAGS_FILE_PATH])
            .ToList();

        var filePath = folderPaths
            .Select(folder => Path.Combine(folder, $"{friendCode}.txt"))
            .FirstOrDefault(File.Exists);

        if (filePath == null) return string.Empty;

        string temp = "";
        var searchTarget = "TagColor:";
        foreach (var line in File.ReadLines(filePath))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("TagColor:").Skip(1).First().Trim().TrimEnd();
                break;
            }
        }
        return temp;
    }

    public static string ReadColoredTag(string friendCode)
    {
        var tag = "";

        string color = ReadTagColor(friendCode);
        string tagName = ReadTagName(friendCode);

        if (color.IsNullOrWhiteSpace() && !tagName.IsNullOrWhiteSpace()) return tagName;
        if (tagName.IsNullOrWhiteSpace()) return "";

        //static color
        if (!Options.GradientTagsOpt.GetBool())
        {
            string startColorCode = "ffff00";
            if (Utils.CheckColorHex(color)) startColorCode = color;
            tag = $"<color=#{startColorCode}>{tagName}</color>";
        }
        // gradient color
        else
        {
            string startColorCode = "ffff00";
            string endColorCode = "ffff00";

            if (color.Split(" ").Length == 2)
            {
                startColorCode = color.Split(" ")[0];
                endColorCode = color.Split(" ")[1];
            }
            if (!Utils.CheckGradientCode(color))
            {
                startColorCode = "ffff00";
                endColorCode = "ffff00";
            }

            if (startColorCode == endColorCode) tag = $"<color=#{startColorCode}>{tagName}</color>";

            else tag = Utils.GradientColorText(startColorCode, endColorCode, tagName);
        }

        return tag;
    }

    public static int ReadPermission(string friendCode)
    {
        var folderPaths = Directory.GetDirectories(TAGS_FILE_PATH)
            .Where(folder => !TagFolders.Contains(Path.GetFileName(folder)))
            .Concat([TAGS_FILE_PATH])
            .ToList();

        var filePath = folderPaths
            .Select(folder => Path.Combine(folder, $"{friendCode}.txt"))
            .FirstOrDefault(File.Exists);

        if (filePath == null) return -1;

        string temp = "";
        var searchTarget = "PermissionLevel:";
        foreach (var line in File.ReadLines(filePath))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("PermissionLevel:").Skip(1).First().Trim().TrimEnd();
                break;
            }
        }
        _ = int.TryParse(temp, out int result);
        return result;
    }

    private static readonly string[] TruePattern = ["yes", "y", "true", "t", "1"];
    public static bool CanUseSayCommand(string friendCode)
    {
        var folderPaths = Directory.GetDirectories(TAGS_FILE_PATH)
            .Where(folder => !TagFolders.Contains(Path.GetFileName(folder)))
            .Concat([TAGS_FILE_PATH])
            .ToList();

        var filePath = folderPaths
            .Select(folder => Path.Combine(folder, $"{friendCode}.txt"))
            .FirstOrDefault(File.Exists);

        if (filePath == null) return false;

        string temp = "";
        var searchTarget = "SayCommandAccess:";
        foreach (var line in File.ReadLines(filePath))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("SayCommandAccess:").Skip(1).First().Trim().TrimEnd().ToLower();
                break;
            }
        }
        if (TruePattern.Any(c => temp.Contains(c))) return true;
        return false;
    }

    public static bool CanUseEndCommand(string friendCode)
    {
        var folderPaths = Directory.GetDirectories(TAGS_FILE_PATH)
            .Where(folder => !TagFolders.Contains(Path.GetFileName(folder)))
            .Concat([TAGS_FILE_PATH])
            .ToList();

        var filePath = folderPaths
            .Select(folder => Path.Combine(folder, $"{friendCode}.txt"))
            .FirstOrDefault(File.Exists);

        if (filePath == null) return false;

        string temp = "";
        var searchTarget = "EndCommandAccess:";
        foreach (var line in File.ReadLines(filePath))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("EndCommandAccess:").Skip(1).First().Trim().TrimEnd().ToLower();
                break;
            }
        }
        if (TruePattern.Any(c => temp.Contains(c))) return true;
        return false;
    }

    public static bool CanUseExecuteCommand(string friendCode)
    {
        var folderPaths = Directory.GetDirectories(TAGS_FILE_PATH)
            .Where(folder => !TagFolders.Contains(Path.GetFileName(folder)))
            .Concat([TAGS_FILE_PATH])
            .ToList();

        var filePath = folderPaths
            .Select(folder => Path.Combine(folder, $"{friendCode}.txt"))
            .FirstOrDefault(File.Exists);

        if (filePath == null) return false;

        string temp = "";
        var searchTarget = "ExecuteCommandAccess:";
        foreach (var line in File.ReadLines(filePath))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("ExecuteCommandAccess:").Skip(1).First().Trim().TrimEnd().ToLower();
                break;
            }
        }
        if (TruePattern.Any(c => temp.Contains(c))) return true;
        return false;
    }

    public static bool AssignGameMaster(string friendCode)
    {
        var folderPaths = Directory.GetDirectories(TAGS_FILE_PATH)
            .Where(folder => !TagFolders.Contains(Path.GetFileName(folder)))
            .Concat([TAGS_FILE_PATH])
            .ToList();

        var filePath = folderPaths
            .Select(folder => Path.Combine(folder, $"{friendCode}.txt"))
            .FirstOrDefault(File.Exists);

        if (filePath == null) return false;

        string temp = "";
        var searchTarget = "AssignGameMaster:";
        foreach (var line in File.ReadLines(filePath))
        {
            if (line.Contains(searchTarget))
            {
                temp = line.Split("AssignGameMaster:").Skip(1).First().Trim().TrimEnd().ToLower();
                break;
            }
        }
        if (TruePattern.Any(c => temp.Contains(c))) return true;
        return false;
    }
}
