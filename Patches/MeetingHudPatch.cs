using AmongUs.GameOptions;
using Hazel;
using System;
using System.Text;
using TMPro;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
class CheckForEndVotingPatch
{
    public static string TempExileMsg;
    public static NetworkedPlayerInfo TempExiledPlayer;
    public static bool Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        //Meeting Skip with vote counting on keystroke (m + delete)
        var shouldSkip = false;
        if (Input.GetKeyDown(KeyCode.F6))
        {
            shouldSkip = true;
        }

        //  HasNotVoted = 255;
        //  MissedVote = 254;
        //  SkippedVote = 253;
        //  DeadVote = 252;

        var voteLog = Logger.Handler("Vote");
        try
        {
            List<MeetingHud.VoterState> statesList = [];
            MeetingHud.VoterState[] states;
            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl pc = GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;

                if (pva.DidVote && pc.PlayerId == pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    if (Madmate.MadmateSpawnMode.GetInt() == 2 && Main.MadmateNum < CustomRoles.Madmate.GetCount() && pc.CanBeMadmate())
                    {
                        Main.MadmateNum++;
                        pc.RpcSetCustomRole(CustomRoles.Madmate);
                        Logger.Info($"Assign in meeting by self vote: {pc?.Data?.PlayerName} = {pc.GetCustomRole()} + {CustomRoles.Madmate}", "Madmate");
                    }
                }

                if (Dictator.CheckVotingForTarget(pc, pva) && !Dictator.ChangeCommandToExpel.GetBool())
                {
                    var voteTarget = GetPlayerById(pva.VotedFor);
                    TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);

                    statesList.Add(new()
                    {
                        VoterId = pva.TargetPlayerId,
                        VotedForId = pva.VotedFor
                    });
                    states = [.. statesList];

                    var exiled = voteTarget.Data;

                    ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiled;
                    Main.LastVotedPlayerInfo = exiled;
                    AntiBlackout.ExilePlayerId = exiled.PlayerId;

                    if (AntiBlackout.BlackOutIsActive)
                    {
                        // Need check BlackOutIsActive again
                        var isBlackOut = AntiBlackout.BlackOutIsActive;

                        if (exiled != null)
                        {
                            AntiBlackout.ShowExiledInfo = isBlackOut;
                            ConfirmEjections(exiled, isBlackOut);
                        }

                        if (isBlackOut)
                            __instance.AntiBlackRpcVotingComplete(states, exiled, false);
                        else
                            __instance.RpcVotingComplete(states, exiled, false);
                    }
                    else
                    {

                        if (exiled != null)
                        {
                            ConfirmEjections(exiled);
                        }

                        __instance.RpcVotingComplete(states, exiled, false);
                    }

                    Logger.Info($"{voteTarget.GetNameWithRole()} expelled by Dictator", "Dictator");

                    CheckForDeathOnExile(PlayerState.DeathReason.Vote, pva.VotedFor);

                    Logger.Info("Dictatorial vote, forced closure of the meeting", "Special Phase");

                    voteTarget.SetRealKiller(pc);

                    return true;
                }

                if (pva.DidVote && pva.VotedFor < 253 && pc.IsAlive())
                {
                    var voteTarget = GetPlayerById(pva.VotedFor);

                    if (voteTarget == null || !voteTarget.IsAlive() || voteTarget.Data.Disconnected)
                    {
                        SendMessage(GetString("VoteDead"), pc.PlayerId);
                        __instance.UpdateButtons();
                        __instance.RpcClearVoteDelay(pc.GetClientId());
                        Swapper.CheckSwapperTarget(pva.VotedFor);
                        continue;
                    }

                    if (voteTarget != null)
                    {
                        pc.GetRoleClass()?.OnVote(pc, voteTarget); // Role has voted
                        voteTarget.GetRoleClass()?.OnVoted(voteTarget, pc); // Role is voted

                        if (voteTarget.Is(CustomRoles.Aware))
                        {
                            Aware.OnVoted(pc, pva);
                        }
                        if (voteTarget.Is(CustomRoles.Rebirth))
                        {
                            Rebirth.CountVotes(voteTarget.PlayerId, pva.TargetPlayerId);
                        }
                    }
                }
            }

            if (!shouldSkip)
            {
                foreach (var ps in __instance.playerStates)
                {
                    //Players who are not dead have not voted
                    if (!ps.DidVote && GetPlayerById(ps.TargetPlayerId)?.IsAlive() == true)
                    {
                        return false;
                    }
                }
            }

            NetworkedPlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
            bool tie = false;

            foreach (var ps in __instance.playerStates)
            {
                if (ps == null) continue;
                voteLog.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, $"({GetVoteName(ps.TargetPlayerId)})".PadRightV2(40), ps.VotedFor, $"({GetVoteName(ps.VotedFor)})"));
                var voter = GetPlayerById(ps.TargetPlayerId);
                if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                if (Options.VoteMode.GetBool())
                {
                    if (ps.VotedFor == 253 && !voter.Data.IsDead &&
                        !(Options.WhenSkipVoteIgnoreFirstMeeting.GetBool() && MeetingStates.FirstMeeting) && // Ignore First Meeting
                        !(Options.WhenSkipVoteIgnoreNoDeadBody.GetBool() && !MeetingStates.IsExistDeadBody) && // No Dead Body
                        !(Options.WhenSkipVoteIgnoreEmergency.GetBool() && MeetingStates.IsEmergencyMeeting) // Ignore Emergency Meeting
                        )
                    {
                        switch (Options.GetWhenSkipVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()} voted to skip, so the player will suicide");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()} voted to skip, so the player voted self");
                                break;
                            default:
                                break;
                        }
                    }
                    if (ps.VotedFor == 254 && !voter.Data.IsDead)
                    {
                        switch (Options.GetWhenNonVote())
                        {
                            case VoteMode.Suicide:
                                TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                voteLog.Info($"{voter.GetNameWithRole()} did not vote, so the player will suicide");
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                voteLog.Info($"{voter.GetNameWithRole()} did not vote, so the player voted self");
                                break;
                            case VoteMode.Skip:
                                ps.VotedFor = 253;
                                voteLog.Info($"{voter.GetNameWithRole()} did not vote, so the player voted skip");
                                break;
                            default:
                                break;
                        }
                    }
                }

                var player = GetPlayerById(ps.TargetPlayerId);
                var playerRoleClass = player.GetRoleClass();

                //Hides vote
                if (playerRoleClass.HideVote(ps)) continue;

                // Assing Madmate Slef Vote
                if (ps.TargetPlayerId == ps.VotedFor && Madmate.MadmateSpawnMode.GetInt() == 2) continue;

                statesList.Add(new MeetingHud.VoterState()
                {
                    VoterId = ps.TargetPlayerId,
                    VotedForId = ps.VotedFor
                });

                // Swapper swap votes
                if (voter.GetRoleClass() is Swapper sw) sw.SwapVotes(__instance);

                playerRoleClass?.AddVisualVotes(ps, ref statesList);

                if (CheckRole(ps.TargetPlayerId, CustomRoles.Stealer))
                {
                    Stealer.AddVisualVotes(ps, ref statesList);
                }
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Paranoia) && Paranoia.DualVotes.GetBool())
                {
                    Paranoia.AddVisualVotes(ps, ref statesList);
                }
                if (CheckRole(ps.TargetPlayerId, CustomRoles.Knighted) && !Monarch.HideAdditionalVotesForKnighted.GetBool())
                {
                    statesList.Add(new MeetingHud.VoterState()
                    {
                        VoterId = ps.TargetPlayerId,
                        VotedForId = ps.VotedFor
                    });
                }
            }

            Dictionary<byte, int> VotingData = [];

            if (CustomRoles.Influenced.RoleExist())
            {
                Influenced.ChangeVotingData(VotingData);
                VotingData = __instance.CustomCalculateVotes(true);
            }
            else
            {
                VotingData = __instance.CustomCalculateVotes();
            }

            for (int i = 0; i < statesList.Count; i++)
            {
                var voterstate = statesList[i];
                var voterpc = GetPlayerById(voterstate.VoterId);
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
                voteLog.Info($"{GetVoteName(data.Key)}({data.Key}): {data.Value} votes");
                if (data.Value > max)
                {
                    voteLog.Info($"{GetVoteName(data.Key)}({data.Key}) have a higher number of votes ({data.Value})");
                    exileId = data.Key;
                    max = data.Value;
                    tie = false;
                }
                else if (data.Value == max)
                {
                    voteLog.Info($"{GetVoteName(data.Key)}({data.Key}) has the same number of votes as {GetVoteName(exileId)}({exileId}) - Count: {data.Value}");
                    exileId = byte.MaxValue;
                    tie = true;
                }
                voteLog.Info($"Exiled ID: {exileId} ({GetVoteName(exileId)}), max: {max} votes");
            }

            voteLog.Info($"Decision to exiled a player: tie: {tie}, {exileId} ({GetVoteName(exileId)})");

            var allPlayers = GameData.Instance.AllPlayers.ToArray();
            var allPlayerCount = allPlayers.Count;

            bool braked = false;
            if (tie)
            {
                byte targetId = byte.MaxValue;
                foreach (var data in VotingData.Where(x => x.Key < allPlayerCount && x.Value == max).ToArray())
                {
                    if (Tiebreaker.VoteFor.Contains(data.Key))
                    {
                        if (targetId != byte.MaxValue)
                        {
                            targetId = byte.MaxValue;
                            break;
                        }
                        targetId = data.Key;
                    }
                }
                if (targetId != byte.MaxValue)
                {
                    Logger.Info("Flat breakers cover expulsion of players", "Tiebreaker Vote");
                    exiledPlayer = GetPlayerInfoById(targetId);
                    tie = false;
                    braked = true;
                }
            }
            List<Collector> CollectorCL = GetRoleBasesByType<Collector>()?.ToList();
            if (Collector.HasEnabled) CollectorCL?.Do(x => { x.CollectAmount(VotingData, __instance); });

            if (Options.VoteMode.GetBool() && Options.WhenTie.GetBool() && tie)
            {
                switch ((TieMode)Options.WhenTie.GetValue())
                {
                    case TieMode.Default:
                        exiledPlayer = allPlayers.FirstOrDefault(info => info.PlayerId == exileId);
                        break;
                    case TieMode.All:
                        var exileIds = VotingData.Where(x => x.Key < allPlayerCount && x.Value == max).Select(kvp => kvp.Key).ToArray();
                        foreach (var playerId in exileIds)
                            GetPlayerById(playerId).SetRealKiller(null);
                        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Vote, exileIds);
                        exiledPlayer = null;
                        break;
                    case TieMode.Random:
                        exiledPlayer = allPlayers.OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => VotingData.TryGetValue(x.PlayerId, out int vote) && vote == max);
                        tie = false;
                        break;
                }
            }
            else if (!braked)
                exiledPlayer = allPlayers.FirstOrDefault(info => !tie && info.PlayerId == exileId);

            if (Keeper.IsTargetExiled(exileId))
            {
                exileId = 0xff;
                exiledPlayer = GetPlayerInfoById(exileId);
            }
            else if (exiledPlayer?.Object.Is(CustomRoles.Rebirth) == true && Rebirth.SwapSkins(exiledPlayer.Object, out var NewExiled))
            {
                exileId = NewExiled.PlayerId;
                exiledPlayer = NewExiled;
            }

            exiledPlayer?.Object.SetRealKiller(null);

            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiledPlayer;
            Main.LastVotedPlayerInfo = exiledPlayer;
            if (exiledPlayer != null) AntiBlackout.ExilePlayerId = exiledPlayer.PlayerId;
            //RPC
            if (AntiBlackout.BlackOutIsActive)
            {
                // Need check BlackOutIsActive again
                var isBlackOut = AntiBlackout.BlackOutIsActive;

                if (exiledPlayer != null)
                {
                    AntiBlackout.ShowExiledInfo = isBlackOut;
                    ConfirmEjections(exiledPlayer, isBlackOut);
                }

                if (isBlackOut)
                    __instance.AntiBlackRpcVotingComplete(states, exiledPlayer, tie);
                else
                    __instance.RpcVotingComplete(states, exiledPlayer, tie);
            }
            else
            {
                if (exiledPlayer != null)
                {
                    ConfirmEjections(exiledPlayer);
                }

                __instance.RpcVotingComplete(states, exiledPlayer, tie); // Normal processing
            }

            CheckForDeathOnExile(PlayerState.DeathReason.Vote, exileId);

            return false;
        }
        catch (Exception ex)
        {
            Logger.SendInGame(string.Format(GetString("Error.MeetingException"), ex.Message));
            throw;
        }
    }

    // Credit：https://github.com/music-discussion/TownOfHost-TheOtherRoles
    public static void ConfirmEjections(NetworkedPlayerInfo exiledPlayer, bool AntiBlackoutStore = false)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (exiledPlayer == null) return;

        var exileId = exiledPlayer.PlayerId;
        if (exileId is < 0 or > 254) return;

        var realName = Main.AllPlayerNames[exiledPlayer.PlayerId];
        Main.LastVotedPlayer = realName;

        var player = GetPlayerById(exiledPlayer.PlayerId);
        var role = GetString(exiledPlayer.GetCustomRole().ToString());
        var crole = exiledPlayer.GetCustomRole();
        var coloredRole = GetDisplayRoleAndSubName(exileId, exileId, false, true);

        if (Options.ConfirmEgoistOnEject.GetBool() && player.Is(CustomRoles.Egoist))
            coloredRole = ColorString(GetRoleColor(CustomRoles.Egoist), coloredRole.RemoveHtmlTags());

        if (Options.ConfirmLoversOnEject.GetBool() && player.Is(CustomRoles.Lovers))
            coloredRole = ColorString(GetRoleColor(CustomRoles.Lovers), coloredRole.RemoveHtmlTags());

        if (Rascal.AppearAsMadmate(player))
            coloredRole = ColorString(GetRoleColor(CustomRoles.Madmate), GetRoleString("Mad-") + coloredRole.RemoveHtmlTags());

        var name = "";
        int impnum = 0;
        int neutralnum = 0;
        int apocnum = 0;
        int covennum = 0;
        int badnum = Main.AllAlivePlayerControls.Count(x => x != exiledPlayer.Object && !Main.AfterMeetingDeathPlayers.ContainsKey(x.PlayerId) && x.GetCountTypes() is not CountTypes.None and not CountTypes.OutOfGame and not CountTypes.Crew); //Number of alive players that keep game going


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
            if (pc_role.IsImpostorTeamV3() && !pc.Is(CustomRoles.Narc) && pc != exiledPlayer.Object)
                impnum++;
            else if (pc_role.IsNK() && pc != exiledPlayer.Object)
                neutralnum++;
            else if (pc_role.IsNA() && pc != exiledPlayer.Object)
                apocnum++;
            else if (pc_role.IsCoven() && pc != exiledPlayer.Object)
                covennum++;
        }
        switch (Options.CEMode.GetInt())
        {
            case 0:
                name = string.Format(GetString("PlayerExiled"), realName);
                break;
            case 1:
                if (Options.ShowBetrayalAddonsOnEject.GetBool() && player.IsAnySubRole(x => x.IsBetrayalAddonV2() && x != CustomRoles.Narc && (x != CustomRoles.Egoist || Egoist.EgoistCountAsConverted.GetBool())))
                {
                    if (player.Is(CustomRoles.Madmate))
                        name = string.Format(GetString("BelongTo"), realName, ColorString(GetRoleColor(CustomRoles.Impostor), GetString("TeamImpostor")));
                    else if (player.Is(CustomRoles.Admired))
                        name = string.Format(GetString("IsGood"), realName);
                    else if (player.IsAnySubRole(x => x.IsConverted() && x is not CustomRoles.Madmate and not CustomRoles.Enchanted))
                        name = string.Format(GetString("BelongTo"), realName, ColorString(new Color32(127, 140, 141, byte.MaxValue), GetString("TeamNeutral")));
                    else if (player.Is(CustomRoles.Enchanted))
                        name = string.Format(GetString("BelongTo"), realName, ColorString(GetRoleColor(CustomRoles.Coven), GetString("TeamCoven")));
                }

                else if (player.GetCustomRole().IsImpostorTeamV3() && !player.Is(CustomRoles.Narc))
                    name = string.Format(GetString("BelongTo"), realName, ColorString(GetRoleColor(CustomRoles.Impostor), GetString("TeamImpostor")));

                else if (player.GetCustomRole().IsCrewmate() || player.Is(CustomRoles.Narc))
                    name = string.Format(GetString("IsGood"), realName);

                else if (player.GetCustomRole().IsNeutral() && !player.GetCustomRole().IsMadmate())
                    name = string.Format(GetString("BelongTo"), realName, ColorString(new Color32(127, 140, 141, byte.MaxValue), GetString("TeamNeutral")));

                else if (player.GetCustomRole().IsCoven())
                    name = string.Format(GetString("BelongTo"), realName, ColorString(GetRoleColor(CustomRoles.Coven), GetString("TeamCoven")));

                break;
            case 2:
                var ejectedRoleText = Options.ShowBetrayalAddonsOnEject.GetBool() || player.Is(CustomRoles.Narc) ? coloredRole : player.GetCustomRole().ToColoredString();
                name = string.Format(GetString("PlayerIsRole"), realName, ejectedRoleText);
                if (Options.ShowTeamNextToRoleNameOnEject.GetBool())
                {
                    name += " (";
                    if (Options.ShowBetrayalAddonsOnEject.GetBool() && player.IsAnySubRole(x => x.IsBetrayalAddonV2() && x != CustomRoles.Narc && (x != CustomRoles.Egoist || Egoist.EgoistCountAsConverted.GetBool())))
                    {
                        if (player.Is(CustomRoles.Madmate))
                            name += ColorString(new Color32(255, 25, 25, byte.MaxValue), GetString("TeamImpostor"));
                        else if (player.IsAnySubRole(x => x.IsConverted() && x is not CustomRoles.Madmate and not CustomRoles.Enchanted))
                            name += ColorString(new Color32(127, 140, 141, byte.MaxValue), GetString("TeamNeutral"));
                        else if (player.Is(CustomRoles.Admired) || player.Is(CustomRoles.Narc))
                            name += ColorString(new Color32(140, 255, 255, byte.MaxValue), GetString("TeamCrewmate"));
                        else if (player.Is(CustomRoles.Enchanted))
                            name += ColorString(new Color32(172, 66, 242, byte.MaxValue), GetString("TeamCoven"));
                    }
                    else if (player.GetCustomRole().IsImpostorTeamV3() && !player.Is(CustomRoles.Narc))
                        name += ColorString(new Color32(255, 25, 25, byte.MaxValue), GetString("TeamImpostor"));
                    else if (player.GetCustomRole().IsNeutral() && !player.GetCustomRole().IsMadmate())
                        name += ColorString(new Color32(127, 140, 141, byte.MaxValue), GetString("TeamNeutral"));
                    else if (player.GetCustomRole().IsCrewmate() || player.Is(CustomRoles.Narc))
                        name += ColorString(new Color32(140, 255, 255, byte.MaxValue), GetString("TeamCrewmate"));
                    else if (player.GetCustomRole().IsCoven())
                        name += ColorString(new Color32(172, 66, 242, byte.MaxValue), GetString("TeamCoven"));
                    name += ")";
                }
                break;
        }
        var DecidedWinner = false;

        player.GetRoleClass()?.CheckExile(exiledPlayer, ref DecidedWinner, isMeetingHud: true, name: ref name);

        CustomRoleManager.AllEnabledRoles.Do(roleClass => roleClass.CheckExileTarget(exiledPlayer, ref DecidedWinner, isMeetingHud: true, name: ref name));

        if (DecidedWinner) name += "<size=0>";
        if (Options.ShowImpRemainOnEject.GetBool() && !DecidedWinner)
        {
            name += "\n";
            if (badnum <= 0)
                name += GetString("NoImpRemain");
            else
            {
                name += impnum switch
                {
                    1 => GetString("OneImpRemain"),
                    > 1 => string.Format(GetString("ImpRemain"), impnum),
                    _ => string.Empty
                };
                if (Options.ShowNKRemainOnEject.GetBool() && neutralnum > 0)
                    name += "\n" + neutralnum switch
                    {
                        1 => GetString("OneNeutralRemain"),
                        > 1 => string.Format(GetString("NeutralRemain"), neutralnum),
                        _ => string.Empty
                    };
                if (Options.ShowNARemainOnEject.GetBool() && apocnum > 0)
                    name += "\n" + apocnum switch
                    {
                        1 => GetString("OneApocRemain"),
                        > 1 => string.Format(GetString("ApocRemain"), apocnum),
                        _ => string.Empty
                    };
                if (Options.ShowCovenRemainOnEject.GetBool() && covennum > 0)
                    name += "\n" + covennum switch
                    {
                        1 => GetString("OneCovenRemain"),
                        > 1 => string.Format(GetString("CovenRemain"), covennum),
                        _ => string.Empty
                    };
                if (impnum <= 0
                    && (neutralnum <= 0 || !Options.ShowNKRemainOnEject.GetBool())
                    && (apocnum <= 0 || !Options.ShowNARemainOnEject.GetBool())
                    && (covennum <= 0 || !Options.ShowCovenRemainOnEject.GetBool()))
                    name += GetString("NoImpRemain") + "\n" + GetString("PotentialThreat");
            }
        }

    EndOfSession:
        name += "<size=0>";
        Logger.Info(name, "ConfirmEjections");
        TempExileMsg = name;
        TempExiledPlayer = exiledPlayer;

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
        if (MeetingHud.Instance == null || !MeetingHud.Instance.playerStates.Any()) return null;
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
            var pc = playerId.GetPlayer();
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
        if (deathReason == PlayerState.DeathReason.Vote)
        {
            foreach (var player in Main.AllPlayerControls)
            {
                player.GetRoleClass()?.OnCheckForEndVoting(deathReason, playerIds);
            }
        }

        foreach (var playerId in playerIds)
        {
            if (CustomRoles.Lovers.IsEnable() && deathReason == PlayerState.DeathReason.Vote && !Main.isLoversDead && Main.LoversPlayers.FirstOrDefault(lp => lp.PlayerId == playerId) != null)
            {
                FixedUpdateInNormalGamePatch.LoversSuicide(playerId, true);
            }

            RevengeOnExile(playerId);
        }
    }
    private static void RevengeOnExile(byte playerId)
    {
        var player = GetPlayerById(playerId);
        if (player == null) return;
        var target = PickRevengeTarget(player);
        if (target == null) return;

        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);

        Logger.Info($"{player.GetNameWithRole()} revenge:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer)
    {
        List<PlayerControl> TargetList = [];
        foreach (var candidate in Main.AllAlivePlayerControls)
        {
            if (candidate.PlayerId == exiledplayer.PlayerId || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
        }
        if (TargetList == null || TargetList.Count == 0) return null;
        var target = TargetList.RandomElement();
        return target;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
class CastVotePatch
{
    public static bool Prefix(MeetingHud __instance, byte srcPlayerId, byte suspectPlayerId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        var voter = GetPlayerById(srcPlayerId);
        if (voter == null || !voter.IsAlive()) return false;

        var target = GetPlayerById(suspectPlayerId);
        if (target == null && suspectPlayerId < 253)
        {
            SendMessage(GetString("VoteDead"), srcPlayerId);
            __instance.RpcClearVoteDelay(voter.GetClientId());
            return false;
        } //Vote a disconnect player

        // Return vote to player if uses checkvote and wants to vote normal without using his abilities.
        if (suspectPlayerId == 253 && voter.GetRoleClass()?.IsMethodOverridden("CheckVote") == true)
        {
            if (!voter.GetRoleClass().HasVoted)
            {
                voter.GetRoleClass().HasVoted = true;
                SendMessage(GetString("VoteNotUseAbility"), voter.PlayerId);
                __instance.RpcClearVoteDelay(voter.GetClientId());
                return false;
            }
        }


        // Coven Leader Retraining
        if (CustomRoles.CovenLeader.RoleExist() && target == voter && CovenLeader.retrainPlayer.ContainsKey(voter.PlayerId))
        {
            PlayerControl CL = CustomRoles.CovenLeader.GetPlayerListByRole().First();

            Logger.Info($"Coven Leader Retraining [{voter.PlayerId}]{voter.GetNameWithRole()} => {CovenLeader.retrainPlayer[voter.PlayerId]}", "CastVotePatch");
            voter.GetRoleClass()?.OnRemove(voter.PlayerId);
            voter.RpcChangeRoleBasis(CovenLeader.retrainPlayer[voter.PlayerId]);
            voter.RpcSetCustomRole(CovenLeader.retrainPlayer[voter.PlayerId]);
            voter.GetRoleClass()?.OnAdd(voter.PlayerId);
            if (CL.GetBetrayalAddon() != CustomRoles.NotAssigned) voter.RpcSetCustomRole(CL.GetBetrayalAddon());
            SendMessage(string.Format(GetString("CovenLeaderAcceptRetrain"), CustomRoles.CovenLeader.ToColoredString(), CovenLeader.retrainPlayer[voter.PlayerId].ToColoredString()), CL.PlayerId);
            SendMessage(string.Format(GetString("RetrainAcceptOffer"), CustomRoles.CovenLeader.ToColoredString(), CovenLeader.retrainPlayer[voter.PlayerId].ToColoredString()), voter.PlayerId);

            CovenLeader.retrainPlayer.Clear();

            if (CovenLeader.List.Any())
                foreach (var covenLeaderId in CovenLeader.List)
                {
                    var covenLeader = covenLeaderId.GetPlayer();
                    if (covenLeader == null) continue;

                    covenLeader.RpcRemoveAbilityUse();
                }

            __instance.RpcClearVoteDelay(voter.GetClientId());
            return false;
        }
        else if (CustomRoles.CovenLeader.RoleExist() && target != voter && CovenLeader.retrainPlayer.ContainsKey(voter.PlayerId) && CovenLeader.retrainPlayer[voter.PlayerId].IsCoven())
        {
            PlayerControl CL = CustomRoles.CovenLeader.GetPlayerListByRole().First();
            SendMessage(string.Format(GetString("CovenLeaderDeclineRetrain"), CovenLeader.retrainPlayer[voter.PlayerId].ToColoredString()), CL.PlayerId);
            SendMessage(string.Format(GetString("RetrainDeclineOffer"), CustomRoles.CovenLeader.ToColoredString()), voter.PlayerId);
            CovenLeader.retrainPlayer.Clear();
            __instance.RpcClearVoteDelay(voter.GetClientId());
            return false;
        }

        // Coven Necronomicon Voting
        if (voter.IsPlayerCoven() && Main.AllAlivePlayerControls.Count(x => x.IsPlayerCoven()) == 1)
        {
            Logger.Info("Solo Coven, Necronomicon Votes will not Activate", "CastVotePatch");
        }
        else if (suspectPlayerId == 253 && voter.IsPlayerCoven())
        {
            if (!voter.GetRoleClass().HasVoted)
            {
                voter.GetRoleClass().HasVoted = true;
                SendMessage(GetString("VoteNotUseAbility"), voter.PlayerId);
                __instance.RpcClearVoteDelay(voter.GetClientId());
                return false;
            }
        }
        else if (voter.IsPlayerCoven() && target.IsPlayerCoven())
        {
            if (!voter.GetRoleClass().HasVoted)
            {
                voter.GetRoleClass().HasVoted = true;
                CovenManager.necroVotes.Add(voter.PlayerId, target.PlayerId);
                SendMessage(string.Format(GetString("NecronomiconVote"), target.GetRealName()), voter.PlayerId);
                __instance.RpcClearVoteDelay(voter.GetClientId());
                return false;
            }
        }

        if (target != null && suspectPlayerId < 253)
        {
            if (!target.IsAlive() || target.Data.Disconnected)
            {
                SendMessage(GetString("VoteDead"), srcPlayerId);
                __instance.RpcClearVoteDelay(voter.GetClientId());
                Swapper.CheckSwapperTarget(suspectPlayerId);
                return false;
            }


            if (!voter.GetRoleClass().HasVoted && voter.GetRoleClass().CheckVote(voter, target) == false)
            {
                Logger.Info($"Canceling {voter.GetRealName()}'s vote because of {voter.GetCustomRole()}", "CastVotePatch.RoleBase.CheckVote");
                voter.GetRoleClass().HasVoted = true;
                __instance.RpcClearVoteDelay(voter.GetClientId());

                // Attempts to set thumbsdown color to the same as playerrole to signify player ability used on (only for modded client)
                PlayerVoteArea pva = __instance.playerStates.FirstOrDefault(pva => pva.TargetPlayerId == target.PlayerId);
                Color color = GetRoleColor(voter.GetCustomRole()).ShadeColor(0.5f);
                pva.ThumbsDown.set_color_Injected(ref color);
                return false;
            }

            switch (voter.GetCustomRole())
            {
                case CustomRoles.Dictator:
                    if (target.Is(CustomRoles.Solsticer))
                    {
                        SendMessage(GetString("ExpelSolsticer"), srcPlayerId);
                        __instance.RpcClearVoteDelay(voter.GetClientId());
                        return false;
                    }
                    break;
            }
        }

        return true;
    }

    public static void Postfix(MeetingHud __instance)
    {
        // Prevent double check end voting
        if (GameStates.IsMeeting && MeetingHud.Instance.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted)
        {
            __instance.CheckForEndVoting();
            //For stuffs in check for end voting to work
        }
    }
}
static class ExtendedMeetingHud
{
    public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance, bool CountInfluenced = false)
    {
        Logger.Info("===Start of vote counting processing===", "Vote");

        Dictionary<byte, int> dic = [];
        Collector.Clear();
        Tiebreaker.Clear();
        Evader.RememberRandom();

        // |Voted By| Number of Times Voted For
        foreach (var ps in __instance.playerStates)
        {
            if (ps == null) continue;

            // whether this player is voted for in the player panel
            if (ps.VotedFor is not 252 and not byte.MaxValue and not 254)
            {
                // Default number of votes 1
                int VoteNum = 1;

                // Judgment only when voting for a valid player
                var target = GetPlayerById(ps.VotedFor);
                if (target != null)
                {
                    // Check Tiebreaker voting
                    Tiebreaker.CheckVote(target, ps);

                    // Check Collector voting data
                    Collector.CollectorVotes(target, ps);
                }

                //Add votes for roles
                var pc = GetPlayerById(ps.TargetPlayerId);
                if (pc != null && CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, pc.GetCustomRole())
                    && ps.TargetPlayerId != ps.VotedFor && ps != null)
                    VoteNum += ps.TargetPlayerId.GetRoleClassById().AddRealVotesNum(ps); // returns + 0 or given role value (+/-)

                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Knighted) // not doing addons lol, so this stays
                    && ps.TargetPlayerId != ps.VotedFor
                    ) VoteNum += 1;

                if (Paranoia.DualVotes.GetBool())
                {
                    if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Paranoia)
                        && ps.TargetPlayerId != ps.VotedFor
                        ) VoteNum += VoteNum;
                }

                // Additional votes
                if (pc != null && pc.Is(CustomRoles.Stealer))
                {
                    VoteNum += Stealer.AddRealVotesNum(ps);
                }

                // Madmate assign by vote
                if (ps.TargetPlayerId == ps.VotedFor && Madmate.MadmateSpawnMode.GetInt() == 2) VoteNum = 0;

                if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.VoidBallot)) VoteNum = 0;
                if (Dreamweaver.IsInsomnia(ps.TargetPlayerId)) VoteNum = 0;

                if (Jailer.IsTarget(ps.VotedFor) || Jailer.IsTarget(ps.TargetPlayerId)) VoteNum = 0; //jailed can't vote and can't get voted

                if (target != null)
                {
                    // Remove all votes for Zombie
                    Zombie.CheckRealVotes(target, ref VoteNum);

                    //Solsticer can not get voted out
                    if (target.Is(CustomRoles.Solsticer)) VoteNum = 0;
                }

                if (!CountInfluenced)
                {
                    if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Influenced))
                    {
                        VoteNum = 0;
                    }
                }
                //Set influenced vote num to zero while counting votes, and count influenced vote upon finishing influenced check

                if (target != null && target.Is(CustomRoles.Evader))
                {
                    Evader.CheckExile(ps.VotedFor, ref VoteNum);
                }

                //Add 1 vote If key is not defined, overwrite with 1 and define
                dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out int num) ? VoteNum : num + VoteNum; //Count the number of times this player has been voted in
            }
        }
        return dic;
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class MeetingHudStartPatch
{
    public static List<(string, byte, string)> msgToSend = [];
    public static void AddMsg(string text, byte sendTo = 255, string title = "")
        => msgToSend.Add((text, sendTo, title));
    public static void NotifyRoleSkillOnMeetingStart()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Main.MeetingIsStarted = true;

        msgToSend = [];

        // Description in first meeting
        if (Options.SendRoleDescriptionFirstMeeting.GetBool() && MeetingStates.FirstMeeting)
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => !x.IsModded()).ToArray())
            {
                var role = pc.GetCustomRole();
                var Des = pc.GetRoleInfo(true);
                var title = $"<color=#ffffff>" + role.GetRoleTitle() + "</color>\n";
                var Conf = new StringBuilder();
                var Sub = new StringBuilder();
                var rlHex = GetRoleColorCode(role);
                var SubTitle = $"<color={rlHex}>" + GetString("YourAddon") + "</color>\n";
                if (Options.CustomRoleSpawnChances.TryGetValue(role, out var opt))
                    ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref Conf);
                var cleared = Conf.ToString();
                var Setting = $"<color={rlHex}>{GetString(role.ToString())} {GetString("Settings:")}</color>\n";
                Conf.Clear().Append($"<color=#ffffff>" + $"<size={ChatCommands.Csize}>" + Setting + cleared + "</size>" + "</color>");

                foreach (var subRole in Main.PlayerStates[pc.PlayerId].SubRoles.ToArray())
                    Sub.Append($"\n\n" + $"<size={ChatCommands.Asize}>" + subRole.GetRoleTitle() + subRole.GetInfoLong() + "</size>");

                if (Sub.ToString() != string.Empty)
                {
                    var ACleared = Sub.ToString().Remove(0, 2);
                    ACleared = ACleared.Length > 1200 ? $"<size={ChatCommands.Asize}>" + ACleared.RemoveHtmlTags() + "</size>" : ACleared;
                    Sub.Clear().Append(ACleared);
                }

                AddMsg(Des, pc.PlayerId, title);
                AddMsg("", pc.PlayerId, Conf.ToString());
                if (Sub.ToString() != string.Empty) AddMsg(Sub.ToString(), pc.PlayerId, SubTitle);

            }

        if (msgToSend.Count >= 1)
        {
            var msgToSendNewList = msgToSend.ToList();
            _ = new LateTask(() =>
            {
                foreach (var (text, sendTo, title) in msgToSendNewList)
                {
                    // check player left
                    if (sendTo != byte.MaxValue && GetPlayerById(sendTo) == null) continue;

                    SendMessage(text, sendTo, title);
                }
            }, 3f, "Skill Description First Meeting");

        }

        msgToSend = [];

        // Madmate spawn mode: Self vote
        if (Madmate.MadmateSpawnMode.GetInt() == 2 && CustomRoles.Madmate.GetCount() > 0)
            AddMsg(string.Format(GetString("Message.MadmateSelfVoteModeNotify"), GetString("MadmateSpawnMode.SelfVote")));

        //Bait Notify
        Bait.SendNotify();

        // Apocalypse Notify, thanks tommy
        var transformRoles = new CustomRoles[] { CustomRoles.Pestilence, CustomRoles.War, CustomRoles.Famine, CustomRoles.Death };
        foreach (var role in transformRoles)
        {
            if (role.RoleExist())
            {
                _ = new LateTask(() =>
                {
                    var roleMessage = role switch
                    {
                        CustomRoles.Pestilence => GetString("PestilenceTransform"),
                        CustomRoles.War => GetString("BerserkerTransform"),
                        CustomRoles.Famine => GetString("BakerTransform"),
                        CustomRoles.Death => GetString("SoulCollectorTransform"),
                        _ => "",
                    };

                    if (roleMessage != "")
                        AddMsg(roleMessage, 255, ColorString(GetRoleColor(role), GetString("ApocalypseIsNigh")));

                }, 3f, $"{role} Apocalypse Notify");
            }
        }

        string MimicMsg = "";
        foreach (var pc in Main.AllPlayerControls)
        {
            pc?.GetRoleClass()?.OnMeetingHudStart(pc);
            Main.PlayerStates.Do(plr => plr.Value.RoleClass.OnOthersMeetingHudStart(pc));

            foreach (var csId in Cyber.CyberDead)
            {
                if (!Cyber.ImpKnowCyberDead.GetBool() && pc.GetCustomRole().IsImpostor()) continue;
                if (!Cyber.NeutralKnowCyberDead.GetBool() && pc.GetCustomRole().IsNeutral()) continue;
                if (!Cyber.CrewKnowCyberDead.GetBool() && pc.GetCustomRole().IsCrewmate()) continue;
                if (!Cyber.CovenKnowCyberDead.GetBool() && pc.GetCustomRole().IsCoven()) continue;

                AddMsg(string.Format(GetString("CyberDead"), Main.AllPlayerNames[csId]), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Cyber), GetString("Cyber").ToUpper()));
            }

            // Sleuth notify msg
            if (Sleuth.SleuthNotify.ContainsKey(pc.PlayerId))
                AddMsg(Sleuth.SleuthNotify[pc.PlayerId], pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Sleuth), GetString("Sleuth").ToUpper()));

            // Check Mimic kill
            if (pc.Is(CustomRoles.Mimic) && !pc.IsAlive())
                Main.AllAlivePlayerControls.Where(x => x.GetRealKiller()?.PlayerId == pc.PlayerId).Do(x => MimicMsg += $"\n{x.GetNameWithRole(true)}");
        }

        if (Eavesdropper.IsEnable)
            Eavesdropper.GetMessage();

        // Add Mimic msg
        if (MimicMsg != "")
        {
            MimicMsg = GetString("MimicDeadMsg") + "\n" + MimicMsg;

            var isImpostorTeamList = Main.AllPlayerControls.Where(x => x.GetCustomRole().IsImpostorTeam()).ToArray();
            foreach (var imp in isImpostorTeamList)
            {
                AddMsg(MimicMsg, imp.PlayerId, ColorString(GetRoleColor(CustomRoles.Mimic), GetString("Mimic").ToUpper()));
            }
        }

        msgToSend.Do(x => Logger.Info($"To:{x.Item2} {x.Item3} => {x.Item1}", "Skill Notice OnMeeting Start"));

        // Send message
        _ = new LateTask(() =>
        {
            foreach (var (text, sendTo, title) in msgToSend)
            {
                // check player left
                if (sendTo != byte.MaxValue && GetPlayerById(sendTo) == null) continue;

                SendMessage(text, sendTo, title);
            }
        }, 3f, "Skill Notice On Meeting Start");

        Main.PlayerStates.Do(x
            => x.Value.RoleClass.MeetingHudClear());

        Cyber.Clear();
        Sleuth.Clear();
    }
    public static void Prefix(/*MeetingHud __instance*/)
    {
        Logger.Info("------------Opening of the session------------", "Phase");
        ChatUpdatePatch.DoBlockChat = true;
        GameStates.AlreadyDied |= !IsAllAlive;
        Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
        MeetingStates.MeetingCalled = true;

        CheckForEndVotingPatch.TempExiledPlayer = null;
        CheckForEndVotingPatch.TempExileMsg = string.Empty;
    }
    public static void Postfix(MeetingHud __instance)
    {
        SoundManager.Instance.ChangeAmbienceVolume(0f);
        if (!GameStates.IsModHost) return;

        GuessManager.textTemplate = UnityEngine.Object.Instantiate(__instance.playerStates[0].NameText);
        GuessManager.textTemplate.enabled = false;

        foreach (var pva in __instance.playerStates)
        {
            var player = PlayerControl.LocalPlayer;
            var target = pva.TargetPlayerId.GetPlayer();
            if (target == null || player == null) continue;

            var playerId = player.PlayerId;
            var targetId = pva.TargetPlayerId;

            var textTemplate = pva.NameText;

            // Create role text in meeting
            var RoleTextData = GetRoleAndSubText(playerId, targetId, isMeeting: true);
            var roleTextMeeting = UnityEngine.Object.Instantiate(textTemplate);
            if (roleTextMeeting.transform.FindChild("DeathReasonTextMeeting") != null)
                UnityEngine.Object.Destroy(roleTextMeeting.transform.FindChild("DeathReasonTextMeeting").gameObject);
            roleTextMeeting.transform.SetParent(textTemplate.transform);
            roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            roleTextMeeting.fontSize = 1.6f;
            roleTextMeeting.text = RoleTextData.Item1;
            if (Main.VisibleTasksCount) roleTextMeeting.text += GetProgressText(target);
            roleTextMeeting.color = RoleTextData.Item2;
            roleTextMeeting.gameObject.name = "RoleTextMeeting";
            roleTextMeeting.enableWordWrapping = false;
            roleTextMeeting.enabled = target.AmOwner || ExtendedPlayerControl.KnowRoleTarget(player, target);

            // Create death reason text in meeting
            var deathReasonText = UnityEngine.Object.Instantiate(textTemplate);
            if (deathReasonText.transform.FindChild("RoleTextMeeting") != null)
                UnityEngine.Object.Destroy(deathReasonText.transform.FindChild("RoleTextMeeting").gameObject);
            deathReasonText.transform.transform.SetParent(textTemplate.transform);
            deathReasonText.transform.localPosition = new Vector3(0f, +0.18f, 0f);
            deathReasonText.fontSize = 1.4f;
            deathReasonText.text = $"『{ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(targetId))}』";
            deathReasonText.color = Color.white;
            deathReasonText.gameObject.name = "DeathReasonTextMeeting";
            deathReasonText.enableWordWrapping = false;
            deathReasonText.enabled = player.KnowDeathReason(target);

            // Thanks BAU (By D1GQ)
            var PlayerLevel = pva.transform.Find("PlayerLevel");
            var LevelDisplay = UnityEngine.Object.Instantiate(PlayerLevel, pva.transform);
            LevelDisplay.localPosition = new Vector3(-1.21f, -0.15f, PlayerLevel.transform.localPosition.z);
            LevelDisplay.transform.SetSiblingIndex(pva.transform.Find("PlayerLevel").GetSiblingIndex() + 1);
            LevelDisplay.gameObject.name = "PlayerId";
            LevelDisplay.GetComponent<SpriteRenderer>().color = Palette.Purple;
            var IdLabel = LevelDisplay.transform.Find("LevelLabel");
            var IdNumber = LevelDisplay.transform.Find("LevelNumber");
            UnityEngine.Object.Destroy(IdLabel.GetComponent<TextTranslatorTMP>());
            IdLabel.GetComponent<TextMeshPro>().text = "ID";
            IdNumber.GetComponent<TextMeshPro>().text = pva.TargetPlayerId.ToString();
            IdLabel.name = "IdLabel";
            IdNumber.name = "IdNumber";

            var myRole = player.GetRoleClass();
            var enable = true;

            if (player.IsAlive() && Overseer.IsRevealedPlayer(player, target))
            {
                var blankRT = new StringBuilder();
                var result = new StringBuilder(roleTextMeeting.text);
                if ((target.Is(CustomRoles.Trickster) && (!target.Is(CustomRoles.Narc) || player.Is(CustomRoles.Madmate))) || Illusionist.IsCovIllusioned(targetId))
                {
                    blankRT.Clear().Append(Overseer.GetRandomRole(playerId));
                    blankRT.Append(TaskState.GetTaskState());
                    result.Clear().Append($"<size={roleTextMeeting.fontSize}>{blankRT}</size>");
                }
                if (target.Is(CustomRoles.Narc) && !player.Is(CustomRoles.Madmate))
                {
                    blankRT.Clear().Append(CustomRoles.Sheriff.ToColoredString());
                    if (Sheriff.ShowShotLimit.GetBool()) blankRT.Append(ColorString(GetRoleColor(CustomRoles.Sheriff).ShadeColor(0.25f), $" ({Sheriff.ShotLimitOpt.GetInt()})"));
                    result.Clear().Append($"<size={roleTextMeeting.fontSize}>{blankRT}</size>");
                }
                if (Illusionist.IsNonCovIllusioned(targetId))
                {
                    var randomRole = CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCoven()).ToList().RandomElement();
                    blankRT.Clear().Append(ColorString(GetRoleColor(randomRole), GetString(randomRole.ToString())));
                    if (randomRole.GetStaticRoleClass().IsMethodOverridden("GetProgressText")) // Roles with Ability Uses
                    {
                        blankRT.Append(randomRole.GetStaticRoleClass().GetProgressText(playerId, false));
                    }
                    result.Clear().Append($"<size={roleTextMeeting.fontSize}>{blankRT}</size>");
                }
                roleTextMeeting.text = result.ToString();
            }

            var suffixBuilder = new StringBuilder(32);
            if (myRole != null)
            {
                suffixBuilder.Append(myRole.GetSuffix(player, target, isForMeeting: true));
            }
            suffixBuilder.Append(CustomRoleManager.GetSuffixOthers(player, target, isForMeeting: true));

            // If Doppelganger.CurrentVictimCanSeeRolesAsDead is disabled and player is the most recent victim from the doppelganger hide role information for player.

            if (suffixBuilder.Length > 0)
            {
                roleTextMeeting.text = suffixBuilder.ToString();
                roleTextMeeting.enabled = enable;
            }
        }

        if (Options.SyncButtonMode.GetBool())
        {
            SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
            Logger.Info("Number of remaining buttons: " + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount), "SyncButtonMode");
        }

        // AntiBlackout Message
        if (AntiBlackout.BlackOutIsActive)
        {
            _ = new LateTask(() =>
            {
                SendMessage(GetString("Warning.AntiBlackoutProtectionMsg"), 255, ColorString(Color.blue, GetString("AntiBlackoutProtectionTitle")), noReplay: true);

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
                    SendMessage(AntiBlackout.StoreExiledMessage, 255, ColorString(Color.red, GetString("DefaultSystemMessageTitle")), noReplay: true);
                    AntiBlackout.StoreExiledMessage = "";
                }, 5.5f, "AntiBlackout.StoreExiledMessage");
            }
        }

        //if (GameStates.DleksIsActive)
        //{
        //    _ = new LateTask(() =>
        //    {
        //        SendMessage(GetString("Warning.BrokenVentsInDleksMessage"), title: ColorString(GetRoleColor(CustomRoles.NiceMini), GetString("WarningTitle")), replay: true);

        //    }, 6f, "Message: Warning Broken Vents In Dleks");
        //}

        if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
        TemplateManager.SendTemplate("OnMeeting", noErr: true);

        try
        {
            if (AmongUsClient.Instance.AmHost)
                NotifyRoleSkillOnMeetingStart();
        }
        catch (Exception error)
        {
            Logger.Error($"Error after notify {error}", "NotifyRoleSkillOnMeetingStart");
        }

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

        foreach (var pva in __instance.playerStates)
        {
            if (pva == null) continue;
            PlayerControl target = pva.TargetPlayerId.GetPlayer();
            if (target == null) continue;

            PlayerControl seer = PlayerControl.LocalPlayer;
            var seerRoleClass = seer.GetRoleClass();

            // if based role is Shapeshifter/Phantom and is Desync Shapeshifter/Phantom
            if (seerRoleClass?.ThisRoleBase.GetRoleTypes() is RoleTypes.Shapeshifter or RoleTypes.Phantom && seer.HasDesyncRole())
            {
                // When target is impostor, set name color as white
                target.cosmetics.SetNameColor(Color.white);
                pva.NameText.color = Color.white;
            }
            if (Main.PlayerStates[seer.PlayerId].IsNecromancer || Main.PlayerStates[target.PlayerId].IsNecromancer)
            {
                // When target is impostor, set name color as white
                target.cosmetics.SetNameColor(Color.white);
                pva.NameText.color = Color.white;
            }

            var sb = new StringBuilder();

            //pva.NameText.text = target.GetRealName(isMeeting: true);
            pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

            //if (seer.KnowDeathReason(target))
            //    sb.Append($"『{ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(target.PlayerId))}』");

            sb.Append(seerRoleClass?.GetMark(seer, target, true));
            sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

            if (seer.GetCustomRole().IsImpostor() && target.GetPlayerTaskState().IsTaskFinished)
            {
                if (target.Is(CustomRoles.Snitch) && target.Is(CustomRoles.Madmate))
                    sb.Append(CustomRoles.Impostor.GetColoredTextByRole("★"));
            }


            var tempNemeText = seer.GetRoleClass().PVANameText(pva, seer, target);
            if (tempNemeText != string.Empty)
            {
                pva.NameText.text = tempNemeText;
            }

            //bool isLover = false;
            foreach (var TargetSubRole in target.GetCustomSubRoles().ToArray())
            {
                switch (TargetSubRole)
                {
                    case CustomRoles.Lovers:
                        if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                        {
                            sb.Append(CustomRoles.Lovers.GetColoredTextByRole("♥"));
                            //isLover = true;
                        }
                        break;
                    case CustomRoles.Cyber when Cyber.CyberKnown.GetBool():
                        sb.Append(CustomRoles.Cyber.GetColoredTextByRole("★"));
                        break;
                }
            }
            //add checks for both seer and target's subrole, maybe one day we can use them...

            pva.NameText.text += sb.ToString();
            pva.ColorBlindName.transform.localPosition -= new Vector3(1.35f, 0f, 0f);
        }

        __instance.SortButtons();
    }
}
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
class MeetingHudUpdatePatch
{
    private static int bufferTime = 10;
    private static void ClearShootButton(MeetingHud __instance, bool forceAll = false)
     => __instance.playerStates.ToList().ForEach(x => { if ((forceAll || (!Main.PlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead)) && x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });

    // Force call RpcClose here
    public static bool Prefix(MeetingHud __instance)
    {
        if (__instance.CurrentState != MeetingHud.VoteStates.Results)
        {
            return true;
        }

        __instance.discussionTimer += Time.deltaTime;

        float num4 = __instance.discussionTimer - __instance.resultsStartedAt;
        float num5 = Mathf.Max(0f, 5f - num4);
        __instance.UpdateTimerText(StringNames.MeetingProceeds, Mathf.CeilToInt(num5));
        if (AmongUsClient.Instance.AmHost && num5 <= 0f)
        {
            __instance.state = MeetingHud.VoteStates.Proceeding;
            __instance.RpcClose();
        }

        return false;
    }

    public static void Postfix(MeetingHud __instance)
    {
        if (__instance == null || !GameStates.IsInGame) return;

        //Meeting Skip with vote counting on keystroke (m + delete)
        if (AmongUsClient.Instance.AmHost && Input.GetKeyDown(KeyCode.F6))
        {
            __instance.CheckForEndVoting();
        }

        if (AmongUsClient.Instance.AmHost && Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
        {
            __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
            {
                var player = GetPlayerById(x.TargetPlayerId);
                if (player != null && !player.Data.IsDead)
                {
                    player.SetDeathReason(PlayerState.DeathReason.Execution);
                    player.SetRealKiller(PlayerControl.LocalPlayer);
                    player.RpcExileV2();
                    Main.PlayerStates[player.PlayerId].SetDead();
                    MurderPlayerPatch.AfterPlayerDeathTasks(PlayerControl.LocalPlayer, player, GameStates.IsMeeting);
                    SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()} was executed", "Execution");
                    __instance.CheckForEndVoting();
                }
            });
        }

        if (!GameStates.IsVoting && __instance.lastSecond < 1)
        {
            if (GameObject.Find("ShootButton") != null) ClearShootButton(__instance, true);
            return;
        }

        bufferTime--;
        if (bufferTime < 0 && __instance.discussionTimer > 0)
        {
            bufferTime = 10;
            var myRole = PlayerControl.LocalPlayer.GetCustomRole();

            //__instance.playerStates.Where(x => !x.TargetPlayerId.GetPlayer().IsAlive() && !x.AmDead)
            //    .Do(x => x.SetDead(x.DidReport, true, x.GAIcon));

            if (myRole is CustomRoles.NiceGuesser or CustomRoles.EvilGuesser or CustomRoles.Doomsayer or CustomRoles.Judge or CustomRoles.Councillor or CustomRoles.Guesser or CustomRoles.Swapper && !PlayerControl.LocalPlayer.IsAlive())
                ClearShootButton(__instance, true);

            if (myRole is CustomRoles.Nemesis && !PlayerControl.LocalPlayer.IsAlive() && GameObject.Find("ShootButton") == null)
                Nemesis.CreateJudgeButton(__instance);
            if (myRole is CustomRoles.Retributionist && !PlayerControl.LocalPlayer.IsAlive() && GameObject.Find("ShootButton") == null)
                Retributionist.CreateJudgeButton(__instance);

            ClearShootButton(__instance);

        }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.HandleProceed))]
class MeetingHudHandleProceedPatch
{
    public static bool Prefix(MeetingHud __instance)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            __instance.StartCoroutine(Effects.SwayX(__instance.HostIcon.transform, 0.75f, 0.25f));
            return false;
        }
        if (__instance.state != MeetingHud.VoteStates.Results)
        {
            return false;
        }
        __instance.state = MeetingHud.VoteStates.Proceeding;
        __instance.RpcClose();
        return false;
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
            AntiBlackout.SetIsDead();

            Main.LastVotedPlayerInfo = null;
            EAC.ReportTimes = [];
        }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.RpcClose))]
class MeetingHudRpcClosePatch
{
    public static bool Prefix(MeetingHud __instance)
    {
        Logger.Info("MeetingHud.RpcClose is being called", "MeetingHudRpcClosePatch");
        // Send SetName rpc together with Close rpc
        if (Options.CurrentGameMode is CustomGameMode.Standard)
        {
            if (AmongUsClient.Instance.AmClient)
            {
                __instance.Close();
            }

            var writer = MessageWriter.Get(SendOption.Reliable);

            writer.StartMessage(5);
            writer.Write(AmongUsClient.Instance.GameId);

            if (CheckForEndVotingPatch.TempExiledPlayer != null)
            {
                var info = CheckForEndVotingPatch.TempExiledPlayer;
                var player = info.Object;

                if (player != null)
                {
                    writer.StartMessage(2);
                    writer.WritePacked(player.NetId);
                    writer.Write((byte)RpcCalls.SetName);
                    writer.Write(info.NetId);
                    writer.Write(CheckForEndVotingPatch.TempExileMsg);
                    writer.EndMessage();
                }
            }

            writer.StartMessage(2);
            writer.WritePacked(__instance.NetId);
            writer.Write((byte)RpcCalls.CloseMeeting);
            writer.Write(CheckForEndVotingPatch.TempExileMsg);
            writer.EndMessage();

            writer.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();

            return false;
        }
        else
        {
            return true;
        }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.HandleRpc))]
class MeetingHudHandleRpcPatch
{
    public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        if (callId == (byte)RpcCalls.CloseMeeting)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                EAC.WarnHost(4);
                Logger.Warn("MeetingHud.HandleRpc CloseMeeting is being called, impossible to receive as host.", "MeetingHudHandleRpcPatch");
                return false;
            }
            else
            {
                Logger.Info("Received Close Meeting Rpc", "MeetingHudHandleRpcPatch");

                if (reader.BytesRemaining > 6)
                {
                    try
                    {
                        var temp = reader.ReadString();

                        if (temp.Contains("<size"))
                        {
                            Logger.Info($"Read Name From Rpc: {temp}", "MeetingHudHandleRpcPatch");
                            CheckForEndVotingPatch.TempExileMsg = temp;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.Serialize))]
class NetworkedPlayerInfoSerializePatch
{
    private static string LastName = string.Empty;
    public static void Prefix(NetworkedPlayerInfo __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (MeetingHud.Instance || ExileController.Instance)
        {
            if (CheckForEndVotingPatch.TempExiledPlayer != null && CheckForEndVotingPatch.TempExiledPlayer.PlayerId == __instance.PlayerId)
            {
                LastName = __instance.PlayerName;
                __instance.PlayerName = CheckForEndVotingPatch.TempExileMsg;
            }
        }
    }

    public static void Postfix(NetworkedPlayerInfo __instance)
    {
        if (__instance.PlayerName == CheckForEndVotingPatch.TempExileMsg)
        {
            __instance.PlayerName = LastName;
        }
    }
}
