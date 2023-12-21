using HarmonyLib;
using Hazel;
using System;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

public static class MafiaRevengeManager
{
    public static bool MafiaMsgCheck(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Mafia)) return false;
        msg = msg.Trim().ToLower();
        if (msg.Length < 3 || msg[..3] != "/rv") return false;
        if (Options.MafiaCanKillNum.GetInt() < 1)
        {
            if (!isUI) Utils.SendMessage(GetString("MafiaKillDisable"), pc.PlayerId);
            else pc.ShowPopUp(GetString("MafiaKillDisable"));
            return true;
        }

        if (!pc.Data.IsDead)
        {
            Utils.SendMessage(GetString("MafiaAliveKill"), pc.PlayerId);
            return true;
        }

        if (msg == "/rv")
        {
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += "\n" + npc.PlayerId.ToString() + " → (" + npc.GetDisplayRoleName() + ") " + npc.GetRealName();
            Utils.SendMessage(text, pc.PlayerId);
            return true;
        }

        if (Main.MafiaRevenged.ContainsKey(pc.PlayerId))
        {
            if (Main.MafiaRevenged[pc.PlayerId] >= Options.MafiaCanKillNum.GetInt())
            {
                if (!isUI) Utils.SendMessage(GetString("MafiaKillMax"), pc.PlayerId);
                else pc.ShowPopUp(GetString("MafiaKillMax"));
                return true;
            }
        }

        else
        {
            Main.MafiaRevenged.Add(pc.PlayerId, 0);
        }

        int targetId;
        PlayerControl target;
        try
        {
            targetId = int.Parse(msg.Replace("/rv", string.Empty));
            target = Utils.GetPlayerById(targetId);
        }
        catch
        {
            if (!isUI) Utils.SendMessage(GetString("MafiaKillDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("MafiaKillDead"));
            return true;
        }

        if (target == null || target.Data.IsDead)
        {
            if (!isUI) Utils.SendMessage(GetString("MafiaKillDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("MafiaKillDead"));
            return true;
        }
        else if (target.Is(CustomRoles.Pestilence))
        {
            if (!isUI) Utils.SendMessage(GetString("PestilenceImmune"), pc.PlayerId);
            else pc.ShowPopUp(GetString("PestilenceImmune"));
            return true;
        }
        else if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18 )
        {
            if (!isUI) Utils.SendMessage(GetString("GuessMini"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessMini"));
            return true;
        }
        else if (target.Is(CustomRoles.Solsticer))
        {
            if (!isUI) Utils.SendMessage(GetString("GuessSolsticer"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessSolsticer"));
            return true;
        }

        Logger.Info($"{pc.GetNameWithRole()} 复仇了 {target.GetNameWithRole()}", "Mafia");

        string Name = target.GetRealName();

        Main.MafiaRevenged[pc.PlayerId]++;

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

            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("MafiaKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), GetString("MafiaRevengeTitle")), true); }, 0.6f, "Mafia Kill");

        }, 0.2f, "Mafia Start Kill");
        return true;
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MafiaRevenge, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        MafiaMsgCheck(pc, $"/rv {PlayerId}", true);
    }

    private static void MafiaOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Mafia UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) MafiaMsgCheck(PlayerControl.LocalPlayer, $"/rv {playerId}", true);
        else SendRPC(playerId);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Mafia) && !PlayerControl.LocalPlayer.IsAlive())
                CreateJudgeButton(__instance);
        }
    }
    public static void CreateJudgeButton(MeetingHud __instance)
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
            button.OnClick.AddListener((Action)(() => MafiaOnClick(pva.TargetPlayerId, __instance)));
        }
    }
}
