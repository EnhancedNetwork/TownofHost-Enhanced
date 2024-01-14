using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
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
        foreach(string comm in comList)
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
            if (!isUI) Utils.SendMessage(GetString("GuessDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessDead"));
            return true;
        }
        if (!pc.Is(CustomRoles.NiceGuesser))
        {
            if (pc.GetCustomRole().IsCrewmate() && !Options.CrewmatesCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.Judge))
            {
                if (!isUI) Utils.SendMessage(GetString("GuessNotAllowed"), pc.PlayerId);
                else pc.ShowPopUp(GetString("GuessNotAllowed"));
                return true;
            }
        }
        if (!pc.Is(CustomRoles.EvilGuesser))
        {
            if (pc.GetCustomRole().IsImpostor() && !Options.ImpostorsCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.Councillor))
            {
                if (!isUI) Utils.SendMessage(GetString("GuessNotAllowed"), pc.PlayerId);
                else pc.ShowPopUp(GetString("GuessNotAllowed"));
                return true;
            }
        }

        if (pc.GetCustomRole().IsNK() && !Options.NeutralKillersCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser))
        {
            if (!isUI) Utils.SendMessage(GetString("GuessNotAllowed"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessNotAllowed"));
            return true;
        }
        if (pc.GetCustomRole().IsNonNK() && !Options.PassiveNeutralsCanGuess.GetBool() && !pc.Is(CustomRoles.Guesser) && !pc.Is(CustomRoles.Doomsayer))
        {
            if (!isUI) Utils.SendMessage(GetString("GuessNotAllowed"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessNotAllowed"));
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
            (pc.Is(CustomRoles.NiceGuesser) && Options.GGTryHideMsg.GetBool()) ||
            (pc.Is(CustomRoles.EvilGuesser) && Options.EGTryHideMsg.GetBool()) ||
            (pc.Is(CustomRoles.Doomsayer) && Doomsayer.DoomsayerTryHideMsg.GetBool()) ||
            (pc.Is(CustomRoles.Guesser) && Options.GTryHideMsg.GetBool()) ||
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
                if (!isUI) Utils.SendMessage(error, pc.PlayerId);
                else pc.ShowPopUp(error);
                return true;
            }
            var target = Utils.GetPlayerById(targetId);

            Logger.Msg($" {pc.PlayerId}", "Guesser - pc.PlayerId");
            Logger.Msg($" {target.PlayerId}", "Guesser - target.PlayerId");
            Logger.Msg($" {role}", "Guesser - role");

            if (target != null)
            {
                GuessMaster.OnGuess(role);
                bool guesserSuicide = false;
                if (CopyCat.playerIdList.Contains(pc.PlayerId))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessDisabled"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessDisabled"));
                    return true;
                }

                if (!Mundane.OnGuess(pc))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessedAsMundane"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessedAsMundane"));
                    return true;
                }

                if (!Main.GuesserGuessed.ContainsKey(pc.PlayerId)) Main.GuesserGuessed.Add(pc.PlayerId, 0);
                if (pc.Is(CustomRoles.NiceGuesser) && Main.GuesserGuessed[pc.PlayerId] >= Options.GGCanGuessTime.GetInt())
                {
                    if (!isUI) Utils.SendMessage(GetString("GGGuessMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GGGuessMax"));
                    return true;
                }
                if (pc.Is(CustomRoles.EvilGuesser) && Main.GuesserGuessed[pc.PlayerId] >= Options.EGCanGuessTime.GetInt())
                {
                    if (!isUI) Utils.SendMessage(GetString("EGGuessMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("EGGuessMax"));
                    return true;
                }
                if (pc.Is(CustomRoles.Solsticer) && (!Solsticer.CanGuess || !Solsticer.SolsticerCanGuess.GetBool()))
                {
                    if (!isUI) Utils.SendMessage(GetString("SolsticerGuessMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("SolsticerGuessMax"));
                    return true;
                }
                if (pc.Is(CustomRoles.Phantom) && !Options.PhantomCanGuess.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessDisabled"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessDisabled"));
                    return true;
                }
                if (target.Is(CustomRoles.Workaholic) && Options.WorkaholicVisibleToEveryone.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessWorkaholic"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessWorkaholic"));
                    return true;
                }
                if (target.Is(CustomRoles.Doctor) && Options.DoctorVisibleToEveryone.GetBool() && !target.IsEvilAddons())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessDoctor"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessDoctor"));
                    return true;
                }
                if (Medic.ProtectList.Contains(target.PlayerId) && !Medic.GuesserIgnoreShield.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessShielded"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessShielded"));
                    return true;
                }
                if (role == CustomRoles.Monarch && target.Is(CustomRoles.Monarch) && CustomRoles.Knighted.RoleExist())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessMonarch"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessMonarch"));
                    return true;
                }
                if (role == CustomRoles.Knighted && pc.Is(CustomRoles.Monarch))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessKnighted"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessKnighted"));
                    return true;
                }
                if (pc.Is(CustomRoles.Masochist))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessMasochistBlocked"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessMasochistBlocked"));
                    return true;
                }
                if (Options.MayorRevealWhenDoneTasks.GetBool())
                {
                    if (target.Is(CustomRoles.Mayor) && target.GetPlayerTaskState().IsTaskFinished)
                    {
                        if (!isUI) Utils.SendMessage(GetString("GuessMayor"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("GuessMayor"));
                        return true;
                    }
                }
                if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessMini"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessMini"));
                    return true;
                }
                if (pc.Is(CustomRoles.Terrorist) && !Options.TerroristCanGuess.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessDisabled"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessDisabled"));
                    return true;
                }
                if (pc.Is(CustomRoles.Workaholic) && !Options.WorkaholicCanGuess.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessDisabled"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessDisabled"));
                    return true;
                }
                if (pc.Is(CustomRoles.God) && !Options.GodCanGuess.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessDisabled"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessDisabled"));
                    return true;
                }
                if (pc.Is(CustomRoles.Solsticer) && !Solsticer.SolsticerCanGuess.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessDisabled"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessDisabled"));
                    return true;
                }
                if (role == CustomRoles.SuperStar || target.Is(CustomRoles.SuperStar))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessSuperStar"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessSuperStar"));
                    return true;
                }
                if ((role == CustomRoles.President || target.Is(CustomRoles.President)) && President.CheckPresidentReveal[target.PlayerId] == true && !President.PresidentCanBeGuessedAfterRevealing.GetBool())
                {
                    Utils.SendMessage(GetString("GuessPresident"), pc.PlayerId);
                    return true;
                }
                if (role == CustomRoles.Bait && target.Is(CustomRoles.Bait) && Options.BaitNotification.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessNotifiedBait"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessNotifiedBait"));
                    return true;
                }
                if (role == CustomRoles.LastImpostor || role == CustomRoles.Mare || role == CustomRoles.Cyber || role == CustomRoles.Flash)//(role == CustomRoles.Glow || role == CustomRoles.LastImpostor || role == CustomRoles.Mare || role == CustomRoles.Cyber)
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessObviousAddon"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessObviousAddon"));
                    return true;
                }
                if (role == CustomRoles.Solsticer && target.Is(CustomRoles.Solsticer))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessSolsticer"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessSolsticer"));
                    return true;
                }
                if (target.Is(CustomRoles.Onbound))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessOnbound"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessOnbound"));
                    return true;
                }
                if (Jailer.JailerTarget.ContainsValue(target.PlayerId))
                {
                    if (!isUI) Utils.SendMessage(GetString("CantGuessJailed"), pc.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")));
                    else pc.ShowPopUp(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")) + "\n" + GetString("CantGuessJailed"));
                    return true;
                }
                if (Jailer.JailerTarget.ContainsValue(pc.PlayerId) && role != CustomRoles.Jailer)
                {
                    if (!isUI) Utils.SendMessage(GetString("JailedCanOnlyGuessJailer"), pc.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")));
                    else pc.ShowPopUp(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jailer), GetString("JailerTitle")) + "\n" + GetString("JailedCanOnlyGuessJailer"));
                    return true;
                }
                if (target.Is(CustomRoles.Pestilence))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessPestilence"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessPestilence"));
                    guesserSuicide = true;
                    Logger.Msg($" {guesserSuicide}", "guesserSuicide - Is Active 1");
                }
                if (role == CustomRoles.Phantom && target.Is(CustomRoles.Phantom))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessPhantom"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessPhantom"));
                    return true;
                }
                if (target.Is(CustomRoles.Masochist))
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessMasochist"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessMasochist"));
                    Main.MasochistKillMax[target.PlayerId]++;

                    if (Main.MasochistKillMax[target.PlayerId] >= Options.MasochistKillMax.GetInt())
                    {
                        if (!CustomWinnerHolder.CheckForConvertedWinner(target.PlayerId))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Masochist);
                            CustomWinnerHolder.WinnerIds.Add(target.PlayerId);
                        }
                    }
                    return true;
                }
                if (pc.Is(CustomRoles.Masochist) && target.PlayerId == pc.PlayerId)
                {
                    if (!isUI) Utils.SendMessage(GetString("SelfGuessMasochist"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("SelfGuessMasochist"));
                    guesserSuicide = true;
                    Logger.Msg($" {guesserSuicide}", "guesserSuicide - Is Active 2");
                }

                if (role == CustomRoles.GM || target.Is(CustomRoles.GM))
                {
                    Utils.SendMessage(GetString("GuessGM"), pc.PlayerId);
                    return true;
                }
                /*   if (role == CustomRoles.Marshall || target.Is(CustomRoles.Marshall))
                   {
                       Utils.SendMessage(GetString("GuessMarshall"), pc.PlayerId);
                       return true;
                   } */
                if (target.Is(CustomRoles.Snitch) && target.GetPlayerTaskState().IsTaskFinished)
                {
                    if (!isUI) Utils.SendMessage(GetString("EGGuessSnitchTaskDone"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("EGGuessSnitchTaskDone"));
                    return true;
                }
                if (target.Is(CustomRoles.Marshall) && target.GetPlayerTaskState().IsTaskFinished)
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessMarshallTask"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessMarshallTask"));
                    return true;
                }
                if (role == CustomRoles.Guardian && target.Is(CustomRoles.Guardian) && target.GetPlayerTaskState().IsTaskFinished)
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessGuardianTask"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessGuardianTask"));
                    return true;
                }
                if (pc.Is(CustomRoles.Doomsayer))
                {
                    if (Doomsayer.CantGuess)
                    {
                        if (!isUI) Utils.SendMessage(GetString("DoomsayerCantGuess"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("DoomsayerCantGuess"));
                        return true;
                    }

                    if (role.IsImpostor() && !Doomsayer.DCanGuessImpostors.GetBool())
                    {
                        if (!isUI) Utils.SendMessage(GetString("GuessNotAllowed"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("GuessNotAllowed"));
                        return true;
                    }
                    if (role.IsCrewmate() && !Doomsayer.DCanGuessCrewmates.GetBool())
                    {
                        if (!isUI) Utils.SendMessage(GetString("GuessNotAllowed"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("GuessNotAllowed"));
                        return true;
                    }
                    if (role.IsNeutral() && !Doomsayer.DCanGuessNeutrals.GetBool())
                    {
                        if (!isUI) Utils.SendMessage(GetString("GuessNotAllowed"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("GuessNotAllowed"));
                        return true;
                    }
                    if (role.IsAdditionRole() && !Doomsayer.DCanGuessAdt.GetBool())
                    {
                        if (!isUI) Utils.SendMessage(GetString("GuessAdtRole"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("GuessAdtRole"));
                        return true;
                    }
                }
                // Assassin Cant Guess Addons
                if (role.IsAdditionRole() && pc.Is(CustomRoles.EvilGuesser) && !Options.EGCanGuessAdt.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessAdtRole"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessAdtRole"));
                    return true;
                }
                // Nice Guesser Cant Guess Addons
                else if (role.IsAdditionRole() && pc.Is(CustomRoles.NiceGuesser) && !Options.GGCanGuessAdt.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessAdtRole"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessAdtRole"));
                    return true;
                }
                // Guesser (add-on) Cant Guess Addons
                else if (role.IsAdditionRole() && pc.Is(CustomRoles.Guesser) && !Options.GCanGuessAdt.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("GuessAdtRole"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GuessAdtRole"));
                    return true;
                }

                // Guesser Mode Can/Cant Guess Addons
                if (Options.GuesserMode.GetBool())
                {
                    if (role.IsAdditionRole() && !Options.CanGuessAddons.GetBool())
                    {
                        // Impostors Cant Guess Addons
                        if (Options.ImpostorsCanGuess.GetBool() && pc.Is(CustomRoleTypes.Impostor) && !(pc.Is(CustomRoles.EvilGuesser) || pc.Is(CustomRoles.Guesser)))
                        {
                            if (!isUI) Utils.SendMessage(GetString("GuessAdtRole"), pc.PlayerId);
                            else pc.ShowPopUp(GetString("GuessAdtRole"));
                            return true;
                        }

                        // Crewmates Cant Guess Addons
                        if (Options.CrewmatesCanGuess.GetBool() && pc.Is(CustomRoleTypes.Crewmate) && !(pc.Is(CustomRoles.NiceGuesser) || pc.Is(CustomRoles.Guesser)))
                        {
                            if (!isUI) Utils.SendMessage(GetString("GuessAdtRole"), pc.PlayerId);
                            else pc.ShowPopUp(GetString("GuessAdtRole"));
                            return true;
                        }

                        // Neutrals Cant Guess Addons
                        if ((Options.NeutralKillersCanGuess.GetBool() || Options.PassiveNeutralsCanGuess.GetBool()) && pc.Is(CustomRoleTypes.Neutral) && !(pc.Is(CustomRoles.Doomsayer) || pc.Is(CustomRoles.Guesser)))
                        {
                            if (!isUI) Utils.SendMessage(GetString("GuessAdtRole"), pc.PlayerId);
                            else pc.ShowPopUp(GetString("GuessAdtRole"));
                            return true;
                        }
                    }
                }

                /* if ((pc.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor) && !Options.ImpCanGuessImp.GetBool()) && Options.GuesserMode.GetBool())
                   {
                       if (!isUI) Utils.SendMessage(GetString("GuessImpRole"), pc.PlayerId);
                       else pc.ShowPopUp(GetString("GuessImpRole"));
                       return true;
                   }
                   if ((role == CustomRoles.Phantom && pc.Is(CustomRoleTypes.Crewmate) && target.Is(CustomRoleTypes.Crewmate) && !Options.CrewCanGuessCrew.GetBool()) && Options.GuesserMode.GetBool())
                   {
                       if (!isUI) Utils.SendMessage(GetString("GuessCrewRole"), pc.PlayerId);
                       else pc.ShowPopUp(GetString("GuessCrewRole"));
                       return true;
                   } */

                if (target.Is(CustomRoles.Merchant) && Merchant.IsBribedKiller(pc, target))
                {
                    if (!isUI) Utils.SendMessage(GetString("BribedByMerchant2"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("BribedByMerchant2"));
                    return true;
                }

                if (pc.PlayerId == target.PlayerId)
                {
                    if (pc.Is(CustomRoles.DoubleShot) && !DoubleShot.IsActive.Contains(pc.PlayerId))
                    {
                        DoubleShot.IsActive.Add(pc.PlayerId);

                        Logger.Msg($"{pc.PlayerId}", "GuesserDoubleShotIsActive-1");

                        if (!isUI) Utils.SendMessage(GetString("GuessDoubleShot"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("GuessDoubleShot"));
                        return true;
                    }
                    else
                    {
                        if (pc.Is(CustomRoles.DoubleShot) && DoubleShot.IsActive.Contains(pc.PlayerId))
                            DoubleShot.IsActive.Remove(pc.PlayerId);

                        if (!isUI) Utils.SendMessage(GetString("LaughToWhoGuessSelf"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
                        else pc.ShowPopUp(Utils.ColorString(Color.cyan, GetString("MessageFromKPD")) + "\n" + GetString("LaughToWhoGuessSelf"));
                        guesserSuicide = true;
                        Logger.Msg($" {guesserSuicide}", "guesserSuicide - self guess");
                    }
                }
                else if (pc.Is(CustomRoles.NiceGuesser) && target.Is(CustomRoleTypes.Crewmate) && !Options.GGCanGuessCrew.GetBool() && !pc.Is(CustomRoles.Madmate))
                {
                    if (pc.Is(CustomRoles.DoubleShot) && !DoubleShot.IsActive.Contains(pc.PlayerId))
                    {
                        DoubleShot.IsActive.Add(pc.PlayerId);

                        Logger.Msg($"{pc.PlayerId}", "GuesserDoubleShotIsActive-2");

                        if (!isUI) Utils.SendMessage(GetString("GuessDoubleShot"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("GuessDoubleShot"));
                        return true;
                    }
                    else
                    {
                        if (pc.Is(CustomRoles.DoubleShot) && DoubleShot.IsActive.Contains(pc.PlayerId))
                            DoubleShot.IsActive.Remove(pc.PlayerId);

                        guesserSuicide = true;
                        Logger.Msg($" {guesserSuicide}", "guesserSuicide - 1");
                    }
                }
                else if (pc.Is(CustomRoles.EvilGuesser) && target.Is(CustomRoleTypes.Impostor) && !Options.EGCanGuessImp.GetBool())
                {
                    if (pc.Is(CustomRoles.DoubleShot) && !DoubleShot.IsActive.Contains(pc.PlayerId))
                    {
                        DoubleShot.IsActive.Add(pc.PlayerId);

                        Logger.Msg($"{pc.PlayerId}", "GuesserDoubleShotIsActive-3");

                        if (!isUI) Utils.SendMessage(GetString("GuessDoubleShot"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("GuessDoubleShot"));
                        return true;
                    }
                    else
                    {
                        if (pc.Is(CustomRoles.DoubleShot) && DoubleShot.IsActive.Contains(pc.PlayerId))
                            DoubleShot.IsActive.Remove(pc.PlayerId);

                        guesserSuicide = true;
                        Logger.Msg($" {guesserSuicide}", "guesserSuicide - 2");
                    }
                }
                //  else if (pc.Is(CustomRoles.Guesser)/* && role.IsImpostor() && !Options.GCanGuessImp.GetBool()*/) guesserSuicide = true;
                //   else if (pc.Is(CustomRoles.Guesser)/* && role.IsCrewmate() && !pc.Is(CustomRoles.Madmate) && !Options.GCanGuessCrew.GetBool() */) guesserSuicide = true;
                else if (!target.Is(role))
                {
                    if (pc.Is(CustomRoles.DoubleShot) && !DoubleShot.IsActive.Contains(pc.PlayerId))
                    {
                        DoubleShot.IsActive.Add(pc.PlayerId);

                        Logger.Msg($"{pc.PlayerId}", "GuesserDoubleShotIsActive-4");

                        if (!isUI) Utils.SendMessage(GetString("GuessDoubleShot"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("GuessDoubleShot"));
                        return true;
                    }
                    else
                    {
                        if (pc.Is(CustomRoles.DoubleShot) && DoubleShot.IsActive.Contains(pc.PlayerId))
                            DoubleShot.IsActive.Remove(pc.PlayerId);

                        guesserSuicide = true;
                        Logger.Msg($" {guesserSuicide}", "guesserSuicide - 3");
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

                Main.GuesserGuessed[pc.PlayerId]++;
                if (target.Is(CustomRoles.Rebound) && pc.Is(CustomRoles.Doomsayer) && !Doomsayer.DoesNotSuicideWhenMisguessing.GetBool() && !Doomsayer.GuessedRoles.Contains(role))
                {
                    guesserSuicide = true;
                    Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()} guessed {target.GetNameWithRole().RemoveHtmlTags()}, doomsayer suicide because rebound", "GuessManager");
                }

                else if (pc.Is(CustomRoles.Doomsayer) && Doomsayer.AdvancedSettings.GetBool())
                {
                    if (Doomsayer.GuessesCountPerMeeting >= Doomsayer.MaxNumberOfGuessesPerMeeting.GetInt() && pc.PlayerId != dp.PlayerId)
                    {
                        if (!isUI) Utils.SendMessage(GetString("DoomsayerCantGuess"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("DoomsayerCantGuess"));
                        return true;
                    }
                    else
                    {
                        Doomsayer.GuessesCountPerMeeting++;

                        if (Doomsayer.GuessesCountPerMeeting >= Doomsayer.MaxNumberOfGuessesPerMeeting.GetInt())
                            Doomsayer.CantGuess = true;
                    }

                    if (!Doomsayer.KillCorrectlyGuessedPlayers.GetBool() && pc.PlayerId != dp.PlayerId)
                    {
                        if (!isUI) Utils.SendMessage(GetString("DoomsayerCorrectlyGuessRole"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("DoomsayerCorrectlyGuessRole"));

                        if (Doomsayer.GuessedRoles.Contains(role))
                        {
                            _ = new LateTask(() =>
                            {
                                Utils.SendMessage(GetString("DoomsayerGuessSameRoleAgainMsg"), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doomsayer), GetString("DoomsayerGuessCountTitle")));
                            }, 0.7f, "Doomsayer Guess Same Role Again Msg");
                        }
                        else
                        {
                            Doomsayer.GuessingToWin[pc.PlayerId]++;
                            Doomsayer.SendRPC(pc);
                            Doomsayer.GuessedRoles.Add(role);

                            _ = new LateTask(() =>
                            {
                                Utils.SendMessage(string.Format(GetString("DoomsayerGuessCountMsg"), Doomsayer.GuessingToWin[pc.PlayerId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doomsayer), GetString("DoomsayerGuessCountTitle")));
                            }, 0.7f, "Doomsayer Guess Msg 1");
                        }

                        Doomsayer.CheckCountGuess(pc);

                        return true;
                    }
                    else if (Doomsayer.DoesNotSuicideWhenMisguessing.GetBool() && pc.PlayerId == dp.PlayerId)
                    {
                        if (!isUI) Utils.SendMessage(GetString("DoomsayerNotCorrectlyGuessRole"), pc.PlayerId);
                        else pc.ShowPopUp(GetString("DoomsayerNotCorrectlyGuessRole"));

                        if (Doomsayer.MisguessRolePrevGuessRoleUntilNextMeeting.GetBool())
                        {
                            Doomsayer.CantGuess = true;
                        }

                        return true;
                    }
                }

                if (dp.Is(CustomRoles.Solsticer))
                {
                    Solsticer.CanGuess = false;
                    _ = new LateTask(() => { Utils.SendMessage(GetString("SolsticerMisGuessed"), dp.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Solsticer), GetString("GuessKillTitle")), true); }, 0.6f, "Solsticer MisGuess Msg");
                    return true;
                }

                string Name = dp.GetRealName();
                if (!Options.DisableKillAnimationOnGuess.GetBool()) CustomSoundsManager.RPCPlayCustomSoundAll("Gunfire");

                if (!GameStates.IsProceeding)
                {
                    _ = new LateTask(() =>
                    {
                        Main.PlayerStates[dp.PlayerId].deathReason = PlayerState.DeathReason.Gambled;
                        dp.SetRealKiller(pc);
                        RpcGuesserMurderPlayer(dp);

                        if (dp.Is(CustomRoles.Medic))
                            Medic.IsDead(dp);

                        if (pc.Is(CustomRoles.Doomsayer) && pc.PlayerId != dp.PlayerId)
                        {
                            Doomsayer.GuessingToWin[pc.PlayerId]++;
                            Doomsayer.SendRPC(pc);

                            if (!Doomsayer.GuessedRoles.Contains(role))
                                Doomsayer.GuessedRoles.Add(role);

                            Doomsayer.CheckCountGuess(pc);
                        }

                        if (dp == pc) GuessMaster.OnGuess(role, isMisguess: true, dp: dp);
                        //死者检查
                        Utils.AfterPlayerDeathTasks(dp, true);

                        Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);

                        _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("GuessKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceGuesser), GetString("GuessKillTitle")), true); }, 0.6f, "Guess Msg");

                        if (pc.Is(CustomRoles.Doomsayer) && pc.PlayerId != dp.PlayerId)
                        {
                            _ = new LateTask(() =>
                            {
                                Utils.SendMessage(string.Format(GetString("DoomsayerGuessCountMsg"), Doomsayer.GuessingToWin[pc.PlayerId]), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doomsayer), GetString("DoomsayerGuessCountTitle")));
                            }, 0.7f, "Doomsayer Guess Msg 2");
                        }

                    }, 0.2f, "Guesser Kill");
                }
            }
        }
        return true;
    }

    public static TextMeshPro NameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;
    public static void RpcGuesserMurderPlayer(this PlayerControl pc) //ゲッサー用の殺し方
    {
        // DEATH STUFF //
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
        foreach (var playerVoteArea in meetingHud.playerStates.ToArray())
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
            meetingHud.CheckForEndVoting();
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuessKill, SendOption.Reliable, -1);
        writer.Write(pc.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
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
                CustomRoles role = roles[rd.Next(0, roles.Length)];
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                msg += Utils.GetRoleName(role);
            }
            var player = Main.AllAlivePlayerControls[rd.Next(0, Main.AllAlivePlayerControls.Length)];
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
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
                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole().IsImpostor() && Options.ImpostorsCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                else if (PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.EvilGuesser && !Options.ImpostorsCanGuess.GetBool())
                    CreateGuesserButton(__instance);

                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole().IsCrewmate() && Options.CrewmatesCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                else if (PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.NiceGuesser && !Options.CrewmatesCanGuess.GetBool())
                    CreateGuesserButton(__instance);

                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole().IsNK() && Options.NeutralKillersCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole().IsNonNK() && Options.PassiveNeutralsCanGuess.GetBool())
                    CreateGuesserButton(__instance);
                else if (PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.Doomsayer && !Options.PassiveNeutralsCanGuess.GetBool() && !Doomsayer.CantGuess)
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

                if (PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.Is(CustomRoles.Doomsayer) && !Doomsayer.CantGuess)
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
            button.OnClick.AddListener((Action)(() => GuesserOnClick(pva.TargetPlayerId, __instance)));
        }
    }

    public const int MaxOneScreenRole = 40;
    public static int Page;
    public static PassiveButton ExitButton;
    public static GameObject guesserUI;
    private static Dictionary<CustomRoleTypes, List<Transform>> RoleButtons;
    private static Dictionary<CustomRoleTypes, SpriteRenderer> RoleSelectButtons;
    private static List<SpriteRenderer> PageButtons;
    public static CustomRoleTypes currentTeamType;
    static void GuesserSelectRole(CustomRoleTypes Role, bool SetPage = true)
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

            List<int> i = [0, 0, 0, 0];
            var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
            var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
            var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
            textTemplate.enabled = true;
            if (textTemplate.transform.FindChild("RoleTextMeeting") != null) UnityEngine.Object.Destroy(textTemplate.transform.FindChild("RoleTextMeeting").gameObject);

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
            exitButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
            {
                __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                UnityEngine.Object.Destroy(container.gameObject);
            }));
            ExitButton = exitButton.GetComponent<PassiveButton>();

            List<Transform> buttons = [];
            Transform selectedButton = null;

            int tabCount = 0;
            for (int index = 0; index < 4; index++)
            {
                if (PlayerControl.LocalPlayer.Is(CustomRoles.EvilGuesser))
                {
                    if (!Options.EGCanGuessImp.GetBool() && index == 1) continue;
                    if (!Options.EGCanGuessAdt.GetBool() && index == 3) continue;
                }
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.NiceGuesser))
                {
                    if (!Options.GGCanGuessCrew.GetBool() && index == 0) continue;
                    if (!Options.GGCanGuessAdt.GetBool() && index == 3) continue;
                }
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.Doomsayer))
                {
                    if (!Doomsayer.DCanGuessCrewmates.GetBool() && index == 0) continue;
                    if (!Doomsayer.DCanGuessImpostors.GetBool() && index == 1) continue;
                    if (!Doomsayer.DCanGuessNeutrals.GetBool() && index == 2) continue;
                    if (!Doomsayer.DCanGuessAdt.GetBool() && index == 3) continue;
                }
                else if (PlayerControl.LocalPlayer.Is(CustomRoles.Guesser))
                {
                    //if (!Options.GCanGuessCrew.GetBool() && index == 0) continue;
                    //if (!Options.GCanGuessImp.GetBool() && index == 1) continue;
                    if (!Options.GCanGuessAdt.GetBool() && index == 3) continue;
                }
                else if (Options.GuesserMode.GetBool() &&
                    !(PlayerControl.LocalPlayer.Is(CustomRoles.EvilGuesser) ||
                      PlayerControl.LocalPlayer.Is(CustomRoles.NiceGuesser) ||
                      PlayerControl.LocalPlayer.Is(CustomRoles.Doomsayer) ||
                      PlayerControl.LocalPlayer.Is(CustomRoles.Guesser)))
                {
                    if (!Options.CrewCanGuessCrew.GetBool() && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Crewmate) && index == 0) continue;
                    if (!Options.ImpCanGuessImp.GetBool() && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) && index == 1) continue;
                    if (!Options.CanGuessAddons.GetBool() && index == 3) continue;
                }
                Transform TeambuttonParent = new GameObject().transform;
                TeambuttonParent.SetParent(container);
                Transform Teambutton = UnityEngine.Object.Instantiate(buttonTemplate, TeambuttonParent);
                Teambutton.FindChild("ControllerHighlight").gameObject.SetActive(false);
                Transform TeambuttonMask = UnityEngine.Object.Instantiate(maskTemplate, TeambuttonParent);
                TextMeshPro Teamlabel = UnityEngine.Object.Instantiate(textTemplate, Teambutton);
                Teambutton.GetComponent<SpriteRenderer>().sprite = CustomButton.Get("GuessPlateWithKPD");
                RoleSelectButtons.Add((CustomRoleTypes)index, Teambutton.GetComponent<SpriteRenderer>());
                TeambuttonParent.localPosition = new(-3.10f + (tabCount++ * 1.47f), 2.225f, -200);
                TeambuttonParent.localScale = new(0.53f, 0.53f, 1f);
                Teamlabel.color = (CustomRoleTypes)index switch
                {
                    CustomRoleTypes.Crewmate => new Color32(140, 255, 255, byte.MaxValue),
                    CustomRoleTypes.Impostor => new Color32(255, 25, 25, byte.MaxValue),
                    CustomRoleTypes.Neutral => new Color32(127, 140, 141, byte.MaxValue),
                    CustomRoleTypes.Addon => new Color32(255, 154, 206, byte.MaxValue),
                    _ => throw new NotImplementedException(),
                };
                Logger.Info(Teamlabel.color.ToString(), ((CustomRoleTypes)index).ToString());
                Teamlabel.text = GetString("Type" + ((CustomRoleTypes)index).ToString());
                Teamlabel.alignment = TextAlignmentOptions.Center;
                Teamlabel.transform.localPosition = new Vector3(0, 0, Teamlabel.transform.localPosition.z);
                Teamlabel.transform.localScale *= 1.6f;
                Teamlabel.autoSizeTextContainer = true;

                static void CreateTeamButton(Transform Teambutton, CustomRoleTypes type)
                {
                    Teambutton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        GuesserSelectRole(type);
                        ReloadPage();
                    }));
                }
                if (PlayerControl.LocalPlayer.IsAlive()) CreateTeamButton(Teambutton, (CustomRoleTypes)index);
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
                Pagebutton.GetComponent<SpriteRenderer>().sprite = CustomButton.Get("GuessPlateWithKPD");
                PagebuttonParent.localPosition = IsNext ? new(3.535f, -2.2f, -200) : new(-3.475f, -2.2f, -200);
                PagebuttonParent.localScale = new(0.55f, 0.55f, 1f);
                Pagelabel.color = Color.white;
                Pagelabel.text = GetString(IsNext ? "NextPage" : "PreviousPage");
                Pagelabel.alignment = TextAlignmentOptions.Center;
                Pagelabel.transform.localPosition = new Vector3(0, 0, Pagelabel.transform.localPosition.z);
                Pagelabel.transform.localScale *= 1.6f;
                Pagelabel.autoSizeTextContainer = true;
                if (!IsNext && Page <= 1) Pagebutton.GetComponent<SpriteRenderer>().color = new(1, 1, 1, 0.1f);
                Pagebutton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => ClickEvent()));
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
            foreach (var role in CustomRolesHelper.AllRoles)
            {
                if (role is CustomRoles.GM
                    or CustomRoles.SpeedBooster
                    //   or CustomRoles.Ritualist
                    or CustomRoles.Engineer
                    or CustomRoles.Crewmate
                    //   or CustomRoles.Loyal
                    or CustomRoles.Oblivious
                    or CustomRoles.ChiefOfPolice
                    or CustomRoles.Rogue
                    or CustomRoles.Scientist
                    or CustomRoles.Impostor
                    or CustomRoles.Shapeshifter
                    or CustomRoles.Flash
                    or CustomRoles.NotAssigned
                    //     or CustomRoles.Marshall 
                    //or CustomRoles.Paranoia 
                    or CustomRoles.SuperStar
                    or CustomRoles.Konan
                    or CustomRoles.Oblivious
                    //     or CustomRoles.Reflective
                    or CustomRoles.GuardianAngelTOHE
                    or CustomRoles.Solsticer
                    or CustomRoles.GuardianAngel
                    or CustomRoles.Killer
                    or CustomRoles.Mini
                    or CustomRoles.Onbound
                    or CustomRoles.Rebound
                    or CustomRoles.LastImpostor
                    or CustomRoles.Mare
                    or CustomRoles.Cyber
                    ) continue;

                if (Options.ShowOnlyEnabledRolesInGuesserUI.GetBool() && !(role.IsEnable() || role.RoleExist(countDead: true))) continue;

                CreateRole(role);
            }
            void CreateRole(CustomRoles role)
            {
                if (40 <= i[(int)role.GetCustomRoleTypes()]) i[(int)role.GetCustomRoleTypes()] = 0;
                Transform buttonParent = new GameObject().transform;
                buttonParent.SetParent(container);
                Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
                button.FindChild("ControllerHighlight").gameObject.SetActive(false);
                Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
                TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);
                button.GetComponent<SpriteRenderer>().sprite = CustomButton.Get("GuessPlate");
                if (!RoleButtons.ContainsKey(role.GetCustomRoleTypes()))
                {
                    RoleButtons.Add(role.GetCustomRoleTypes(), []);
                }
                RoleButtons[role.GetCustomRoleTypes()].Add(button);
                buttons.Add(button);
                int row = i[(int)role.GetCustomRoleTypes()] / 5;
                int col = i[(int)role.GetCustomRoleTypes()] % 5;
                buttonParent.localPosition = new Vector3(-3.47f + 1.75f * col, 1.5f - 0.45f * row, -200f);
                buttonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
                label.text = GetString(role.ToString());
                label.color = Utils.GetRoleColor(role);
                label.alignment = TextAlignmentOptions.Center;
                label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
                label.transform.localScale *= 1.6f;
                label.autoSizeTextContainer = true;
                int copiedIndex = i[(int)role.GetCustomRoleTypes()];

                button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
                if (PlayerControl.LocalPlayer.IsAlive()) button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
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
                        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                        UnityEngine.Object.Destroy(container.gameObject);
                        textTemplate.enabled = false;

                    }
                }));
                i[(int)role.GetCustomRoleTypes()]++;
                ind++;
            }
            container.transform.localScale *= 0.75f;
            GuesserSelectRole(CustomRoleTypes.Crewmate);
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