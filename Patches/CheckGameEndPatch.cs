using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Double;
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

            switch (CustomWinnerHolder.WinnerTeam)
            {
                case CustomWinner.Crewmate:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoleTypes.Crewmate) && !pc.Is(CustomRoles.Madmate) && !pc.Is(CustomRoles.Rogue) && !pc.Is(CustomRoles.Charmed) && !pc.Is(CustomRoles.Recruit) && !pc.Is(CustomRoles.Infected) && !pc.Is(CustomRoles.Contagious) && !pc.Is(CustomRoles.EvilSpirit) && !pc.Is(CustomRoles.Recruit) || pc.Is(CustomRoles.Admired))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Impostor:
                    Main.AllPlayerControls
                        .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoles.Madmate) || pc.Is(CustomRoles.Crewpostor) || pc.Is(CustomRoles.Parasite) || pc.Is(CustomRoles.Refugee) || pc.Is(CustomRoles.Convict)) && !pc.Is(CustomRoles.Rogue) && !pc.Is(CustomRoles.Charmed) && !pc.Is(CustomRoles.Recruit) && !pc.Is(CustomRoles.Infected) && !pc.Is(CustomRoles.Contagious) && !pc.Is(CustomRoles.EvilSpirit) && !pc.Is(CustomRoles.Recruit) && !pc.Is(CustomRoles.Admired))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Succubus:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.Succubus) || pc.Is(CustomRoles.Charmed) && !pc.Is(CustomRoles.Rogue) && !pc.Is(CustomRoles.Admired))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.CursedSoul:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.CursedSoul) || pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Rogue) && !pc.Is(CustomRoles.Admired))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Infectious:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.Infectious) || pc.Is(CustomRoles.Infected) && !pc.Is(CustomRoles.Rogue) && !pc.Is(CustomRoles.Admired))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Virus:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoles.Virus) || pc.Is(CustomRoles.Contagious) && !pc.Is(CustomRoles.Rogue) && !pc.Is(CustomRoles.Admired))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Jackal:
                    Main.AllPlayerControls
                        .Where(pc => (pc.Is(CustomRoles.Jackal) || pc.Is(CustomRoles.Sidekick) || pc.Is(CustomRoles.Recruit)) && !pc.Is(CustomRoles.Infected) && !pc.Is(CustomRoles.Rogue) && !pc.Is(CustomRoles.Admired))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Spiritcaller:
                    Main.AllPlayerControls
                        .Where(pc => (pc.Is(CustomRoles.Spiritcaller) || pc.Is(CustomRoles.EvilSpirit)))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.RuthlessRomantic:
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.RuthlessRomantic))
                        {
                            CustomWinnerHolder.WinnerIds.Add(Romantic.BetPlayer[pc.PlayerId]);

                        }

                    }
                    //Main.AllPlayerControls
                    //    .Where(pc => (pc.Is(CustomRoles.RuthlessRomantic) || (Romantic.BetPlayer.TryGetValue(pc.PlayerId, out var RomanticPartner)) && pc.PlayerId == RomanticPartner))
                    //    .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
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
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DarkHide);
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    }
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.Phantom) && pc.GetPlayerTaskState().IsTaskFinished && pc.Data.IsDead
                        && (((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor || CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate || CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal || CustomWinnerHolder.WinnerTeam == CustomWinner.BloodKnight || CustomWinnerHolder.WinnerTeam == CustomWinner.SerialKiller || CustomWinnerHolder.WinnerTeam == CustomWinner.Juggernaut || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Doppelganger || CustomWinnerHolder.WinnerTeam == CustomWinner.PotionMaster || CustomWinnerHolder.WinnerTeam == CustomWinner.Poisoner || CustomWinnerHolder.WinnerTeam == CustomWinner.Succubus || CustomWinnerHolder.WinnerTeam == CustomWinner.Infectious || CustomWinnerHolder.WinnerTeam == CustomWinner.Jinx || CustomWinnerHolder.WinnerTeam == CustomWinner.Virus || CustomWinnerHolder.WinnerTeam == CustomWinner.Arsonist || CustomWinnerHolder.WinnerTeam == CustomWinner.Pelican || CustomWinnerHolder.WinnerTeam == CustomWinner.Wraith || CustomWinnerHolder.WinnerTeam == CustomWinner.Agitater || CustomWinnerHolder.WinnerTeam == CustomWinner.Pestilence || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Rogue || CustomWinnerHolder.WinnerTeam == CustomWinner.Spiritcaller ) && (Options.PhantomSnatchesWin.GetBool()))))  //|| CustomWinnerHolder.WinnerTeam == CustomWinner.Occultist
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Phantom);
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    }
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.CursedSoul) && !pc.Data.IsDead
                        && (((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor || CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate || CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal || CustomWinnerHolder.WinnerTeam == CustomWinner.BloodKnight || CustomWinnerHolder.WinnerTeam == CustomWinner.SerialKiller || CustomWinnerHolder.WinnerTeam == CustomWinner.Juggernaut || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Doppelganger || CustomWinnerHolder.WinnerTeam == CustomWinner.PotionMaster || CustomWinnerHolder.WinnerTeam == CustomWinner.Poisoner || CustomWinnerHolder.WinnerTeam == CustomWinner.Succubus || CustomWinnerHolder.WinnerTeam == CustomWinner.Infectious || CustomWinnerHolder.WinnerTeam == CustomWinner.Jinx || CustomWinnerHolder.WinnerTeam == CustomWinner.Virus || CustomWinnerHolder.WinnerTeam == CustomWinner.Arsonist || CustomWinnerHolder.WinnerTeam == CustomWinner.Pelican || CustomWinnerHolder.WinnerTeam == CustomWinner.Wraith || CustomWinnerHolder.WinnerTeam == CustomWinner.Agitater || CustomWinnerHolder.WinnerTeam == CustomWinner.Pestilence || CustomWinnerHolder.WinnerTeam == CustomWinner.Bandit || CustomWinnerHolder.WinnerTeam == CustomWinner.Rogue || CustomWinnerHolder.WinnerTeam == CustomWinner.Jester || CustomWinnerHolder.WinnerTeam == CustomWinner.Executioner)))) // || CustomWinnerHolder.WinnerTeam == CustomWinner.Occultist
                    {
                        reason = GameOverReason.ImpostorByKill;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CursedSoul);
                        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Soulless);
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
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
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
                    Main.AllPlayerControls
                        .Where(p => p.Is(CustomRoles.God) && p.IsAlive())
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
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
                    //NiceMini
                    //if (pc.Is(CustomRoles.NiceMini) && pc.IsAlive())
                    //{
                    //    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    //    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.NiceMini);
                    //}
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
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Lovers && !CustomWinnerHolder.AdditionalWinnerTeams.Contains(AdditionalWinners.Lovers) && !CustomRolesHelper.RoleExist(CustomRoles.Lovers) && !CustomRolesHelper.RoleExist(CustomRoles.Ntr))
                {
                    foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.FFF)))
                    {
                        if (Main.AllPlayerControls.Where(x => (x.Is(CustomRoles.Lovers) || x.Is(CustomRoles.Ntr)) && x.GetRealKiller()?.PlayerId == pc.PlayerId).Any())
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.FFF);
                        }
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

            if (CustomRolesHelper.RoleExist(CustomRoles.Sunnyboy) && Main.AllAlivePlayerControls.Any()) return false;

            int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
            int Jackal = Utils.AlivePlayersCount(CountTypes.Jackal);
            int Pel = Utils.AlivePlayersCount(CountTypes.Pelican);
            int Crew = Utils.AlivePlayersCount(CountTypes.Crew);
            int Gam = Utils.AlivePlayersCount(CountTypes.Gamer);
            int BK = Utils.AlivePlayersCount(CountTypes.BloodKnight);
            int Pois = Utils.AlivePlayersCount(CountTypes.Poisoner);
            int CM = Utils.AlivePlayersCount(CountTypes.Succubus);
            //int Occ = Utils.AlivePlayersCount(CountTypes.Occultist);
            int Hex = Utils.AlivePlayersCount(CountTypes.HexMaster);
            int Wraith = Utils.AlivePlayersCount(CountTypes.Wraith);
            int Agitater = Utils.AlivePlayersCount(CountTypes.Agitater);
            int Pestilence = Utils.AlivePlayersCount(CountTypes.Pestilence);
            int PB = Utils.AlivePlayersCount(CountTypes.PlagueBearer);
            int SK = Utils.AlivePlayersCount(CountTypes.NSerialKiller);
            int Witch = Utils.AlivePlayersCount(CountTypes.NWitch); //why is this here? whats the need of counting this?
            int Juggy = Utils.AlivePlayersCount(CountTypes.Juggernaut);
            int Necro = Utils.AlivePlayersCount(CountTypes.Necromancer);
            int Pyro = Utils.AlivePlayersCount(CountTypes.Pyromaniac);
            int Hunt = Utils.AlivePlayersCount(CountTypes.Huntsman);
            int Bandit = Utils.AlivePlayersCount(CountTypes.Bandit);
            int Doppelganger = Utils.AlivePlayersCount(CountTypes.Doppelganger);
            int Vamp = Utils.AlivePlayersCount(CountTypes.Infectious);
            int Virus = Utils.AlivePlayersCount(CountTypes.Virus);
            int Rogue = Utils.AlivePlayersCount(CountTypes.Rogue);
            int DH = Utils.AlivePlayersCount(CountTypes.DarkHide);
            int Jinx = Utils.AlivePlayersCount(CountTypes.Jinx);
            int Rit = Utils.AlivePlayersCount(CountTypes.PotionMaster);
            int PP = Utils.AlivePlayersCount(CountTypes.Pickpocket);
            int Traitor = Utils.AlivePlayersCount(CountTypes.Traitor);
            int Med = Utils.AlivePlayersCount(CountTypes.Medusa);
            int SC = Utils.AlivePlayersCount(CountTypes.Spiritcaller);
            int Glitch = Utils.AlivePlayersCount(CountTypes.Glitch);
            int Arso = Utils.AlivePlayersCount(CountTypes.Arsonist);
            int Shr = Utils.AlivePlayersCount(CountTypes.Shroud);
            int WW = Utils.AlivePlayersCount(CountTypes.Werewolf);
            int RR = Utils.AlivePlayersCount(CountTypes.RuthlessRomantic);

            Imp += Main.AllAlivePlayerControls.Count(x => x.GetCustomRole().IsImpostor() && x.Is(CustomRoles.DualPersonality));
            Crew += Main.AllAlivePlayerControls.Count(x => x.GetCustomRole().IsCrewmate() && x.Is(CustomRoles.DualPersonality));
            Crew += Main.AllAlivePlayerControls.Count(x => (x.GetCustomRole().IsImpostor() && x.Is(CustomRoles.Admired)) && x.Is(CustomRoles.DualPersonality));
            Crew += Main.AllAlivePlayerControls.Count(x => (x.GetCustomRole().IsNeutral() && x.Is(CustomRoles.Admired)) && x.Is(CustomRoles.DualPersonality));
            Crew += Main.AllAlivePlayerControls.Count(x => (x.GetCustomRole().IsCrewmate() && x.Is(CustomRoles.Admired)) && x.Is(CustomRoles.DualPersonality));
            CM += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Charmed) && x.Is(CustomRoles.DualPersonality));
            Jackal += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Sidekick) && x.Is(CustomRoles.DualPersonality));
            Jackal += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Recruit) && x.Is(CustomRoles.DualPersonality));
            Vamp += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Infected) && x.Is(CustomRoles.DualPersonality));
            Virus += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Contagious) && x.Is(CustomRoles.DualPersonality));
            Imp += Main.AllAlivePlayerControls.Count(x => x.Is(CustomRoles.Madmate) && x.Is(CustomRoles.DualPersonality));

            int totalNKAlive = new int[] { Rit, Traitor, Med, PP, Jackal, Vamp, DH, Rogue, Wraith, Agitater, Pestilence, PB, Juggy, Doppelganger, Hunt, Necro, Pyro, Hex, Bandit, RR, WW, Shr, Arso, Glitch, Jinx, SK, Pel, Gam, BK, Pois, Virus, SC, CM }.Sum(); // Occ,

            CustomWinner? winner = null;
            CustomRoles? rl = null;

            if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers))) // if lover is alive lover wins
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                return true;
            }

            else if (totalNKAlive == 0) // total number of nks alive 0
            {
                if (Crew == 0 && Imp == 0) // crew and Imp both 0 everyone dead
                {
                    reason = GameOverReason.ImpostorByKill;
                    winner = CustomWinner.None;
                }
                else if (Crew <= Imp) // crew less than imps Imp wins
                {
                    reason = GameOverReason.ImpostorByKill;
                    winner = CustomWinner.Impostor;
                }
                else if (Imp == 0) // remaining imp 0 crew win (neutral already dead)
                {
                    reason = GameOverReason.HumansByVote;
                    winner = CustomWinner.Crewmate;
                }
                else return false; // crew greater than Imps and Imp not dead game must continue
                if (winner != null)
                    CustomWinnerHolder.ResetAndSetWinner((CustomWinner)winner);
                return true;
            }

            else
            {
                if (Imp >= 1) return false; // both imp and nk alive game must continue
                else if (Crew > totalNKAlive) return false; // Imps dead but crew still out numbers nk (game must continue)
                else // Imps dead, Crew <= NK, Checking if All nk alive are in 1 team 
                {
                    if (Rit == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.PotionMaster;
                        rl = CustomRoles.PotionMaster;
                    }
                    else if (Traitor == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Traitor;
                        rl = CustomRoles.Traitor;
                    }
                    else if (Med == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Medusa;
                        rl = CustomRoles.Medusa;
                    }
                    else if (PP == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Pickpocket;
                        rl = CustomRoles.Pickpocket;
                    }
                    else if (Jackal == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Jackal;
                    }
                    else if (Vamp == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Infectious;
                    }
                    else if (DH == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.DarkHide;
                    }
                    else if (Rogue == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Rogue;
                    }
                    else if (Wraith == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Wraith;
                        rl = CustomRoles.Wraith;
                    }
                    else if (Agitater == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Agitater;
                    }
                    else if (Pestilence == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Pestilence;
                        rl = CustomRoles.Pestilence;
                    }
                    else if (PB == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Plaguebearer;
                        rl = CustomRoles.PlagueBearer;
                    }
                    else if (Juggy == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Juggernaut;
                        rl = CustomRoles.Juggernaut;
                    }
                    else if (Doppelganger == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Doppelganger;
                        rl = CustomRoles.Doppelganger;
                    }
                    else if (Hunt == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Huntsman;
                        rl = CustomRoles.Huntsman;
                    }
                    else if (Necro == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Huntsman;
                        rl = CustomRoles.Huntsman;
                    }
                    else if (Pyro == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Pyromaniac;
                        rl = CustomRoles.Pyromaniac;
                    }
                    else if (Hex == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.HexMaster;
                        rl = CustomRoles.HexMaster;
                    }
                    else if (Bandit == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Bandit;
                        rl = CustomRoles.Bandit;
                    }
                    else if (RR == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.RuthlessRomantic; 
                    }
                    else if (WW == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Werewolf;
                        rl = CustomRoles.Werewolf;
                    }
                    else if (Shr == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Shroud;
                        rl = CustomRoles.Shroud;
                    }
                    else if (Arso == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Arsonist;
                        rl = CustomRoles.Arsonist;
                    }
                    else if (Glitch == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Glitch;
                        rl = CustomRoles.Glitch;
                    }
                    else if (Jinx == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Jinx;
                        rl = CustomRoles.Jinx;
                    }
                    else if (SK == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.SerialKiller;
                        rl = CustomRoles.NSerialKiller;
                    }
                    //else if (Occ == totalNKAlive)
                    //{
                    //    reason = GameOverReason.ImpostorByKill;
                    //    winner = CustomWinner.Occultist;
                    //    rl = CustomRoles.Occultist;
                    //}
                    else if (Pel == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Pelican;
                        rl = CustomRoles.Pelican;
                    }
                    else if (Gam == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Gamer;
                        rl = CustomRoles.Gamer;
                    }
                    else if (BK == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.BloodKnight;
                        rl = CustomRoles.BloodKnight;
                    }
                    else if (Pois == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Poisoner;
                        rl = CustomRoles.Poisoner;
                    }
                    else if (Virus == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Virus;
                    }
                    else if (SC == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Spiritcaller;
                    }
                    else if (CM == totalNKAlive)
                    {
                        reason = GameOverReason.ImpostorByKill;
                        winner = CustomWinner.Succubus;
                    }
                    else return false; // not all alive neutrals were in one team
                    if (winner != null) CustomWinnerHolder.ResetAndSetWinner((CustomWinner)winner);
                    if (rl != null) CustomWinnerHolder.WinnerRoles.Add((CustomRoles)rl);
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