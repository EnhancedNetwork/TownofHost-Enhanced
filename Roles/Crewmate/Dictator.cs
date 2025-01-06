using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Dictator : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 11600;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Dictator);
    }

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

            if (target.Is(CustomRoles.Solsticer))
            {
                pc.ShowInfoMessage(isUI, GetString("ExpelSolsticer"));
                MeetingHud.Instance.RpcClearVoteDelay(pc.GetClientId());
                return true;
            }

            statesList.Add(new()
            {
                VoterId = pc.PlayerId,
                VotedForId = target.PlayerId
            });
            states = [.. statesList];
            var exiled = target.Data;
            var isBlackOut = AntiBlackout.BlackOutIsActive;
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);
            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiled;
            Main.LastVotedPlayerInfo = exiled;
            AntiBlackout.ExilePlayerId = exiled.PlayerId;
            if (AntiBlackout.BlackOutIsActive)
            {
                if (isBlackOut)
                    MeetingHud.Instance.AntiBlackRpcVotingComplete(states, exiled, false);
                else
                    MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), exiled, false);
                if (exiled != null)
                {
                    AntiBlackout.ShowExiledInfo = isBlackOut;
                    CheckForEndVotingPatch.ConfirmEjections(exiled, isBlackOut);
                    MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
                    MeetingHud.Instance.RpcClose();
                }
            }
            else
            {
                MeetingHud.Instance.RpcVotingComplete(states, exiled, false);

                if (exiled != null)
                {
                    CheckForEndVotingPatch.ConfirmEjections(exiled);
                }
            }

            Logger.Info($"{target.GetNameWithRole()} expelled by Dictator", "Dictator");

            CheckForEndVotingPatch.CheckForDeathOnExile(PlayerState.DeathReason.Vote, target.PlayerId);

            Logger.Info("Dictatorial vote, forced closure of the meeting", "Special Phase");

            target.SetRealKiller(pc);

        }
        return true;
    }
    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting && ChangeCommandToExpel.GetBool() ? ColorString(GetRoleColor(CustomRoles.Dictator), target.PlayerId.ToString()) + " " + TargetPlayerName : "";

    private void SendDictatorRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DictatorRPC, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void OnReceiveDictatorRPC(MessageReader reader, PlayerControl pc)
    {
        byte pid = reader.ReadByte();
        if (pc.Is(CustomRoles.Dictator) && pc.IsAlive() && GameStates.IsVoting)
        {
            if (pc.GetRoleClass() is Dictator dictator)
                dictator.ExilePlayer(pc, $"/exp {pid}", true);
        }
    }

    private void DictatorOnClick(byte playerId, MeetingHud __instance)
    {
        Logger.Msg($"Click: ID {playerId}", "Dictator UI");
        var pc = playerId.GetPlayer();
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;

        if (AmongUsClient.Instance.AmHost) ExilePlayer(PlayerControl.LocalPlayer, $"/exp {playerId}");
        else SendDictatorRPC(playerId);

        CreateDictatorButton(__instance);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartMeetingPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer.GetRoleClass() is Dictator dictator)
                if (ChangeCommandToExpel.GetBool())
                    dictator.CreateDictatorButton(__instance);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class OnDestroyPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            foreach (var pva in __instance.playerStates)
            {
                if (pva.transform.Find("DictatorButton") != null)
                    UnityEngine.Object.Destroy(pva.transform.Find("DictatorButton").gameObject);
            }
        }
    }

    private void CreateDictatorButton(MeetingHud __instance)
    {
        foreach (var pva in __instance.playerStates)
        {
            if (pva.transform.Find("DictatorButton") != null) UnityEngine.Object.Destroy(pva.transform.Find("DictatorButton").gameObject);

            var pc = pva.TargetPlayerId.GetPlayer();
            var local = PlayerControl.LocalPlayer;
            if (pc == null || !pc.IsAlive()) continue;

            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "DictatorButton";
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            renderer.sprite = CustomButton.Get("JudgeIcon");

            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                DictatorOnClick(pva.TargetPlayerId, __instance);
            }));
        }
    }
}
