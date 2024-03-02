using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class EvilGuesser : RoleBase
{
    private const int Id = 1300;

    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem EGCanGuessTime;
    private static OptionItem EGCanGuessImp;
    private static OptionItem EGCanGuessAdt;
    //private static OptionItem EGCanGuessTaskDoneSnitch; Not used
    private static OptionItem EGTryHideMsg;

    public static void SetupCustomOption()
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
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public static bool NeedHideMsg(PlayerControl pc) => pc.Is(CustomRoles.EvilGuesser) && EGTryHideMsg.GetBool();

    public static bool HideTabInGuesserUI(int TabId)
    {
        if (!EGCanGuessImp.GetBool() && TabId == 1) return true;
        if (!EGCanGuessAdt.GetBool() && TabId == 3) return true;

        return false;
    }

    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role)
    {
        // Check limit
        if (Main.GuesserGuessed[guesser.PlayerId] >= EGCanGuessTime.GetInt())
        {
            if (!isUI) Utils.SendMessage(Translator.GetString("EGGuessMax"), guesser.PlayerId);
            else guesser.ShowPopUp(Translator.GetString("EGGuessMax"));
            return true;
        }

        // Evil Guesser Can't Guess Addons
        if (role.IsAdditionRole() && !EGCanGuessAdt.GetBool())
        {
            if (!isUI) Utils.SendMessage(Translator.GetString("GuessAdtRole"), guesser.PlayerId);
            else guesser.ShowPopUp(Translator.GetString("GuessAdtRole"));
            return true;
        }

        // Evil Guesser Can't Guess Impostors
        if (role.IsImpostor() && !EGCanGuessImp.GetBool())
        {
            if (!isUI) Utils.SendMessage(Translator.GetString("GuessImpRole"), guesser.PlayerId);
            else guesser.ShowPopUp(Translator.GetString("GuessImpRole"));
            return true;
        }

        return false;
    }
}
