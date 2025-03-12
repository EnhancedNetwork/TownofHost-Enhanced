using Hazel;
using System;
using System.Text.RegularExpressions;
using TMPro;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

public static class GuessManager
{
    public static string GetFormatString()
    {
        string text = GetString("PlayerIdList");
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            string id = pc.PlayerId.ToString();
            string name = pc.GetRealName();
            text += $"\n{id} → {name}";
        }
        return text;
    }

    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        foreach (string comm in comList)
        {
            if (exact)
            {
                if (msg == "/" + comm) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comm))
                {
                    msg = msg.Replace("/" + comm, string.Empty);
                    return true;
                }
            }
        }
        return false;
    }

    public static byte GetColorFromMsg(string msg)
    {
        if (ComfirmIncludeMsg(msg, "红|紅|red")) return 0;
        if (ComfirmIncludeMsg(msg, "蓝|藍|深蓝|blue")) return 1;
        if (ComfirmIncludeMsg(msg, "绿|綠|深绿|green")) return 2;
        if (ComfirmIncludeMsg(msg, "粉红|粉紅|pink")) return 3;
        if (ComfirmIncludeMsg(msg, "橘|橘|orange")) return 4;
        if (ComfirmIncludeMsg(msg, "黄|黃|yellow")) return 5;
        if (ComfirmIncludeMsg(msg, "黑|黑|black")) return 6;
        if (ComfirmIncludeMsg(msg, "白|白|white")) return 7;
        if (ComfirmIncludeMsg(msg, "紫|紫|perple")) return 8;
        if (ComfirmIncludeMsg(msg, "棕|棕|brown")) return 9;
        if (ComfirmIncludeMsg(msg, "青|青|cyan")) return 10;
        if (ComfirmIncludeMsg(msg, "黄绿|黃綠|浅绿|lime")) return 11;
        if (ComfirmIncludeMsg(msg, "红褐|紅褐|深红|maroon")) return 12;
        if (ComfirmIncludeMsg(msg, "玫红|玫紅|浅粉|rose")) return 13;
        if (ComfirmIncludeMsg(msg, "焦黄|焦黃|淡黄|banana")) return 14;
        if (ComfirmIncludeMsg(msg, "灰|灰|gray")) return 15;
        if (ComfirmIncludeMsg(msg, "茶|茶|tan")) return 16;
        if (ComfirmIncludeMsg(msg, "珊瑚|珊瑚|coral")) return 17;
        else return byte.MaxValue;
    }

    private static bool ComfirmIncludeMsg(string msg, string key)
    {
        var keys = key.Split('|');
        foreach (string str in keys)
        {
            if (msg.Contains(str)) return true;
        }
        return false;
    }

    public static void ShowInfoMessage(this PlayerControl player, bool isUI, string message, string title = "")
    {
        if (isUI) player.ShowPopUp(message, title);
        else Utils.SendMessage(message, player.PlayerId, title);
    }

    public static readonly Dictionary<byte, int> GuesserGuessed = [];
    public static bool GuesserMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return false;
        if (!pc.Is(CustomRoles.NiceGuesser)
            && !pc.Is(CustomRoles.EvilGuesser)
            && !pc.Is(CustomRoles.Doomsayer)
            && !pc.Is(CustomRoles.Judge)
            && !pc.Is(CustomRoles.Councillor)
            && !pc.Is(CustomRoles.Guesser)
            && !Options.GuesserMode.GetBool()) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id||編號|玩家編號")) operate = 1;
        else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌|賭", false)) operate = 2;
        else return false;

        Logger.Msg(msg, "Msg Guesser");
        Logger.Msg($"{operate}", "Operate");

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("GuessDead"));
            return true;
        }
        if (!pc.Is(CustomRoles.NiceGuesser))
        {
            if (pc.GetCustomRole().IsCrewmate() && !Options.CrewmatesCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.Judge))
            {
                pc.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
                return true;
            }
        }
        if (!pc.Is(CustomRoles.EvilGuesser))
        {
            if ((pc.Is(Custom_Team.Impostor) || pc.GetCustomRole().IsMadmate()) && !Options.ImpostorsCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.Councillor))
            {
                pc.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
                return true;
            }
        }

        if (pc.GetCustomRole().IsNK() && !Options.NeutralKillersCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
            return true;
        }
        if (pc.GetCustomRole().IsNonNK() && !Options.PassiveNeutralsCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.Doomsayer))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
            return true;
        }
        if (pc.GetCustomRole().IsNA() && !Options.NeutralApocalypseCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
            return true;
        }
        if (pc.GetCustomRole().IsCoven() && !Options.CovenCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessNotAllowed"));
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {

            if (
            NiceGuesser.NeedHideMsg(pc) ||
            EvilGuesser.NeedHideMsg(pc) ||
            Doomsayer.NeedHideMsg(pc) ||
            (pc.Is(CustomRoles.Guesser) && Guesser.GTryHideMsg.GetBool()) ||
            (Options.GuesserMode.GetBool() && Options.HideGuesserCommands.GetBool())
            )
            {
                //if (Options.NewHideMsg.GetBool()) ChatManager.SendPreviousMessagesToAll();
                //else TryHideMsg(); 
                TryHideMsg();
                ChatManager.SendPreviousMessagesToAll();
            }
            else if (pc.AmOwner && !isUI) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                pc.ShowInfoMessage(isUI, error);
                return true;
            }
            var target = Utils.GetPlayerById(targetId);

            Logger.Msg($" {pc.PlayerId}", "Guesser - pc.PlayerId");
            Logger.Msg($" {target.PlayerId}", "Guesser - target.PlayerId");
            Logger.Msg($" {role}", "Guesser - role");

            if (target != null)
            {

                if (target.Is(CustomRoles.VoodooMaster) && VoodooMaster.Dolls[target.PlayerId].Count > 0)
                {
                    target = Utils.GetPlayerById(VoodooMaster.Dolls[target.PlayerId].Where(x => Utils.GetPlayerById(x).IsAlive()).ToList().RandomElement());
                    _ = new LateTask(() =>
                    {
                        Utils.SendMessage(string.Format(GetString("VoodooMasterTargetInMeeting"), target.GetRealName()), Utils.GetPlayerListByRole(CustomRoles.VoodooMaster).First().PlayerId);
                    }, 2f, "Voodoo Master Notify");
                }
                GuessMaster.OnGuess(role);
                bool guesserSuicide = false;

                if (!GuesserGuessed.ContainsKey(pc.PlayerId)) GuesserGuessed.Add(pc.PlayerId, 0);

                if (pc.GetRoleClass().GuessCheck(isUI, pc, target, role, ref guesserSuicide)) return true;

                if (target.GetRoleClass().OnRoleGuess(isUI, target, pc, role, ref guesserSuicide)) return true;
                // Used to be a exploit. Guess may be canceled even misguessed
                // You need to manually check whether guessed correct and then perform role abilities

                if (CopyCat.playerIdList.Contains(pc.PlayerId))
                {
                    Logger.Info($"Guess Disabled for this player {pc.PlayerId}", "GuessManager");
                    pc.ShowInfoMessage(isUI, GetString("GuessDisabled"));
                    return true;
                }
                if (Jailer.IsTarget(pc.PlayerId) && role != CustomRoles.Jailer)
                {
                    pc.ShowInfoMessage(isUI, GetString("JailedCanOnlyGuessJailer"), Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")));
                    return true;
                }
                if (Jailer.IsTarget(target.PlayerId))
                {
                    pc.ShowInfoMessage(isUI, GetString("CantGuessJailed"), Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")));
                    return true;
                }
                if (!Mundane.OnGuess(pc))
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessedAsMundane"));
                    return true;
                }
                if (Medic.IsProtected(target.PlayerId) && !Medic.GuesserIgnoreShield.GetBool())
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessShielded"));
                    return true;
                }

                if (!role.IsEnable() && !role.RoleExist(true) && Options.CanOnlyGuessEnabled.GetBool())
                {
                    pc.ShowInfoMessage(isUI, string.Format(GetString("GuessRoleNotEnabled"), role.ToString()));
                    return true;
                }
                if (role == CustomRoles.Bait && target.Is(CustomRoles.Bait) && Bait.BaitNotification.GetBool())
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessNotifiedBait"));
                    return true;
                }
                if (role == CustomRoles.Rainbow && target.Is(CustomRoles.Rainbow))
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessRainbow"));
                    return true;
                }
                if (role is CustomRoles.LastImpostor or CustomRoles.Mare or CustomRoles.Cyber or CustomRoles.Flash or CustomRoles.Glow or CustomRoles.Sloth)
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessObviousAddon"));
                    return true;
                }
                if (target.Is(CustomRoles.Onbound))
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessOnbound"));
                    return true;
                }

                if (role == CustomRoles.GM || target.Is(CustomRoles.GM))
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessGM"));
                    return true;
                }

                if (role.IsTNA() && role != CustomRoles.Pestilence && !Options.TransformedNeutralApocalypseCanBeGuessed.GetBool() || role == CustomRoles.Pestilence && !PlagueBearer.PestilenceKillsGuessers.GetBool())
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessImmune"));
                    return true;
                }

                // Guesser (add-on) Cant Guess Addons
                if (role.IsAdditionRole() && pc.Is(CustomRoles.Guesser) && !Guesser.GCanGuessAdt.GetBool())
                {
                    pc.ShowInfoMessage(isUI, GetString("GuessAdtRole"));
                    return true;
                }

                // Guesser Mode Can/Cant Guess Addons
                if (Options.GuesserMode.GetBool())
                {
                    if (role.IsAdditionRole() && !Options.CanGuessAddons.GetBool())
                    {
                        // Impostors Cant Guess Addons
                        if (Options.ImpostorsCanGuess.GetBool() && (pc.Is(Custom_Team.Impostor) || pc.GetCustomRole().IsMadmate()) && !(pc.Is(CustomRoles.EvilGuesser) || pc.Is(CustomRoles.Guesser)))
                        {
                            pc.ShowInfoMessage(isUI, GetString("GuessAdtRole"));
                            return true;
                        }

                        // Crewmates Cant Guess Addons
                        if (Options.CrewmatesCanGuess.GetBool() && pc.Is(Custom_Team.Crewmate) && !(pc.Is(CustomRoles.NiceGuesser) || pc.Is(CustomRoles.Guesser)))
                        {
                            pc.ShowInfoMessage(isUI, GetString("GuessAdtRole"));
                            return true;
                        }

                        // Coven Cant Guess Addons
                        if (Options.CovenCanGuess.GetBool() && pc.Is(Custom_Team.Coven) && !pc.Is(CustomRoles.Guesser))
                        {
                            pc.ShowInfoMessage(isUI, GetString("GuessAdtRole"));
                            return true;
                        }

                        // Neutrals Cant Guess Addons
                        if ((Options.NeutralKillersCanGuess.GetBool() || Options.PassiveNeutralsCanGuess.GetBool()) && pc.Is(Custom_Team.Neutral) && !(pc.Is(CustomRoles.Doomsayer) || pc.Is(CustomRoles.Guesser)))
                        {
                            pc.ShowInfoMessage(isUI, GetString("GuessAdtRole"));
                            return true;
                        }
                    }
                    if ((role.IsImpostor() || role.IsMadmate()) && !Options.ImpCanGuessImp.GetBool())
                    {
                        if (Options.ImpostorsCanGuess.GetBool() && (pc.Is(Custom_Team.Impostor) || pc.GetCustomRole().IsMadmate()) && !(pc.Is(CustomRoles.EvilGuesser) || pc.Is(CustomRoles.Guesser)))
                        {
                            pc.ShowInfoMessage(isUI, GetString("GuessImpRole"));
                            return true;
                        }
                    }
                    if (role.IsCoven() && !Options.CovenCanGuessCoven.GetBool())
                    {
                        if (Options.CovenCanGuess.GetBool() && (pc.Is(Custom_Team.Coven) || pc.Is(CustomRoles.Enchanted)) && !pc.Is(CustomRoles.Guesser))
                        {
                            pc.ShowInfoMessage(isUI, GetString("GuessCovenRole"));
                            return true;
                        }
                    }
                    if (role.IsCrewmate() && !Options.CrewCanGuessCrew.GetBool())
                    {
                        if (Options.CrewmatesCanGuess.GetBool() && pc.Is(Custom_Team.Crewmate) && !(pc.Is(CustomRoles.NiceGuesser) || pc.Is(CustomRoles.Guesser)))
                        {
                            pc.ShowInfoMessage(isUI, GetString("GuessCrewRole"));
                            return true;
                        }
                    }
                    if (role.IsNA() && !Options.ApocCanGuessApoc.GetBool())
                    {
                        if (Options.NeutralApocalypseCanGuess.GetBool() && pc.IsNeutralApocalypse() && !pc.Is(CustomRoles.Guesser))
                        {
                            pc.ShowInfoMessage(isUI, GetString("GuessApocRole"));
                            return true;
                        }
                    }
                }

                if (pc.PlayerId == target.PlayerId)
                {
                    if (pc.Is(CustomRoles.DoubleShot) && !DoubleShot.IsActive.Contains(pc.PlayerId))
                    {
                        DoubleShot.IsActive.Add(pc.PlayerId);

                        Logger.Msg($"{pc.PlayerId}", "GuesserDoubleShotIsActive-1");

                        pc.ShowInfoMessage(isUI, GetString("GuessDoubleShot"));
                        return true;
                    }
                    else
                    {
                        if (pc.Is(CustomRoles.DoubleShot) && DoubleShot.IsActive.Contains(pc.PlayerId))
                            DoubleShot.IsActive.Remove(pc.PlayerId);

                        pc.ShowInfoMessage(isUI, GetString("LaughToWhoGuessSelf"), Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
                        guesserSuicide = true;
                        Logger.Msg($"Self guess: guesserSuicide - {guesserSuicide}", "GuesserSuicide");
                    }
                }
                else if (!target.Is(role))
                {
                    if (pc.Is(CustomRoles.DoubleShot) && !DoubleShot.IsActive.Contains(pc.PlayerId))
                    {
                        DoubleShot.IsActive.Add(pc.PlayerId);

                        Logger.Msg($"{pc.PlayerId}", "GuesserDoubleShotIsActive-4");

                        pc.ShowInfoMessage(isUI, GetString("GuessDoubleShot"));
                        return true;
                    }
                    else
                    {
                        if (pc.Is(CustomRoles.DoubleShot) && DoubleShot.IsActive.Contains(pc.PlayerId))
                            DoubleShot.IsActive.Remove(pc.PlayerId);

                        guesserSuicide = true;
                        Logger.Msg($"The guesser didn't guess the role: guesserSuicide - {guesserSuicide}", "GuesserSuicide");
                    }
                }

                if (target.Is(role) && target.Is(CustomRoles.Rebound))
                {
                    guesserSuicide = true;
                    Logger.Info($"{pc.GetNameWithRole()} guessed {target.GetNameWithRole()}, guesser suicide because rebound", "GuessManager");
                }

                Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()} guessed => {target.GetNameWithRole().RemoveHtmlTags()}", "Guesser");

                var dp = guesserSuicide ? pc : target;
                target = dp;

                Logger.Info($" Player：{target.GetNameWithRole().RemoveHtmlTags()} was guessed", "Guesser");

                GuesserGuessed[pc.PlayerId]++;

                if (pc.GetRoleClass().CheckMisGuessed(isUI, pc, target, role, ref guesserSuicide)) return true;

                string Name = dp.GetRealName();
                if (!Options.DisableKillAnimationOnGuess.GetBool()) CustomSoundsManager.RPCPlayCustomSoundAll("Gunfire");

                if (!GameStates.IsProceeding)
                {
                    _ = new LateTask(() =>
                    {
                        dp.SetDeathReason(PlayerState.DeathReason.Gambled);
                        dp.SetRealKiller(pc);
                        RpcGuesserMurderPlayer(dp);

                        if (dp == pc) GuessMaster.OnGuess(role, isMisguess: true, dp: dp);

                        Main.PlayersDiedInMeeting.Add(dp.PlayerId);
                        MurderPlayerPatch.AfterPlayerDeathTasks(pc, dp, true);

                        _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("GuessKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceGuesser), GetString("GuessKillTitle")), true); }, 0.6f, "Guess Msg");

                        var doomsayers = Utils.GetPlayerListByRole(CustomRoles.Doomsayer);
                        if (Doomsayer.HasEnabled && doomsayers != null && doomsayers.Any()) doomsayers?.Select(x => x?.GetRoleClass())
                            .Do(x => { if (x is Doomsayer ds) ds.SendMessageAboutGuess(pc, dp, role); });

                    }, 0.2f, "Guesser Kill");
                }
            }
        }
        return true;
    }

    public static TextMeshPro NameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;
    public static void RpcGuesserMurderPlayer(this PlayerControl pc)
    {
        try
        {
            // DEATH STUFF //
            GameEndCheckerForNormal.ShouldNotCheck = true;
            var amOwner = pc.AmOwner;
            pc.Data.IsDead = true;
            pc.RpcExileV2();
            Main.PlayerStates[pc.PlayerId].SetDead();
            var meetingHud = MeetingHud.Instance;
            var hudManager = DestroyableSingleton<HudManager>.Instance;
            SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
            if (!Options.DisableKillAnimationOnGuess.GetBool()) hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
            if (amOwner)
            {
                hudManager.ShadowQuad.gameObject.SetActive(false);
                pc.NameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
                pc.RpcSetScanner(false);
                ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
                importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
                meetingHud.SetForegroundForDead();
            }
            PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
                x => x.TargetPlayerId == pc.PlayerId
            );
            if (voteArea == null) return;
            if (voteArea.DidVote) voteArea.UnsetVote();
            voteArea.AmDead = true;
            voteArea.Overlay.gameObject.SetActive(true);
            voteArea.Overlay.color = Color.white;
            voteArea.XMark.gameObject.SetActive(true);
            voteArea.XMark.transform.localScale = Vector3.one;
            foreach (var playerVoteArea in meetingHud.playerStates)
            {
                if (playerVoteArea.VotedFor != pc.PlayerId) continue;
                playerVoteArea.UnsetVote();
                var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                if (!voteAreaPlayer.AmOwner) continue;
                meetingHud.ClearVote();
            }
            Swapper.CheckSwapperTarget(pc.PlayerId);

            // Prevent double check end voting
            if (meetingHud.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted)
            {
                meetingHud.CheckForEndVoting();
            }
            _ = new LateTask(() => hudManager.SetHudActive(false), 0.3f, "SetHudActive in GuesserMurderPlayer", shoudLog: false);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuessKill, SendOption.Reliable, -1);
            writer.Write(pc.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            GameEndCheckerForNormal.ShouldNotCheck = false;
        }
        catch (Exception error)
        {
            // try{} catch{} added in case an exception occurs, and "ShouldNotCheck" will remain true forever
            Logger.Error($"Error after guesser murder: {error}", "RpcGuesserMurderPlayer");
            GameEndCheckerForNormal.ShouldNotCheck = false;
        }
    }
    public static void RpcClientGuess(PlayerControl pc)
    {
        var amOwner = pc.AmOwner;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.NameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        //pc.Die(DeathReason.Kill);
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates.ToArray())
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
        hudManager.SetHudActive(false);
        _ = new LateTask(() => hudManager.SetHudActive(false), 0.3f, "SetHudActive in ClientGuess", shoudLog: false);
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("GuessHelp");
            role = new();
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessNull");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByName(msg, out role))
        {
            error = GetString("GuessHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void TryHideMsg()
    {
        ChatUpdatePatch.DoBlockChat = true;

        if (ChatManager.quickChatSpamMode != Options.QuickChatSpamMode.QuickChatSpam_Disabled)
        {
            ChatManager.SendQuickChatSpam();
        }
        else
        {
            var roles = CustomRolesHelper.AllRoles.Where(x => x is not CustomRoles.NotAssigned).ToArray();
            var rd = IRandom.Instance;
            string msg;
            string[] command = ["bet", "bt", "guess", "gs", "shoot", "st", "赌", "猜", "审判", "tl", "判", "审"];
            for (int i = 0; i < 20; i++)
            {
                msg = "/";
                if (rd.Next(1, 100) < 20)
                {
                    msg += "id";
                }
                else
                {
                    msg += command[rd.Next(0, command.Length - 1)];
                    msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                    msg += rd.Next(0, 15).ToString();
                    msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                    CustomRoles role = roles.RandomElement();
                    msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                    msg += Utils.GetRoleName(role);
                }
                var player = Main.AllAlivePlayerControls.RandomElement();
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                writer.StartMessage(-1);
                writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                writer.EndMessage();
                writer.SendMessage();
            }
        }
        ChatUpdatePatch.DoBlockChat = false;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {

            if (Options.GuesserMode.GetBool())
            {
                if (PlayerControl.LocalPlayer.IsAlive() && (PlayerControl.LocalPlayer.Is(Custom_Team.Impostor) || PlayerControl.LocalPlayer.GetCustomRole().IsMadmate()) && Options.ImpostorsCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                else if (PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.EvilGuesser && !Options.ImpostorsCanGuess.GetBool())
                    CreateGuesserButton(__instance);

                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole().IsCrewmate() && Options.CrewmatesCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                else if (PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.NiceGuesser && !Options.CrewmatesCanGuess.GetBool())
                    CreateGuesserButton(__instance);

                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole().IsNK() && Options.NeutralKillersCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole().IsNA() && Options.NeutralApocalypseCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole().IsNonNK() && Options.PassiveNeutralsCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole().IsCoven() && Options.CovenCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                else if (PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.Doomsayer && !Options.PassiveNeutralsCanGuess.GetBool() && !Doomsayer.CheckCantGuess)
                    CreateGuesserButton(__instance);
            }
            else
            {
                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.Is(CustomRoles.EvilGuesser))
                    CreateGuesserButton(__instance);

                /*    if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.Is(CustomRoles.Ritualist))
                        CreateGuesserButton(__instance); */

                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.Is(CustomRoles.NiceGuesser))
                    CreateGuesserButton(__instance);

                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.Is(CustomRoles.Doomsayer) && !Doomsayer.CheckCantGuess)
                    CreateGuesserButton(__instance);

                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.Is(CustomRoles.Guesser))
                    CreateGuesserButton(__instance);
            }
        }
    }
    public static void CreateGuesserButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates.ToArray())
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !pc.IsAlive()) continue;
            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.Get("TargetIcon");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => GuesserOnClick(pva.TargetPlayerId, __instance)));
        }
    }

    public const int MaxOneScreenRole = 40;
    public static int Page;
    public static PassiveButton ExitButton;
    public static GameObject guesserUI;
    private static Dictionary<Custom_Team, List<Transform>> RoleButtons;
    private static Dictionary<Custom_Team, SpriteRenderer> RoleSelectButtons;
    private static List<SpriteRenderer> PageButtons;
    public static Custom_Team currentTeamType;
    static void GuesserSelectRole(Custom_Team Role, bool SetPage = true)
    {
        currentTeamType = Role;
        if (SetPage) Page = 1;
        foreach (var RoleButton in RoleButtons)
        {
            int index = 0;
            foreach (var RoleBtn in RoleButton.Value.ToArray())
            {
                if (RoleBtn == null) continue;
                index++;
                if (index <= (Page - 1) * 40) { RoleBtn.gameObject.SetActive(false); continue; }
                if ((Page * 40) < index) { RoleBtn.gameObject.SetActive(false); continue; }
                RoleBtn.gameObject.SetActive(RoleButton.Key == Role);
            }
        }
        foreach (var RoleButton in RoleSelectButtons)
        {
            if (RoleButton.Value == null) continue;
            RoleButton.Value.color = new(0, 0, 0, RoleButton.Key == Role ? 1 : 0.25f);
        }
    }

    public static TextMeshPro textTemplate;
    static void GuesserOnClick(byte playerId, MeetingHud __instance)
    {
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || guesserUI != null || !GameStates.IsVoting) return;

        try
        {
            Page = 1;
            RoleButtons = [];
            RoleSelectButtons = [];
            PageButtons = [];
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));

            Transform container = UnityEngine.Object.Instantiate(GameObject.Find("PhoneUI").transform, __instance.transform);
            container.transform.localPosition = new Vector3(0, 0, -200f);
            guesserUI = container.gameObject;

            List<int> info = [0, 0, 0, 0, 0];
            var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
            var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
            var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
            textTemplate.enabled = true;
            if (textTemplate.transform.FindChild("RoleTextMeeting") != null) UnityEngine.Object.Destroy(textTemplate.transform.FindChild("RoleTextMeeting").gameObject);
            if (textTemplate.transform.FindChild("DeathReasonTextMeeting") != null) UnityEngine.Object.Destroy(textTemplate.transform.FindChild("DeathReasonTextMeeting").gameObject);

            Transform exitButtonParent = new GameObject().transform;
            exitButtonParent.SetParent(container);
            Transform exitButton = UnityEngine.Object.Instantiate(buttonTemplate, exitButtonParent);
            exitButton.FindChild("ControllerHighlight").gameObject.SetActive(false);
            Transform exitButtonMask = UnityEngine.Object.Instantiate(maskTemplate, exitButtonParent);
            exitButtonMask.transform.localScale = new Vector3(2.88f, 0.8f, 1f);
            exitButtonMask.transform.localPosition = new Vector3(0f, 0f, 1f);
            exitButton.gameObject.GetComponent<SpriteRenderer>().sprite = smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
            exitButtonParent.transform.localPosition = new Vector3(3.88f, 2.12f, -200f);
            exitButtonParent.transform.localScale = new Vector3(0.22f, 0.9f, 1f);
            exitButtonParent.transform.SetAsFirstSibling();
            exitButton.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
            exitButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                __instance.playerStates.ToList().ForEach(x =>
                {
                    x.gameObject.SetActive(true);
                    x.Buttons.transform.gameObject.SetActive(false);
                });

                UnityEngine.Object.Destroy(container.gameObject);
            }));
            ExitButton = exitButton.GetComponent<PassiveButton>();

            List<Transform> buttons = [];
            Transform selectedButton = null;

            /* 
                TabId:
                Crewmate   = 0
                Impostor   = 1
                Neutral    = 2
                Coven      = 3
                Add-ons    = 4
            */

            int tabCount = 0;
            for (int TabId = 0; TabId < 5; TabId++)
            {
                if (PlayerControl.LocalPlayer.Is(CustomRoles.EvilGuesser))
                {
                    if (EvilGuesser.HideTabInGuesserUI(TabId)) continue;
                }
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.NiceGuesser))
                {
                    if (NiceGuesser.HideTabInGuesserUI(TabId)) continue;
                }
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.Doomsayer))
                {
                    if (Doomsayer.HideTabInGuesserUI(TabId)) continue;
                }
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.Guesser))
                {
                    //if (!Options.GCanGuessCrew.GetBool() && TabId == 0) continue;
                    //if (!Options.GCanGuessImp.GetBool() && TabId == 1) continue;
                    if (!Guesser.GCanGuessAdt.GetBool() && TabId == 4) continue;
                }
                else if (Options.GuesserMode.GetBool() &&
                    !(PlayerControl.LocalPlayer.Is(CustomRoles.EvilGuesser) ||
                      PlayerControl.LocalPlayer.Is(CustomRoles.NiceGuesser) ||
                      PlayerControl.LocalPlayer.Is(CustomRoles.Doomsayer) ||
                      PlayerControl.LocalPlayer.Is(CustomRoles.Guesser)))
                {
                    if (!Options.CrewCanGuessCrew.GetBool() && PlayerControl.LocalPlayer.Is(Custom_Team.Crewmate) && TabId == 0) continue;
                    if (!Options.ImpCanGuessImp.GetBool() && PlayerControl.LocalPlayer.Is(Custom_Team.Impostor) && TabId == 1) continue;
                    if (!Options.CovenCanGuessCoven.GetBool() && PlayerControl.LocalPlayer.Is(Custom_Team.Coven) && TabId == 3) continue;
                    if (!Options.CanGuessAddons.GetBool() && TabId == 4) continue;
                }

                Transform TeambuttonParent = new GameObject().transform;
                TeambuttonParent.SetParent(container);
                Transform Teambutton = UnityEngine.Object.Instantiate(buttonTemplate, TeambuttonParent);
                Teambutton.FindChild("ControllerHighlight").gameObject.SetActive(false);
                Transform TeambuttonMask = UnityEngine.Object.Instantiate(maskTemplate, TeambuttonParent);
                TextMeshPro Teamlabel = UnityEngine.Object.Instantiate(textTemplate, Teambutton);
                Teambutton.GetComponent<SpriteRenderer>().sprite = CustomButton.Get("GuessPlateKPD");
                RoleSelectButtons.Add((Custom_Team)TabId, Teambutton.GetComponent<SpriteRenderer>());
                TeambuttonParent.localPosition = new(-3.10f + (tabCount++ * 1.47f), 2.225f, -200);
                TeambuttonParent.localScale = new(0.53f, 0.53f, 1f);
                Teamlabel.color = (Custom_Team)TabId switch
                {
                    Custom_Team.Crewmate => new Color32(140, 255, 255, byte.MaxValue),
                    Custom_Team.Impostor => new Color32(255, 25, 25, byte.MaxValue),
                    Custom_Team.Neutral => new Color32(127, 140, 141, byte.MaxValue),
                    Custom_Team.Coven => new Color32(172, 66, 242, byte.MaxValue),
                    Custom_Team.Addon => new Color32(255, 154, 206, byte.MaxValue),
                    _ => throw new NotImplementedException(),
                };
                //Logger.Info(Teamlabel.color.ToString(), ((Custom_Team)TabId).ToString());
                Teamlabel.text = GetString("Type" + ((Custom_Team)TabId).ToString());
                Teamlabel.alignment = TextAlignmentOptions.Center;
                Teamlabel.transform.localPosition = new Vector3(0, 0, Teamlabel.transform.localPosition.z);
                Teamlabel.transform.localScale *= 1.6f;
                Teamlabel.autoSizeTextContainer = true;

                static void CreateTeamButton(Transform Teambutton, Custom_Team type)
                {
                    Teambutton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        GuesserSelectRole(type);
                        ReloadPage();
                    }));
                }
                if (PlayerControl.LocalPlayer.IsAlive()) CreateTeamButton(Teambutton, (Custom_Team)TabId);
            }
            static void ReloadPage()
            {
                PageButtons[0].color = new(1, 1, 1, 1f);
                PageButtons[1].color = new(1, 1, 1, 1f);
                if ((RoleButtons[currentTeamType].Count / MaxOneScreenRole + (RoleButtons[currentTeamType].Count % MaxOneScreenRole != 0 ? 1 : 0)) < Page)
                {
                    Page -= 1;
                    PageButtons[1].color = new(1, 1, 1, 0.1f);
                }
                else if ((RoleButtons[currentTeamType].Count / MaxOneScreenRole + (RoleButtons[currentTeamType].Count % MaxOneScreenRole != 0 ? 1 : 0)) < Page + 1)
                {
                    PageButtons[1].color = new(1, 1, 1, 0.1f);
                }
                if (Page <= 1)
                {
                    Page = 1;
                    PageButtons[0].color = new(1, 1, 1, 0.1f);
                }
                GuesserSelectRole(currentTeamType, false);
            }
            static void CreatePage(bool IsNext, MeetingHud __instance, Transform container)
            {
                var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
                var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
                var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
                Transform PagebuttonParent = new GameObject().transform;
                PagebuttonParent.SetParent(container);
                Transform Pagebutton = UnityEngine.Object.Instantiate(buttonTemplate, PagebuttonParent);
                Pagebutton.FindChild("ControllerHighlight").gameObject.SetActive(false);
                Transform PagebuttonMask = UnityEngine.Object.Instantiate(maskTemplate, PagebuttonParent);
                TextMeshPro Pagelabel = UnityEngine.Object.Instantiate(textTemplate, Pagebutton);
                Pagebutton.GetComponent<SpriteRenderer>().sprite = CustomButton.Get("GuessPlateKPD");
                PagebuttonParent.localPosition = IsNext ? new(3.535f, -2.2f, -200) : new(-3.475f, -2.2f, -200);
                PagebuttonParent.localScale = new(0.55f, 0.55f, 1f);
                Pagelabel.color = Color.white;
                Pagelabel.text = GetString(IsNext ? "NextPage" : "PreviousPage");
                Pagelabel.alignment = TextAlignmentOptions.Center;
                Pagelabel.transform.localPosition = new Vector3(0, 0, Pagelabel.transform.localPosition.z);
                Pagelabel.transform.localScale *= 1.6f;
                Pagelabel.autoSizeTextContainer = true;
                if (!IsNext && Page <= 1) Pagebutton.GetComponent<SpriteRenderer>().color = new(1, 1, 1, 0.1f);
                Pagebutton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() => ClickEvent()));
                void ClickEvent()
                {
                    if (IsNext) Page += 1;
                    else Page -= 1;
                    if (Page < 1) Page = 1;
                    ReloadPage();
                }
                PageButtons.Add(Pagebutton.GetComponent<SpriteRenderer>());
            }
            if (PlayerControl.LocalPlayer.IsAlive())
            {
                CreatePage(false, __instance, container);
                CreatePage(true, __instance, container);
            }
            int ind = 0;

            CustomRoles[] arrayOfRoles = [];

            if (Options.ShowOnlyEnabledRolesInGuesserUI.GetBool())
            {

                List<CustomRoles> listOfRoles = CustomRolesHelper.AllRoles.Where(role => !role.IsGhostRole() && (role.IsEnable() || role.RoleExist(countDead: true))).ToList();

                // Always show
                if (!listOfRoles.Contains(CustomRoles.ImpostorTOHE))
                    listOfRoles.Add(CustomRoles.ImpostorTOHE);

                if (!listOfRoles.Contains(CustomRoles.ShapeshifterTOHE))
                    listOfRoles.Add(CustomRoles.ShapeshifterTOHE);

                if (!listOfRoles.Contains(CustomRoles.CrewmateTOHE))
                    listOfRoles.Add(CustomRoles.CrewmateTOHE);

                if (!listOfRoles.Contains(CustomRoles.ScientistTOHE))
                    listOfRoles.Add(CustomRoles.ScientistTOHE);

                if (!listOfRoles.Contains(CustomRoles.EngineerTOHE))
                    listOfRoles.Add(CustomRoles.EngineerTOHE);

                if (!listOfRoles.Contains(CustomRoles.Amnesiac))
                    listOfRoles.Add(CustomRoles.Amnesiac);

                if (CustomRoles.Jackal.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.Recruit))
                        listOfRoles.Add(CustomRoles.Recruit);
                }

                if (CustomRoles.Cultist.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.Charmed))
                        listOfRoles.Add(CustomRoles.Charmed);
                }

                if (CustomRoles.Infectious.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.Infected))
                        listOfRoles.Add(CustomRoles.Infected);
                }

                if (CustomRoles.Virus.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.Contagious))
                        listOfRoles.Add(CustomRoles.Contagious);
                }

                if (CustomRoles.Admirer.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.Admired))
                        listOfRoles.Add(CustomRoles.Admired);
                }

                if (CustomRoles.PlagueBearer.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.Pestilence))
                        listOfRoles.Add(CustomRoles.Pestilence);
                }

                if (CustomRoles.SoulCollector.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.Death))
                        listOfRoles.Add(CustomRoles.Death);
                }

                if (CustomRoles.Baker.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.Famine))
                        listOfRoles.Add(CustomRoles.Famine);
                }

                if (CustomRoles.Berserker.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.War))
                        listOfRoles.Add(CustomRoles.War);
                }

                if (CustomRoles.Ritualist.IsEnable())
                {
                    if (!listOfRoles.Contains(CustomRoles.Enchanted))
                        listOfRoles.Add(CustomRoles.Enchanted);
                }

                arrayOfRoles = [.. listOfRoles];
            }
            else
            {
                arrayOfRoles = [.. CustomRolesHelper.AllRoles.Where(role => !role.IsGhostRole())];
            }

            var roleMap = arrayOfRoles.ToDictionary(role => role, role => Utils.GetRoleName(role));

            var orderedRoleList = roleMap.OrderBy(kv => kv.Value).Select(kv => kv.Key).ToArray();


            foreach (var role in orderedRoleList)
            {
                if (role.IsVanilla()) continue;

                if (role is CustomRoles.GM
                    or CustomRoles.SpeedBooster
                    or CustomRoles.Oblivious
                    or CustomRoles.Flash
                    or CustomRoles.NotAssigned
                    or CustomRoles.SuperStar
                    or CustomRoles.Oblivious
                    or CustomRoles.Solsticer
                    or CustomRoles.Killer
                    or CustomRoles.Mini
                    or CustomRoles.Onbound
                    or CustomRoles.Rebound
                    or CustomRoles.LastImpostor
                    or CustomRoles.Mare
                    or CustomRoles.Cyber
                    or CustomRoles.Sloth
                    or CustomRoles.Apocalypse
                    or CustomRoles.Coven
                    || (role.IsTNA() && !Options.TransformedNeutralApocalypseCanBeGuessed.GetBool())) continue;

                if (role is CustomRoles.NiceMini && Mini.Age < 18) continue;
                if (role is CustomRoles.EvilMini && Mini.Age < 18 && !Mini.CanGuessEvil.GetBool()) continue;

                CreateRole(role);
            }
            void CreateRole(CustomRoles role)
            {
                if (40 <= info[(int)role.GetCustomRoleTeam()]) info[(int)role.GetCustomRoleTeam()] = 0;
                Transform buttonParent = new GameObject().transform;
                buttonParent.SetParent(container);
                Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
                button.FindChild("ControllerHighlight").gameObject.SetActive(false);
                Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
                TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);
                button.GetComponent<SpriteRenderer>().sprite = CustomButton.Get("GuessPlate");
                if (!RoleButtons.ContainsKey(role.GetCustomRoleTeam()))
                {
                    RoleButtons.Add(role.GetCustomRoleTeam(), []);
                }
                RoleButtons[role.GetCustomRoleTeam()].Add(button);
                buttons.Add(button);
                int row = info[(int)role.GetCustomRoleTeam()] / 5;
                int col = info[(int)role.GetCustomRoleTeam()] % 5;
                buttonParent.localPosition = new Vector3(-3.47f + 1.75f * col, 1.5f - 0.45f * row, -200f);
                buttonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
                label.text = GetString(role.ToString());
                label.color = Utils.GetRoleColor(role);
                label.alignment = TextAlignmentOptions.Center;
                label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
                label.transform.localScale *= 1.6f;
                label.autoSizeTextContainer = true;
                //int copiedIndex = info[(int)role.GetCustomRoleTeam()];

                button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
                if (PlayerControl.LocalPlayer.IsAlive()) button.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    if (selectedButton != button)
                    {
                        selectedButton = button;
                        buttons.ForEach(x => x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Utils.GetRoleColor(PlayerControl.LocalPlayer.GetCustomRole()) : Color.white);
                    }
                    else
                    {
                        if (!(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted) || !PlayerControl.LocalPlayer.IsAlive()) return;

                        Logger.Msg($"Click: {pc.GetNameWithRole().RemoveHtmlTags()} => {role}", "Guesser UI");

                        if (AmongUsClient.Instance.AmHost) GuesserMsg(PlayerControl.LocalPlayer, $"/bt {playerId} {GetString(role.ToString())}", true);
                        else SendRPC(playerId, role);

                        // Reset the GUI
                        __instance.playerStates.ToList().ForEach(x =>
                        {
                            x.gameObject.SetActive(true);
                            x.Buttons.transform.gameObject.SetActive(false);
                        });
                        UnityEngine.Object.Destroy(container.gameObject);
                        textTemplate.enabled = false;

                    }
                }));
                info[(int)role.GetCustomRoleTeam()]++;
                ind++;
            }
            container.transform.localScale *= 0.75f;
            GuesserSelectRole(Custom_Team.Crewmate);
            ReloadPage();
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Guesser UI");
            return;
        }

        PlayerControl.LocalPlayer.RPCPlayCustomSound("Gunload");

    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class MeetingHudOnDestroyGuesserUIClose
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (__instance == null || textTemplate == null)
            {
                return;
            }
            UnityEngine.Object.Destroy(textTemplate.gameObject);
        }
    }

    // Modded non-host client guess role/add-on
    private static void SendRPC(int playerId, CustomRoles role)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (int)CustomRPC.Guess, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((int)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        Logger.Msg($"{pc}", "PlayerControl pc");

        int PlayerId = reader.ReadInt32();
        Logger.Msg($"{PlayerId}", "Player Id");

        CustomRoles role = (CustomRoles)reader.ReadInt32();
        Logger.Msg($"{role}", "Role Int32");
        Logger.Msg($"{GetString(role.ToString())}", "Role String");

        GuesserMsg(pc, $"/bt {PlayerId} {GetString(role.ToString())}", true);
    }
}
