using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
namespace TOHE;

public class dbConnect
{
    public static async Task<Dictionary<string, object>> GetUserInfoByFriendCode(string friendCode)
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
                return null;
            }
        }
        using (var httpClient = new HttpClient())
        {
            string encodedFriendCode = Uri.EscapeDataString(friendCode);
            string apiUrl = "https://api.tohre.dev"; // Replace with your actual API URL
            string endpoint = $"{apiUrl}/userInfo?token={apiToken}&friendcode={encodedFriendCode}";

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
        return true;
    }
    public static bool CanAccessDev(string friendCode)
    {
        return true;
    }
}