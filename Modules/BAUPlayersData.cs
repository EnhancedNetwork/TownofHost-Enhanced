
namespace TOHE.Modules;

public class BAUPlayersData
{
    private readonly Dictionary<NetworkedPlayerInfo, string> _players = new Dictionary<NetworkedPlayerInfo, string>();

    public string this[NetworkedPlayerInfo key]
    {
        get
        {
            CleanUpNullEntries();
            return _players.ContainsKey(key) ? _players[key] : null;
        }
        set
        {
            CleanUpNullEntries();
            if (key != null)
            {
                _players[key] = value;
            }
        }
    }

    private void CleanUpNullEntries()
    {
        var keysToRemove = _players.Where(kvp => kvp.Key == null).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _players.Remove(key);
        }
    }

    public bool TryGetValue(NetworkedPlayerInfo key, out string value)
    {
        CleanUpNullEntries();
        return _players.TryGetValue(key, out value);
    }

    public void Add(NetworkedPlayerInfo key, string value)
    {
        CleanUpNullEntries();
        if (key != null)
        {
            _players[key] = value;
        }
    }

    public bool Remove(NetworkedPlayerInfo key)
    {
        CleanUpNullEntries();
        return _players.Remove(key);
    }

    public bool ContainsKey(NetworkedPlayerInfo key)
    {
        CleanUpNullEntries();
        return _players.ContainsKey(key);
    }

    public Dictionary<NetworkedPlayerInfo, string>.KeyCollection Keys
    {
        get
        {
            CleanUpNullEntries();
            return _players.Keys;
        }
    }

    public Dictionary<NetworkedPlayerInfo, string>.ValueCollection Values
    {
        get
        {
            CleanUpNullEntries();
            return _players.Values;
        }
    }
}

