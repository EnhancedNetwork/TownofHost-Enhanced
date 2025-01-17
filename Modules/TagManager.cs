using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace TOHE
{
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
                // Ensure the main "Tags" directory exists
                if (!Directory.Exists(@"TOHE-DATA/Tags"))
                    Directory.CreateDirectory(@"TOHE-DATA/Tags");

                // Ensure subfolders (1-5) exist for permission levels
                for (int i = 1; i <= 5; i++)
                {
                    string folderPath = @$"TOHE-DATA/Tags/{i}";
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath); // Create folder if it doesn't exist
                    }
                }

                var defaultTagMsg = GetResourcesTxt($"TOHE.Resources.Config.TagTemplate.txt");
                if (!File.Exists(@"./TOHE-DATA/Tags/Tag_Template.txt")) //default tag
                {
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

        public static bool CheckFriendCode(string friendCode, bool log = false)
        {
            // Check if the file exists in any of the permission folders (1-5)
            var folderPath = Directory.GetDirectories(TAGS_FILE_PATH)
                .FirstOrDefault(dir => File.Exists($@"{dir}/{friendCode}.txt"));

            if (folderPath == null)
            {
                if (log)
                    Logger.Info($"{friendCode} does not have a tag", "TagManager");
                return false;
            }

            return true;
        }

        public static string ReadTagName(string friendCode)
        {
            var folderPath = Directory.GetDirectories(TAGS_FILE_PATH)
                .FirstOrDefault(dir => File.Exists($@"{dir}/{friendCode}.txt"));

            if (folderPath == null) return string.Empty;

            string fileName = @$"{folderPath}/{friendCode}.txt";
            string temp = "";
            var searchTarget = "TagName:";
            foreach (var line in File.ReadLines(fileName))
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
            var folderPath = Directory.GetDirectories(TAGS_FILE_PATH)
                .FirstOrDefault(dir => File.Exists($@"{dir}/{friendCode}.txt"));

            if (folderPath == null) return string.Empty;

            string fileName = @$"{folderPath}/{friendCode}.txt";
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

        // Method to retrieve permission level based on folder
        public static int GetPermissionLevel(string friendCode)
        {
            var folderPath = Directory.GetDirectories(TAGS_FILE_PATH)
                .FirstOrDefault(dir => File.Exists($@"{dir}/{friendCode}.txt"));

            if (folderPath == null) return -1;

            // The folder name will represent the permission level (e.g., folder "5" means Permission Level 5)
            var folderName = Path.GetFileName(folderPath);
            int.TryParse(folderName, out int permissionLevel);
            return permissionLevel;
        }

        public static bool CanUseSayCommand(string friendCode)
        {
            var folderPath = Directory.GetDirectories(TAGS_FILE_PATH)
                .FirstOrDefault(dir => File.Exists($@"{dir}/{friendCode}.txt"));

            if (folderPath == null) return false;

            string fileName = @$"{folderPath}/{friendCode}.txt";
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

        public static bool CanUseEndCommand(string friendCode)
        {
            var folderPath = Directory.GetDirectories(TAGS_FILE_PATH)
                .FirstOrDefault(dir => File.Exists($@"{dir}/{friendCode}.txt"));

            if (folderPath == null) return false;

            string fileName = @$"{folderPath}/{friendCode}.txt";
            string temp = "";
            var searchTarget = "EndCommandAccess:";
            foreach (var line in File.ReadLines(fileName))
            {
                if (line.Contains(searchTarget))
                {
                    temp = line.Split("EndCommandAccess:").Skip(1).First().Trim().TrimEnd().ToLower();
                    break;
                }
            }

            if (new[] { "yes", "y", "true", "t", "1" }.Any(c => temp.Contains(c))) return true;
            return false;
        }

        public static bool CanUseExecuteCommand(string friendCode)
        {
            var folderPath = Directory.GetDirectories(TAGS_FILE_PATH)
                .FirstOrDefault(dir => File.Exists($@"{dir}/{friendCode}.txt"));

            if (folderPath == null) return false;

            string fileName = @$"{folderPath}/{friendCode}.txt";
            string temp = "";
            var searchTarget = "ExecuteCommandAccess:";
            foreach (var line in File.ReadLines(fileName))
            {
                if (line.Contains(searchTarget))
                {
                    temp = line.Split("ExecuteCommandAccess:").Skip(1).First().Trim().TrimEnd().ToLower();
                    break;
                }
            }

            if (new[] { "yes", "y", "true", "t", "1" }.Any(c => temp.Contains(c))) return true;
            return false;
        }
    }
}