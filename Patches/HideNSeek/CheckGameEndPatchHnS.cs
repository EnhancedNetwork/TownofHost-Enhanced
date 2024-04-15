using HarmonyLib;

namespace TOHE.Patches.HideNSeek;

[HarmonyPatch(typeof(LogicGameFlowHnS), nameof(LogicGameFlowHnS.CheckEndCriteria))]
class GameEndCheckerForHnS
{
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.Error) return false;

        return true;
    }
}
[HarmonyPatch(typeof(LogicGameFlowHnS), nameof(LogicGameFlowHnS.OnGameEnd))]
class OnGameEndForHnS
{
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.Error) return false;

        return true;
    }
}
