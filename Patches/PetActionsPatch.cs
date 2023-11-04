using HarmonyLib;
using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE;

/*
 * HUGE THANKS TO
 * ImaMapleTree / 단풍잎 / Tealeaf
 * FOR THE CODE
 *
 * Thanks to tohe+ for the code
 */

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TryPet))]
class LocalPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = new();
    public static bool Prefix(PlayerControl __instance)
    {
        if (!Main.UsePetSystem.Value) return true;
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return true;
        if (GameStates.IsLobby) return true;

        if (__instance.petting) return true;
        __instance.petting = true;

        if (!LastProcess.ContainsKey(__instance.PlayerId)) LastProcess.TryAdd(__instance.PlayerId, Utils.GetTimeStamp() - 2);
        if (LastProcess[__instance.PlayerId] + 1 >= Utils.GetTimeStamp()) return true;

        ExternalRpcPetPatch.Prefix(__instance.MyPhysics, (byte)RpcCalls.Pet);

        LastProcess[__instance.PlayerId] = Utils.GetTimeStamp();
        return !__instance.GetCustomRole().IsUsingPetSystem();
    }

    public static void Postfix(PlayerControl __instance)
    {
        if (!Main.UsePetSystem.Value) return;
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return;
        __instance.petting = false;
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
class ExternalRpcPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = new();
    public static Dictionary<byte, long> SkillCountDown = new();
    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callID)
    {
        if (!Main.UsePetSystem.Value || !AmongUsClient.Instance.AmHost || (RpcCalls)callID != RpcCalls.Pet) return;

        var pc = __instance.myPlayer;
        var physics = __instance;

        if (pc == null || physics == null) return;

        if (pc != null
            && !pc.inVent
            && !pc.inMovingPlat
            && !pc.walkingToVent
            && !pc.onLadder
            && !physics.Animations.IsPlayingEnterVentAnimation()
            && !physics.Animations.IsPlayingClimbAnimation()
            && !physics.Animations.IsPlayingAnyLadderAnimation()
            && !Pelican.IsEaten(pc.PlayerId)
            && GameStates.IsInTask
            && pc.GetCustomRole().IsUsingPetSystem())
            physics.CancelPet();

        if (!LastProcess.ContainsKey(pc.PlayerId)) LastProcess.TryAdd(pc.PlayerId, Utils.GetTimeStamp() - 2);
        if (LastProcess[pc.PlayerId] + 1 >= Utils.GetTimeStamp()) return;
        LastProcess[pc.PlayerId] = Utils.GetTimeStamp();

        Logger.Info($"Player {pc.GetNameWithRole().RemoveHtmlTags()} petted their pet", "PetActionTrigger");

        var target = Utils.GetClosestPlayer(pc, true);

        _ = new LateTask(() =>
        {
            OnPetUse(pc, target);
        }, 0.2f, $"player {pc.GetNameWithRole().RemoveHtmlTags()} triggered pet on {target.GetNameWithRole().RemoveHtmlTags()}");
    }
    public static void OnPetUse(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null ||
            player.inVent || target.inVent ||
            player.onLadder || target.onLadder ||
            player.inMovingPlat || target.inMovingPlat ||
            player.walkingToVent || target.walkingToVent ||
            player.MyPhysics.Animations.IsPlayingEnterVentAnimation() || target.MyPhysics.Animations.IsPlayingEnterVentAnimation() ||
            player.MyPhysics.Animations.IsPlayingClimbAnimation() || target.MyPhysics.Animations.IsPlayingClimbAnimation() ||
            player.MyPhysics.Animations.IsPlayingAnyLadderAnimation() || target.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
            Pelican.IsEaten(player.PlayerId) || Pelican.IsEaten(target.PlayerId))
        {
            Logger.Info($"Canceled Pet action from {player.GetRealName()} to {target.GetRealName()}", "Pet Action");
            return;
        }

        var playerRole = player.GetCustomRole();
        

        //if (target.Is(CustomRoles.Fragile))
        //{
        //    if ((playerRole.IsImpostorTeamV3() && Options.ImpCanKillFragile.GetBool()) ||
        //        (playerRole.IsNeutral() && Options.NeutralCanKillFragile.GetBool()) ||
        //        (playerRole.IsCrewmate() && Options.CrewCanKillFragile.GetBool()))
        //    {
        //        if (Options.FragileKillerLunge.GetBool())
        //        {
        //            player.RpcMurderPlayerV3(target);
        //        }
        //        else
        //        {
        //            target.RpcMurderPlayerV3(target);
        //        }
        //        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Shattered;
        //        target.SetRealKiller(target);
        //        target.SetRealKiller(player);
        //        player.ResetKillCooldown();
        //        player.SetKillCooldown();
        //        return;
        //    }
        //}
        if (!SkillCountDown.ContainsKey(player.PlayerId))
        {
            SkillCountDown.Add(player.PlayerId, Utils.GetTimeStamp());
        }
        switch (playerRole)
        {
            case CustomRoles.Witch:
            case CustomRoles.Puppeteer:
            case CustomRoles.Mastermind:
            case CustomRoles.EvilDiviner:
            case CustomRoles.HexMaster:
            case CustomRoles.Glitch:
            case CustomRoles.Infectious:
            case CustomRoles.PotionMaster:
            case CustomRoles.Pyromaniac:
                return; //Using double trigger

            case CustomRoles.Warlock:
                return; //wtf

            case CustomRoles.Gangster:
                return; //Lmao i got to recode ganster

            case CustomRoles.BallLightning:
                return; //recode

            case CustomRoles.Sheriff:
        }
    }

}