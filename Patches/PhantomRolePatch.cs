using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Core;

namespace TOHE.Patches;

[HarmonyPatch(typeof(PlayerControl))]
public static class PhantomRolePatch
{
    private static readonly Il2CppSystem.Collections.Generic.List<PlayerControl> InvisibilityList = new();

    /*
     *  InnerSloth is doing careless stuffs. They didnt put amModdedHost check in cmd check vanish appear
     *  We temporary need to patch the whole cmd function and wait for the next hotfix from them
    */
    [HarmonyPatch(nameof(PlayerControl.CmdCheckVanish)), HarmonyPrefix]
    private static bool CmdCheckVanish_Prefix(PlayerControl __instance, float maxDuration)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            __instance.CheckVanish();
            return false;
        }
        __instance.SetRoleInvisibility(true, true, false);
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.CheckVanish, SendOption.Reliable, AmongUsClient.Instance.HostId);
        messageWriter.Write(maxDuration);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        return false;
    }

    [HarmonyPatch(nameof(PlayerControl.CmdCheckAppear)), HarmonyPrefix]
    private static bool CmdCheckAppear_Prefix(PlayerControl __instance, bool shouldAnimate)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            __instance.CheckAppear(shouldAnimate);
            return false;
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.CheckAppear, SendOption.Reliable, AmongUsClient.Instance.HostId);
        messageWriter.Write(shouldAnimate);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        return false;
    }

    // Called when Phantom press vanish button when visible
    [HarmonyPatch(nameof(PlayerControl.CheckVanish)), HarmonyPrefix]
    private static void CheckVanish_Prefix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var phantom = __instance;
        Logger.Info($"Player: {phantom.GetRealName()}", "CheckVanish");

        foreach (var target in Main.AllPlayerControls)
        {
            if (!target.IsAlive() || phantom == target || target.AmOwner || !target.HasDesyncRole()) continue;

            // Set Phantom when his start vanish
            phantom.RpcSetRoleDesync(RoleTypes.Phantom, target.GetClientId());
            // Check vanish again for desync role
            phantom.RpcCheckVanishDesync(target);

            _ = new LateTask(() =>
            {
                phantom?.RpcExileDesync(target);
            }, 1.2f, "Set Phantom invisible", shoudLog: false);
        }
        InvisibilityList.Add(phantom);
    }
    // Called when Phantom press appear button when is invisible
    [HarmonyPatch(nameof(PlayerControl.CheckAppear)), HarmonyPrefix]
    private static void CheckAppear_Prefix(PlayerControl __instance, bool shouldAnimate)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var phantom = __instance;
        Logger.Info($"Player: {phantom.GetRealName()} => shouldAnimate {shouldAnimate}", "CheckAppear");

        foreach (var target in Main.AllPlayerControls)
        {
            if (!target.IsAlive() || phantom == target || target.AmOwner || !target.HasDesyncRole()) continue;

            var clientId = target.GetClientId();

            // Set Phantom when his end vanish
            phantom.RpcSetRoleDesync(RoleTypes.Phantom, clientId);

            _ = new LateTask(() =>
            {
                // Check appear again for desync role
                if (target != null)
                    phantom?.RpcCheckAppearDesync(shouldAnimate, target);
            }, 0.5f, "Check Appear when vanish is over", shoudLog: false);

            _ = new LateTask(() =>
            {
                phantom?.RpcSetRoleDesync(RoleTypes.Scientist, clientId);
            }, 1.8f, "Set Scientist when vanish is over", shoudLog: false);
        }
        InvisibilityList.Remove(phantom);
    }
    [HarmonyPatch(nameof(PlayerControl.SetRoleInvisibility)), HarmonyPrefix]
    private static void SetRoleInvisibility_Prefix(PlayerControl __instance, bool isActive, bool shouldAnimate, bool playFullAnimation)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Logger.Info($"Player: {__instance.GetRealName()} => Is Active {isActive}, Animate:{shouldAnimate}, Full Animation:{playFullAnimation}", "SetRoleInvisibility");
    }

    public static void OnReportBody(PlayerControl seer)
    {
        if (InvisibilityList.Count == 0 || !seer.IsAlive() || seer.Data.Role.Role is RoleTypes.Phantom || seer.AmOwner || !seer.HasDesyncRole()) return;

        foreach (var phantom in InvisibilityList.GetFastEnumerator())
        {
            if (!phantom.IsAlive()) continue;

            var clientId = seer.GetClientId();
            _ = new LateTask(() =>
            {
                phantom?.RpcSetRoleDesync(RoleTypes.Scientist, clientId);
            }, 0.01f, "Set Scientist in meeting", shoudLog: false);

            _ = new LateTask(() =>
            {
                phantom?.RpcSetRoleDesync(RoleTypes.Phantom, clientId);
            }, 1f, "Set Phantom in meeting", shoudLog: false);

            _ = new LateTask(() =>
            {
                if (seer != null)
                    phantom?.RpcStartAppearDesync(false, seer);
            }, 1.5f, "Check Appear in meeting", shoudLog: false);

            _ = new LateTask(() =>
            {
                phantom?.RpcSetRoleDesync(RoleTypes.Scientist, clientId);

                InvisibilityList.Clear();
            }, 4f, "Set Scientist in meeting after reset", shoudLog: false);
        }
    }
}
