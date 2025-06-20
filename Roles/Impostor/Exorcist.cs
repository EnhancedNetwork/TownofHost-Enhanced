using Hazel;
using TMPro;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Exorcist : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Exorcist;
    private const int Id = 31200;
    public static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
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
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Exorcist);
        ExorcismActiveFor = FloatOptionItem.Create(Id + 11, "ExorcismActiveFor", new(1f, 10f, 1f), 3f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetValueFormat(OptionFormat.Seconds);
        ExorcismPerGame = IntegerOptionItem.Create(Id + 12, "ExorcismPerGame", new(1, 10, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        ExorcismDelay = FloatOptionItem.Create(Id + 13, "ExorcismDelay", new(0f, 10f, 1f), 3f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetValueFormat(OptionFormat.Seconds);
        ExorcismSacrificesToDispel = IntegerOptionItem.Create(Id + 14, "ExorcismSacrificesToDispel", new(1, 10, 1), 2, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        ExorcismLimitMeeting = IntegerOptionItem.Create(Id + 15, "ExorcismLimitMeeting", new(1, 5, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        ExorcismEndOnKill = BooleanOptionItem.Create(Id + 16, "ExorcismEndOnKill", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        TryHideMsg = BooleanOptionItem.Create(Id + 17, "ExorcistTryHideMsg", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetColor(Color.green);
    }
    public override void Init()
    {
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
        ExorcismLimitPerMeeting = ExorcismLimitMeeting.GetInt();
        playerId.SetAbilityUseLimit(ExorcismPerGame.GetInt());
    }
    public override void Remove(byte playerId)
    {
        PlayerIds.Remove(playerId);
    }

    public override void AfterMeetingTasks()
    {
        ExorcismLimitPerMeeting = ExorcismLimitMeeting.GetInt();
    }

    public bool CheckCommand(PlayerControl player, string msg, bool isUI = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || player == null || GameStates.IsExilling) return false;
        if (!player.Is(CustomRoles.Exorcist) || !player.IsAlive()) return false;


        msg = msg.ToLower().Trim();

        var commands = new[] { "exorcise", "exorcism", "ex" };
        foreach (var cmd in commands)
        {
            if (msg.StartsWith("/" + cmd))
            {
                if (player.PlayerId.GetAbilityUseLimit() <= 0 || ExorcismLimitPerMeeting <= 0)
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
                GetProgressText(player.PlayerId, false);
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
        if (ExorcismEndOnKill.GetBool() && IsExorcismActive)
        {
            IsExorcismActive = false;
            RPC.PlaySoundRPC(Sounds.TaskComplete, byte.MaxValue);
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

    public static void ActivateExorcism(PlayerControl player)
    {
        var exorcist = (Exorcist)player.GetRoleClass();
        exorcist.ExorcismLimitPerMeeting--;
        player.RPCPlayCustomSound("Line");
        player.RpcRemoveAbilityUse();

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
            RPC.PlaySoundRPC(Sounds.SabotageSound, byte.MaxValue);
            Utils.SendMessage(string.Format(Translator.GetString("ExorcistStart"), ExorcismActiveFor.GetFloat()));

            _ = new LateTask(() =>
            {
                if (IsExorcismActive)
                {
                    IsExorcismActive = false;

                    RPC.PlaySoundRPC(Sounds.TaskComplete, byte.MaxValue);
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
        => Utils.ColorString(playerId.GetAbilityUseLimit() <= 0 ? Color.gray : Utils.GetRoleColor(CustomRoles.Exorcist), $"({playerId.GetAbilityUseLimit()})") ?? "Invalid";

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.GetRoleClass() is Exorcist exorcist)
                exorcist.CreateExorcistButton(__instance);
        }
    }

    public void CreateExorcistButton(MeetingHud __instance)
    {

        if (GameObject.Find("ExorcistButton") != null)
            GameObject.Destroy(GameObject.Find("ExorcistButton"));

        PlayerControl pc = PlayerControl.LocalPlayer;
        if (!pc.IsAlive()) return;

        GameObject parent = GameObject.Find("Main Camera").transform.Find("Hud").Find("ChatUi").Find("ChatScreenRoot").Find("ChatScreenContainer").gameObject;
        GameObject template = __instance.transform.Find("MeetingContents").Find("ButtonStuff").Find("button_skipVoting").gameObject;
        GameObject exorcistButton = UnityEngine.Object.Instantiate(template, parent.transform);
        exorcistButton.name = "ExorcistButton";
        exorcistButton.transform.localPosition = new Vector3(3.46f, 0f, 45f);
        exorcistButton.SetActive(true);
        SpriteRenderer renderer = exorcistButton.GetComponent<SpriteRenderer>();
        renderer.sprite = CustomButton.Get("shush");
        renderer.color = Color.white;
        GameObject Text_TMP = exorcistButton.GetComponentInChildren<TextMeshPro>().gameObject;
        Text_TMP.SetActive(false);
        PassiveButton button = exorcistButton.GetComponent<PassiveButton>();
        button.OnClick.RemoveAllListeners();
        button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => ExorcistOnClick(exorcistButton)));
        GameObject ControllerHighlight = exorcistButton.transform.Find("ControllerHighlight").gameObject;
        ControllerHighlight.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
    }


    private static void ExorcistOnClick(GameObject exorcistButton)
    {
        if (!PlayerControl.LocalPlayer.IsAlive()) return;
        Logger.Msg($"Exorcist Click: ID {PlayerControl.LocalPlayer.PlayerId}", "Exorcist UI");
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.GetRoleClass() is Exorcist exorcist)
        {
            if (exorcist._Player.PlayerId.GetAbilityUseLimit() <= 0)
            {
                PlayerControl.LocalPlayer.ShowInfoMessage(true, GetString("ExorcistOutOfUsages"));
                return;
            }
            exorcist.CheckCommand(PlayerControl.LocalPlayer, "/ex", true);
        }
        else if (PlayerControl.LocalPlayer.GetRoleClass() is Exorcist exorcist1)
        {
            SendExorcismRPC(PlayerControl.LocalPlayer.PlayerId);
        }
        exorcistButton.SetActive(false);
        _ = new LateTask(() => exorcistButton.SetActive(true), 1f, "ExorcistButton");
    }

    private static void SendExorcismRPC(byte exorcistId)
    {
        var msg = new RpcExorcistExorcise(PlayerControl.LocalPlayer.NetId, exorcistId);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }

    public static void ReceiveRPC_Custom(MessageReader reader, PlayerControl pc)
    {
        if (pc.GetRoleClass() is Exorcist exorcist)
        {
            byte exorcistId = reader.ReadByte();
            PlayerControl exorcistPlayer = Utils.GetPlayerById(exorcistId);
            if (exorcistPlayer == null) return;
            exorcist.CheckCommand(exorcistPlayer, "/ex", false);
        }
    }
}
