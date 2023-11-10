using MySql.Data.MySqlClient;
using System;
using System.Data;
namespace TOHE;


class DBQueries
{
    public static void InsertData()
    {
        var dbCon = DBConnection.Instance();
        dbCon.Server = ""; // server name
        dbCon.DatabaseName = ""; // database name
        dbCon.UserName = ""; // usernmae
        dbCon.Password = ""; // password

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

        dbCon.Close();
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