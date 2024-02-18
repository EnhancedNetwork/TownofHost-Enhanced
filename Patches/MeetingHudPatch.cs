using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
class CheckForEndVotingPatch
{
    public static string TempExileMsg;
    public static bool Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (Medic.IsEnable) Medic.OnCheckMark();
        //Meeting Skip with vote counting on keystroke (m + delete)
        var shouldSkip = false;
        if (Input.GetKeyDown(KeyCode.F6))
        {
            shouldSkip = true;
        }
        //
        var voteLog = Logger.Handler("Vote");
        try
        {
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.VoterState[] states;
            foreach (var pva in __instance.playerStates.ToArray())
            {
                if (pva == null) continue;
                PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                //死んでいないディクテーターが投票済み

                //主动叛变
                if (pva.DidVote && pc.PlayerId == pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    if (Madmate.MadmateSpawnMode.GetInt() == 2 && Main.MadmateNum < CustomRoles.Madmate.GetCount() && pc.CanBeMadmate(inGame: true))
                    {
                        Main.MadmateNum++;
                        pc.RpcSetCustomRole(CustomRoles.Madmate);
                        ExtendedPlayerControl.RpcSetCustomRole(pc.PlayerId, CustomRoles.Madmate);
                        Utils.NotifyRoles(isForMeeting: true, SpecifySeer: pc, NoCache: true);
                        Logger.Info("Setting up a career:" + pc?.Data?.PlayerName + " = " + pc.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
                    }
                }

                if (pc.Is(CustomRoles.Dictator) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                    TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);
                    statesList.Add(new()
                    {
                        VoterId = pva.TargetPlayerId,
                        VotedForId = pva.VotedFor
                    });
                    states = [.. statesList];

                    if (AntiBlackout.BlackOutIsActive)
                    {
                        ExileControllerWrapUpPatch.AntiBlackout_LastExiled = voteTarget.Data;
                        //__instance.RpcVotingComplete(states, null, true);

                        // Need check BlackOutIsActive again
                        if (AntiBlackout.BlackOutIsActive)
                            __instance.AntiBlackRpcVotingComplete(states, voteTarget.Data, false);

                        AntiBlackout.ShowExiledInfo = true;
                        ConfirmEjections(voteTarget.Data, true);
                    }
                    else __instance.RpcVotingComplete(states, voteTarget.Data, false);

                    Logger.Info($"{voteTarget.GetNameWithRole()} 被独裁者驱逐", "Dictator");
                    CheckForDeathOnExile(PlayerState.DeathReason.Vote, pva.VotedFor);
                    Logger.Info("独裁投票，会议强制结束", "Special Phase");
                    voteTarget.SetRealKiller(pc);
                    Main.LastVotedPlayerInfo = voteTarget.Data;
                    if (Main.LastVotedPlayerInfo != null)
                        ConfirmEjections(Main.LastVotedPlayerInfo);
                    return true;
                }

                if (pva.DidVote && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                    if (voteTarget == null)
                    {
                        Utils.SendMessage(GetString("VoteDead"), pc.PlayerId);
                        __instance.RpcClearVote(pc.GetClientId());
                        Swapper.CheckSwapperTarget(pva.VotedFor);
                        continue;
                    }
                    else if (!voteTarget.IsAlive() || voteTarget.Data.Disconnected)
                    {
                        Utils.SendMessage(GetString("VoteDead"), pc.PlayerId);
                        __instance.RpcClearVote(pc.GetClientId());
                        Swapper.CheckSwapperTarget(pva.VotedFor);
                        continue;
                    }

                    if (voteTarget != null)
                    {
                        switch (pc.GetCustomRole())
                        {
                            case CustomRoles.Divinator:
                                Divinator.OnVote(pc, voteTarget);
                                break;
                            case CustomRoles.Oracle:
                                Oracle.OnVote(pc, voteTarget);
                                break;
                            case CustomRoles.Eraser:
                                Eraser.OnVote(pc, voteTarget);
                                break;
                            case CustomRoles.Cleanser:
                                Cleanser.OnVote(pc, voteTarget);
                                break;

                            case CustomRoles.Tracker:
                                Tracker.OnVote(pc, voteTarget);
                                break;
                            case CustomRoles.SoulCollector:
                                SoulCollector.OnVote(pc, voteTarget);
                                break;
                            case CustomRoles.Godfather:
                                if (pc == null || voteTarget == null) break;
                                Main.GodfatherTarget.Add(voteTarget.PlayerId);
                                break;
                            case CustomRoles.Jailer:
                                Jailer.OnVote(pc, voteTarget);
                                break;
                        }
                        if (voteTarget.Is(CustomRoles.Aware))
                        {
                            Aware.OnVoted(pc, pva);
                        }

                        if (voteTarget.Is(CustomRoles.Captain))
                        {
                            if (!Captain.CaptainVoteTargets.ContainsKey(voteTarget.PlayerId)) Captain.CaptainVoteTargets[voteTarget.PlayerId] = [];
                            if (!Captain.CaptainVoteTargets[voteTarget.PlayerId].Contains(pc.PlayerId))
                            {
                                Captain.CaptainVoteTargets[voteTarget.PlayerId].Add(pc.PlayerId);
                                Captain.SendRPCVoteAdd(voteTarget.PlayerId, pc.PlayerId);
                            }
                        }

                    }
                }
            }

            if (!shouldSkip)
            {
                foreach (var ps in __instance.playerStates.ToArray())
                {
                    //死んでいないプレイヤーが投票していない
                    if (!ps.DidVote && Utils.GetPlayerById(ps.TargetPlayerId)?.IsAlive() == true)
                    {
                        return false;
                    }
                }
            }

            GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
            bool tie = false;

            foreach (var ps in __instance.playerStates.ToArray())
            {
                if (ps == null) continue;
                voteLog.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"));
                var voter = Utils.GetPlayerById(ps.TargetPlayerId);
                if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                if (Options.VoteMode.GetBool())
                {
                    if (ps.VotedFor == 253 && !voter.Data.IsDead && //スキップ
                        !(Options.WhenSkipVoteIgnoreFirstMeeting.GetBool() && MeetingStates.FirstMeeting) && //初手会議を除く
                        !(Options.WhenSkipVoteIgnoreNoDeadBody.GetBool() && !MeetingStates.IsExistDeadBody) && //死体がない時を除く
                        !(Options.WhenSkipVoteIgnoreEmergency.GetBool() && MeetingStates.IsEmergencyMeeting) //緊急ボタンを除く
                        )
                    {
                        switch (Options.GetWhenSkipVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()}因跳过投票自杀");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()}因跳过投票自票");
                                break;
                            default:
                                break;
                        }
                    }
                    if (ps.VotedFor == 254 && !voter.Data.IsDead)//無投票
                    {
                        switch (Options.GetWhenNonVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票自杀");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票自票");
                                break;
                            case VoteMode.Skip:
                                ps.VotedFor = 253;
                                voteLog.Info($"{voter.GetNameWithRole()}因未投票跳过");
                                break;
                            default:
                                break;
                        }
                    }
                }
                // Hide Divinator Vote
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Divinator) && Divinator.HideVote.GetBool() && Divinator.TempCheckLimit[ps.TargetPlayerId] > 0) continue;
                // Hide Eraser Vote
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Eraser) && Eraser.HideVote.GetBool() && Eraser.TempEraseLimit[ps.TargetPlayerId] > 0) continue;
                // Hide Tracker Vote
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Tracker) && Tracker.HideVote.GetBool() && Tracker.TempTrackLimit[ps.TargetPlayerId] > 0) continue;
                // Hide Oracle Vote
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Oracle) && Oracle.HideVote.GetBool() && Oracle.TempCheckLimit[ps.TargetPlayerId] > 0) continue;
                // Hide Cleanser Vote
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Cleanser) && Cleanser.HideVote.GetBool() && Cleanser.CleanserUses[ps.TargetPlayerId] > 0) continue;
                // Hide Keeper Vote
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Keeper) && Keeper.HideVote.GetBool() && Keeper.keeperUses[ps.TargetPlayerId] > 0) continue;
                // Hide Jester Vote
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Jester) && Options.HideJesterVote.GetBool()) continue;
                // Assing Madmate Slef Vote
                if (ps.TargetPlayerId == ps.VotedFor && Madmate.MadmateSpawnMode.GetInt() == 2) continue;

                statesList.Add(new MeetingHud.VoterState()
                {
                    VoterId = ps.TargetPlayerId,
                    VotedForId = ps.VotedFor
                });

                //Swapper swap votes
                foreach (var pid in Swapper.playerIdList)
                {
                    if (Swapper.ResultSent.Contains(pid)) continue;
                    //idk why this would be triggered repeatedly.
                    var pc = Utils.GetPlayerById(pid);
                    if (pc == null || !pc.IsAlive()) continue;

                    if (!Swapper.Vote.TryGetValue(pc.PlayerId, out var tid1) || !Swapper.VoteTwo.TryGetValue(pc.PlayerId, out var tid2)) continue;
                    if (tid1 == 253 || tid2 == 253 || tid1 == tid2) continue;

                    var target1 = Utils.GetPlayerById(tid1);
                    var target2 = Utils.GetPlayerById(tid2);

                    if (target1 == null || target2 == null || !target1.IsAlive() || !target2.IsAlive()) continue;

                    List<byte> templist = [];

                    foreach (var pva in __instance.playerStates.ToArray())
                    {
                        if (pva.VotedFor != target1.PlayerId || pva.AmDead) continue;
                        templist.Add(pva.TargetPlayerId);
                        pva.VotedFor = target2.PlayerId;
                        ReturnChangedPva(pva);
                    }

                    foreach (var pva in __instance.playerStates.ToArray())
                    {
                        if (pva.VotedFor != target2.PlayerId || pva.AmDead) continue;
                        if (templist.Contains(pva.TargetPlayerId)) continue;
                        pva.VotedFor = target1.PlayerId;
                        ReturnChangedPva(pva);
                    }

                    if (!Swapper.ResultSent.Contains(pid))
                    {
                        Swapper.ResultSent.Add(pid);
                        Utils.SendMessage(string.Format(GetString("SwapVote"), target1.GetRealName(), target2.GetRealName()), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapTitle")));
                        Swapper.Swappermax[pid] -= 1;
                        Swapper.SendSkillRPC(pid);
                    }
                }

                if (CheckRole(ps.TargetPlayerId, CustomRoles.Mayor) && !Options.MayorHideVote.GetBool()) //Mayorの投票数
                {
                    for (var i2 = 0; i2 < Options.MayorAdditionalVote.GetFloat(); i2++)
                    {
                        statesList.Add(new MeetingHud.VoterState()
                        {
                            VoterId = ps.TargetPlayerId,
                            VotedForId = ps.VotedFor
                        });
                    }
                }
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Vindicator) && !Options.VindicatorHideVote.GetBool()) //Vindicator
                {
                    for (var i2 = 0; i2 < Options.VindicatorAdditionalVote.GetFloat(); i2++)
                    {
                        statesList.Add(new MeetingHud.VoterState()
                        {
                            VoterId = ps.TargetPlayerId,
                            VotedForId = ps.VotedFor
                        });
                    }
                }
            }

            var VotingData = __instance.CustomCalculateVotes(); //Influenced vote mun isnt counted here

            if (CustomRoles.Influenced.RoleExist())
            {
                Influenced.ChangeVotingData(VotingData);
                VotingData = __instance.CustomCalculateVotes(true);
            }
            //Change voting data for influenced, vote num counted here

            for (int i = 0; i < statesList.Count; i++)
            {
                var voterstate = statesList[i];
                var voterpc = Utils.GetPlayerById(voterstate.VoterId);
                if (voterpc == null || !voterpc.IsAlive()) continue;
                var voterpva = GetPlayerVoteArea(voterstate.VoterId);
                if (voterpva.VotedFor != voterstate.VotedForId)
                {
                    voterstate.VotedForId = voterpva.VotedFor;
                }
                if (voterpc.Is(CustomRoles.Silent))
                {
                    voterstate.VotedForId = 254; //Change to non should work
                }
                statesList[i] = voterstate;
            }
            /*This change the voter icon on meetinghud to the player the voter actually voted for.
             Should work for Influenced and swapeer , Also change role like mayor that has mutiple vote icons
             Does not effect the votenum and vote result, simply change display icons
             God Niko cant think about a better way to do this, so niko just loops every voterstate LOL*/

            states = [.. statesList];

            byte exileId = byte.MaxValue;
            int max = 0;
            voteLog.Info("=========Vote Result=========");
            foreach (var data in VotingData)
            {
                voteLog.Info($"{data.Key}({Utils.GetVoteName(data.Key)}): {data.Value} votes");
                if (data.Value > max)
                {
                    voteLog.Info($"{data.Key} have a higher number of votes ({data.Value})");
                    exileId = data.Key;
                    max = data.Value;
                    tie = false;
                }
                else if (data.Value == max)
                {
                    voteLog.Info($"{data.Key} has the same number of votes as {exileId} ({data.Value})");
                    exileId = byte.MaxValue;
                    tie = true;
                }
                voteLog.Info($"Expulsion ID: {exileId}, max: {max} votes");
            }

            voteLog.Info($"Decision to exiled a player: {exileId} ({Utils.GetVoteName(exileId)})");

            bool braked = false;
            if (tie)
            {
                byte target = byte.MaxValue;
                foreach (var data in VotingData.Where(x => x.Key < 15 && x.Value == max).ToArray())
                {
                    if (Tiebreaker.VoteFor.Contains(data.Key))
                    {
                        if (target != byte.MaxValue)
                        {
                            target = byte.MaxValue;
                            break;
                        }
                        target = data.Key;
                    }
                }
                if (target != byte.MaxValue)
                {
                    Logger.Info("Flat breakers cover expulsion of players", "Tiebreaker Vote");
                    exiledPlayer = Utils.GetPlayerInfoById(target);
                    tie = false;
                    braked = true;
                }
            }

            Collector.CollectAmount(VotingData, __instance);

            if (Options.VoteMode.GetBool() && Options.WhenTie.GetBool() && tie)
            {
                switch ((TieMode)Options.WhenTie.GetValue())
                {
                    case TieMode.Default:
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == exileId);
                        break;
                    case TieMode.All:
                        var exileIds = VotingData.Where(x => x.Key < 15 && x.Value == max).Select(kvp => kvp.Key).ToArray();
                        foreach (var playerId in exileIds)
                            Utils.GetPlayerById(playerId).SetRealKiller(null);
                        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Vote, exileIds);
                        exiledPlayer = null;
                        break;
                    case TieMode.Random:
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => VotingData.TryGetValue(x.PlayerId, out int vote) && vote == max);
                        tie = false;
                        break;
                }
            }
            else if (!braked)
                exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);

            if (Keeper.IsTargetExiled(exileId))
            {
                exileId = 0xff;
                exiledPlayer = Utils.GetPlayerInfoById(exileId);
            }

            exiledPlayer?.Object.SetRealKiller(null);

            //RPC
            if (AntiBlackout.BlackOutIsActive)
            {
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiledPlayer;
                //__instance.RpcVotingComplete(states, null, true);

                // Need check BlackOutIsActive again
                if (AntiBlackout.BlackOutIsActive)
                    __instance.AntiBlackRpcVotingComplete(states, exiledPlayer, tie);

                if (exiledPlayer != null)
                {
                    AntiBlackout.ShowExiledInfo = true;
                    ConfirmEjections(exiledPlayer, true);
                }
            }
            else __instance.RpcVotingComplete(states, exiledPlayer, tie); // Normal processing

            CheckForDeathOnExile(PlayerState.DeathReason.Vote, exileId);

            Main.LastVotedPlayerInfo = exiledPlayer;
            if (Main.LastVotedPlayerInfo != null)
            {
                ConfirmEjections(Main.LastVotedPlayerInfo);
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.SendInGame(string.Format(GetString("Error.MeetingException"), ex.Message));
            throw;
        }
    }

    // 参考：https://github.com/music-discussion/TownOfHost-TheOtherRoles
    private static void ConfirmEjections(GameData.PlayerInfo exiledPlayer, bool AntiBlackoutStore = false)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (exiledPlayer == null) return;

        var exileId = exiledPlayer.PlayerId;
        if (exileId is < 0 or > 254) return;

        var realName = exiledPlayer.Object.GetRealName(isMeeting: true);
        Main.LastVotedPlayer = realName;

        var player = Utils.GetPlayerById(exiledPlayer.PlayerId);
        var role = GetString(exiledPlayer.GetCustomRole().ToString());
        var crole = exiledPlayer.GetCustomRole();
        var coloredRole = Utils.GetDisplayRoleAndSubName(exileId, exileId, true);

        if (Options.ConfirmEgoistOnEject.GetBool() && player.Is(CustomRoles.Egoist))
            coloredRole = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Egoist), coloredRole.RemoveHtmlTags());

        if (Options.ConfirmLoversOnEject.GetBool() && player.Is(CustomRoles.Lovers))
            coloredRole = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), coloredRole.RemoveHtmlTags());

        if (Rascal.AppearAsMadmate(player))
            coloredRole = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetRoleString("Mad-") + coloredRole.RemoveHtmlTags());

        var name = "";
        int impnum = 0;
        int neutralnum = 0;

        if (CustomRoles.Bard.RoleExist())
        {
            Main.BardCreations++;
            try { name = ModUpdater.Get("https://v1.hitokoto.cn/?encode=text"); }
            catch { name = GetString("ByBardGetFailed"); }
            name += "\n\t\t——" + GetString("ByBard");
            goto EndOfSession;
        }

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var pc_role = pc.GetCustomRole();
            if (pc_role.IsImpostor() && pc != exiledPlayer.Object)
                impnum++;
            else if (pc_role.IsNK() && pc != exiledPlayer.Object)
                neutralnum++;
        }
        switch (Options.CEMode.GetInt())
        {
            case 0:
                name = string.Format(GetString("PlayerExiled"), realName);
                break;
            case 1:
                if (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.Parasite) || player.Is(CustomRoles.Crewpostor) || player.Is(CustomRoles.Refugee) || player.Is(CustomRoles.Convict))
                    name = string.Format(GetString("BelongTo"), realName, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), GetString("TeamImpostor")));

                else if (player.GetCustomRole().IsCrewmate())
                    name = string.Format(GetString("IsGood"), realName);

                else if (player.GetCustomRole().IsNeutral() && !player.Is(CustomRoles.Parasite) && !player.Is(CustomRoles.Refugee) && !player.Is(CustomRoles.Crewpostor) && !player.Is(CustomRoles.Convict))
                    name = string.Format(GetString("BelongTo"), realName, Utils.ColorString(new Color32(127, 140, 141, byte.MaxValue), GetString("TeamNeutral")));

                break;
            case 2:
                name = string.Format(GetString("PlayerIsRole"), realName, coloredRole);
                if (Options.ShowTeamNextToRoleNameOnEject.GetBool())
                {
                    name += " (";
                    if (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.Madmate))
                        name += Utils.ColorString(new Color32(255, 25, 25, byte.MaxValue), GetString("TeamImpostor"));
                    else if (player.GetCustomRole().IsNeutral() || player.Is(CustomRoles.Charmed))
                        name += Utils.ColorString(new Color32(127, 140, 141, byte.MaxValue), GetString("TeamNeutral"));
                    else if (player.GetCustomRole().IsCrewmate())
                        name += Utils.ColorString(new Color32(140, 255, 255, byte.MaxValue), GetString("TeamCrewmate"));
                    name += ")";
                }
                break;
        }
        var DecidedWinner = false;

        //迷你船员长大前被驱逐抢夺胜利
        if (crole.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            name = string.Format(GetString("ExiledNiceMini"), realName, coloredRole);
            DecidedWinner = true;
        }
        //if (crole.Is(CustomRoles.Captain))
        //    Captain.OnExile(exileId); /*Runs multiple times here*/

        //小丑胜利
        if (crole.Is(CustomRoles.Jester))
        {
            if (Options.MeetingsNeededForJesterWin.GetInt() <= Main.MeetingsPassed)
            {
                name = string.Format(GetString("ExiledJester"), realName, coloredRole);
                DecidedWinner = true;
            }
            else if (Options.CEMode.GetInt() == 2) name += string.Format(GetString("JesterMeetingLoose"), Options.MeetingsNeededForJesterWin.GetInt() + 1);
        }

        //处刑人胜利
        if (Executioner.CheckExileTarget(exiledPlayer, DecidedWinner, true))
        {
            name = string.Format(GetString("ExiledExeTarget"), realName, coloredRole);
            DecidedWinner = true;
        }

        //冤罪师胜利
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == exileId))
        {
            if (!(!Options.InnocentCanWinByImp.GetBool() && crole.IsImpostor()))
            {
                if (DecidedWinner) name += string.Format(GetString("ExiledInnocentTargetAddBelow"));
                else name = string.Format(GetString("ExiledInnocentTargetInOneLine"), realName, coloredRole);
                DecidedWinner = true;
            }
        }

        if (DecidedWinner) name += "<size=0>";
        if (Options.ShowImpRemainOnEject.GetBool() && !DecidedWinner)
        {
            name += "\n";
            string comma = neutralnum > 0 ? "" : "";
            if (impnum == 0) name += GetString("NoImpRemain") + comma;
            if (impnum == 1) name += GetString("OneImpRemain") + comma;
            if (impnum == 2) name += GetString("TwoImpRemain") + comma;
            if (impnum == 3) name += GetString("ThreeImpRemain") + comma;
            //    else name += string.Format(GetString("ImpRemain"), impnum) + comma;
            if (Options.ShowNKRemainOnEject.GetBool() && neutralnum > 0)
                if (neutralnum == 1)
                    name += string.Format(GetString("OneNeutralRemain"), neutralnum) + comma;
                else
                    name += string.Format(GetString("NeutralRemain"), neutralnum) + comma;
        }

    EndOfSession:
        name += "<size=0>";
        TempExileMsg = name;

        _ = new LateTask(() =>
        {
            Main.DoBlockNameChange = true;
            if (GameStates.IsInGame)
            {
                player.RpcSetName(name);
            }
        }, 3.0f, "Change Exiled Player Name");

        _ = new LateTask(() =>
        {
            if (GameStates.IsInGame && !player.Data.Disconnected)
            {
                player.RpcSetName(realName);
                Main.DoBlockNameChange = false;
            }

            if (GameStates.IsInGame && player.Data.Disconnected)
            {
                player.Data.PlayerName = realName;
                //Await Next Send Data or Next Meeting
            }
        }, 11.5f, "Change Exiled Player Name Back");

        if (AntiBlackoutStore)
        {
            AntiBlackout.StoreExiledMessage = name;
            Logger.Info(AntiBlackout.StoreExiledMessage, "AntiBlackoutStore");
        }
    }
    public static bool CheckRole(byte id, CustomRoles role)
    {
        var player = Main.AllPlayerControls.FirstOrDefault(pc => pc.PlayerId == id);
        return player != null && player.Is(role);
    }
    public static PlayerVoteArea GetPlayerVoteArea(byte playerId)
    {
        if (MeetingHud.Instance == null || MeetingHud.Instance.playerStates.Count < 1) return null;
        //This function should only be used to get vote states after voting complete

        foreach (var pva in MeetingHud.Instance.playerStates)
        {
            if (pva.TargetPlayerId == playerId) return pva;
        }

        return null; //if pva doesnt exist
    }
    public static void ReturnChangedPva(PlayerVoteArea pva)
    {
        var playerStates = MeetingHud.Instance.playerStates;

        int index = playerStates.IndexOf(playerStates.FirstOrDefault(ipva => ipva.TargetPlayerId == pva.TargetPlayerId));
        if (index != -1)
        {
            MeetingHud.Instance.playerStates[index] = pva;
        }
    }

    public static void TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason deathReason, params byte[] playerIds)
    {

        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
        {
            var pc = Utils.GetPlayerById(playerId);
            if (pc == null) return;
            if (pc.Is(CustomRoles.Susceptible))
            {
                Susceptible.CallEnabledAndChange(pc);
                deathReason = Susceptible.randomReason;
            }

            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
                AddedIdList.Add(playerId);
        }
        CheckForDeathOnExile(deathReason, [.. AddedIdList]);
    }
    public static void CheckForDeathOnExile(PlayerState.DeathReason deathReason, params byte[] playerIds)
    {
        Witch.OnCheckForEndVoting(deathReason, playerIds);
        HexMaster.OnCheckForEndVoting(deathReason, playerIds);
        //Occultist.OnCheckForEndVoting(deathReason, playerIds);
        Virus.OnCheckForEndVoting(deathReason, playerIds);

        foreach (var playerId in playerIds)
        {
            if (CustomRoles.Lovers.IsEnable() && !Main.isLoversDead && Main.LoversPlayers.Any(lp => lp.PlayerId == playerId))
            {
                FixedUpdateInNormalGamePatch.LoversSuicide(playerId, true);
            }

            RevengeOnExile(playerId/*, deathReason*/);
        }
    }
    private static void RevengeOnExile(byte playerId/*, PlayerState.DeathReason deathReason*/)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        var target = PickRevengeTarget(player);
        if (target == null) return;

        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);

        Logger.Info($"{player.GetNameWithRole()} revenge:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer)//道連れ先選定
    {
        List<PlayerControl> TargetList = [];
        foreach (var candidate in Main.AllAlivePlayerControls)
        {
            if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
        }
        if (TargetList == null || TargetList.Count == 0) return null;
        var rand = IRandom.Instance;
        var target = TargetList[rand.Next(TargetList.Count)];
        return target;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
class CastVotePatch
{
    public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte srcPlayerId, [HarmonyArgument(1)] byte suspectPlayerId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        var voter = Utils.GetPlayerById(srcPlayerId);
        if (voter == null || !voter.IsAlive()) return false;

        var target = Utils.GetPlayerById(suspectPlayerId);
        if (target == null && suspectPlayerId < 253)
        {
            Utils.SendMessage(GetString("VoteDead"), srcPlayerId);
            __instance.RpcClearVote(voter.GetClientId());
            return false;
        } //Vote a disconnect player

        if (target != null && suspectPlayerId < 253)
        {
            if (!target.IsAlive() || target.Data.Disconnected)
            {
                Utils.SendMessage(GetString("VoteDead"), srcPlayerId);
                __instance.RpcClearVote(voter.GetClientId());
                Swapper.CheckSwapperTarget(suspectPlayerId);
                return false;
            }

            switch (voter.GetCustomRole())
            {
                case CustomRoles.Dictator:
                    if (target.Is(CustomRoles.Solsticer))
                    {
                        Utils.SendMessage(GetString("VoteSolsticer"), srcPlayerId);
                        __instance.RpcClearVote(voter.GetClientId());
                        return false;
                    }
                    if (!target.IsAlive())
                    {
                        Utils.SendMessage(GetString("VoteDead"), srcPlayerId);
                        __instance.RpcClearVote(voter.GetClientId());
                        return false;
                    } //patch here so checkend is not triggered
                    break;
                case CustomRoles.Keeper:
                    if (!Keeper.OnVote(voter, target))
                    {
                        __instance.RpcClearVote(voter.GetClientId());
                        return false;
                    }
                    break;
            }
        }

        return true;
    }

    public static void Postfix(MeetingHud __instance)
    {
        __instance.CheckForEndVoting();
        //For stuffs in check for end voting to work
    }
}
static class ExtendedMeetingHud
{
    public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance, bool CountInfluenced = false)
    {
        Logger.Info("===Start of vote counting processing===", "Vote");
        
        Dictionary<byte, int> dic = [];
        Collector.CollectorVoteFor = [];
        Tiebreaker.Clear();

        // |Voted By| Number of Times Voted For
        foreach (var ps in __instance.playerStates.ToArray())
        {
            if (ps == null) continue;

            // whether this player is voted for in the player panel
            if (ps.VotedFor is not 252 and not byte.MaxValue and not 254)
            {
                // Default number of votes 1
                int VoteNum = 1;

                // Judgment only when voting for a valid player
                var target = Utils.GetPlayerById(ps.VotedFor);
                if (target != null)
                {
                    if (target.Is(CustomRoles.Zombie)) VoteNum = 0;
                    
                    //Solsticer can not get voted out
                    if (target.Is(CustomRoles.Solsticer)) VoteNum = 0;

                    // Check Tiebreaker voting
                    Tiebreaker.CheckVote(target, ps);

                    // Check Collector voting data
                    Collector.CollectorVotes(target, ps);
                }

                //市长附加票数
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Mayor)
                    && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum += Options.MayorAdditionalVote.GetInt();

                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Knighted)
                    && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum += 1;

                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Vindicator)
                    && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum += Options.VindicatorAdditionalVote.GetInt();

                if (Schizophrenic.DualVotes.GetBool())
                {
                    if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Schizophrenic)
                        && ps.TargetPlayerId != ps.VotedFor
                        ) VoteNum += VoteNum;
                }

                // Additional votes
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.TicketsStealer))
                {
                    VoteNum += (int)(Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == ps.TargetPlayerId) * Stealer.TicketsPerKill.GetFloat());
                }
                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Pickpocket))
                {
                    VoteNum += (int)(Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == ps.TargetPlayerId) * Pickpocket.VotesPerKill.GetFloat());
                }

                // 主动叛变模式下自票无效
                if (ps.TargetPlayerId == ps.VotedFor && Madmate.MadmateSpawnMode.GetInt() == 2) VoteNum = 0;

                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.VoidBallot)) VoteNum = 0;

                if (Jailer.JailerTarget.ContainsValue(ps.VotedFor) || Jailer.JailerTarget.ContainsValue(ps.TargetPlayerId)) VoteNum = 0; //jailed can't vote and can't get voted

                if (!CountInfluenced)
                {
                    if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Influenced))
                    {
                        VoteNum = 0;
                    }
                }
                //Set influenced vote num to zero while counting votes, and count influenced vote upon finishing influenced check

                //投票を1追加 キーが定義されていない場合は1で上書きして定義
                dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out int num) ? VoteNum : num + VoteNum;//统计该玩家被投的数量
            }
        }
        return dic;
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class MeetingHudStartPatch
{
    public static List<(string, byte, string)> msgToSend = [];
    public static void NotifyRoleSkillOnMeetingStart()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Main.MeetingIsStarted = true;

        msgToSend = [];

        static void AddMsg(string text, byte sendTo = 255, string title = "")
            => msgToSend.Add((text, sendTo, title));

        //首次会议技能提示
        if (Options.SendRoleDescriptionFirstMeeting.GetBool() && MeetingStates.FirstMeeting)
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => !x.IsModClient()).ToArray())
            {
                var role = pc.GetCustomRole();
                var sb = new StringBuilder();
                sb.Append(GetString(role.ToString()) + Utils.GetRoleMode(role) + pc.GetRoleInfo(true));
                if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                    Utils.ShowChildrenSettings(opt, ref sb, command: true);
                var txt = sb.ToString();
                sb.Clear().Append(txt.RemoveHtmlTags());
                foreach (var subRole in Main.PlayerStates[pc.PlayerId].SubRoles.ToArray())
                    sb.Append($"\n\n" + GetString($"{subRole}") + Utils.GetRoleMode(subRole) + GetString($"{subRole}InfoLong"));
                if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and not CustomRoles.Ntr))
                    sb.Append($"\n\n" + GetString($"Lovers") + Utils.GetRoleMode(CustomRoles.Lovers) + GetString($"LoversInfoLong"));
                AddMsg(sb.ToString(), pc.PlayerId);
            }
        if (msgToSend.Count >= 1)
        {
            var msgTemp = msgToSend.ToList();
            _ = new LateTask(() =>
            {
                msgTemp.Do(x => Utils.SendMessage(x.Item1, x.Item2, x.Item3));
            }, 3f, "Skill Description First Meeting");
        }
        msgToSend = [];

        //主动叛变模式提示
        if (Madmate.MadmateSpawnMode.GetInt() == 2 && CustomRoles.Madmate.GetCount() > 0)
            AddMsg(string.Format(GetString("Message.MadmateSelfVoteModeNotify"), GetString("MadmateSpawnMode.SelfVote")));
        //提示神存活
        if (CustomRoles.God.RoleExist() && Options.NotifyGodAlive.GetBool())
            AddMsg(GetString("GodNoticeAlive"), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.God), GetString("GodAliveTitle")));
        //工作狂的生存技巧
        if (MeetingStates.FirstMeeting && CustomRoles.Workaholic.RoleExist() && Options.WorkaholicGiveAdviceAlive.GetBool() && !Options.WorkaholicCannotWinAtDeath.GetBool() && !Options.GhostIgnoreTasks.GetBool())
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Workaholic)).ToArray())
            {
                Main.WorkaholicAlive.Add(pc.PlayerId);
            }
            List<string> workaholicAliveList = [];
            foreach (var whId in Main.WorkaholicAlive.ToArray())
            {
                workaholicAliveList.Add(Main.AllPlayerNames[whId]);
            }
            string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";
            AddMsg(string.Format(GetString("WorkaholicAdviceAlive"), string.Join(separator, workaholicAliveList)), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Workaholic), GetString("WorkaholicAliveTitle")));
        }
        //Bait Notify
        if (MeetingStates.FirstMeeting && CustomRoles.Bait.RoleExist() && Bait.BaitNotification.GetBool())
        {
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Bait)).ToArray())
            {
                Bait.BaitAlive.Add(pc.PlayerId);
            }
            List<string> baitAliveList = [];
            foreach (var whId in Bait.BaitAlive.ToArray())
            {
                PlayerControl whpc = Utils.GetPlayerById(whId);
                if (whpc == null) continue;
                baitAliveList.Add(whpc.GetRealName());
            }
            string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";
            AddMsg(string.Format(GetString("BaitAdviceAlive"), string.Join(separator, baitAliveList)), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), GetString("BaitAliveTitle")));
        }
        string MimicMsg = "";
        foreach (var pc in Main.AllPlayerControls)
        {
            //黑手党死后技能提示
            if (pc.Is(CustomRoles.Mafia) && !pc.IsAlive())
                AddMsg(GetString("MafiaDeadMsg"), pc.PlayerId);
            //惩罚者死后技能提示
            if (pc.Is(CustomRoles.Retributionist) && !pc.IsAlive())
                AddMsg(GetString("RetributionistDeadMsg"), pc.PlayerId);
            //网红死亡消息提示
            foreach (var csId in Main.CyberStarDead)
            {
                if (!Options.ImpKnowCyberStarDead.GetBool() && pc.GetCustomRole().IsImpostor()) continue;
                if (!Options.NeutralKnowCyberStarDead.GetBool() && pc.GetCustomRole().IsNeutral()) continue;
                AddMsg(string.Format(GetString("CyberStarDead"), Main.AllPlayerNames[csId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.CyberStar), GetString("CyberStarNewsTitle")));
            }

            foreach (var csId in Cyber.CyberDead)
            {
                if (!Cyber.ImpKnowCyberDead.GetBool() && pc.GetCustomRole().IsImpostor()) continue;
                if (!Cyber.NeutralKnowCyberDead.GetBool() && pc.GetCustomRole().IsNeutral()) continue;
                if (!Cyber.CrewKnowCyberDead.GetBool() && pc.GetCustomRole().IsCrewmate()) continue;
                AddMsg(string.Format(GetString("CyberDead"), Main.AllPlayerNames[csId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cyber), GetString("CyberNewsTitle")));
            }

            //勒索者勒索警告
            if (Blackmailer.IsEnable && pc != null && Blackmailer.ForBlackmailer.Contains(pc.PlayerId))
            {
                var playername = pc.GetRealName();
                if (Doppelganger.DoppelVictim.ContainsKey(pc.PlayerId)) playername = Doppelganger.DoppelVictim[pc.PlayerId];
                AddMsg(string.Format(GetString("BlackmailerDead"), playername, pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), GetString("BlackmaileKillTitle"))));
            }
            //侦探报告线索
            if (Main.DetectiveNotify.ContainsKey(pc.PlayerId))
                AddMsg(Main.DetectiveNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Detective), GetString("DetectiveNoticeTitle")));
            if (Sleuth.SleuthNotify.ContainsKey(pc.PlayerId))
                AddMsg(Sleuth.SleuthNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Sleuth), GetString("SleuthNoticeTitle")));
            //宝箱怪的消息（记录）
            if (pc.Is(CustomRoles.Mimic) && !pc.IsAlive())
                Main.AllAlivePlayerControls.Where(x => x.GetRealKiller()?.PlayerId == pc.PlayerId).Do(x => MimicMsg += $"\n{x.GetNameWithRole(true)}");
            //入殓师的检查
            if (Mortician.msgToSend.ContainsKey(pc.PlayerId))
                AddMsg(Mortician.msgToSend[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mortician), GetString("MorticianCheckTitle")));
            //调查员的提示（自己）
            if (Mediumshiper.ContactPlayer.ContainsValue(pc.PlayerId))
                AddMsg(string.Format(GetString("MediumshipNotifySelf"), Main.AllPlayerNames[Mediumshiper.ContactPlayer.Where(x => x.Value == pc.PlayerId).FirstOrDefault().Key], Mediumshiper.ContactLimit[pc.PlayerId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));
            //调查员的提示（目标）
            if (Mediumshiper.ContactPlayer.ContainsKey(pc.PlayerId) && (!Mediumshiper.OnlyReceiveMsgFromCrew.GetBool() || pc.GetCustomRole().IsCrewmate()))
                AddMsg(string.Format(GetString("MediumshipNotifyTarget"), Main.AllPlayerNames[Mediumshiper.ContactPlayer[pc.PlayerId]]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mediumshiper), GetString("MediumshipTitle")));
            if (Main.VirusNotify.ContainsKey(pc.PlayerId))
                AddMsg(Main.VirusNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Virus), GetString("VirusNoticeTitle")));
            if (Enigma.MsgToSend.ContainsKey(pc.PlayerId))
                AddMsg(Enigma.MsgToSend[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Enigma), Enigma.MsgToSendTitle[pc.PlayerId]));
            if (pc.Is(CustomRoles.Solsticer))
            {
                Solsticer.SetShortTasksToAdd();
                if (Solsticer.MurderMessage == "")
                    Solsticer.MurderMessage = string.Format(GetString("SolsticerOnMeeting"), Solsticer.AddShortTasks);
                AddMsg(Solsticer.MurderMessage, pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Solsticer), GetString("SolsticerTitle")));
            }
        }
        //宝箱怪的消息（合并）
        if (MimicMsg != "")
        {
            MimicMsg = GetString("MimicDeadMsg") + "\n" + MimicMsg;

            var isImpostorTeamList = Main.AllPlayerControls.Where(x => x.GetCustomRole().IsImpostorTeam()).ToArray();
            foreach (var imp in isImpostorTeamList)
            {
                AddMsg(MimicMsg, imp.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mimic), GetString("MimicMsgTitle")));
            }
        }

        msgToSend.Do(x => Logger.Info($"To:{x.Item2} {x.Item3} => {x.Item1}", "Skill Notice OnMeeting Start"));

        //总体延迟发送
        _ = new LateTask(() =>
        {
            msgToSend.Do(x => Utils.SendMessage(x.Item1, x.Item2, x.Item3));
        }, 3f, "Skill Notice On Meeting Start");

        Main.CyberStarDead.Clear();
        Main.DetectiveNotify.Clear();
        Main.VirusNotify.Clear();
        Mortician.msgToSend.Clear();
        Enigma.MsgToSend.Clear();
        
        Cyber.Clear();
        Sleuth.Clear();
        Pirate.OnMeetingStart();
    }
    public static void Prefix(/*MeetingHud __instance*/)
    {
        Logger.Info("------------Opening of the session------------", "Phase");
        ChatUpdatePatch.DoBlockChat = true;
        GameStates.AlreadyDied |= !Utils.IsAllAlive;
        Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
        MeetingStates.MeetingCalled = true;
    }
    public static void Postfix(MeetingHud __instance)
    {
        SoundManager.Instance.ChangeAmbienceVolume(0f);
        if (!GameStates.IsModHost) return;

        //提前储存赌怪游戏组件的模板
        GuessManager.textTemplate = UnityEngine.Object.Instantiate(__instance.playerStates[0].NameText);
        GuessManager.textTemplate.enabled = false;

        foreach (var pva in __instance.playerStates.ToArray())
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null) continue;
            var RoleTextData = Utils.GetRoleAndSubText(PlayerControl.LocalPlayer.PlayerId, pc.PlayerId);
            var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
            roleTextMeeting.transform.SetParent(pva.NameText.transform);
            roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            roleTextMeeting.fontSize = 1.5f;
            roleTextMeeting.text = RoleTextData.Item1;
            if (Main.VisibleTasksCount) roleTextMeeting.text += Utils.GetProgressText(pc);
            roleTextMeeting.color = RoleTextData.Item2;
            roleTextMeeting.gameObject.name = "RoleTextMeeting";
            roleTextMeeting.enableWordWrapping = false;
            roleTextMeeting.enabled = pc.AmOwner || ExtendedPlayerControl.KnowRoleTarget(PlayerControl.LocalPlayer, pc);

            if (!PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.IsRevealedPlayer(pc) && pc.Is(CustomRoles.Trickster))
            {
                roleTextMeeting.text = Farseer.RandomRole[PlayerControl.LocalPlayer.PlayerId];
                roleTextMeeting.text += Farseer.GetTaskState();
            }

            if (EvilTracker.IsTrackTarget(PlayerControl.LocalPlayer, pc) && EvilTracker.CanSeeLastRoomInMeeting)
            {
                roleTextMeeting.text = EvilTracker.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                roleTextMeeting.enabled = true;
            }
            if (Tracker.IsTrackTarget(PlayerControl.LocalPlayer, pc) && Tracker.CanSeeLastRoomInMeeting)
            {
                roleTextMeeting.text = Tracker.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                roleTextMeeting.enabled = true;
            }
        }

        if (Options.SyncButtonMode.GetBool())
        {
            Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
            Logger.Info("紧急会议剩余 " + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + " 次使用次数", "SyncButtonMode");
        }

        // AntiBlackout Message
        if (AntiBlackout.BlackOutIsActive)
        {
            _ = new LateTask(() =>
            {
                Utils.SendMessage(GetString("Warning.AntiBlackoutProtectionMsg"), 255, Utils.ColorString(Color.blue, GetString("AntiBlackoutProtectionTitle")), replay: true);

            }, 5f, "Warning BlackOut Is Active");
        }

        if (AntiBlackout.ShowExiledInfo)
        {
            AntiBlackout.ShowExiledInfo = false;
            if (AntiBlackout.StoreExiledMessage != "")
            {
                AntiBlackout.StoreExiledMessage = GetString("Warning.ShowAntiBlackExiledPlayer") + AntiBlackout.StoreExiledMessage;
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(AntiBlackout.StoreExiledMessage, 255, Utils.ColorString(Color.red, GetString("DefaultSystemMessageTitle")), replay: true);
                    AntiBlackout.StoreExiledMessage = "";
                }, 5.5f, "AntiBlackout.StoreExiledMessage");
            }
        }

        //if (GameStates.DleksIsActive)
        //{
        //    _ = new LateTask(() =>
        //    {
        //        Utils.SendMessage(GetString("Warning.BrokenVentsInDleksMessage"), title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("WarningTitle")), replay: true);

        //    }, 6f, "Message: Warning Broken Vents In Dleks");
        //}

        if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
        TemplateManager.SendTemplate("OnMeeting", noErr: true);

        if (AmongUsClient.Instance.AmHost)
            NotifyRoleSkillOnMeetingStart();

        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                }
                ChatUpdatePatch.DoBlockChat = false;
            }, 3f, "SetName To Chat");
        }

        foreach (var pva in __instance.playerStates.ToArray())
        {
            if (pva == null) continue;
            PlayerControl seer = PlayerControl.LocalPlayer;
            PlayerControl target = Utils.GetPlayerById(pva.TargetPlayerId);
            if (target == null) continue;

            var sb = new StringBuilder();

            //会議画面での名前変更
            //自分自身の名前の色を変更
            //NameColorManager準拠の処理
            pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);


            // Guesser Mode //
            if (Options.GuesserMode.GetBool())
            {
                if (Options.CrewmatesCanGuess.GetBool() && seer.GetCustomRole().IsCrewmate() && !seer.Is(CustomRoles.Judge) && !seer.Is(CustomRoles.Lookout) && !seer.Is(CustomRoles.Swapper) && !seer.Is(CustomRoles.Inspector))
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + pva.NameText.text;
                if (Options.ImpostorsCanGuess.GetBool() && seer.GetCustomRole().IsImpostor() && !seer.Is(CustomRoles.Councillor))
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + pva.NameText.text;
                if (Options.NeutralKillersCanGuess.GetBool() && seer.GetCustomRole().IsNK())
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + pva.NameText.text;
                if (Options.PassiveNeutralsCanGuess.GetBool() && seer.GetCustomRole().IsNonNK() && !seer.Is(CustomRoles.Doomsayer))
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.GetCustomRole()), target.PlayerId.ToString()) + " " + pva.NameText.text;

            }
            //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

            if (seer.KnowDeathReason(target))
                sb.Append($" ({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");
            /*        if (seer.KnowDeadTeam(target))
                    {
                        if (target.Is(CustomRoleTypes.Crewmate) && !(target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Egoist) || target.Is(CustomRoles.Charmed) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Infected) || target.Is(CustomRoles.Contagious) || target.Is(CustomRoles.Rogue) || target.Is(CustomRoles.Rascal) || target.Is(CustomRoles.Soulless) || !target.Is(CustomRoles.Admired)))
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), "★"));

                        if (target.Is(CustomRoleTypes.Impostor) || target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Rascal) || target.Is(CustomRoles.Parasite) || target.Is(CustomRoles.Refugee) || target.Is(CustomRoles.Crewpostor) || target.Is(CustomRoles.Convict) || !target.Is(CustomRoles.Admired))
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★"));

                        if (target.Is(CustomRoleTypes.Neutral) || target.Is(CustomRoles.Rogue) || target.Is(CustomRoles.Contagious) || target.Is(CustomRoles.Charmed) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Infected) || target.Is(CustomRoles.Egoist) || target.Is(CustomRoles.Soulless) || !target.Is(CustomRoles.Admired) || !target.Is(CustomRoles.Parasite) || !target.Is(CustomRoles.Refugee) || !target.Is(CustomRoles.Crewpostor) || !target.Is(CustomRoles.Convict))
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), "★"));



                    }
                    if (seer.KnowLivingTeam(target))
                    {
                        if (target.Is(CustomRoleTypes.Crewmate) && !(target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Egoist) || target.Is(CustomRoles.Charmed) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Infected) || target.Is(CustomRoles.Contagious) || target.Is(CustomRoles.Rogue) || target.Is(CustomRoles.Rascal) || target.Is(CustomRoles.Admired)))
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), "★"));

                        if (target.Is(CustomRoleTypes.Impostor) || target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Rascal) || target.Is(CustomRoles.Parasite) || target.Is(CustomRoles.Refugee) || target.Is(CustomRoles.Crewpostor) || target.Is(CustomRoles.Convict) || !target.Is(CustomRoles.Admired))
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★"));

                        if (target.Is(CustomRoleTypes.Neutral) || target.Is(CustomRoles.Rogue) || target.Is(CustomRoles.Contagious) || target.Is(CustomRoles.Charmed) || target.Is(CustomRoles.Recruit) || target.Is(CustomRoles.Infected) || target.Is(CustomRoles.Egoist) || target.Is(CustomRoles.Soulless) || !target.Is(CustomRoles.Admired) || !target.Is(CustomRoles.Parasite) || !target.Is(CustomRoles.Refugee) || !target.Is(CustomRoles.Crewpostor) || !target.Is(CustomRoles.Convict))
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), "★"));



                    } */
            //インポスター表示
            switch (seer.GetCustomRole().GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    if (target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate) && target.GetPlayerTaskState().IsTaskFinished)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "★")); //変更対象にSnitchマークをつける
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
                case CustomRoleTypes.Crewmate:
                    if (target.Is(CustomRoles.Marshall) && target.GetPlayerTaskState().IsTaskFinished)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Marshall), "★")); //変更対象にSnitchマークをつける
                    sb.Append(Marshall.GetWarningMark(seer, target));
                    break;
            }
            if (Captain.IsEnable)
                if ((target.PlayerId != seer.PlayerId) && (target.Is(CustomRoles.Captain) && Captain.OptionCrewCanFindCaptain.GetBool()) &&
                    (target.GetPlayerTaskState().CompletedTasksCount >= Captain.OptionTaskRequiredToReveal.GetInt()) &&
                    (seer.GetCustomRole().IsCrewmate() && !seer.Is(CustomRoles.Madmate) || (seer.Is(CustomRoles.Madmate) && Captain.OptionMadmateCanFindCaptain.GetBool())))
                    sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Captain), " ☆"));
            switch (seer.GetCustomRole())
            {
                case CustomRoles.Arsonist:
                    if (seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), "▲"));
                    break;
                case CustomRoles.Executioner:
                    sb.Append(Executioner.TargetMark(seer, target));
                    break;
                case CustomRoles.Lawyer:
                    //   sb.Append(Lawyer.TargetMark(seer, target));
                    break;
                //   case CustomRoles.Jackal:
                //   case CustomRoles.Sidekick:
                case CustomRoles.Poisoner:
                case CustomRoles.SerialKiller:
                case CustomRoles.Werewolf:
                case CustomRoles.Pelican:
                case CustomRoles.DarkHide:
                case CustomRoles.BloodKnight:
                case CustomRoles.Infectious:
                case CustomRoles.RuthlessRomantic:
                case CustomRoles.Necromancer:
                case CustomRoles.Virus:
                case CustomRoles.PlagueDoctor:
                case CustomRoles.Pyromaniac:
                case CustomRoles.Medusa:
                case CustomRoles.Succubus:
                case CustomRoles.Pickpocket:
                case CustomRoles.PotionMaster:
                case CustomRoles.Huntsman:
                case CustomRoles.Traitor:
                case CustomRoles.Spiritcaller:
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
                case CustomRoles.Jackal:
                case CustomRoles.Sidekick:
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;
                case CustomRoles.EvilTracker:
                    sb.Append(EvilTracker.GetTargetMark(seer, target));
                    break;
                case CustomRoles.Revolutionist:
                    if (seer.IsDrawPlayer(target))
                        sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Revolutionist), "●"));
                    break;
                case CustomRoles.Psychic:
                    if (target.IsRedForPsy(seer) && !seer.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), pva.NameText.text);
                    break;
                case CustomRoles.Mafia:
                    if (seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.Retributionist:
                    if (seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Retributionist), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.NiceGuesser:
                case CustomRoles.EvilGuesser:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.Is(CustomRoles.NiceGuesser) ? CustomRoles.NiceGuesser : CustomRoles.EvilGuesser), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.Judge:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.Swapper:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.Lookout:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lookout), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.Doomsayer:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doomsayer), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;
                case CustomRoles.Inspector:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Inspector), target.PlayerId.ToString()) + " " + pva.NameText.text;
                    break;

                case CustomRoles.Councillor:
                    if (!seer.Data.IsDead && !target.Data.IsDead)
                        pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Councillor), target.PlayerId.ToString()) + " " + pva.NameText.text;

                    break;

                case CustomRoles.Gamer:
                    sb.Append(Gamer.TargetMark(seer, target));
                    sb.Append(Snitch.GetWarningMark(seer, target));
                    break;

                case CustomRoles.Tracker:
                    sb.Append(Tracker.GetTargetMark(seer, target));
                    break;

                case CustomRoles.Quizmaster:
                    sb.Append(Quizmaster.TargetMark(seer, target));
                    break;
            }

            bool isLover = false;

            foreach (var SeerSubRole in seer.GetCustomSubRoles().ToArray())
            {
                switch (SeerSubRole)
                {
                    case CustomRoles.Guesser:
                        if (!seer.Data.IsDead && !target.Data.IsDead)
                            pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), target.PlayerId.ToString()) + " " + pva.NameText.text;
                        break;
                }
            }

            foreach (var TargetSubRole in target.GetCustomSubRoles().ToArray())
            {
                switch (TargetSubRole)
                {
                    case CustomRoles.Lovers:
                        if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                        {
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♥"));
                            isLover = true;
                        }
                        break;
                        /*     case CustomRoles.Sidekick:
                             if (seer.Is(CustomRoles.Sidekick) && target.Is(CustomRoles.Sidekick) && Options.SidekickKnowOtherSidekick.GetBool())
                             {
                                 sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), " ♥")); //変更対象にSnitchマークをつける
                             sb.Append(Snitch.GetWarningMark(seer, target));
                             }
                             break; */
                }
            }
            //add checks for both seer and target's subrole, maybe one day we can use them...

            //海王相关显示
            if ((seer.Is(CustomRoles.Ntr) || target.Is(CustomRoles.Ntr)) && !seer.Data.IsDead && !isLover)
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♥"));
            else if (seer == target && CustomRolesHelper.RoleExist(CustomRoles.Ntr) && !isLover)
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♥"));

            //呪われている場合
            sb.Append(Witch.GetSpelledMark(target.PlayerId, true));
            sb.Append(HexMaster.GetHexedMark(target.PlayerId, true));
            //sb.Append(Occultist.GetCursedMark(target.PlayerId, true));
            sb.Append(Shroud.GetShroudMark(target.PlayerId, true));

            if (target.PlayerId == Pirate.PirateTarget)
                sb.Append(Pirate.GetPlunderedMark(target.PlayerId, true));

            //如果是大明星
            if (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.SuperStar), "★"));

            //网络人提示
            if (target.Is(CustomRoles.Cyber) && Cyber.CyberKnown.GetBool())
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cyber), "★"));

            //玩家被勒索提示
            if (Blackmailer.ForBlackmailer.Contains(target.PlayerId))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), "╳"));

            //迷你船员提示
            if (target.Is(CustomRoles.NiceMini) && Mini.EveryoneCanKnowMini.GetBool())
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mini), Mini.Age != 18 && Mini.UpDateAge.GetBool() ? $"({Mini.Age})" : ""));

            //迷你船员提示
            if (target.Is(CustomRoles.EvilMini) && Mini.EveryoneCanKnowMini.GetBool())
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mini), Mini.Age != 18 && Mini.UpDateAge.GetBool() ? $"({Mini.Age})" : ""));

            //球状闪电提示
            if (BallLightning.IsGhost(target))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.BallLightning), "■"));

            //医生护盾提示
            if (seer.PlayerId == target.PlayerId && (Medic.InProtect(seer.PlayerId) || Medic.TempMarkProtected == seer.PlayerId) && (Medic.WhoCanSeeProtect.GetInt() is 0 or 2))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medic), "✚"));

            if (seer.Is(CustomRoles.Medic) && (Medic.InProtect(target.PlayerId) || Medic.TempMarkProtected == target.PlayerId) && (Medic.WhoCanSeeProtect.GetInt() is 0 or 1))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medic), "✚"));

            if (seer.Data.IsDead && Medic.InProtect(target.PlayerId) && !seer.Is(CustomRoles.Medic))
                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Medic), "✚"));

            //赌徒提示
            sb.Append(Totocalcio.TargetMark(seer, target));
            sb.Append(Romantic.TargetMark(seer, target));


            sb.Append(Lawyer.LawyerMark(seer, target));

            if (PlagueDoctor.IsEnable)
                sb.Append(PlagueDoctor.GetMarkOthers(seer, target));

            //会議画面ではインポスター自身の名前にSnitchマークはつけません。

            pva.NameText.text += sb.ToString();
            pva.ColorBlindName.transform.localPosition -= new Vector3(1.35f, 0f, 0f);
        }
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
class MeetingHudUpdatePatch
{
    private static int bufferTime = 10;
    private static void ClearShootButton(MeetingHud __instance, bool forceAll = false)
     => __instance.playerStates.ToList().ForEach(x => { if ((forceAll || (!Main.PlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead)) && x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });

    public static void Postfix(MeetingHud __instance)
    {
        //Meeting Skip with vote counting on keystroke (m + delete)
        if (AmongUsClient.Instance.AmHost && Input.GetKeyDown(KeyCode.F6))
        {
            __instance.CheckForEndVoting();
        }
        //

        if (AmongUsClient.Instance.AmHost && Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
        {
            __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
            {
                var player = Utils.GetPlayerById(x.TargetPlayerId);
                if (player != null && !player.Data.IsDead)
                {
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Execution;
                    player.RpcExileV2();
                    Main.PlayerStates[player.PlayerId].SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
                }
            });
        }

        //投票结束时销毁全部技能按钮
        if (!GameStates.IsVoting && __instance.lastSecond < 1)
        {
            if (GameObject.Find("ShootButton") != null) ClearShootButton(__instance, true);
            return;
        }

        //会议技能UI处理
        bufferTime--;
        if (bufferTime < 0 && __instance.discussionTimer > 0)
        {
            bufferTime = 10;
            var myRole = PlayerControl.LocalPlayer.GetCustomRole();

            //若某玩家死亡则修复会议该玩家状态
            __instance.playerStates.Where(x => (!Main.PlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead) && !x.AmDead).Do(x => x.SetDead(x.DidReport, true));

            //若玩家死亡则销毁技能按钮
            if (myRole is CustomRoles.NiceGuesser or CustomRoles.EvilGuesser or CustomRoles.Doomsayer or CustomRoles.Judge or CustomRoles.Councillor or CustomRoles.Guesser or CustomRoles.Swapper && !PlayerControl.LocalPlayer.IsAlive())
                ClearShootButton(__instance, true);

            //若黑手党死亡则创建技能按钮
            if (myRole is CustomRoles.Mafia && !PlayerControl.LocalPlayer.IsAlive() && GameObject.Find("ShootButton") == null)
                MafiaRevengeManager.CreateJudgeButton(__instance);
            if (myRole is CustomRoles.Retributionist && !PlayerControl.LocalPlayer.IsAlive() && GameObject.Find("ShootButton") == null)
                RetributionistRevengeManager.CreateJudgeButton(__instance);

            //销毁死亡玩家身上的技能按钮
            ClearShootButton(__instance);

        }
    }
}
[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
class SetHighlightedPatch
{
    public static bool Prefix(PlayerVoteArea __instance, bool value)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.HighlightedFX) return false;
        __instance.HighlightedFX.enabled = value;
        return false;
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
class MeetingHudOnDestroyPatch
{
    public static void Postfix()
    {
        MeetingStates.FirstMeeting = false;
        Logger.Info("------------End Meeting------------", "Phase");
        if (AmongUsClient.Instance.AmHost)
        {
            if (Quizmaster.IsEnable)
                Quizmaster.OnMeetingEnd();

            AntiBlackout.SetIsDead();
            Main.AllPlayerControls.Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);

            Main.LastVotedPlayerInfo = null;
            EAC.ReportTimes = [];
        }
    }
}