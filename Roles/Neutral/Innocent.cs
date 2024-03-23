using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Innocent : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14300;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem InnocentCanWinByImp;

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Innocent);
        InnocentCanWinByImp = BooleanOptionItem.Create(Id + 2, "InnocentCanWinByImp", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Innocent]);
    }
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        target.RpcMurderPlayerV3(killer);
        return false;
    }

    public override void CheckExileTarget(GameData.PlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        var role = exiled.GetCustomRole();
        var pcArray = Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exiled.PlayerId).ToArray();

        if (pcArray.Length <= 0) return;

        if (!InnocentCanWinByImp.GetBool() && role.IsImpostor())
        {
            if (!isMeetingHud)
                Logger.Info("Exeiled Winner Check for impostor", "Innocent");
        }
        else
        {
            if (isMeetingHud)
            {
                if (DecidedWinner) name += string.Format(GetString("ExiledInnocentTargetAddBelow"));
                else name = string.Format(GetString("ExiledInnocentTargetInOneLine"), Main.LastVotedPlayer, Utils.GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, true));
            }
            else
            {
                bool isInnocentWinConverted = false;
                foreach (var Innocent in pcArray)
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

                    pcArray.Do(x => CustomWinnerHolder.WinnerIds.Add(x.PlayerId));
                }
            }
            DecidedWinner = true;
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("InnocentButtonText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Suidce");
}
