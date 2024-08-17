using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using TOHE.Modules;
using static UnityEngine.GraphicsBuffer;
using TOHE.Roles.Neutral;

namespace TOHE.Roles.AddOns.Common;

public class Rebirth : IAddon
{
    private const int Id = 29500;
    public AddonTypes Type => AddonTypes.Helpful;
    public static OptionItem RebirthUses;
    public static Dictionary<byte, int> Rebirths = [];
    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Rebirth, canSetNum: true, teamSpawnOptions: true);
        RebirthUses = IntegerOptionItem.Create(Id + 11, "RebirthUses", new(1, 14, 1), 1, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Rebirth])
           .SetValueFormat(OptionFormat.Times);
    }
    public static void Add(byte Playerid)
    {
        Rebirths[Playerid] = RebirthUses.GetInt();
    }
    public static void Remove(byte Playerid) 
    {
        Rebirths.Remove(Playerid);
    }

    public static bool SwapSkins(PlayerControl pc, out NetworkedPlayerInfo NewExiledPlayer)
    {
        NewExiledPlayer = default;
        if (!pc.Is(CustomRoles.Rebirth)) return false;
        var ViablePlayer = Main.AllAlivePlayerControls.Where(x => x != pc).Shuffle(IRandom.Instance)
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
        _ = new LateTask(() => Utils.NotifyRoles(SpecifySeer: pc, SpecifyTarget: pc), 3f);
        if (Rebirths[pc.PlayerId] <= 0)
        {
            Main.PlayerStates[pc.PlayerId].RemoveSubRole(CustomRoles.Rebirth);
        }
        return true;

    }
}