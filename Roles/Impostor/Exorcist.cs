using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using UnityEngine;
using Hazel;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Exorcist : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 30800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Exorcist);
    public override CustomRoles ThisRoleBase => CustomRoles.Exorcist;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\
    private static OptionItem ExorcismActiveFor;
    private static OptionItem ExorcismPerGame;
    private static OptionItem ExorcismDelay;
    private static OptionItem ExorcismSacrificesToDispel;
    private static OptionItem ExorcismLimitMeeting;
    private static OptionItem ExorcismEndOnKill;
    private static OptionItem TryHideMsg;

    private int ExorcismLimitPerMeeting;
    private static bool IsExorcismActive;
    private static bool IsDelayActive;
    private static PlayerControl ExorcistPlayer;
    private int Sacrifices = 0;
    private bool Dispelled = false;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Exorcist, 1, zeroOne: false);
        ExorcismActiveFor = FloatOptionItem.Create(Id + 2, "ExorcismActiveFor", new(1f, 10f, 1f), 3f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetValueFormat(OptionFormat.Seconds);
        ExorcismPerGame = IntegerOptionItem.Create(Id + 3, "ExorcismPerGame", new(1, 10, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        ExorcismDelay = FloatOptionItem.Create(Id + 4, "ExorcismDelay", new(0f, 10f, 1f), 3f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetValueFormat(OptionFormat.Seconds);
        ExorcismSacrificesToDispel = IntegerOptionItem.Create(Id + 5, "ExorcismSacrificesToDispel", new(1, 10, 1), 2, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        ExorcismLimitMeeting = IntegerOptionItem.Create(Id + 6, "ExorcismLimitMeeting", new(1, 5, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        ExorcismEndOnKill = BooleanOptionItem.Create(Id + 7, "ExorcismEndOnKill", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        TryHideMsg = BooleanOptionItem.Create(Id + 8, "ExorcistTryHideMsg", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetColor(Color.green);
    }

    public override void Add(byte playerId)
    {
        ExorcismLimitPerMeeting = ExorcismLimitMeeting.GetInt();
        AbilityLimit = ExorcismPerGame.GetInt();
    }

    public override void AfterMeetingTasks()
    {
        ExorcismLimitPerMeeting = ExorcismLimitMeeting.GetInt();
    }

    public bool CheckCommand(PlayerControl player, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || player == null || GameStates.IsExilling) return false;
        if (!player.Is(CustomRoles.Exorcist)) return false;

        msg = msg.ToLower().Trim();

        var commands = new[] { "exorcise", "exorcism", "ex" };
        foreach (var cmd in commands)
        {
            if (msg.StartsWith("/" + cmd))
            {
                if (!player.IsAlive()) return false;
                

                if (AbilityLimit <= 0 || ExorcismLimitPerMeeting <= 0)
                {
                    if (TryHideMsg.GetBool() && !player.Data.IsHost())
                        GuessManager.TryHideMsg();
                    player.ShowInfoMessage(isUI, GetString("ExorcistOutOfUsages"));
                    return true;
                }
                if (Dispelled)
                {
                    if (TryHideMsg.GetBool() && !player.Data.IsHost())
                        GuessManager.TryHideMsg();
                    player.ShowInfoMessage(isUI, GetString("ExorcistDispelled"));
                    return true;
                }
                if (IsExorcismActive || IsDelayActive)
                {
                    if (TryHideMsg.GetBool() && !player.Data.IsHost())
                        GuessManager.TryHideMsg();
                    player.ShowInfoMessage(isUI, GetString("ExorcistActive"));
                    return true;
                }
                ActivateExorcism(player);
                return true;
            }
        }
        return false;
    }

    public static bool IsExorcismCurrentlyActive()
    {
        return IsExorcismActive;
    }

    public static void ExorcisePlayer(PlayerControl player)
    {
        if (ExorcismEndOnKill.GetBool() && IsExorcismActive && ExorcistPlayer == player)
        {
            IsExorcismActive = false;
            Utils.SendMessage(Translator.GetString("ExorcistEnd"));
        }
        player.SetDeathReason(PlayerState.DeathReason.Exorcised);
        player.SetRealKiller(ExorcistPlayer);
        GuessManager.RpcGuesserMurderPlayer(player);
        Main.PlayersDiedInMeeting.Add(player.PlayerId);
        MurderPlayerPatch.AfterPlayerDeathTasks(player, PlayerControl.LocalPlayer, true);
        Utils.SendMessage(string.Format(Translator.GetString("ExorcistKill"), player.name.RemoveHtmlTags()));
        Exorcist exorcist = (Exorcist)ExorcistPlayer.GetRoleClass();
        exorcist.Sacrifice();
    }

    public void ActivateExorcism(PlayerControl player)
    {
        ExorcismLimitPerMeeting--;
        AbilityLimit--;
        if (TryHideMsg.GetBool())
            GuessManager.TryHideMsg();
        ExorcistPlayer = player;
        IsDelayActive = true;
        if (ExorcismDelay.GetFloat() > 0)
            Utils.SendMessage(string.Format(GetString("ExorcistNotify"), ExorcismDelay.GetFloat()));
        
        _ = new LateTask(() =>
        {
            IsExorcismActive = true;
            IsDelayActive = false;
            Utils.SendMessage(string.Format(Translator.GetString("ExorcistStart"), ExorcismActiveFor.GetFloat()));
            _ = new LateTask(() =>
            {
                if (IsExorcismActive)
                {
                    IsExorcismActive = false;
                    Utils.SendMessage(GetString("ExorcistEnd"));
                }
            }, ExorcismActiveFor.GetFloat(), "ExorcistNotify");

        }, ExorcismDelay.GetFloat(), "ExorcistNotify");
    }

    public void Sacrifice()
    {
        Sacrifices++;
        if (Sacrifices >= ExorcismSacrificesToDispel.GetInt())
            Dispelled = true;
    }

    public override string GetProgressText(byte playerId, bool coooms)
        => Utils.ColorString(AbilityLimit <= 0 ? Color.gray : Utils.GetRoleColor(CustomRoles.Exorcist), $"({AbilityLimit})") ?? "Invalid";

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Exorcist) && PlayerControl.LocalPlayer.IsAlive())
                CreateExorcistButton(__instance);
        }
    }

    public static void CreateExorcistButton(MeetingHud __instance)
    {
        PlayerControl pc = PlayerControl.LocalPlayer;
        PlayerVoteArea pva = __instance.playerStates[pc.PlayerId];
        if (pc == null || !pc.IsAlive()) return;

        GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
        GameObject exorcistButton = UnityEngine.Object.Instantiate(template, pva.transform);
        exorcistButton.name = "ExorcistButton";
        exorcistButton.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
        SpriteRenderer renderer = exorcistButton.GetComponent<SpriteRenderer>();
        renderer.sprite = CustomButton.Get("MeetingKillButton");
        PassiveButton button = exorcistButton.GetComponent<PassiveButton>();
        button.OnClick.RemoveAllListeners();
        button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => ExorcistOnClick()));
        
    }

    private static void ExorcistOnClick()
    {
        if (!PlayerControl.LocalPlayer.IsAlive()) return;
        Logger.Msg($"Exorcist Click: ID {PlayerControl.LocalPlayer.PlayerId}", "Exorcist UI");
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.GetRoleClass() is Exorcist exorcist)
        {
            exorcist.CheckCommand(PlayerControl.LocalPlayer, "/ex", true);
        }
        else
        {
            SendExorcismRPC(PlayerControl.LocalPlayer.PlayerId);
        }
    }

    private static void SendExorcismRPC(byte exorcistId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ExorcistExorcise, SendOption.Reliable);
        writer.Write(exorcistId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC_Custom(MessageReader reader, PlayerControl pc)
    {
        if (pc.GetRoleClass() is Exorcist exorcist && exorcist.AbilityLimit > 0)
        {
            byte exorcistId = reader.ReadByte();
            PlayerControl exorcistPlayer = Utils.GetPlayerById(exorcistId);
            if (exorcistPlayer == null) return;
            exorcist.CheckCommand(exorcistPlayer, "/ex", false);
        }
    }
}
