/*
using HarmonyLib;

namespace TOHE;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
public static class HostOnly_GodmodePostfix
{
    //Postfix patch of PlayerPhysics.LateUpdate to revive LocalPlayer when CheatSettings.godMode enabled
    public static void Postfix(PlayerPhysics __instance)
    {
        if(Main.GodMode.Value == false){
            if (__instance.myPlayer.Data.IsDead && __instance.AmOwner){
                __instance.myPlayer.Revive();
            }
        }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
public static class HostOnly_CastVotePrefix
{
    //Prefix patch of MeetingHud.CastVote
    public static bool Prefix(byte srcPlayerId, byte suspectPlayerId, MeetingHud __instance)
    {
        if (Main.EvilVote.Value == false)
        {
            //Detects votes from LocalPlayer
            if (PlayerControl.LocalPlayer.PlayerId == srcPlayerId)
            {

                for (int i = 0; i < __instance.playerStates.Length; i++) //Loops through all players
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];

                    if (!playerVoteArea.AmDead)
                    { //Dead players shouldn't be able to vote

                        playerVoteArea.VotedFor = suspectPlayerId; //and makes them vote for whoever LocalPlayer voted for (including skipping)

                    }
                }

                __instance.CheckForEndVoting(); //Ends the vote since everyone as voted

                return false; //Skips original method
            }
        }
        return true;
    }
        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
        public static class Player_SpeedBoostPostfix
    {

        //Postfix patch of PlayerPhysics.LateUpdate to double player speed
        public static void Postfix(PlayerPhysics __instance)
        {
            //try-catch to avoid some errors I was reciving in the logs related to this cheat
            try
            {

                //PlayerControl.LocalPlayer.MyPhysics.Speed is the base speed of a player
                //Among Us uses this value with the associated game setting to calculate the TrueSpeed of the player
                if (Main.SpeedBoost.Value == false)
                {
                    PlayerControl.LocalPlayer.MyPhysics.Speed = 2.5f * 2;
                }
                else
                {
                    PlayerControl.LocalPlayer.MyPhysics.Speed = 2.5f; //By default, Speed is always 2.5f
                }

            }
            catch { }
        }
    }
}