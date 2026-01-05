using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE;

public abstract class PariahManager : RoleBase // NO, THIS IS NOT A ROLE
{
    [Obfuscation(Exclude = true)]
    public enum VisOptionList
    {
        On,
        PariahPerRole
    }
    [Obfuscation(Exclude = true)]
    public enum VentOptionList
    {
        On,
        PariahPerRole
    }

    private static readonly Dictionary<CustomRoles, OptionItem> PariahImpVisOptions = [];
    private static readonly Dictionary<CustomRoles, OptionItem> PariahVentOptions = [];

    public static void RunSetUpImpVisOptions(int Id)
    {
        foreach (var np in CustomRolesHelper.AllRoles.Where(x => x.IsNP()).ToArray())
        {
            SetUpImpVisOption(np, Id, true, PariahImpVisMode);
            Id++;
        }
    }
    public static void RunSetUpVentOptions(int Id)
    {
        foreach (var np in CustomRolesHelper.AllRoles.Where(x => x.IsNP()).ToArray())
        {
            SetUpVentOption(np, Id, true, PariahVentMode);
            Id++;
        }
    }
    private static void SetUpImpVisOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        var roleName = GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", ColorString(GetRoleColor(role), roleName) } };
        PariahImpVisOptions[role] = BooleanOptionItem.Create(Id, "%role%HasImpVis", defaultValue, TabGroup.NeutralRoles, false).SetParent(parent);
        PariahImpVisOptions[role].ReplacementDictionary = replacementDic;
    }
    private static void SetUpVentOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        var roleName = GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", ColorString(GetRoleColor(role), roleName) } };
        PariahVentOptions[role] = BooleanOptionItem.Create(Id, "%role%CanVent", defaultValue, TabGroup.NeutralRoles, false).SetParent(parent);
        PariahVentOptions[role].ReplacementDictionary = replacementDic;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        if (!PariahHasImpVis.GetBool())
            opt.SetVision(false);
        else if (PariahImpVisMode.GetValue() == 0)
            opt.SetVision(true);
        else
        {
            PariahImpVisOptions.TryGetValue(GetPlayerById(playerId).GetCustomRole(), out var option);
            opt.SetVision(option.GetBool());
        }
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc)
    {
        if (!PariahCanVent.GetBool())
            return false;
        else if (PariahVentMode.GetValue() == 0)
            return true;
        else
        {
            PariahVentOptions.TryGetValue(pc.GetCustomRole(), out var option);
            return option.GetBool();
        }
    }

    public static void CheckAdditionalWin()
    {
        var winTeam = CustomWinnerHolder.WinnerTeam;

        if (winTeam is CustomWinner.None or CustomWinner.Default or CustomWinner.Draw or CustomWinner.Crewmate or CustomWinner.Lovers or CustomWinner.Youtuber or CustomWinner.Error) return;

        List<PlayerControl> nps = [.. Main.AllPlayerControls.Where(x => x.GetCustomRole().IsNP())];

        foreach (var np in nps)
        {
            if (!np.IsAlive() && !PariahWinWhenDead.GetBool()) continue;

            if (!CustomWinnerHolder.WinnerIds.Contains(np.PlayerId))
            {
                CustomWinnerHolder.WinnerIds.Add(np.PlayerId);
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.NeutralPariah);
            }
        }
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer != target && !killer.GetCustomRole().IsCrewmateTeamV2()) return false;
        return base.OnCheckMurderAsTarget(killer, target);
    }
}