using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE.Patches;

[HarmonyPatch(typeof(PlayerControl))]
public static class PhantomRolePatch
{
    private static readonly Il2CppSystem.Collections.Generic.List<PlayerControl> InvisibilityList = new();
    private static readonly Dictionary<byte, string> PetsList = [];

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
            if (!target.IsAlive() || phantom == target || target.AmOwner || !(target.HasDesyncRole() || Main.PlayerStates[target.PlayerId].IsNecromancer)) continue;

            // Set Phantom when his start vanish
            phantom.RpcSetRoleDesync(RoleTypes.Phantom, target.GetClientId());
            // Check vanish again for desync role
            phantom.RpcCheckVanishDesync(target);

            _ = new LateTask(() =>
            {
                if (Main.MeetingIsStarted || phantom == null) return;

                var petId = phantom.Data.DefaultOutfit.PetId;
                if (petId != "")
                {
                    PetsList[phantom.PlayerId] = petId;
                    phantom?.RpcSetPetDesync("", target);
                }
                phantom?.RpcExileDesync(target);
            }, 1.2f, $"Set Phantom invisible {target.PlayerId}", shoudLog: false);
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

        if (phantom.inVent)
        {
            phantom.MyPhysics.RpcBootFromVent(Main.LastEnteredVent[phantom.PlayerId].Id);
        }

        foreach (var target in Main.AllPlayerControls)
        {
            if (!target.IsAlive() || phantom == target || target.AmOwner || !(target.HasDesyncRole() || Main.PlayerStates[target.PlayerId].IsNecromancer)) continue;

            var clientId = target.GetClientId();

            // Set Phantom when his end vanish
            phantom.RpcSetRoleDesync(RoleTypes.Phantom, clientId);

            _ = new LateTask(() =>
            {
                // Check appear again for desync role
                if (target != null)
                    phantom?.RpcCheckAppearDesync(shouldAnimate, target);
            }, 0.5f, $"Check Appear when vanish is over {target.PlayerId}", shoudLog: false);

            _ = new LateTask(() =>
            {
                if (Main.MeetingIsStarted || phantom == null) return;

                InvisibilityList.Remove(phantom);
                phantom?.RpcSetRoleDesync(RoleTypes.Scientist, clientId);

                if (PetsList.TryGetValue(phantom.PlayerId, out var petId))
                {
                    phantom?.RpcSetPetDesync(petId, target);
                }
            }, 1.8f, $"Set Scientist when vanish is over {target.PlayerId}", shoudLog: false);
        }
    }
    [HarmonyPatch(nameof(PlayerControl.SetRoleInvisibility)), HarmonyPrefix]
    private static void SetRoleInvisibility_Prefix(PlayerControl __instance, bool isActive, bool shouldAnimate, bool playFullAnimation)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Logger.Info($"Player: {__instance.GetRealName()} => Is Active {isActive}, Animate:{shouldAnimate}, Full Animation:{playFullAnimation}", "SetRoleInvisibility");
    }

    public static void OnReportDeadBody(PlayerControl seer)
    {
        try
        {
            if (InvisibilityList.Count == 0 || !seer.IsAlive() || seer.Data?.Role.Role is RoleTypes.Phantom || seer.AmOwner || !(seer.HasDesyncRole() || Main.PlayerStates[seer.PlayerId].IsNecromancer)) return;

            foreach (var phantom in InvisibilityList.GetFastEnumerator())
            {
                if (!phantom.IsAlive())
                {
                    InvisibilityList.Remove(phantom);
                    continue;
                }

                Main.Instance.StartCoroutine(CoRevertInvisible(phantom, seer));
            }
        }
        catch (System.Exception error)
        {
            Logger.Error(error.ToString(), "PhantomRole.OnReportDeadBody");
        }
    }
    private static bool InValid(PlayerControl phantom, PlayerControl seer) => phantom == null || seer.GetClientId() == -1;
    private static System.Collections.IEnumerator CoRevertInvisible(PlayerControl phantom, PlayerControl seer)
    {
        // Set Scientist for meeting
        if (InValid(phantom, seer)) yield break;

        phantom?.RpcSetRoleDesync(RoleTypes.Scientist, seer.GetClientId());

        // Return Phantom in meeting
        yield return new WaitForSeconds(1f);
        {
            if (InValid(phantom, seer)) yield break;

            phantom?.RpcSetRoleDesync(RoleTypes.Phantom, seer.GetClientId());
        }
        // Revert invis for phantom
        yield return new WaitForSeconds(1f);
        {
            if (InValid(phantom, seer)) yield break;

            phantom?.RpcStartAppearDesync(false, seer);
        }
        // Set Scientist back
        yield return new WaitForSeconds(4f);
        {
            if (InValid(phantom, seer)) yield break;

            phantom?.RpcSetRoleDesync(RoleTypes.Scientist, seer.GetClientId());

            if (PetsList.TryGetValue(phantom.PlayerId, out var petId))
            {
                phantom?.RpcSetPetDesync(petId, seer);
            }
        }
        yield break;
    }
    public static void AfterMeeting()
    {
        InvisibilityList.Clear();
        PetsList.Clear();
    }
}
// Fixed vanilla bug for host (from TOH-Y)
[HarmonyPatch(typeof(PhantomRole), nameof(PhantomRole.UseAbility))]
public static class PhantomRoleUseAbilityPatch
{
    public static bool Prefix(PhantomRole __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        if (__instance.Player.AmOwner && !__instance.Player.Data.IsDead && __instance.Player.moveable && !Minigame.Instance && !__instance.IsCoolingDown && !__instance.fading)
        {
            System.Func<RoleEffectAnimation, bool> roleEffectAnimation = x => x.effectType == RoleEffectAnimation.EffectType.Vanish_Charge;
            if (!__instance.Player.currentRoleAnimations.Find(roleEffectAnimation) && !__instance.Player.walkingToVent && !__instance.Player.inMovingPlat)
            {
                if (__instance.isInvisible)
                {
                    __instance.MakePlayerVisible(true, true);
                    return false;
                }
                DestroyableSingleton<HudManager>.Instance.AbilityButton.SetSecondImage(__instance.Ability);
                DestroyableSingleton<HudManager>.Instance.AbilityButton.OverrideText(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PhantomAbilityUndo, new Il2CppReferenceArray<Il2CppSystem.Object>(0)));
                __instance.Player.CmdCheckVanish(GameManager.Instance.LogicOptions.GetPhantomDuration());
                return false;
            }
        }
        return false;
    }
}
