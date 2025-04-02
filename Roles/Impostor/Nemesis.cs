using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Nemesis : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Nemesis;
    private const int Id = 3600;
    public override CustomRoles ThisRoleBase => LegacyNemesis.GetBool() ? CustomRoles.Shapeshifter : CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem NemesisCanKillNum;
    public static OptionItem PreventSeeRolesBeforeSkillUsedUp;
    public static OptionItem LegacyNemesis;
    private static OptionItem NemesisShapeshiftCD;
    private static OptionItem NemesisShapeshiftDur;

    private static readonly Dictionary<byte, int> NemesisRevenged = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Nemesis);
        NemesisCanKillNum = IntegerOptionItem.Create(Id + 10, "NemesisCanKillNum", new(0, 15, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis])
                .SetValueFormat(OptionFormat.Players);
        PreventSeeRolesBeforeSkillUsedUp = BooleanOptionItem.Create(Id + 14, "PreventSeeRolesBeforeSkillUsedUp", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis]);
        LegacyNemesis = BooleanOptionItem.Create(Id + 11, "UseLegacyVersion", false, TabGroup.ImpostorRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Nemesis]);
        NemesisShapeshiftCD = FloatOptionItem.Create(Id + 12, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(1f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false)
                .SetParent(LegacyNemesis)
                .SetValueFormat(OptionFormat.Seconds);
        NemesisShapeshiftDur = FloatOptionItem.Create(Id + 13, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false)
                .SetParent(LegacyNemesis)
                .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        NemesisRevenged.Clear();
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = NemesisShapeshiftCD.GetFloat();
        AURoleOptions.ShapeshifterDuration = NemesisShapeshiftDur.GetFloat();
    }
    public static bool PreventKnowRole(PlayerControl seer)
    {
        if (!seer.Is(CustomRoles.Nemesis) || seer.IsAlive()) return false;
        if (PreventSeeRolesBeforeSkillUsedUp.GetBool() && NemesisRevenged.TryGetValue(seer.PlayerId, out var killNum) && killNum < NemesisCanKillNum.GetInt())
            return true;
        return false;
    }
    public override void OnMeetingHudStart(PlayerControl player)
    {
        if (!player.IsAlive())
            AddMsg(GetString("NemesisDeadMsg"), player.PlayerId);
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
            pc.ShowInfoMessage(isUI, GetString("NemesisKillDisable"));
            return true;
        }

        if (pc.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("NemesisAliveKill"));
            return true;
        }

        if (msg == "/rv")
        {
            bool canSeeRoles = PreventSeeRolesBeforeSkillUsedUp.GetBool();
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += $"\n{npc.PlayerId} → " + (canSeeRoles ? $"({npc.GetDisplayRoleAndSubName(npc, false, false)}) " : string.Empty) + npc.GetRealName();
            Utils.SendMessage(text, pc.PlayerId);
            return true;
        }

        if (NemesisRevenged.TryGetValue(pc.PlayerId, out var killNum) && killNum >= NemesisCanKillNum.GetInt())
        {
            pc.ShowInfoMessage(isUI, GetString("NemesisKillMax"));
            return true;
        }

        else
        {
            NemesisRevenged.Add(pc.PlayerId, 0);
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
            pc.ShowInfoMessage(isUI, GetString("NemesisKillDead"));
            return true;
        }

        if (target == null || !target.IsAlive())
        {
            pc.ShowInfoMessage(isUI, GetString("NemesisKillDead"));
            return true;
        }
        else if (target.IsTransformedNeutralApocalypse())
        {
            pc.ShowInfoMessage(isUI, GetString("ApocalypseImmune"));
            return true;
        }
        else if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            pc.ShowInfoMessage(isUI, GetString("GuessMini"));
            return true;
        }
        else if (target.Is(CustomRoles.Solsticer))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessSolsticer"));
            return true;
        }
        else if (target.Is(CustomRoles.Jinx) || target.Is(CustomRoles.CursedWolf))
        {
            pc.ShowInfoMessage(isUI, GetString("GuessImmune"));
            return true;
        }
        else if (pc.RpcCheckAndMurder(target, true) == false)
        {
            pc.ShowInfoMessage(isUI, GetString("GuessImmune"));
            Logger.Info($"Guess Immune target {target.PlayerId} have role {target.GetCustomRole()}", "Nemesis");
            return true;
        }

        Logger.Info($"{pc.GetNameWithRole()} revenge {target.GetNameWithRole()}", "Nemesis");

        string Name = target.GetRealName();

        NemesisRevenged[pc.PlayerId]++;

        CustomSoundsManager.RPCPlayCustomSoundAll("AWP");

        _ = new LateTask(() =>
        {
            target.SetDeathReason(PlayerState.DeathReason.Revenge);
            if (GameStates.IsMeeting)
            {
                Main.PlayersDiedInMeeting.Add(target.PlayerId);
                GuessManager.RpcGuesserMurderPlayer(target);
                MurderPlayerPatch.AfterPlayerDeathTasks(pc, target, true);
            }
            else
            {
                target.RpcMurderPlayer(target);
            }
            target.SetRealKiller(pc);

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
    public static void ReceiveRPC_Custom(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        NemesisMsgCheck(pc, $"/rv {PlayerId}", true);
    }

    public override bool CanUseKillButton(PlayerControl pc) => CheckCanUseKillButton();

    public static bool CheckCanUseKillButton()
    {
        if (Main.PlayerStates == null) return false;

        //  Number of Living Impostors excluding Nemesis
        int LivingImpostorsNum = 0;
        foreach (var player in Main.AllAlivePlayerControls)
        {
            var role = player.GetCustomRole();
            if (role != CustomRoles.Nemesis && role.IsImpostor()) LivingImpostorsNum++;
        }

        return LivingImpostorsNum <= 0;
    }

    private static void NemesisOnClick(byte playerId /*, MeetingHud __instance*/)
    {
        Logger.Msg($"Click: ID {playerId}", "Nemesis UI");
        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || !GameStates.IsVoting) return;
        if (AmongUsClient.Instance.AmHost) NemesisMsgCheck(PlayerControl.LocalPlayer, $"/rv {playerId}", true);
        else SendRPC(playerId);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (!seer.IsAlive() && seen.IsAlive())
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Nemesis), " " + seen.PlayerId.ToString()) + " ";

        return string.Empty;
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
            targetBox.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.Get("MeetingKillButton");
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => NemesisOnClick(pva.TargetPlayerId/*, __instance*/)));
        }
    }
}
