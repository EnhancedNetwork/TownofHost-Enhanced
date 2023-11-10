using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text.Json;
namespace TOHE;

public class Config
{
    public DatabaseConfig Database { get; set; }

    public static Config LoadConfig(string resourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourcePath = $"{assembly.GetName().Name}.{resourceName}";
        using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
        {
            if (stream == null)
            {
                throw new ArgumentException($"Resource {resourcePath} not found in assembly.");
            }

            using (StreamReader reader = new(stream))
            {
                string json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<Config>(json);
            }
        }
    }
}

public class DatabaseConfig
{
    public string Server { get; set; }
    public string DatabaseName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

class DBQueries
{
    public static void InsertData()
    {
        var config = Config.LoadConfig("dbConfig.json");
        /*
         1. Json file should be named `dbConfig.json` and should be present in ROOT FOLDER
         2. Ensure that `dbConfig.json` build action is set to Embed Resource
         3. Json file format :-
            {
              "Database": {
                "Server": "your-server",
                "DatabaseName": "your-database",
                "UserName": "your-username",
                "Password": "your-password"
              }
            }
         */
        var dbCon = DBConnection.Instance();
        dbCon.Server = config.Database.Server;
        dbCon.DatabaseName = config.Database.DatabaseName;
        dbCon.UserName = config.Database.UserName;
        dbCon.Password = config.Database.Password;


        if (dbCon.IsConnect())
        {
            try
            {
                // Define your INSERT INTO query
                string insertQuery = "INSERT INTO LoggedIn (friendcode, userName, version_number, gitLink, isDirty, time) " +
                                 "VALUES (@friendcode, @userName, @version_number, @gitLink, @isDirty, @time)";
                using (var cmd = new MySqlCommand(insertQuery, dbCon.Connection))
                {
                    // Set the values for the parameters in the query
                    cmd.Parameters.AddWithValue("@friendcode", $"{PlayerControl.LocalPlayer.FriendCode}");
                    cmd.Parameters.AddWithValue("@userName", $"{Main.AllPlayerNames[PlayerControl.LocalPlayer.PlayerId]}");
                    cmd.Parameters.AddWithValue("@version_number", $"{Main.PluginVersion}");
                    cmd.Parameters.AddWithValue("@gitLink", $"{ThisAssembly.Git.RepositoryUrl}");
                    cmd.Parameters.AddWithValue("@isDirty", $"{(ThisAssembly.Git.IsDirty ? 1 : 0)}");
                    cmd.Parameters.AddWithValue("@time", $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

                    // Execute the INSERT query
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions, logging error
                Logger.Error("Error: " + ex.Message,"dbConnet");
            }
            finally
            {
                dbCon.Close(); //connection is closed, whether an exception occurs or not
            }
        }
    }
}


public class DBConnection
{
    private DBConnection()
    {
    }

    public string Server { get; set; }
    public string DatabaseName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }

    public MySqlConnection Connection { get; set; }

    private static DBConnection _instance = null;
    public static DBConnection Instance()
    {
        if (_instance == null)
            _instance = new DBConnection();
        return _instance;
    }

    public bool IsConnect()
    {
        if (Connection == null)
        {
            if (string.IsNullOrEmpty(DatabaseName))
                return false;
            string connstring = string.Format("Server={0}; database={1}; UID={2}; password={3}", Server, DatabaseName, UserName, Password);
            Connection = new MySqlConnection(connstring);
        }
        if (Connection.State == ConnectionState.Closed || Connection.State == ConnectionState.Broken)
        {
            try
            {
                Connection.Open();
            }
            catch (MySqlException)
            {
                // Handle connection opening error if needed
                return false;
            }
        }

        return true;
    }

    public void Close()
    {
        Connection.Close();
    }
}