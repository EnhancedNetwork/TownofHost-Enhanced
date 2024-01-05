using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TOHE;

public static class Translator
{
    public static Dictionary<string, Dictionary<int, string>> translateMaps;
    public const string LANGUAGE_FOLDER_NAME = "Language";
    public static void Init()
    {
        Logger.Info("加载语言文件...", "Translator");
        LoadLangs();
        Logger.Info("加载语言文件成功", "Translator");
    }
    public static void LoadLangs()
    {
        try
        {
            // Get the directory containing the JSON files (e.g., TOHE.Resources.Lang)
            string jsonDirectory = "TOHE.Resources.Lang";
            // Get the assembly containing the resources
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string[] jsonFileNames = GetJsonFileNames(assembly, jsonDirectory);

            translateMaps = new Dictionary<string, Dictionary<int, string>>();


            if (jsonFileNames.Length == 0)
            {
                Logger.Warn("Json Translation files does not exist.", "Translator");
                return;
            }
            foreach (string jsonFileName in jsonFileNames)
            {
                // Read the JSON file content
                using (Stream resourceStream = assembly.GetManifestResourceStream(jsonFileName))
                {
                    if (resourceStream != null)
                    {
                        using (StreamReader reader = new StreamReader(resourceStream))
                        {
                            string jsonContent = reader.ReadToEnd();

                            // Deserialize the JSON into a dictionary
                            Dictionary<string, string> jsonDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                            if (jsonDictionary.TryGetValue("LanguageID", out string languageIdObj) && int.TryParse(languageIdObj, out int languageId))
                            {
                                // Remove the "LanguageID" entry
                                jsonDictionary.Remove("LanguageID");

                                // Handle the rest of the data and merge it into the resulting translation map
                                MergeJsonIntoTranslationMap(translateMaps, languageId, jsonDictionary);
                            }
                            else
                            {
                                //Logger.Warn(jsonDictionary["HostText"], "Translator");
                                Logger.Warn($"Invalid JSON format in {jsonFileName}: Missing or invalid 'LanguageID' field.", "Translator");
                            }

                        }
                    }
                }
            }

            // Convert the resulting translation map to JSON
            string mergedJson = JsonSerializer.Serialize(translateMaps, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}", "Translator");
        }
        //カスタム翻訳ファイルの読み込み
        if (!Directory.Exists(LANGUAGE_FOLDER_NAME)) Directory.CreateDirectory(LANGUAGE_FOLDER_NAME);

        // 翻訳テンプレートの作成
        CreateTemplateFile();
        foreach (var lang in EnumHelper.GetAllValues<SupportedLangs>())
        {
            if (File.Exists(@$"./{LANGUAGE_FOLDER_NAME}/{lang}.dat"))
            {
                UpdateCustomTranslation($"{lang}.dat", lang);
                LoadCustomTranslation($"{lang}.dat", lang);
            }
        }
    }
    static void MergeJsonIntoTranslationMap(Dictionary<string, Dictionary<int, string>> translationMaps, int languageId, Dictionary<string, string> jsonDictionary)
    {
        foreach (var kvp in jsonDictionary)
        {
            string textString = kvp.Key;
            if (kvp.Value is string translation)
            {

                // If the textString is not already in the translation map, add it
                if (!translationMaps.ContainsKey(textString))
                {
                    translationMaps[textString] = new Dictionary<int, string>();
                }

                // Add or update the translation for the current id and textString
                translationMaps[textString][languageId] = translation.Replace("\\n", "\n").Replace("\\r", "\r");
            }
        }
    }

    // Function to get a list of JSON file names in a directory
    static string[] GetJsonFileNames(System.Reflection.Assembly assembly, string directoryName)
    {
        string[] resourceNames = assembly.GetManifestResourceNames();
        return resourceNames.Where(resourceName => resourceName.StartsWith(directoryName) && resourceName.EndsWith(".json")).ToArray();
    }

    //public static void LoadLangs()
    //{
    //    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
    //    var stream = assembly.GetManifestResourceStream("TOHE.Resources.String.csv");
    //    translateMaps = new Dictionary<string, Dictionary<int, string>>();

    //    var options = new CsvOptions()
    //    {
    //        HeaderMode = HeaderMode.HeaderPresent,
    //        AllowNewLineInEnclosedFieldValues = false,
    //    };
    //    foreach (var line in CsvReader.ReadFromStream(stream, options))
    //    {
    //        if (line.Values[0][0] == '#') continue;
    //        try
    //        {
    //            Dictionary<int, string> dic = new();
    //            for (int i = 1; i < line.ColumnCount; i++)
    //            {
    //                int id = int.Parse(line.Headers[i]);
    //                dic[id] = line.Values[i].Replace("\\n", "\n").Replace("\\r", "\r");
    //            }
    //            if (!translateMaps.TryAdd(line.Values[0], dic))
    //                Logger.Warn($"待翻译的 CSV 文件中存在重复项：第{line.Index}行 => \"{line.Values[0]}\"", "Translator");
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Warn($"翻译文件错误：第{line.Index}行 => \"{line.Values[0]}\"", "Translator");
    //            Logger.Warn(ex.ToString(), "Translator");
    //        }
    //    }

    //    // カスタム翻訳ファイルの読み込み
    //    if (!Directory.Exists(LANGUAGE_FOLDER_NAME)) Directory.CreateDirectory(LANGUAGE_FOLDER_NAME);

    //    // 翻訳テンプレートの作成
    //    CreateTemplateFile();
    //    foreach (var lang in Enum.GetValues(typeof(SupportedLangs)))
    //    {
    //        if (File.Exists(@$"./{LANGUAGE_FOLDER_NAME}/{lang}.dat"))
    //            LoadCustomTranslation($"{lang}.dat", (SupportedLangs)lang);
    //    }
    //}

    public static string GetString(string s, Dictionary<string, string> replacementDic = null, bool console = false, bool showInvalid = true)
    {
        var langId = TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.English;
        if (console) langId = SupportedLangs.English;
        if (Main.ForceOwnLanguage.Value) langId = GetUserTrueLang();
        string str = GetString(s, langId, showInvalid);
        if (replacementDic != null)
            foreach (var rd in replacementDic)
            {
                str = str.Replace(rd.Key, rd.Value);
            }
        return str;
    }

    public static string GetString(string str, SupportedLangs langId, bool showInvalid = true)
    {
        var res = showInvalid ? $"<INVALID:{str}>" : str;
        try
        {
            if (translateMaps.TryGetValue(str, out var dic) && (!dic.TryGetValue((int)langId, out res) || res == "" || (langId is not SupportedLangs.SChinese and not SupportedLangs.TChinese && Regex.IsMatch(res, @"[\u4e00-\u9fa5]") && res == GetString(str, SupportedLangs.SChinese)))) //strに該当する&無効なlangIdかresが空
            {
                if (langId == SupportedLangs.English) res = $"*{str}";
                else res = GetString(str, SupportedLangs.English);
            }
            if (!translateMaps.ContainsKey(str)) //translateMapsにない場合、StringNamesにあれば取得する
            {
                var stringNames = EnumHelper.GetAllValues<StringNames>().Where(x => x.ToString() == str).ToArray();
                if (stringNames != null && stringNames.Length > 0)
                    res = GetString(stringNames.FirstOrDefault());
            }
        }
        catch (Exception Ex)
        {
            Logger.Fatal($"Error oucured at [{str}] in String.csv", "Translator");
            Logger.Error("Here was the error:\n" + Ex.ToString(), "Translator");
        }
        return res;
    }
    public static string GetString(StringNames stringName)
        => DestroyableSingleton<TranslationController>.Instance.GetString(stringName, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
    public static string GetRoleString(string str, bool forUser = true)
    {
        var CurrentLanguage = TranslationController.Instance.currentLanguage.languageID;
        var lang = forUser ? CurrentLanguage : SupportedLangs.English;
        if (Main.ForceOwnLanguageRoleName.Value)
            lang = GetUserTrueLang();

        return GetString(str, lang);
    }
    public static SupportedLangs GetUserTrueLang()
    {
        try
        {
            var name = CultureInfo.CurrentUICulture.Name;
            if (name.StartsWith("en")) return SupportedLangs.English;
            if (name.StartsWith("zh_CHT")) return SupportedLangs.TChinese;
            if (name.StartsWith("zh")) return SupportedLangs.SChinese;
            if (name.StartsWith("ru")) return SupportedLangs.Russian;
            return TranslationController.Instance.currentLanguage.languageID;
        }
        catch
        {
            return SupportedLangs.English;
        }
    }
    static void UpdateCustomTranslation(string filename, SupportedLangs lang)
    {
        string path = @$"./{LANGUAGE_FOLDER_NAME}/{filename}";
        if (File.Exists(path))
        {
            Logger.Info("Updating Custom Translations", "UpdateCustomTranslation");
            try
            {
                List<string> textStrings = new();
                using (StreamReader reader = new(path, Encoding.GetEncoding("UTF-8")))
                { 
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Split the line by ':' to get the first part
                        string[] parts = line.Split(':');

                        // Check if there is at least one part before ':'
                        if (parts.Length >= 1)
                        {
                            // Trim any leading or trailing spaces and add it to the list
                            string textString = parts[0].Trim();
                            textStrings.Add(textString);
                        }
                    }
                }
                var sb = new StringBuilder();
                foreach (var templateString in translateMaps.Keys)
                {
                    if (!textStrings.Contains(templateString)) sb.Append($"{templateString}:\n");
                }
                using (FileStream fileStream = new FileStream(path, FileMode.Append, FileAccess.Write))
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    writer.WriteLine(sb.ToString());
                }

            }
            catch (Exception e)
            {
                Logger.Error("An error occurred: " + e.Message, "Translator");
            }
        }
    }
    public static void LoadCustomTranslation(string filename, SupportedLangs lang)
    {
        string path = @$"./{LANGUAGE_FOLDER_NAME}/{filename}";
        if (File.Exists(path))
        {
            Logger.Info($"加载自定义翻译文件：{filename}", "LoadCustomTranslation");
            using StreamReader sr = new(path, Encoding.GetEncoding("UTF-8"));
            string text;
            string[] tmp = Array.Empty<string>();
            while ((text = sr.ReadLine()) != null)
            {
                tmp = text.Split(":");
                if (tmp.Length > 1 && tmp[1] != "")
                {
                    try
                    {
                        translateMaps[tmp[0]][(int)lang] = tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n").Replace("\\r", "\r");
                    }
                    catch (KeyNotFoundException)
                    {
                        Logger.Warn($"无效密钥：{tmp[0]}", "LoadCustomTranslation");
                    }
                }
            }
        }
        else
        {
            Logger.Error($"找不到自定义翻译文件：{filename}", "LoadCustomTranslation");
        }
    }

    private static void CreateTemplateFile()
    {
        var sb = new StringBuilder();
        foreach (var title in translateMaps) sb.Append($"{title.Key}:\n");
        File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/template.dat", sb.ToString());
    }
    public static void ExportCustomTranslation()
    {
        LoadLangs();
        var sb = new StringBuilder();
        var lang = TranslationController.Instance.currentLanguage.languageID;
        foreach (var title in translateMaps)
        {
            if (!title.Value.TryGetValue((int)lang, out var text)) text = "";
            sb.Append($"{title.Key}:{text.Replace("\n", "\\n").Replace("\r", "\\r")}\n");
        }
        File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/export_{lang}.dat", sb.ToString());
    }
}