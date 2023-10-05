using HarmonyLib;
using Hazel;
using System;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

public static class RetributionistRevengeManager
{
    public static bool RetributionistMsgCheck(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Retributionist)) return false;
        msg = msg.Trim().ToLower();
        if (msg.Length < 4 || msg[..4] != "/ret") return false;
        if (Options.RetributionistCanKillNum.GetInt() < 1)
        {
            if (!isUI) Utils.SendMessage(GetString("RetributionistKillDisable"), pc.PlayerId);
            else pc.ShowPopUp(GetString("RetributionistKillDisable"));
            return true;
        }
        int playerCount = Main.AllAlivePlayerControls.Count();
        {
            if (playerCount <= Options.MinimumPlayersAliveToRetri.GetInt())
            {
            if (pc.Data.IsDead)
                {
                    if (!isUI) Utils.SendMessage(GetString("RetributionistKillTooManyDead"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("RetributionistKillTooManyDead"));
                    return true;
                }
            }
         
        }
        if (Options.CanOnlyRetributeWithTasksDone.GetBool())
        {
            if (!pc.GetPlayerTaskState().IsTaskFinished && pc.Data.IsDead && !CopyCat.playerIdList.Contains(pc.PlayerId) && !Main.TasklessCrewmate.Contains(pc.PlayerId))
            {
                if (!isUI) Utils.SendMessage(GetString("RetributionistKillDisable"), pc.PlayerId);
                else pc.ShowPopUp(GetString("RetributionistKillDisable"));
                return true;
            }
        }
        if (!pc.Data.IsDead)
        {
            Utils.SendMessage(GetString("RetributionistAliveKill"), pc.PlayerId);
            return true;
        }

        if (msg == "/ret")
        {
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += "\n" + npc.PlayerId.ToString() + " → (" + npc.GetDisplayRoleName() + ") " + npc.GetRealName();
            Utils.SendMessage(text, pc.PlayerId);
            return true;
        }

        if (Main.RetributionistRevenged.ContainsKey(pc.PlayerId))
        {
            if (Main.RetributionistRevenged[pc.PlayerId] >= Options.RetributionistCanKillNum.GetInt())
            {
                if (!isUI) Utils.SendMessage(GetString("RetributionistKillMax"), pc.PlayerId);
                else pc.ShowPopUp(GetString("RetributionistKillMax"));
                return true;
            }
        }
        else
        {
            Main.RetributionistRevenged.Add(pc.PlayerId, 0);
        }

        int targetId;
        PlayerControl target;
        try
        {
            targetId = int.Parse(msg.Replace("/ret", string.Empty));
            target = Utils.GetPlayerById(targetId);
        }
        catch
        {
            if (!isUI) Utils.SendMessage(GetString("RetributionistKillDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("RetributionistKillDead"));
            return true;
        }

        if (target == null || target.Data.IsDead)
        {
            if (!isUI) Utils.SendMessage(GetString("RetributionistKillDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("RetributionistKillDead"));
            return true;
        }
        if (target.Is(CustomRoles.Pestilence))
        {
            if (!isUI) Utils.SendMessage(GetString("PestilenceImmune"), pc.PlayerId);
            else pc.ShowPopUp(GetString("PestilenceImmune"));
            return true;
        }
        if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
        {
            if (!isUI) Utils.SendMessage(GetString("GuessMini"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessMini"));
            return true;
        }

        Logger.Info($"{pc.GetNameWithRole()} 复仇了 {target.GetNameWithRole()}", "Retributionist");

        string Name = target.GetRealName();

        Main.RetributionistRevenged[pc.PlayerId]++;

        CustomSoundsManager.RPCPlayCustomSoundAll("AWP");

        _ = new LateTask(() =>
        {
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
            target.SetRealKiller(pc);

            if (GameStates.IsMeeting)
            {
                GuessManager.RpcGuesserMurderPlayer(target);

                //死者检查
                Utils.AfterPlayerDeathTasks(target, true);

                Utils.NotifyRoles(isForMeeting: GameStates.IsMeeting, NoCache: true);
            }
            else
            {
                target.RpcMurderPlayerV3(target);
                Main.PlayerStates[target.PlayerId].SetDead();
            }

            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("RetributionistKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Retributionist), GetString("RetributionistRevengeTitle"))); }, 0.6f, "Retributionist Kill");

        }, 0.2f, "Retributionist Kill");
        return true;
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RetributionistRevenge, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        RetributionistMsgCheck(pc, $"/ret {PlayerId}", true);
    }

    private static void RetributionistOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Retributionist UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) RetributionistMsgCheck(PlayerControl.LocalPlayer, $"/ret {playerId}", true);
        else SendRPC(playerId);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Retributionist) && !PlayerControl.LocalPlayer.IsAlive())
                CreateJudgeButton(__instance);
        }
    }
    public static void CreateJudgeButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
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
            button.OnClick.AddListener((Action)(() => RetributionistOnClick(pva.TargetPlayerId, __instance)));
        }
    }
}
