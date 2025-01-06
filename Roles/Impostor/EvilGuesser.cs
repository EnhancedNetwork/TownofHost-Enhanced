﻿using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class EvilGuesser : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 1300;

    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem EGCanGuessTime;
    private static OptionItem EGCanGuessImp;
    private static OptionItem EGCanGuessAdt;
    //private static OptionItem EGCanGuessTaskDoneSnitch; Not used
    private static OptionItem EGTryHideMsg;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EvilGuesser);
        EGCanGuessTime = IntegerOptionItem.Create(Id + 2, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser])
            .SetValueFormat(OptionFormat.Times);
        EGCanGuessImp = BooleanOptionItem.Create(Id + 3, "EGCanGuessImp", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGCanGuessAdt = BooleanOptionItem.Create(Id + 4, "EGCanGuessAdt", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        //EGCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 5, "EGCanGuessTaskDoneSnitch", true, TabGroup.ImpostorRoles, false)
        //    .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
        EGTryHideMsg = BooleanOptionItem.Create(Id + 6, "GuesserTryHideMsg", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser])
            .SetColor(Color.green);
    }
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
        => seer.IsAlive() && target.IsAlive() ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.EvilGuesser), target.PlayerId.ToString()) + " " + pva.NameText.text : string.Empty;

    public static bool NeedHideMsg(PlayerControl pc) => pc.Is(CustomRoles.EvilGuesser) && EGTryHideMsg.GetBool();

    public static bool HideTabInGuesserUI(int TabId)
    {
        if (!EGCanGuessImp.GetBool() && TabId == 1) return true;
        if (!EGCanGuessAdt.GetBool() && TabId == 3) return true;

        return false;
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        // Check limit
        if (GuessManager.GuesserGuessed[guesser.PlayerId] >= EGCanGuessTime.GetInt())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("EGGuessMax"));
            return true;
        }

        // Evil Guesser Can't Guess Addons
        if (role.IsAdditionRole() && !EGCanGuessAdt.GetBool())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessAdtRole"));
            return true;
        }

        // Evil Guesser Can't Guess Impostors
        if (role.IsImpostor() && !EGCanGuessImp.GetBool())
        {
            guesser.ShowInfoMessage(isUI, Translator.GetString("GuessImpRole"));
            return true;
        }

        return false;
    }
}
