using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
namespace TOHE;
public class dbConnect
{
    public static async Task<Dictionary<string, object>> GetUserInfoByFriendCode(string friendCode)
    {
        using (var httpClient = new HttpClient())
        {
            string encodedFriendCode = Uri.EscapeDataString(friendCode);
            string apiUrl = "https://api.tohre.dev"; // Replace with your actual API URL
            string endpoint = $"{apiUrl}/userInfo?token=1234&friendcode={encodedFriendCode}";

            try
            {
                var response = await httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        return await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(responseStream);
                    }
                }
                else
                {
                    Logger.Error($"No user found with friendcode : {friendCode}", "dbConnect");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"error: {ex}", "dbConnect");
                return null;
            }
        }
    }

    public static bool CanAccessCanary(string friendCode)
    {
        var userInfoTask = GetUserInfoByFriendCode(friendCode);
        userInfoTask.Wait(); // Wait for the task to complete
        var userInfo = userInfoTask.Result; // Get the result

        if (userInfo == null)
        {
            Logger.Error($"no user found, {userInfo}", "CanAccessCanary");
            return false;
        }

        if (userInfo.ContainsKey("error"))
        { 
            Logger.Error($"Error = {userInfo["error"]}", "CanAccessCanary");
            Logger.Warn($"No user found with friendcode : {friendCode}", "dbConnect");
            return false;
        }
        // Customize this condition based on the actual structure of your user info
        return true;
    }
    public static bool CanAccessDev(string friendCode)
    {
        var userInfoTask = GetUserInfoByFriendCode(friendCode);
        userInfoTask.Wait(); // Wait for the task to complete
        var userInfo = userInfoTask.Result; // Get the result

        if (userInfo == null)
        {
            Logger.Error($"no user found, {userInfo}", "CanAccessDev");
            return false;
        }

        if (userInfo.ContainsKey("error") || !userInfo.ContainsKey("type"))
        {
            Logger.Error($"Error = {userInfo["error"]}", "CanAccessDev");
            Logger.Warn($"No user found with friendcode : {friendCode}", "dbConnect");
            return false;
        }
        string userType = userInfo["type"].ToString();
        if (userType == "s_bo" || userType == "s_it" || userType.StartsWith("t_"))
        {
            Logger.Error($"Error : Dev access denied to user {friendCode}, type =  {userType}", "CanAccessDev");
            return false;
        }
        // Customize this condition based on the actual structure of your user info
        return true;
    }
}

