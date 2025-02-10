using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Modules;
internal class AnomalyManager
{
    enum WhichAnomaly
    {
        ClownFest,
        Retrial,
        NewYear,
        Holiday,
        Shuffle,
    };
    private static readonly List<WhichAnomaly> which1 = [];

    public static Dictionary<byte, CustomRoles> FormerRoles = [];

    public static void AnomalyChance()
    {
        var rand = IRandom.Instance;
        if (rand.Next(100) <= AnomalyMeetingPCT.GetInt())
        {
            if (Options.ClownFest.GetBool())
            {
                which1.Add(WhichAnomaly.ClownFest);
            }
            if (Options.Retrial.GetBool())
            {
                which1.Add(WhichAnomaly.Retrial);
            }
            if (Options.NewYear.GetBool())
            {
                which1.Add(WhichAnomaly.NewYear);
            }
            if (Options.Holiday.GetBool())
            {
                which1.Add(WhichAnomaly.Holiday);
            }
            if (Options.Shuffle.GetBool())
            {
                which1.Add(WhichAnomaly.Shuffle);
            }
            var which2 = which1.RandomElement();
            switch (which2)
            {
                case WhichAnomaly.ClownFest:
                    AfterAnomaly();
                    ClownFest();
                    break;
                case WhichAnomaly.Retrial:
                    AfterAnomaly();
                    Retrial();
                    break;
                case WhichAnomaly.NewYear:
                    AfterAnomaly();
                    NewYear();
                    break;
                case WhichAnomaly.Holiday:
                    AfterAnomaly();
                    Holiday();
                    break;
                case WhichAnomaly.Shuffle:
                    AfterAnomaly();
                    Shuffle();
                    break;
            }
        }
        else AfterAnomaly();
    }

    public static void AfterAnomaly()
    {
        foreach (var former in FormerRoles)
        {
            var role = former.Value;
            var player = Utils.GetPlayer(former.Key);
            player.RpcChangeRoleBasis(role);
            player.RpcSetCustomRole(role);
            FormerRoles.Remove(former.Key);
        }
    }

    public static void ClownFest()
    {
        foreach (var clown in Main.AllAlivePlayerControls)
        {
            var role = clown.GetCustomRole();
            if (role.IsImpostor()) return;
            if (role.IsCoven()) return;
            if (role.IsNeutral()) return;
            if (role.IsCrewmate())
            {
                FormerRoles[clown.PlayerId] = role;
                clown.RpcChangeRoleBasis(CustomRoles.Jester);
                clown.RpcSetCustomRole(CustomRoles.Jester);
            }
            clown.Notify(ColorString(GetRoleColor(CustomRoles.Jester), GetString("ClownFestAnomaly")));
        }
    }
    public static void Retrial()
    {
        foreach (var player in Main.AllPlayerControls)
        {
            if (player.IsHost())
            {
                player.NoCheckStartMeeting(null, force: true);
            }
        }
    }

    public static void NewYear()
    {
        List<PlayerControl> assigning = [];
        List<CustomRoles> AddOnStorage = [];
        foreach (var player in Main.AllAlivePlayerControls)
        {
            assigning.Add(player);
            var addons = Main.PlayerStates[player.PlayerId].SubRoles.ToList();
            foreach (var role in addons)
            {
                if (role.IsImpOnlyAddon() ||
                    role is CustomRoles.Lovers ||
                    role.IsBetrayalAddon()) return;
                addons.Remove(role);
                AddOnStorage.Add(role);

                var RandomAddOn = AddOnStorage.RandomElement();
                if (assigning.Contains(player))
                {
                    assigning.Remove(player);
                    AddOnStorage.Remove(RandomAddOn);
                    addons.Add(RandomAddOn);
                }
            }
            player.Notify(ColorString(GetRoleColor(CustomRoles.Jester), GetString("NewYearAnomaly")));
        }

    }

    public static void Holiday()
    {
        foreach (var player in Main.AllAlivePlayerControls)
        {
            var role = player.GetCustomRole();
            if (role.IsImpostor())
            {
                FormerRoles[player.PlayerId] = role;
                player.RpcChangeRoleBasis(CustomRoles.ImpostorTOHO);
                player.RpcSetCustomRole(CustomRoles.ImpostorTOHO);
            }
            if (role.IsCoven()) return;
            if (role.IsNeutral()) return;
            if (role.IsCrewmate())
            {
                FormerRoles[player.PlayerId] = role;
                player.RpcChangeRoleBasis(CustomRoles.CrewmateTOHO);
                player.RpcSetCustomRole(CustomRoles.CrewmateTOHO);
            }
            player.Notify(ColorString(GetRoleColor(CustomRoles.Jester), GetString("HolidayAnomaly")));
        }
    }

    public static void Shuffle()
    {
        List<PlayerControl> assigning = [];
        List<CustomRoles> AddOnStorage = [];
        foreach (var player in Main.AllAlivePlayerControls)
        {
            assigning.Add(player);
            var role = Main.PlayerStates[player.PlayerId].MainRole;
            if (role.IsImpostor() ||
                role.IsCoven() ||
                role.IsNK() ||
                role.IsNA()) return;
            AddOnStorage.Add(role);

            var RandomAddOn = AddOnStorage.RandomElement();
            if (assigning.Contains(player))
            {
                player.RpcChangeRoleBasis(RandomAddOn);
                player.RpcSetCustomRole(RandomAddOn);
                assigning.Remove(player);
                AddOnStorage.Remove(RandomAddOn);
            }
            player.Notify(ColorString(GetRoleColor(CustomRoles.Jester), GetString("ShuffleAnomaly")));
        }
    }
}
