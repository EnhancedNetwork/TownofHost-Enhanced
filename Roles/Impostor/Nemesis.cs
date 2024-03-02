using HarmonyLib;
using Hazel;
using Hazel.Crypto;
using System;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Nemesis
{
    public static readonly int Id = 3600;
    public static OptionItem NemesisCanKillNum;
    public static OptionItem LegacyNemesis;
    public static OptionItem NemesisShapeshiftCD;
    public static OptionItem NemesisShapeshiftDur;

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Nemesis);
        NemesisCanKillNum = IntegerOptionItem.Create(Id + 10, "NemesisCanKillNum", new(0, 15, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis])
                .SetValueFormat(OptionFormat.Players);
        LegacyNemesis = BooleanOptionItem.Create(Id + 11, "LegacyNemesis", false, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis]);
        NemesisShapeshiftCD = FloatOptionItem.Create(Id + 12, "ShapeshiftCooldown", new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
                .SetParent(LegacyNemesis)
                .SetValueFormat(OptionFormat.Seconds);
        NemesisShapeshiftDur = FloatOptionItem.Create(Id + 13, "ShapeshiftDuration", new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false)
                .SetParent(LegacyNemesis)
                .SetValueFormat(OptionFormat.Seconds);
    }
    public static bool NemesisMsgCheck(PlayerControl pc, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Nemesis)) return false;
        msg = msg.Trim().ToLower();
        if (msg.Length < 3 || msg[..3] != "/rv") return false;
        if (NemesisCanKillNum.GetInt() < 1)
        {
            if (!isUI) Utils.SendMessage(GetString("NemesisKillDisable"), pc.PlayerId);
            else pc.ShowPopUp(GetString("NemesisKillDisable"));
            return true;
        }

        if (pc.IsAlive())
        {
            Utils.SendMessage(GetString("NemesisAliveKill"), pc.PlayerId);
            return true;
        }

        if (msg == "/rv")
        {
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += "\n" + npc.PlayerId.ToString() + " → (" + npc.GetDisplayRoleAndSubName(npc, false) + ") " + npc.GetRealName();
            Utils.SendMessage(text, pc.PlayerId);
            return true;
        }

        if (Main.NemesisRevenged.ContainsKey(pc.PlayerId))
        {
            if (Main.NemesisRevenged[pc.PlayerId] >= NemesisCanKillNum.GetInt())
            {
                if (!isUI) Utils.SendMessage(GetString("NemesisKillMax"), pc.PlayerId);
                else pc.ShowPopUp(GetString("NemesisKillMax"));
                return true;
            }
        }

        else
        {
            Main.NemesisRevenged.Add(pc.PlayerId, 0);
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
            if (!isUI) Utils.SendMessage(GetString("NemesisKillDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("NemesisKillDead"));
            return true;
        }

        if (target == null || !target.IsAlive())
        {
            if (!isUI) Utils.SendMessage(GetString("NemesisKillDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("NemesisKillDead"));
            return true;
        }
        else if (target.Is(CustomRoles.Pestilence))
        {
            if (!isUI) Utils.SendMessage(GetString("PestilenceImmune"), pc.PlayerId);
            else pc.ShowPopUp(GetString("PestilenceImmune"));
            return true;
        }
        else if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
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

        Logger.Info($"{pc.GetNameWithRole()} 复仇了 {target.GetNameWithRole()}", "Nemesis");

        string Name = target.GetRealName();

        Main.NemesisRevenged[pc.PlayerId]++;

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
            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("NemesisKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Nemesis), GetString("NemesisRevengeTitle")), true); }, 0.6f, "Nemesis Kill");
        }, 0.2f, "Nemesis Start Kill");
        return true;
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.NemesisRevenge, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        NemesisMsgCheck(pc, $"/rv {PlayerId}", true);
    }

    private static void NemesisOnClick(byte playerId /*, MeetingHud __instance*/)
    {
        Logger.Msg($"Click: ID {playerId}", "Nemesis UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) NemesisMsgCheck(PlayerControl.LocalPlayer, $"/rv {playerId}", true);
        else SendRPC(playerId);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Nemesis) && !PlayerControl.LocalPlayer.IsAlive())
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
            button.OnClick.AddListener((Action)(() => NemesisOnClick(pva.TargetPlayerId/*, __instance*/)));
        }
    }
}