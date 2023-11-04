using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
class GameEndChecker
{
    private static GameEndPredicate predicate;
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        //ゲーム終了判定済みなら中断
        if (predicate == null) return false;

        //ゲーム終了しないモードで廃村以外の場合は中断
        if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.Error) return false;

        //廃村用に初期値を設定
        var reason = GameOverReason.ImpostorByKill;

        //ゲーム終了判定
        predicate.CheckForEndGame(out reason);

        //ゲーム終了時
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
        {
            // Reset Camouflage
            Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true, GameEnd: true));

            if (reason == GameOverReason.ImpostorBySabotage && (CustomRoles.Jackal.RoleExist() || CustomRoles.Sidekick.RoleExist()) && Jackal.CanWinBySabotageWhenNoImpAlive.GetBool() && !Main.AllAlivePlayerControls.Any(x => x.GetCustomRole().IsImpostorTeam()))
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.WinnerIds.Clear();
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
            }
            var allPlayerList = Main.AllPlayerControls.ToList();
            int allPlayerCount = allPlayerList.Count;
            for (int i = 0; i < allPlayerCount; i++)
            {

                var pc = allPlayerList[i];
                var countType = Main.PlayerStates[pc.PlayerId].countTypes;

                switch (CustomWinnerHolder.WinnerTeam)
                {
                    case CustomWinner.Crewmate:
                        if ((pc.Is(CustomRoleTypes.Crewmate) && (countType == CountTypes.Crew || pc.Is(CustomRoles.Soulless))) ||
                            pc.Is(CustomRoles.Admired) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.Impostor:
                        if (((pc.Is(CustomRoleTypes.Impostor) || pc.GetCustomRole().IsMadmate()) && (countType == CountTypes.Impostor) || pc.Is(CustomRoles.Soulless))
                            || pc.Is(CustomRoles.Madmate) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        break;
                    case CustomWinner.Succubus:
                        if (pc.Is(CustomRoles.Charmed) && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
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
                //潜藏者抢夺胜利
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.DarkHide) && !pc.Data.IsDead
                        && ((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorBySabotage)) || CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide
                        || (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.HumansByTask) && (DarkHide.IsWinKill[pc.PlayerId] == true && DarkHide.SnatchesWin.GetBool()))))
                    {
                        if (!CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DarkHide);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                    }
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Phantom) && pc.GetPlayerTaskState().IsTaskFinished && pc.Data.IsDead
                        && (((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor || CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate || CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal || CustomWinnerHolder.WinnerTeam == CustomWinner.BloodKnight || CustomWinnerHolder.WinnerTeam == CustomWinner.SerialKiller || CustomWinnerHolder.WinnerTeam == CustomWinner.Juggernaut || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Doppelganger || CustomWinnerHolder.WinnerTeam == CustomWinner.PotionMaster || CustomWinnerHolder.WinnerTeam == CustomWinner.Poisoner || CustomWinnerHolder.WinnerTeam == CustomWinner.Succubus || CustomWinnerHolder.WinnerTeam == CustomWinner.Infectious || CustomWinnerHolder.WinnerTeam == CustomWinner.Jinx || CustomWinnerHolder.WinnerTeam == CustomWinner.Virus || CustomWinnerHolder.WinnerTeam == CustomWinner.Arsonist || CustomWinnerHolder.WinnerTeam == CustomWinner.Pelican || CustomWinnerHolder.WinnerTeam == CustomWinner.Wraith || CustomWinnerHolder.WinnerTeam == CustomWinner.Agitater || CustomWinnerHolder.WinnerTeam == CustomWinner.Pestilence || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Rogue || CustomWinnerHolder.WinnerTeam == CustomWinner.Spiritcaller ) && (Options.PhantomSnatchesWin.GetBool()))))  //|| CustomWinnerHolder.WinnerTeam == CustomWinner.Occultist
                    {
                        reason = GameOverReason.ImpostorByKill;
                        if (!CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Phantom);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                    }
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.CursedSoul) && !pc.Data.IsDead
                        && (((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor || CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate || CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal || CustomWinnerHolder.WinnerTeam == CustomWinner.BloodKnight || CustomWinnerHolder.WinnerTeam == CustomWinner.SerialKiller || CustomWinnerHolder.WinnerTeam == CustomWinner.Juggernaut || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Doppelganger || CustomWinnerHolder.WinnerTeam == CustomWinner.PotionMaster || CustomWinnerHolder.WinnerTeam == CustomWinner.Poisoner || CustomWinnerHolder.WinnerTeam == CustomWinner.Succubus || CustomWinnerHolder.WinnerTeam == CustomWinner.Infectious || CustomWinnerHolder.WinnerTeam == CustomWinner.Jinx || CustomWinnerHolder.WinnerTeam == CustomWinner.Virus || CustomWinnerHolder.WinnerTeam == CustomWinner.Arsonist || CustomWinnerHolder.WinnerTeam == CustomWinner.Pelican || CustomWinnerHolder.WinnerTeam == CustomWinner.Wraith || CustomWinnerHolder.WinnerTeam == CustomWinner.Agitater || CustomWinnerHolder.WinnerTeam == CustomWinner.Pestilence || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Rogue || CustomWinnerHolder.WinnerTeam == CustomWinner.Jester || CustomWinnerHolder.WinnerTeam == CustomWinner.Executioner)))) // || CustomWinnerHolder.WinnerTeam == CustomWinner.Occultist
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
                    var egoistCrewList = Main.AllAlivePlayerControls.Where(x => x != null && x.GetCustomRole().IsCrewmate() && x.Is(CustomRoles.Egoist));

                    if (egoistCrewList.Any())
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);

                        foreach (var egoistCrew in egoistCrewList)
                        {
                            CustomWinnerHolder.WinnerIds.Add(egoistCrew.PlayerId);
                        }
                    }
                }

                // Egoist (Impostor)
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                {
                    var egoistImpList = Main.AllAlivePlayerControls.Where(x => x != null && x.GetCustomRole().IsImpostor() && x.Is(CustomRoles.Egoist));

                    if (egoistImpList.Any())
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);

                        foreach (var egoistImp in egoistImpList)
                        {
                            CustomWinnerHolder.WinnerIds.Add(egoistImp.PlayerId);
                        }
                    }
                }

                //神抢夺胜利
                if (CustomRolesHelper.RoleExist(CustomRoles.God))
                {
                    bool isGodWinConverted = false;
                    foreach (var x in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.God)))
                    {
                        if (CustomWinnerHolder.CheckForConvertedWinner(x.PlayerId))
                        {
                            isGodWinConverted = true;
                            break;
                        }
                    }
                    if (!isGodWinConverted) {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
                        Main.AllPlayerControls
                            .Where(p => p.Is(CustomRoles.God) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                }

                //恋人抢夺胜利
                else if (CustomRolesHelper.RoleExist(CustomRoles.Lovers) && !reason.Equals(GameOverReason.HumansByTask))
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
                    //Witch
                    if (pc.Is(CustomRoles.NWitch) && pc.IsAlive() && CustomWinnerHolder.WinnerTeam != CustomWinner.Crewmate && CustomWinnerHolder.WinnerTeam != CustomWinner.Lovers)
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Witch);
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
                    if (!Options.PhantomSnatchesWin.GetBool())
                    {
                        //Phantom
                        if (pc.Is(CustomRoles.Phantom) && !pc.IsAlive() && pc.GetPlayerTaskState().IsTaskFinished)
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Phantom);
                        }
                    }
                    //自爆卡车来咯
                    if (pc.Is(CustomRoles.Provocateur) && Main.Provoked.TryGetValue(pc.PlayerId, out var tar))
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
                    foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Lovers)))
                    {
                        if (CustomWinnerHolder.WinnerIds.Where(x => Utils.GetPlayerById(x).Is(CustomRoles.Lovers)).Any())
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lovers);
                        }
                    }
                }

                //FFF
                if (FFF.isWon)
                {
                    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.FFF);
                    // You have a player id list, no need for another list; also use a for loop instead of LINQ
                    //FFF.winnerFFFList.Do(x => CustomWinnerHolder.WinnerIds.Add(x));
                    for (int i = 0; i < FFF.playerIdList.Count; i++)
                    {
                        CustomWinnerHolder.WinnerIds.Add(FFF.playerIdList[i]);
                    }
                }

                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Totocalcio)))
                {
                    if (Totocalcio.BetPlayer.TryGetValue(pc.PlayerId, out var betTarget) && (
                        CustomWinnerHolder.WinnerIds.Contains(betTarget) ||
                        (Main.PlayerStates.TryGetValue(betTarget, out var ps) && CustomWinnerHolder.WinnerRoles.Contains(ps.MainRole)
                        )))
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Totocalcio);
                    }
                }
                //Romantic win condition
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Romantic)))
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

                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.RuthlessRomantic)))
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
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.VengefulRomantic)))
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
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Lawyer)))
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

                //补充恋人胜利名单
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Lovers || CustomWinnerHolder.AdditionalWinnerTeams.Contains(AdditionalWinners.Lovers))
                {
                    Main.AllPlayerControls
                        .Where(p => p.Is(CustomRoles.Lovers) && !CustomWinnerHolder.WinnerIds.Contains(p.PlayerId))
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }

                //Neutral Win Together
                if (Options.NeutralWinTogether.GetBool() && !CustomWinnerHolder.WinnerIds.Where(x => Utils.GetPlayerById(x) != null && (Utils.GetPlayerById(x).GetCustomRole().IsCrewmate() || Utils.GetPlayerById(x).GetCustomRole().IsImpostor())).Any())
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
                foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Hurried)))
                {
                    if (!Hurried.CheckWinState(pc) && CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                    {
                        CustomWinnerHolder.WinnerIds.Remove(pc.PlayerId);
                        Logger.Info($"Removed {pc.GetNameWithRole()} from winner ids", "Hurried Win Check");
                    }
                }
            }
            ShipStatus.Instance.enabled = false;
            StartEndGame(reason);
            predicate = null;
        }
        return false;
    }
    public static void StartEndGame(GameOverReason reason)
    {
        var sender = new CustomRpcSender("EndGameSender", SendOption.Reliable, true);
        sender.StartMessage(-1); // 5: GameData
        MessageWriter writer = sender.stream;

        //changing back to original names so that summary has OG names.
        //if (Doppelganger.IsEnable)
        //{ 
        //    foreach (var pid in Doppelganger.DoppelVictim.Keys)
        //    {
        //        var pc = Utils.GetPlayerById(pid);
        //        if (pc == null) continue;
        //        if (pid == PlayerControl.LocalPlayer.PlayerId) Main.nickName = Doppelganger.DoppelVictim[pid];
        //        else pc.RpcSetName(Doppelganger.DoppelVictim[pid]);
        //    }
        //}
        if (Blackmailer.IsEnable) Blackmailer.ForBlackmailer.Clear();
        //ゴーストロール化
        List<byte> ReviveRequiredPlayerIds = new();
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
                if (!pc.Data.IsDead) ReviveRequiredPlayerIds.Add(pc.PlayerId);
                if (ToGhostImpostor)
                {
                    Logger.Info($"{pc.GetNameWithRole()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                    sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                        .Write((ushort)RoleTypes.ImpostorGhost)
                        .EndRpc();
                    pc.SetRole(RoleTypes.ImpostorGhost);
                }
                else
                {
                    Logger.Info($"{pc.GetNameWithRole()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                    sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                        .Write((ushort)RoleTypes.CrewmateGhost)
                        .EndRpc();
                    pc.SetRole(RoleTypes.Crewmate);
                }
            }
            SetEverythingUpPatch.LastWinsReason = winner is CustomWinner.Crewmate or CustomWinner.Impostor ? GetString($"GameOverReason.{reason}") : "";
        }

        // CustomWinnerHolderの情報の同期
        sender.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame);
        CustomWinnerHolder.WriteTo(sender.stream);
        sender.EndRpc();

        // GameDataによる蘇生処理
        writer.StartMessage(1); // Data
        {
            writer.WritePacked(GameData.Instance.NetId); // NetId
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (ReviveRequiredPlayerIds.Contains(info.PlayerId))
                {
                    // 蘇生&メッセージ書き込み
                    info.IsDead = false;
                    writer.StartMessage(info.PlayerId);
                    info.Serialize(writer);
                    writer.EndMessage();
                }
            }
            writer.EndMessage();
        }

        sender.EndMessage();

        // バニラ側のゲーム終了RPC
        writer.StartMessage(8); //8: EndGame
        {
            writer.Write(AmongUsClient.Instance.GameId); //GameId
            writer.Write((byte)reason); //GameoverReason
            writer.Write(false); //showAd
        }
        writer.EndMessage();

        sender.SendMessage();
    }

    public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();

    // ===== ゲーム終了条件 =====
    // 通常ゲーム用
    class NormalGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
            if (CheckGameEndByLivingPlayers(out reason)) return true;
            if (CheckGameEndByTask(out reason)) return true;
            if (CheckGameEndBySabotage(out reason)) return true;

            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;

            if (CustomRoles.Sunnyboy.RoleExist() && Main.AllAlivePlayerControls.Count() > 1) return false;
            var neutralRoleCounts = new Dictionary<CountTypes, int>();
            var apcList = Main.AllAlivePlayerControls.ToList();
            int dual = 0, impCount = 0, crewCount = 0;

            for (int i = 0; i < apcList.Count; i++)
            {
                PlayerControl pc = apcList[i];
                if (pc == null) continue;

                dual = pc.Is(CustomRoles.DualPersonality) ? 1 : 0;
                var countType = Main.PlayerStates[pc.PlayerId].countTypes;
                switch (countType)
                {
                    case CountTypes.OutOfGame:
                    case CountTypes.None:
                    case CountTypes.NWitch:
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

            if (Main.AllAlivePlayerControls.Any() && Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers))) // if lover is alive lover wins
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                return true;
            }

            else if (totalNKAlive == 0) // total number of nks alive 0
            {
                if (crewCount == 0 && impCount == 0) // Crew and Imp both 0, everyone is dead
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }

                else if (crewCount <= impCount) // Crew less than or equal to Imps, Imp wins
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (impCount == 0) // Remaining Imps are 0, Crew wins (neutral is already dead)
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                return true;
            }

            else
            {
                if (impCount >= 1) return false; // Both Imp and NK are alive, the game must continue
                if (crewCount > totalNKAlive) return false; // Imps are dead, but Crew still outnumbers NK (the game must continue)
                else // Imps dead, Crew <= NK, Checking if All nk alive are in 1 team 
                {
                    var winners = neutralRoleCounts.Where(kvp => kvp.Value == totalNKAlive).ToList();
                    var winnnerLength = winners.Count;
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
            return true;
        }

        return false;
    }
}