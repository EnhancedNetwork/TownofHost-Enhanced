using System.Text;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Skeleton : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Skeleton;
    private const int Id = 36000;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem NumberOfReportsToWin;
    private static OptionItem KillCooldown;

    private static int BodiesReported = 0;
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Skeleton);
        NumberOfReportsToWin = IntegerOptionItem.Create(Id + 10, "SkeletonNumberOfReportsToWin", new(1, 10, 1), 5, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Skeleton]);
        KillCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.KillCooldown, new(0f, 60f, 1f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Skeleton])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody, PlayerControl killer)
    {
        if (Main.UnreportableBodies.Contains(deadBody.PlayerId)) return false;

        if (killer.Is(CustomRoles.Skeleton))
        {
            if (!reporter.Is(CustomRoles.Skeleton))
            {
                Main.UnreportableBodies.Add(deadBody.PlayerId);
                BodiesReported++;
                if (BodiesReported >= NumberOfReportsToWin.GetInt())
                {
                    foreach (var player in Main.AllAlivePlayerControls)
                    {
                        if (player.Is(CustomRoles.Skeleton))
                        {
                            if (!CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
                            {
                                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Skeleton);
                                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                            }
                        }
                    }
                }
            }
            
            reporter.RpcGuardAndKill();
            killer.RpcGuardAndKill();
            return false;
        }

        return base.OnCheckReportDeadBody(reporter, deadBody, killer);
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        ProgressText.Append(ColorString(Utils.GetRoleColor(CustomRoles.Impostor), $"({BodiesReported}/{NumberOfReportsToWin.GetInt()})"));
        return ProgressText.ToString();
    }
}
