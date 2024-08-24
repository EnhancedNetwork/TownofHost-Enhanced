using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using TOHE.Modules;

namespace TOHE.Roles.AddOns.Common;

public class Rebirth : IAddon
{
    private const int Id = 29500;
    public AddonTypes Type => AddonTypes.Helpful;
    public static OptionItem RebirthUses;
    public static OptionItem OnlyVoted;

    public static Dictionary<byte, int> Rebirths = [];
    public static Dictionary<byte, List<byte>> VotedCount = [];
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rebirth, canSetNum: true, teamSpawnOptions: true);
        RebirthUses = IntegerOptionItem.Create(Id + 11, "RebirthUses", new(1, 14, 1), 1, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Rebirth])
           .SetValueFormat(OptionFormat.Times);
        OnlyVoted = BooleanOptionItem.Create(Id + 12, "RebirthCountVotes", false, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Rebirth]);
    }
    public static void Init()
    {
        Rebirths.Clear();
        VotedCount.Clear();
    }
    public static void Add(byte Playerid)
    {
        Rebirths[Playerid] = RebirthUses.GetInt();
        VotedCount[Playerid] = [];
    }
    public static void Remove(byte Playerid) 
    {
        Rebirths.Remove(Playerid);
    }
    public static void CountVotes(byte playerid, byte voter)
    {
        if (!VotedCount.ContainsKey(playerid)) return;

        VotedCount[playerid].Add(voter);

    }
    public static void OnReportDeadBody()
    {
        foreach(var KvP in VotedCount)
        {
            KvP.Value.Clear();
        }
    }
    public static bool SwapSkins(PlayerControl pc, out NetworkedPlayerInfo NewExiledPlayer)
    {
        NewExiledPlayer = default;
        if (!pc.Is(CustomRoles.Rebirth)) return false;
        List<PlayerControl> list = [..Main.AllAlivePlayerControls];
        if (OnlyVoted.GetBool())
        {
            list = [..VotedCount[pc.PlayerId].Select(x => GetPlayerById(x))];
        }

        var ViablePlayer = list.Where(x => x != pc).Shuffle(IRandom.Instance)
            .FirstOrDefault(x => x != null && !x.OwnedByHost() && !x.IsAnySubRole(x => x.IsConverted()) && !x.Is(CustomRoles.Admired) && !x.Is(CustomRoles.Knighted) && 
/*All converters */    (!x.Is(CustomRoles.Cultist) && !x.Is(CustomRoles.Infectious) && !x.Is(CustomRoles.Virus) && !x.Is(CustomRoles.Jackal) && !x.Is(CustomRoles.Admirer)) &&
                !x.Is(CustomRoles.Lovers) && !x.Is(CustomRoles.Romantic) && !x.GetCustomRole().IsImpostor());

        if (ViablePlayer == null)
        {
            var tytyl = ColorString(GetRoleColor(CustomRoles.Rebirth), GetString("Rebirth").ToUpper());
            Utils.SendMessage(GetString("RebirthFailed"), pc.PlayerId, title: tytyl);
            return false;
        }
        Rebirths[pc.PlayerId]--;
        pc.ResetPlayerOutfit(Main.PlayerStates[ViablePlayer.PlayerId].NormalOutfit, ViablePlayer.Data.PlayerLevel, true);
        Main.OvverideOutfit[pc.PlayerId] = (Main.PlayerStates[ViablePlayer.PlayerId].NormalOutfit, Main.PlayerStates[ViablePlayer.PlayerId].NormalOutfit.PlayerName);

        ViablePlayer.ResetPlayerOutfit(Main.PlayerStates[pc.PlayerId].NormalOutfit, pc.Data.PlayerLevel, true);
        Main.OvverideOutfit[ViablePlayer.PlayerId] = (Main.PlayerStates[pc.PlayerId].NormalOutfit, Main.PlayerStates[pc.PlayerId].NormalOutfit.PlayerName);

        NewExiledPlayer = ViablePlayer.Data;
        if (Rebirths[pc.PlayerId] <= 0)
        {
            Main.PlayerStates[pc.PlayerId].RemoveSubRole(CustomRoles.Rebirth);
        }
        return true;

    }
}