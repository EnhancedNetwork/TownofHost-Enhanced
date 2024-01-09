using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.Reflection;
using static TOHE.Translator;
using System.Threading.Tasks;

namespace TOHE;

public class dbConnect
{
    private static Dictionary<string, string> userType = new();

    public static async void Init()
    {
        Logger.Info("Begin dbConnect Login flow", "dbConnect.init");
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
        Logger.Info("Finished flow.", "dbConnect.init");
    }
    private static string getToken()
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
                using (StreamReader reader = new StreamReader(stream))
                {
                    // Read the content of the .env file
                    string content = reader.ReadToEnd();

                    // Process the content as needed
                    apiToken = content.Replace("API_TOKEN=", string.Empty).Trim();
                }
            }
            if (stream == null || apiToken == "")
            {
                Logger.Warn("Embedded resource not found.", "apiToken");
            }
        }
        return apiToken;
    }
    private static void GetRoleTable()
    {
        userType = new();
        string apiToken = getToken();
        if (apiToken == "")
        {
            Logger.Warn("Embedded resource not found.", "apiToken");
            return;
        }
        using (var httpClient = new HttpClient())
        {
            string apiUrl = "https://api.tohre.dev"; // Replace with your actual API URL
            string endpoint = $"{apiUrl}/userInfo?token={apiToken}";

            try
            {
                var response = httpClient.GetAsync(endpoint).Result;

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
                                userType[user["friendcode"].ToString()] = user["type"].ToString();
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            // If deserialization as a list fails, try deserializing as a single JSON object
                            Logger.Error($"Error deserializing JSON: {jsonEx.Message}", "dbConnect");
                            return;
                        }
                    }
                }
                else
                {
                    Logger.Error($"Error in fetching the User List, Success status code is false", "dbConnect");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"error: {ex}", "dbConnect");
                return;
            }
        }
    }

    public static string ToAutoTranslate(System.Text.Json.JsonElement tag)
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
    private static void GetEACList()
    {
        string apiToken = getToken();
        if (apiToken == "")
        {
            Logger.Warn("Embedded resource not found.", "apiToken");
            return;
        }
        using (var httpClient = new HttpClient())
        {
            string apiUrl = "https://api.tohre.dev"; // Replace with your actual API URL
            string endpoint = $"{apiUrl}/eac?token={apiToken}";

            try
            {
                var response = httpClient.GetAsync(endpoint).Result;

                if (response.IsSuccessStatusCode)
                {
                    using (var responseStream = response.Content.ReadAsStreamAsync().Result)
                    {
                        try
                        {
                            BanManager.EACDict = JsonSerializer.DeserializeAsync<List<Dictionary<string, JsonElement>>>(responseStream).Result;
                        }
                        catch (JsonException jsonEx)
                        {
                            // If deserialization as a list fails, try deserializing as a single JSON object
                            Logger.Error($"Error deserializing JSON: {jsonEx.Message}", "GetEACList");
                            return;
                        }
                    }
                }
                else
                {
                    Logger.Error($"Error in fetching the EAC List, Success status code is false", "GetEACList");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"error: {ex}", "GetEACList");
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

