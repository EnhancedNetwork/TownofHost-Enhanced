using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Innocent : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Innocent;
    private const int Id = 14300;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem InnocentCanWinByImp;
    private bool TargetIsKilled = false;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Innocent);
        InnocentCanWinByImp = BooleanOptionItem.Create(Id + 2, "InnocentCanWinByImp", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Innocent]);
    }
    public override void Init()
    {
        TargetIsKilled = false;
    }
    public override void Add(byte playerId)
    {
        TargetIsKilled = false;
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        TargetIsKilled = true;
        target.RpcMurderPlayer(killer);
        return false;
    }

    public override void CheckExileTarget(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        if (exiled == null || !TargetIsKilled) return;

        var exiledRole = exiled.GetCustomRole();
        var innocentArray = Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exiled.PlayerId);

        if (!innocentArray.Any()) return;

        if (!InnocentCanWinByImp.GetBool() && exiledRole.IsImpostor())
        {
            if (!isMeetingHud)
                Logger.Info("Exeiled Winner Check for impostor", "Innocent");
        }
        else
        {
            if (isMeetingHud)
            {
                if (DecidedWinner) name += string.Format(GetString("ExiledInnocentTargetAddBelow"));
                else name = string.Format(GetString("ExiledInnocentTargetInOneLine"), Main.LastVotedPlayer, Utils.GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, false, true));
            }
            else
            {
                bool isInnocentWinConverted = false;
                foreach (var Innocent in innocentArray)
                {
                    if (CustomWinnerHolder.CheckForConvertedWinner(Innocent.PlayerId))
                    {
                        isInnocentWinConverted = true;
                        break;
                    }
                }
                if (!isInnocentWinConverted)
                {
                    if (DecidedWinner)
                    {
                        CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Innocent);
                    }
                    else
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Innocent);
                    }

                    innocentArray.Do(x => CustomWinnerHolder.WinnerIds.Add(x.PlayerId));
                    if (exiledRole.IsImpostor())
                    {
                        CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                    }
                }
            }
            DecidedWinner = true;
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("InnocentButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Suidce");
}
