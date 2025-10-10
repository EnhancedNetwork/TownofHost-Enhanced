using TOHE.Roles.Coven;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Aware : IAddon
{
    public CustomRoles Role => CustomRoles.Aware;
    private const int Id = 21600;
    public static bool IsEnable = false;
    public AddonTypes Type => AddonTypes.Mixed;

    public static OptionItem ImpCanBeAware;
    public static OptionItem CrewCanBeAware;
    public static OptionItem NeutralCanBeAware;
    private static OptionItem AwareknowRole;

    public static readonly Dictionary<byte, HashSet<string>> AwareInteracted = [];

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(21600, CustomRoles.Aware, canSetNum: true, teamSpawnOptions: true);
        AwareknowRole = BooleanOptionItem.Create(Id + 13, "AwareKnowRole", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Aware]);
    }

    public void Init()
    {
        AwareInteracted.Clear();
        IsEnable = false;
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        AwareInteracted[playerId] = [];
        IsEnable = true;
    }
    public void Remove(byte playerId)
    {
        AwareInteracted.Remove(playerId);

        if (!AwareInteracted.Any())
            IsEnable = false;
    }

    public static void OnCheckMurder(CustomRoles killerRole, PlayerControl target)
    {
        if (!target.Is(CustomRoles.Aware)) return;

        switch (killerRole)
        {
            case CustomRoles.Baker when Baker.CurrentBread() == 0:
            case CustomRoles.PotionMaster when PotionMaster.CurrentPotion() == 0:
            case CustomRoles.Investigator:
            case CustomRoles.Consigliere:
            case CustomRoles.Overseer:
            case CustomRoles.CopyCat:
                if (!AwareInteracted.ContainsKey(target.PlayerId))
                {
                    AwareInteracted.Add(target.PlayerId, []);
                }
                if (!AwareInteracted[target.PlayerId].Contains(Utils.GetRoleName(killerRole)))
                {
                    AwareInteracted[target.PlayerId].Add(Utils.GetRoleName(killerRole));
                }
                break;
        }
    }

    public static void OnReportDeadBody()
    {
        foreach (var (pid, list) in AwareInteracted)
        {
            var Awarepc = pid.GetPlayer();
            if (list.Any() && Awarepc.IsAlive())
            {
                string rolelist = "Someone";
                _ = new LateTask(() =>
                {
                    if (AwareknowRole.GetBool())
                        rolelist = string.Join(", ", list);

                    Utils.SendMessage(string.Format(GetString("AwareInteracted"), rolelist), pid, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Aware), GetString("AwareTitle")));
                    AwareInteracted[pid] = [];
                }, 0.5f, "Aware Check Msg");
            }
        }

    }
    public static void OnVoted(PlayerControl pc, PlayerVoteArea pva)
    {
        switch (pc.GetCustomRole())
        {
            case CustomRoles.FortuneTeller:
            case CustomRoles.Oracle:
            case CustomRoles.Inspector:
                AwareInteracted[pva.VotedFor].Add(Utils.GetRoleName(pc.GetCustomRole()));
                break;
        }
    }
}

