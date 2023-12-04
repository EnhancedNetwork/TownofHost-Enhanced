using HarmonyLib;

namespace TOHE;

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsFreechatAllowed))]
public static class Passive_FreechatPrefix
{
    //Prefix patch of EOSManager.IsFreechatAllowed to unlock freechat
    public static bool Prefix(EOSManager __instance, ref bool __result)
    {

        __result = true;
        return true;
    }
}

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsFriendsListAllowed))]
public static class Passive_FriendListPrefix
{
    //Prefix patch of EOSManager.IsFriendsListAllowed to unlock friend list
    public static bool Prefix(EOSManager __instance, ref bool __result)
    {
        
        __result = true;
        return true;
    }
}

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsMinorOrWaiting))]
public static class Passive_MinorPrefix
{
    //Prefix patch of EOSManager.IsMinorOrWaiting to remove minor status
    public static bool Prefix(EOSManager __instance, ref bool __result)
    {
        
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(FullAccount), nameof(FullAccount.CanSetCustomName))]
public static class Passive_CustomNamePrefix
{
    //Prefix patch of FullAccount.CanSetCustomName to unlock custom names
    public static bool Prefix(bool canSetName, FullAccount __instance)
    {

        if (!canSetName){
            __instance.CanSetCustomName(true);
            return true;
        }

        return true;
    }
}

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
public static class Passive_OnlineGamePlayPrefix
{
    //Prefix patch of EOSManager.IsAllowedOnline to unlock online gameplay
    public static bool Prefix(bool canOnline, EOSManager __instance)
    {

        if (!canOnline){
            __instance.IsAllowedOnline(true);
            return true;
        }

        return true;
    }
}