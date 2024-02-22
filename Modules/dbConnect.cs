using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.Reflection;
using static TOHE.Translator;
using System.Threading.Tasks;
using System.Linq;
using AmongUs.Data;
using Epic.OnlineServices;

namespace TOHE;

public class dbConnect
{
    private static Dictionary<string, string> userType = [];
    public static bool InitOnce = false;
    public static async void Init()
    {
        Logger.Info("Begin dbConnect Login flow", "dbConnect.init");
        if (!InitOnce)
        {
            try
            {
                GetRoleTable();
            }
            catch (Exception Ex)
            {
                Logger.Error($"Error in fetching roletable {Ex}", "dbConnect.init");
                goto firstFailure;
            }
            try
            {
                GetEACList();
            }
            catch (Exception Ex)
            {
                Logger.Error($"Error in fetching eaclist {Ex}", "dbConnect.init");
                goto firstFailure;
            }
        }
        else //init once, following will be async
        {
            try
            {
                await Task.Run(() => GetRoleTable());
            }
            catch (Exception Ex)
            {
                Logger.Error($"Error in fetching roletable {Ex}", "dbConnect.init");
            }
            try
            {
                await Task.Run(() => GetEACList());
            }
            catch (Exception Ex)
            {
                Logger.Error($"Error in fetching eaclist {Ex}", "dbConnect.init");
            }
        }

        if (!ErrorExists())
        {
            if (!InitOnce)
            {
                Logger.Info("Finished first init flow.", "dbConnect.init");
                InitOnce = true;
            }
            else
            {
                Logger.Info("Finished Sync flow.", "dbConnect.init");
            }
        }
        else
        {
            if (!InitOnce)
            {
                Logger.Error("first init flow failed by Logic!", "dbConnect.init");
                InitOnce = false;
                goto firstFailure;
            }
            else
            {
                Logger.Error("Sync flow failed by Logic!", "dbConnect.init");
            }
        }

        if (EOSManager.Instance.friendCode != null && EOSManager.Instance.friendCode != "")
        {
            if (Main.devRelease && !CanAccessDev(EOSManager.Instance.friendCode))
                Main.hasAccess = false;
        }
        else
        {
            DestroyableSingleton<DisconnectPopup>.Instance.ShowCustom(GetString("dbConnect.nullFriendCode"));
            DestroyableSingleton<EOSManager>.Instance.loginFlowFinished = false;
        }
        return;

    firstFailure:
        if (AmongUsClient.Instance.mode != InnerNet.MatchMakerModes.None)
            AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);

        DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.Offline;
        DataManager.Player.Save();
        DestroyableSingleton<DisconnectPopup>.Instance.ShowCustom(GetString("dbConnect.InitFailure"));
    }
    private static string GetToken()
    {
        string apiToken = "";
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Specify the full name of the embedded resource
        string resourceName = "TOHE.token.env";
        /*
         make a token.env file in the root folder and add `API_TOKEN=your_api_token_here`
        for example :- API_TOKEN=1234567890
         */

        // Read the embedded resource
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                using (StreamReader reader = new(stream))
                {
                    // Read the content of the .env file
                    string content = reader.ReadToEnd();

                    // Process the content as needed
                    apiToken = content.Replace("API_TOKEN=", string.Empty).Trim();
                }
            }
            if (stream == null || apiToken == "")
            {
                Logger.Warn("Embedded resource not found.", "apiToken.error");
            }
        }
        return apiToken;
    }
    private static void GetRoleTable()
    {
        var tempUserType = new Dictionary<string, string>(); // Create a temporary dictionary
        string apiToken = GetToken();
        if (apiToken == "")
        {
            Logger.Warn("Embedded resource not found.", "GetRoleTable.error");
            return;
        }
        using (var httpClient = new HttpClient())
        {
            string apiUrl = "https://api.tohre.dev"; // Replace with your actual API URL
            string endpoint = $"{apiUrl}/userInfo?token={apiToken}";

            try
            {
                using (var response = httpClient.GetAsync(endpoint).Result) // Move the using statement inside the try block
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var responseStream = response.Content.ReadAsStreamAsync().Result)
                        {
                            try
                            {
                                var userList = JsonSerializer.DeserializeAsync<List<Dictionary<string, JsonElement>>>(responseStream).Result;
                                foreach (var user in userList)
                                {
                                    if (!DevManager.IsDevUser(user["friendcode"].ToString()))
                                    {
                                        DevManager.DevUserList.Add(new(
                                            code: user["friendcode"].ToString(),
                                            color: user["color"].ToString(),
                                            tag: ToAutoTranslate(user["overhead_tag"]),
                                            isUp: user["isUP"].GetInt32() == 1,
                                            isDev: user["isDev"].GetInt32() == 1,
                                            deBug: user["debug"].GetInt32() == 1,
                                            colorCmd: user["colorCmd"].GetInt32() == 1,
                                            upName: user["name"].ToString()));

                                    }
                                    tempUserType[user["friendcode"].ToString()] = user["type"].ToString(); // Store the data in the temporary dictionary
                                }
                                if (tempUserType.Count > 1)
                                    userType = tempUserType; // Replace userType with the temporary dictionary
                                else if (!InitOnce)
                                {
                                    Logger.Error($"Incoming RoleTable is null, cannot init!", "GetRoleTable.error");
                                }
                                return;
                            }
                            catch (JsonException jsonEx)
                            {
                                // If deserialization as a list fails, try deserializing as a single JSON object
                                Logger.Error($"Error deserializing JSON: {jsonEx.Message}", "GetRoleTable.error");
                                return;
                            }
                        }
                    }
                    else
                    {
                        Logger.Error($"Error in fetching the User List, Success status code is false", "GetRoleTable.error");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"error: {ex}", "GetRoleTable.error");
                return;
            }
        }
    }

    public static string ToAutoTranslate(JsonElement tag)
    {
        //Translates the mostly used tags.
        string text = tag.ToString();
        switch (text)
        {
            case "Contributor":
                text = GetString("Contributor");
                break;
            case "Translator":
                text = GetString("Translator");
                break;
            case "Sponsor":
                text = GetString("Sponsor");
                break;
        }
        return text;
    }
    public static bool IsBooster(string friendcode)
    {
        if (!userType.ContainsKey(friendcode)) return false;
        return userType[friendcode] == "s_bo";
    }

    public static bool ErrorExists()
    {
        // Simple Logic and Works
        bool apiTokenError = GetToken() is "" or null;
        bool RoleTableError = userType.Count < 1;
        bool EacListError = BanManager.EACDict.Count < 1;

        return apiTokenError || RoleTableError || EacListError;
    }
    private static void GetEACList()
    {
        string apiToken = GetToken();
        if (apiToken == "")
        {
            Logger.Warn("Embedded resource not found.", "GetEACList.error");
            return;
        }
        using (var httpClient = new HttpClient())
        {
            string apiUrl = "https://api.tohre.dev"; // Replace with your actual API URL
            string endpoint = $"{apiUrl}/eac?token={apiToken}";

            try
            {
                using (var response = httpClient.GetAsync(endpoint).Result) // Move the using statement inside the try block
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var responseStream = response.Content.ReadAsStreamAsync().Result)
                        {
                            try
                            {
                                List<Dictionary<string, JsonElement>> tempEACDict = JsonSerializer.DeserializeAsync<List<Dictionary<string, JsonElement>>>(responseStream).Result;
                                BanManager.EACDict = BanManager.EACDict.Concat(tempEACDict).ToList(); // Merge the temporary list with BanManager.EACDict
                                return;
                            }
                            catch (JsonException jsonEx)
                            {
                                // If deserialization as a list fails, try deserializing as a single JSON object
                                Logger.Error($"Error deserializing JSON: {jsonEx.Message}", "GetEACList.error");
                                return;
                            }
                        }
                    }
                    else
                    {
                        Logger.Error($"Error in fetching the EAC List, Success status code is false", "GetEACList.error");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"error: {ex}", "GetEACList.error");
                return;
            }
        }
    }

    public static bool CanAccessDev(string friendCode)
    {
        if (!userType.ContainsKey(friendCode))
        {
            Logger.Error($"no user found, with friendcode {friendCode}", "CanAccessDev");
            return false;
        }

        if (userType[friendCode] == "s_bo" || userType[friendCode] == "s_it" || userType[friendCode].StartsWith("t_"))
        {
            Logger.Error($"Error : Dev access denied to user {friendCode}, type =  {userType[friendCode]}", "CanAccessDev");
            return false;
        }
        return true;
    }
}