using AmongUs.Data;
using System;
using System.IO;
using System.Text.Json;
using UnityEngine.Networking;
using static TOHE.Translator;
using IEnumerator = System.Collections.IEnumerator;

namespace TOHE;

public class dbConnect
{
    private static bool InitOnce = false;
    private static Dictionary<string, string> UserType = [];

    private const string ApiUrl = "https://api.weareten.ca";
    private const string FallBackUrl = "https://tohe.niko233.me"; // Mirror of Enhanced Api

    public static IEnumerator Init()
    {
        Logger.Info("Begin dbConnect Login flow", "dbConnect.init");

        if (!InitOnce)
        {
            yield return GetRoleTable();

            if (!(Main.devRelease || Main.canaryRelease || Main.fullRelease))
            {
                HandleFailure(FailedConnectReason.Build_Not_Specified);
                yield break;
            }

            if (GetToken() is "" or null)
            {
                HandleFailure(FailedConnectReason.API_Token_Is_Empty);
                yield break;
            }

            if (UserType.Count < 1)
            {
                HandleFailure(FailedConnectReason.Error_Getting_User_Role_Table);
                yield break;
            }

            yield return GetEACList();
            if (BanManager.EACDict.Count < 1)
            {
                HandleFailure(FailedConnectReason.Error_Getting_EAC_List);
                yield break;
            }
        }
        else
        {
            yield return GetRoleTable();
            yield return GetEACList();
        }

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

    private static void HandleFailure(FailedConnectReason errorReason)
    {
        var errorMessage = errorReason switch
        {
            FailedConnectReason.Build_Not_Specified => "Build not specified",
            FailedConnectReason.API_Token_Is_Empty => "API token is empty",
            FailedConnectReason.Error_Getting_User_Role_Table => "Error in fetching roletable",
            FailedConnectReason.Error_Getting_EAC_List => "Error in fetching EAC list",
            _ => "Reason not specified"
        };

        Logger.Error(errorMessage, "dbConnect.init");

        bool shouldDisconnect;
        if (Main.devRelease)
        {
            // is dev build
            shouldDisconnect = true;
        }
        else if (Main.canaryRelease || Main.fullRelease)
        {
            shouldDisconnect = false;

            // Show waring message
            if (GameStates.IsLobby || GameStates.IsInGame)
            {
                DestroyableSingleton<HudManager>.Instance.ShowPopUp(GetString("dbConnect.InitFailurePublic"));
            }
            else
            {
                DestroyableSingleton<DisconnectPopup>.Instance.ShowCustom(GetString("dbConnect.InitFailurePublic"));
            }
        }
        else
        {
            // Build not found
            shouldDisconnect = true;
        }

        if (shouldDisconnect)
        {
            if (AmongUsClient.Instance.mode != InnerNet.MatchMakerModes.None)
                AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);

            DataManager.Player.Account.LoginStatus = EOSManager.AccountLoginStatus.Offline;
            DataManager.Player.Save();
            DestroyableSingleton<DisconnectPopup>.Instance.ShowCustom(GetString("dbConnect.InitFailure"));
        }
    }

    private static string decidedApiToken = "";
    private static string GetToken()
    {
        if (decidedApiToken != "")
            return decidedApiToken;

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
                using StreamReader reader = new(stream);
                // Read the content of the .env file
                string content = reader.ReadToEnd();

                // Process the content as needed
                apiToken = content.Replace("API_TOKEN=", string.Empty).Trim();
            }
        }

        // Check if the token contains spaces or is empty
        if (string.IsNullOrWhiteSpace(apiToken) || apiToken.Contains(' '))
        {
            Logger.Info("No api token provided in token.env", "db.Connect");
            if (!string.IsNullOrEmpty(Main.FileHash) && Main.FileHash.Length >= 16)
            {
                string prefix = Main.FileHash.Substring(0, 8);
                string suffix = Main.FileHash.Substring(Main.FileHash.Length - 8, 8);
                apiToken = $"hash{prefix}{suffix}";
            }
            else
            {
                Logger.Info("Main.FileHash is not valid for generating token.", "db.Connect");
                return "";
            }
        }

        decidedApiToken = apiToken;
        return apiToken;
    }

    private static IEnumerator GetRoleTable()
    {
        var tempUserType = new Dictionary<string, string>(); // Create a temporary dictionary
        string apiToken = GetToken();
        if (apiToken == "")
        {
            Logger.Warn("Embedded resource not found.", "GetRoleTable.error");
            yield return null;
        }

        string[] apiUrls = [ApiUrl, FallBackUrl];
        int maxAttempts = !InitOnce ? 4 : 2;
        int attempt = 0;
        bool success = false;

        while (attempt < maxAttempts && !success)
        {
            string apiUrl = apiUrls[attempt % 2];
            string endpoint = $"{apiUrl}/userInfo?token={apiToken}";

            UnityWebRequest webRequest = UnityWebRequest.Get(endpoint);
            Logger.Info($"Fetching UserInfo from {apiUrls[attempt % 2]}", "GetRoleTable");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var userList = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(webRequest.downloadHandler.text);
                    foreach (var user in userList)
                    {
                        var userData = user;
                        if (!DevManager.IsDevUser(userData["friendcode"].ToString()))
                        {
                            DevManager.DevUserList.Add(new(
                                code: userData["friendcode"].ToString(),
                                color: userData["color"].ToString(),
                                tag: ToAutoTranslate(userData["overhead_tag"]),
                                userType: userData["type"].ToString(),
                                isUp: userData["isUP"].GetInt32() == 1,
                                isDev: userData["isDev"].GetInt32() == 1,
                                deBug: userData["debug"].GetInt32() == 1,
                                colorCmd: userData["colorCmd"].GetInt32() == 1,
                                upName: userData["name"].ToString()));
                        }
                        tempUserType[userData["friendcode"].ToString()] = userData["type"].ToString(); // Store the data in the temporary dictionary
                    }
                    if (tempUserType.Count > 1)
                    {
                        UserType = tempUserType; // Replace userType with the temporary dictionary
                        success = true;
                    }
                    else if (!InitOnce)
                    {
                        Logger.Error($"Incoming RoleTable is null, cannot init!", "GetRoleTable.error");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error processing response: {ex.Message}", "GetRoleTable.error");
                }
                finally
                {
                    webRequest.Dispose();
                }
            }
            else
            {
                Logger.Error($"Error in fetching the User List: {webRequest.error}", "GetRoleTable.error");
            }

            attempt++;
        }

        if (!success)
        {
            Logger.Error("Failed to fetch User List from both primary and fallback URLs.", "GetRoleTable.error");
        }
    }


    private static string ToAutoTranslate(JsonElement tag)
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
        if (!UserType.ContainsKey(friendcode)) return false;
        return UserType[friendcode] == "s_bo";
    }

    private static IEnumerator GetEACList()
    {
        string apiToken = GetToken();
        if (apiToken == "")
        {
            Logger.Warn("Embedded resource not found.", "GetEACList.error");
            yield break;
        }

        string[] apiUrls = { ApiUrl, FallBackUrl };
        int maxAttempts = !InitOnce ? 4 : 2;
        int attempt = 0;
        bool success = false;

        while (attempt < maxAttempts && !success)
        {
            string apiUrl = apiUrls[attempt % 2];
            string endpoint = $"{apiUrl}/eac?token={apiToken}&hash={Main.FileHash}";

            Logger.Info($"Fetching EAC List from {apiUrls[attempt % 2]}", "GetEACList");
            UnityWebRequest webRequest = UnityWebRequest.Get(endpoint);

            // Send the request
            yield return webRequest.SendWebRequest();

            // Check for errors
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var tempEACDict = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(webRequest.downloadHandler.text);
                    BanManager.EACDict = [.. BanManager.EACDict, .. tempEACDict]; // Merge the temporary list with BanManager.EACDict
                    success = true;
                }
                catch (JsonException jsonEx)
                {
                    // If deserialization fails
                    Logger.Error($"Error deserializing JSON: {jsonEx.Message}", "GetEACList.error");
                }
                finally
                {
                    webRequest.Dispose();
                }
            }
            else
            {
                Logger.Error($"Error in fetching the EAC List: {webRequest.error}", "GetEACList.error");
            }

            attempt++;
        }

        if (!success)
        {
            Logger.Error("Failed to fetch EAC List from both primary and fallback URLs.", "GetEACList.error");
        }
    }

    private static bool CanAccessDev(string friendCode)
    {
        if (!UserType.ContainsKey(friendCode))
        {
            Logger.Error($"no user found, with friendcode {friendCode}", "CanAccessDev");
            return false;
        }

        if (UserType[friendCode] == "s_bo" || UserType[friendCode] == "s_it" || UserType[friendCode].StartsWith("t_"))
        {
            Logger.Error($"Error : Dev access denied to user {friendCode}, type =  {UserType[friendCode]}", "CanAccessDev");
            return false;
        }
        return true;
    }

    [Obfuscation(Exclude = true)]
    private enum FailedConnectReason
    {
        Build_Not_Specified,
        API_Token_Is_Empty,
        Error_Getting_User_Role_Table,
        Error_Getting_EAC_List,
    }
}
