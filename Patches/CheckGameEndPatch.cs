using System.Collections;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hazel;
using UnityEngine;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Neutral;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckEndGameViaTasks))]
class CheckEndGameViaTasksForNormalPatch
{
    public static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}
[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
class GameEndCheckerForNormal
{
    public static GameEndPredicate predicate;
    public static bool ShowAllRolesWhenGameEnd = false;
    public static bool ShouldNotCheck = false;

    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        if (predicate == null || ShouldNotCheck) return false;

        if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.Error) return false;

        ShowAllRolesWhenGameEnd = false;
        var reason = GameOverReason.ImpostorByKill;
        predicate.CheckForEndGame(out reason);

        // FFA
        if (Options.CurrentGameMode == CustomGameMode.FFA)
        {
            if (CustomWinnerHolder.WinnerIds.Count > 0 || CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                ShipStatus.Instance.enabled = false;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }

        // Start end game
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
        {
            // Clear all Notice players 
            NameNotifyManager.Reset();

            // Reset Camouflage
            Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true, GameEnd: true));

            // Show all roles
            ShowAllRolesWhenGameEnd = true;

            // Update all Notify Roles
            Utils.DoNotifyRoles(ForceLoop: true, NoCache: true);

            Logger.Info("Start end", "CheckEndCriteria.Prefix");

            if (reason == GameOverReason.ImpostorBySabotage && (CustomRoles.Jackal.RoleExist() || CustomRoles.Sidekick.RoleExist()) && Jackal.CanWinBySabotageWhenNoImpAlive.GetBool() && !Main.AllAlivePlayerControls.Any(x => x.GetCustomRole().IsImpostorTeam()))
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.WinnerIds.Clear();
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                var countType = Main.PlayerStates[pc.PlayerId].countTypes;

                switch (CustomWinnerHolder.WinnerTeam)
                {
                    case CustomWinner.Crewmate:
                        if ((pc.Is(Custom_Team.Crewmate) && (countType == CountTypes.Crew || pc.Is(CustomRoles.Soulless))) ||
                            pc.Is(CustomRoles.Admired) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.Impostor:
                        if (((pc.Is(Custom_Team.Impostor) || pc.GetCustomRole().IsMadmate()) && (countType == CountTypes.Impostor || pc.Is(CustomRoles.Soulless)))
                            || pc.Is(CustomRoles.Madmate) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.Cultist:
                        if ((pc.Is(CustomRoles.Charmed) || pc.Is(CustomRoles.Cultist)) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.CursedSoul:
                        if (pc.Is(CustomRoles.Soulless) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.Infectious:
                        if (pc.Is(CustomRoles.Infected) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.PlagueDoctor:
                        if (pc.Is(CustomRoles.PlagueDoctor) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.Virus:
                        if (pc.Is(CustomRoles.Contagious) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.Jackal:
                        if ((pc.Is(CustomRoles.Sidekick) || pc.Is(CustomRoles.Recruit)) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.Spiritcaller:
                        if (pc.Is(CustomRoles.EvilSpirit) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.RuthlessRomantic:
                        if (pc.Is(CustomRoles.RuthlessRomantic) && !CustomWinnerHolder.WinnerIds.Contains(Romantic.BetPlayer[pc.PlayerId]))
                        {
                            CustomWinnerHolder.WinnerIds.Add(Romantic.BetPlayer[pc.PlayerId]);
                        }
                        break;
                }
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None and not CustomWinner.Error)
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Stalker) && pc.IsAlive()
                        && ((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorBySabotage)) || CustomWinnerHolder.WinnerTeam == CustomWinner.Stalker
                        || (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.HumansByTask) && (Stalker.IsWinKill[pc.PlayerId] == true && Stalker.SnatchesWin.GetBool()))))
                    {
                        if (!CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Stalker);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                    }
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Specter) && pc.GetPlayerTaskState().IsTaskFinished && pc.Data.IsDead
                        && (((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor || CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate || CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal || CustomWinnerHolder.WinnerTeam == CustomWinner.BloodKnight || CustomWinnerHolder.WinnerTeam == CustomWinner.SerialKiller || CustomWinnerHolder.WinnerTeam == CustomWinner.Juggernaut || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Doppelganger || CustomWinnerHolder.WinnerTeam == CustomWinner.PotionMaster || CustomWinnerHolder.WinnerTeam == CustomWinner.Poisoner || CustomWinnerHolder.WinnerTeam == CustomWinner.Cultist || CustomWinnerHolder.WinnerTeam == CustomWinner.Infectious || CustomWinnerHolder.WinnerTeam == CustomWinner.Jinx || CustomWinnerHolder.WinnerTeam == CustomWinner.Virus || CustomWinnerHolder.WinnerTeam == CustomWinner.Arsonist || CustomWinnerHolder.WinnerTeam == CustomWinner.Pelican || CustomWinnerHolder.WinnerTeam == CustomWinner.Wraith || CustomWinnerHolder.WinnerTeam == CustomWinner.Agitater || CustomWinnerHolder.WinnerTeam == CustomWinner.Pestilence || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Spiritcaller || CustomWinnerHolder.WinnerTeam == CustomWinner.Quizmaster ) && (Specter.SnatchesWin.GetBool() || CustomWinnerHolder.WinnerTeam == CustomWinner.PlagueDoctor))))  //|| CustomWinnerHolder.WinnerTeam == CustomWinner.Occultist
                    {
                        reason = GameOverReason.ImpostorByKill;
                        if (!CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Specter);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                    }
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.CursedSoul) && !pc.Data.IsDead
                        && (((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor || CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate || CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal || CustomWinnerHolder.WinnerTeam == CustomWinner.BloodKnight || CustomWinnerHolder.WinnerTeam == CustomWinner.SerialKiller || CustomWinnerHolder.WinnerTeam == CustomWinner.Juggernaut || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Doppelganger || CustomWinnerHolder.WinnerTeam == CustomWinner.PotionMaster || CustomWinnerHolder.WinnerTeam == CustomWinner.Poisoner || CustomWinnerHolder.WinnerTeam == CustomWinner.Cultist || CustomWinnerHolder.WinnerTeam == CustomWinner.Infectious || CustomWinnerHolder.WinnerTeam == CustomWinner.Jinx || CustomWinnerHolder.WinnerTeam == CustomWinner.Virus || CustomWinnerHolder.WinnerTeam == CustomWinner.Arsonist || CustomWinnerHolder.WinnerTeam == CustomWinner.Pelican || CustomWinnerHolder.WinnerTeam == CustomWinner.Wraith || CustomWinnerHolder.WinnerTeam == CustomWinner.Agitater || CustomWinnerHolder.WinnerTeam == CustomWinner.Pestilence || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Jester || CustomWinnerHolder.WinnerTeam == CustomWinner.Executioner || CustomWinnerHolder.WinnerTeam == CustomWinner.PlagueDoctor)))) // || CustomWinnerHolder.WinnerTeam == CustomWinner.Occultist
                    {
                        reason = GameOverReason.ImpostorByKill;
                        if (!CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CursedSoul);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Soulless);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        else CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Soulless);
                    }
                }

                // Egoist (Crewmate)
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
                {
                    var egoistCrewArray = Main.AllAlivePlayerControls.Where(x => x != null && x.GetCustomRole().IsCrewmate() && x.Is(CustomRoles.Egoist)).ToArray();

                    if (egoistCrewArray.Length > 0)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);

                        foreach (var egoistCrew in egoistCrewArray)
                        {
                            CustomWinnerHolder.WinnerIds.Add(egoistCrew.PlayerId);
                        }
                    }
                }

                // Egoist (Impostor)
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                {
                    var egoistImpArray = Main.AllAlivePlayerControls.Where(x => x != null && x.GetCustomRole().IsImpostor() && x.Is(CustomRoles.Egoist)).ToArray();

                    if (egoistImpArray.Length > 0)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);

                        foreach (var egoistImp in egoistImpArray)
                        {
                            CustomWinnerHolder.WinnerIds.Add(egoistImp.PlayerId);
                        }
                    }
                }

                //神抢夺胜利
                if (CustomRoles.God.RoleExist())
                {
                    bool isGodWinConverted = false;
                    var godArray = Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.God)).ToArray();
                    
                    foreach (var god in godArray)
                    {
                        if (CustomWinnerHolder.CheckForConvertedWinner(god.PlayerId))
                        {
                            isGodWinConverted = true;
                            break;
                        }
                    }
                    if (!isGodWinConverted) 
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
                        godArray.Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                }

                //恋人抢夺胜利
                else if (CustomRoles.Lovers.RoleExist() && !reason.Equals(GameOverReason.HumansByTask))
                {
                    if (!(!Main.LoversPlayers.ToArray().All(p => p.IsAlive()) && Options.LoverSuicide.GetBool()))
                    {
                        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican)
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.Lovers))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                        }
                    }
                }

                //追加胜利
                foreach (var pc in Main.AllPlayerControls)
                {
                    //Opportunist
                    if (pc.Is(CustomRoles.Opportunist) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Opportunist);
                    }
                    //pixie
                    if (pc.Is(CustomRoles.Pixie) && !CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId)) Pixie.PixieWinCondition(pc);
                    //Shaman
                    if (pc.Is(CustomRoles.Shaman) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Shaman);
                    }
                    if (pc.Is(CustomRoles.Taskinator) && pc.IsAlive() && CustomWinnerHolder.WinnerTeam != CustomWinner.Crewmate)
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Taskinator);
                    }
                    if (pc.Is(CustomRoles.Pursuer) && pc.IsAlive() && CustomWinnerHolder.WinnerTeam != CustomWinner.Jester && CustomWinnerHolder.WinnerTeam != CustomWinner.Lovers && CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist && CustomWinnerHolder.WinnerTeam != CustomWinner.Executioner && CustomWinnerHolder.WinnerTeam != CustomWinner.Collector && CustomWinnerHolder.WinnerTeam != CustomWinner.Innocent && CustomWinnerHolder.WinnerTeam != CustomWinner.Youtuber)
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Pursuer);
                    }
                    //Sunnyboy
                    if (pc.Is(CustomRoles.Sunnyboy) && !pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Sunnyboy);
                    }
                    //Maverick
                    if (pc.Is(CustomRoles.Maverick) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Maverick);
                    }
                    if (!Specter.SnatchesWin.GetBool())
                    {
                        //Phantom
                        if (pc.Is(CustomRoles.Specter) && !pc.IsAlive() && pc.GetPlayerTaskState().IsTaskFinished)
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Specter);
                        }
                    }
                    //自爆卡车来咯
                    if (pc.Is(CustomRoles.Provocateur) && Provocateur.Provoked.TryGetValue(pc.PlayerId, out var tar))
                    {
                        if (!CustomWinnerHolder.WinnerIds.Contains(tar))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Provocateur);
                        }
                    }
                }

                //Lovers follow winner
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Lovers)
                {
                    var loverArray = Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Lovers)).ToArray();

                    foreach (var lover in loverArray)
                    {
                        if (CustomWinnerHolder.WinnerIds.Any(x => Utils.GetPlayerById(x).Is(CustomRoles.Lovers)))
                        {
                            CustomWinnerHolder.WinnerIds.Add(lover.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lovers);
                        }
                    }
                }

                // Hater
                if (Hater.isWon)
                {
                    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Hater);
                    // You have a player id list, no need for another list; also use a for loop instead of LINQ
                    //Hater.winnerHaterList.Do(x => CustomWinnerHolder.WinnerIds.Add(x));

                    var HaterArray = Hater.playerIdList.ToArray();
                    foreach (var Hater in HaterArray)
                    {
                        CustomWinnerHolder.WinnerIds.Add(Hater);
                    }
                }

                //Romantic win condition
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Romantic)).ToArray())
                {
                    if (Romantic.BetPlayer.TryGetValue(pc.PlayerId, out var betTarget) && (
                        CustomWinnerHolder.WinnerIds.Contains(betTarget) ||
                        (Main.PlayerStates.TryGetValue(betTarget, out var ps) && CustomWinnerHolder.WinnerRoles.Contains(ps.MainRole)
                        )))
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Romantic);
                    }
                }

                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.RuthlessRomantic)).ToArray())
                {
                    if (Romantic.BetPlayer.TryGetValue(pc.PlayerId, out var betTarget) && (
                        CustomWinnerHolder.WinnerIds.Contains(betTarget) ||
                        (Main.PlayerStates.TryGetValue(betTarget, out var ps) && CustomWinnerHolder.WinnerRoles.Contains(ps.MainRole)
                        )))
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        //    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.RuthlessRomantic);
                    }
                }

                //Vengeful Romantic win condition
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.VengefulRomantic)).ToArray())
                {
                    if (VengefulRomantic.hasKilledKiller)
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.WinnerIds.Add(Romantic.BetPlayer[pc.PlayerId]);
                        //if ((Romantic.BetPlayer.TryGetValue(pc.PlayerId, out var RomanticPartner)) && pc.PlayerId == RomanticPartner)
                        //    CustomWinnerHolder.WinnerIds.Add(RomanticPartner);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.VengefulRomantic);
                    }
                }

                //Lawyer win cond
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Lawyer)).ToArray())
                {
                    if (Lawyer.Target.TryGetValue(pc.PlayerId, out var lawyertarget) && (
                        CustomWinnerHolder.WinnerIds.Contains(lawyertarget) ||
                        (Main.PlayerStates.TryGetValue(lawyertarget, out var ps) && CustomWinnerHolder.WinnerRoles.Contains(ps.MainRole)
                        )))
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lawyer);
                    }
                }


                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Follower)).ToArray())
                {
                    if (Follower.BetPlayer.TryGetValue(pc.PlayerId, out var betTarget) && (
                        CustomWinnerHolder.WinnerIds.Contains(betTarget) ||
                        (Main.PlayerStates.TryGetValue(betTarget, out var ps) && CustomWinnerHolder.WinnerRoles.Contains(ps.MainRole)
                        )))
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Follower);
                    }
                }


                //补充恋人胜利名单
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Lovers || CustomWinnerHolder.AdditionalWinnerTeams.Contains(AdditionalWinners.Lovers))
                {
                    Main.AllPlayerControls
                        .Where(p => p.Is(CustomRoles.Lovers) && !CustomWinnerHolder.WinnerIds.Contains(p.PlayerId))
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }

                //Neutral Win Together
                if (Options.NeutralWinTogether.GetBool() && !CustomWinnerHolder.WinnerIds.Any(x => Utils.GetPlayerById(x) != null && (Utils.GetPlayerById(x).GetCustomRole().IsCrewmate() || Utils.GetPlayerById(x).GetCustomRole().IsImpostor())))
                {
                    foreach (var pc in Main.AllPlayerControls)
                        if (pc.GetCustomRole().IsNeutral() && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) && !CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole()))
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }
                else if (!Options.NeutralWinTogether.GetBool() && Options.NeutralRoleWinTogether.GetBool())
                {
                    foreach (var id in CustomWinnerHolder.WinnerIds)
                    {
                        var pc = Utils.GetPlayerById(id);
                        if (pc == null || !pc.GetCustomRole().IsNeutral()) continue;

                        foreach (var tar in Main.AllPlayerControls)
                            if (!CustomWinnerHolder.WinnerIds.Contains(tar.PlayerId) && tar.GetCustomRole() == pc.GetCustomRole())
                                CustomWinnerHolder.WinnerIds.Add(tar.PlayerId);
                    }
                }

                //Remove hurried task not done player from winner id
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Hurried)).ToArray())
                {
                    if (!Hurried.CheckWinState(pc) && CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                    {
                        CustomWinnerHolder.WinnerIds.Remove(pc.PlayerId);
                        Logger.Info($"Removed {pc.GetNameWithRole()} from winner ids", "Hurried Win Check");
                    }
                }
            }

            /*Keep Schrodinger cat win condition at last*/
            Main.AllPlayerControls.Where(pc => pc.Is(CustomRoles.SchrodingersCat)).ToList().ForEach(SchrodingersCat.SchrodingerWinCondition);

            // Remember true win to display in chat
            var winner = CustomWinnerHolder.WinnerTeam;
            SetEverythingUpPatch.LastWinsReason = winner is CustomWinner.Crewmate or CustomWinner.Impostor ? GetString($"GameOverReason.{reason}") : "";

            ShipStatus.Instance.enabled = false;
            // When crewmates win, show as impostor win, for displaying all names players
            //reason = reason is GameOverReason.HumansByVote or GameOverReason.HumansByTask ? GameOverReason.ImpostorByVote : reason;
            StartEndGame(reason);
            predicate = null;
        }
        return false;
    }
    public static void StartEndGame(GameOverReason reason)
    {
        // Sync of CustomWinnerHolder info
        var winnerWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, SendOption.Reliable);
        CustomWinnerHolder.WriteTo(winnerWriter);
        AmongUsClient.Instance.FinishRpcImmediately(winnerWriter);

        AmongUsClient.Instance.StartCoroutine(CoEndGame(AmongUsClient.Instance, reason).WrapToIl2Cpp());
    }
    public static bool ForEndGame = false;
    private static IEnumerator CoEndGame(AmongUsClient self, GameOverReason reason)
    {
        CustomRoleManager.AllEnabledRoles.Do(roleClass => roleClass.OnCoEndGame());
        ForEndGame = true;

        // Set ghost role
        List<byte> ReviveRequiredPlayerIds = [];
        var winner = CustomWinnerHolder.WinnerTeam;
        foreach (var pc in Main.AllPlayerControls)
        {
            if (winner == CustomWinner.Draw)
            {
                SetGhostRole(ToGhostImpostor: true);
                continue;
            }
            bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                    CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
            bool isCrewmateWin = reason.Equals(GameOverReason.HumansByVote) || reason.Equals(GameOverReason.HumansByTask);
            SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);

            void SetGhostRole(bool ToGhostImpostor)
            {
                var isDead = pc.Data.IsDead;
                if (!isDead) ReviveRequiredPlayerIds.Add(pc.PlayerId);

                if (ToGhostImpostor)
                {
                    Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()}: changed to ImpostorGhost", "ResetRoleAndEndGame");
                    pc.RpcSetRole(RoleTypes.ImpostorGhost);
                }
                else
                {
                    Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()}: changed to CrewmateGhost", "ResetRoleAndEndGame");
                    pc.RpcSetRole(RoleTypes.CrewmateGhost);
                }
                // Put it back on so it can't be auto-muted during the delay until resuscitation ~~ TOH comment
                pc.Data.IsDead = isDead;
            }
        }

        // Delay to ensure that resuscitation is delivered after the ghost roll setting
        yield return new WaitForSeconds(EndGameDelay);

        if (ReviveRequiredPlayerIds.Count > 0)
        {
            // Resuscitation Resuscitate one person per transmission to prevent the packet from swelling up and dying
            for (int i = 0; i < ReviveRequiredPlayerIds.Count; i++)
            {
                var playerId = ReviveRequiredPlayerIds[i];
                var playerInfo = GameData.Instance.GetPlayerById(playerId);
                // resuscitation
                playerInfo.IsDead = false;
                // transmission
                playerInfo.SetDirtyBit(0b_1u << playerId);
                AmongUsClient.Instance.SendAllStreamedObjects();
            }
            // Delay to ensure that the end of the game is delivered at the end of the game
            yield return new WaitForSeconds(EndGameDelay);
        }

        // Update all Notify Roles
        Utils.DoNotifyRoles(ForceLoop: true, NoCache: true);

        // Start End Game
        GameManager.Instance.RpcEndGame(reason, false);
    }
    private const float EndGameDelay = 0.2f;

    public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
    public static void SetPredicateToFFA() => predicate = new FFAGameEndPredicate();


    // ===== Check Game End =====
    // For Normal Games
    class NormalGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
            if (CheckGameEndByLivingPlayers(out reason) || CheckGameEndByTask(out reason) || CheckGameEndBySabotage(out reason)) return true;
            return false;
        }

        public static bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;

            if (Sunnyboy.HasEnabled && Sunnyboy.CheckGameEnd()) return false;
            var neutralRoleCounts = new Dictionary<CountTypes, int>();
            var allAlivePlayerList = Main.AllAlivePlayerControls.ToArray();
            int dual = 0, impCount = 0, crewCount = 0;

            foreach (var pc in allAlivePlayerList)
            {
                if (pc == null) continue;

                dual = Paranoia.IsExistInGame(pc) ? 1 : 0;
                var countType = Main.PlayerStates[pc.PlayerId].countTypes;
                switch (countType)
                {
                    case CountTypes.OutOfGame:
                    case CountTypes.None:
                        continue;
                    case CountTypes.Impostor:
                        impCount++;
                        impCount += dual;
                        break;
                    case CountTypes.Crew:
                        crewCount++;
                        crewCount += dual;
                        break;
                    default:
                        if (neutralRoleCounts.ContainsKey(countType))
                            neutralRoleCounts[countType]++;
                        else
                            neutralRoleCounts[countType] = 1;
                        neutralRoleCounts[countType] += dual;
                        break;
                }
            }

            int totalNKAlive = neutralRoleCounts.Sum(kvp => kvp.Value);

            if (crewCount == 0 && impCount == 0 && totalNKAlive == 0) // Everyone is dead
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                return true;
            }

            else if (Main.AllAlivePlayerControls.Length > 0 && Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers))) // if lover is alive lover wins
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                return true;
            }

            else if (totalNKAlive == 0) // total number of nks alive 0
            {
                if (crewCount <= impCount) // Crew less than or equal to Imps, Imp wins
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }

                else if (impCount == 0) // Remaining Imps are 0, Crew wins (neutral is already dead)
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }

                else if (crewCount > impCount) return false; // crewmate is more than imp (the game must continue)
                return true;
            }

            else
            {
                if (impCount >= 1) return false; // Both Imp and NK are alive, the game must continue
                if (crewCount > totalNKAlive) return false; // Imps are dead, but Crew still outnumbers NK (the game must continue)
                else // Imps dead, Crew <= NK, Checking if All nk alive are in 1 team 
                {
                    var winners = neutralRoleCounts.Where(kvp => kvp.Value == totalNKAlive).ToArray();
                    var winnnerLength = winners.Length;
                    if (winnnerLength == 1)
                    {
                        var winnerRole = winners[0].Key.GetNeutralCustomRoleFromCountType();
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(winnerRole.GetNeutralCustomWinnerFromRole());
                        CustomWinnerHolder.WinnerRoles.Add(winnerRole);
                    }
                    else if (winnnerLength == 0)
                    {
                        return false; // Not all alive neutrals were in one team
                    }
                    return true;
                }
            }
        }
    }
}

// For FFA Games
class FFAGameEndPredicate : GameEndPredicate
{
    public override bool CheckForEndGame(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorByKill;
        if (CustomWinnerHolder.WinnerIds.Count > 0) return false;
        if (CheckGameEndByLivingPlayers(out reason)) return true;
        return false;
    }

    public static bool CheckGameEndByLivingPlayers(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorByKill;

        if (FFAManager.RoundTime <= 0)
        {
            var winner = Main.AllPlayerControls.Where(x => !x.Is(CustomRoles.GM) && x != null).OrderBy(x => FFAManager.GetRankOfScore(x.PlayerId)).First();

            byte winnerId;
            if (winner == null) winnerId = 0;
            else winnerId = winner.PlayerId;

            Logger.Warn($"Winner: {Utils.GetPlayerById(winnerId).GetRealName().RemoveHtmlTags()}", "FFA");

            CustomWinnerHolder.WinnerIds = [winnerId];

            Main.DoBlockNameChange = true;

            return true;
        }
        else if (Main.AllAlivePlayerControls.Length == 1)
        {
            var winner = Main.AllAlivePlayerControls.FirstOrDefault();

            Logger.Info($"Winner: {winner.GetRealName().RemoveHtmlTags()}", "FFA");

            CustomWinnerHolder.WinnerIds = [winner.PlayerId];

            Main.DoBlockNameChange = true;

            return true;
        }
        else if (Main.AllAlivePlayerControls.Length == 0)
        {
            FFAManager.RoundTime = 0;
            Logger.Warn("No players alive. Force ending the game", "FFA");
            return false;
        }
        else return false;
    }
}

public abstract class GameEndPredicate
{
    /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
    /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
    /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
    public abstract bool CheckForEndGame(out GameOverReason reason);

    /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
    public virtual bool CheckGameEndByTask(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorByKill;
        if (Options.DisableTaskWin.GetBool() || TaskState.InitialTotalTasks == 0) return false;

        if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
        {
            reason = GameOverReason.HumansByTask;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
            Logger.Info($"Game End By Completed All Tasks", "CheckGameEndBySabotage");
            return true;
        }
        return false;
    }
    /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
    public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorByKill;
        if (ShipStatus.Instance.Systems == null) return false;

        // TryGetValueは使用不可
        var systems = ShipStatus.Instance.Systems;
        LifeSuppSystemType LifeSupp;
        if (systems.ContainsKey(SystemTypes.LifeSupp) && // サボタージュ存在確認
            (LifeSupp = systems[SystemTypes.LifeSupp].TryCast<LifeSuppSystemType>()) != null && // キャスト可能確認
            LifeSupp.Countdown < 0f) // タイムアップ確認
        {
            // 酸素サボタージュ
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            reason = GameOverReason.ImpostorBySabotage;
            LifeSupp.Countdown = 10000f;
            Logger.Info($"Game End By LifeSupp Sabotage", "CheckGameEndBySabotage");
            return true;
        }

        ISystemType sys = null;
        if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
        else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];
        else if (systems.ContainsKey(SystemTypes.HeliSabotage)) sys = systems[SystemTypes.HeliSabotage];

        ICriticalSabotage critical;
        if (sys != null && // サボタージュ存在確認
            (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
            critical.Countdown < 0f) // タイムアップ確認
        {
            // リアクターサボタージュ
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            reason = GameOverReason.ImpostorBySabotage;
            critical.ClearSabotage();
            Logger.Info($"Game End By Critical Sabotage", "CheckGameEndBySabotage");
            return true;
        }

        return false;
    }
}