using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Executioner : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14200;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem CanTargetImpostor;
    private static OptionItem CanTargetNeutralKiller;
    private static OptionItem CanTargetNeutralBenign;
    private static OptionItem CanTargetNeutralEvil;
    private static OptionItem CanTargetNeutralChaos;
    private static OptionItem CanTargetNeutralApocalypse;
    private static OptionItem KnowTargetRole;
    private static OptionItem ChangeRolesAfterTargetKilled;
    private static OptionItem RevealExeTargetUponEjection;

    public static HashSet<byte> TargetList = [];
    private byte TargetId;

    private enum ChangeRolesSelectList
    {
        Role_Crewmate,
        Role_Celebrity,
        Role_Bodyguard,
        Role_Dictator,
        Role_Mayor,
        Role_Doctor,
        Role_Jester,
        Role_Opportunist
    }
    public static readonly CustomRoles[] CRoleChangeRoles =
    [
        CustomRoles.CrewmateTOHE,
        CustomRoles.Celebrity,
        CustomRoles.Bodyguard,
        CustomRoles.Dictator,
        CustomRoles.Mayor,
        CustomRoles.Doctor,
        CustomRoles.Jester,
        CustomRoles.Opportunist,
    ];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Executioner);
        CanTargetImpostor = BooleanOptionItem.Create(Id + 10, "ExecutionerCanTargetImpostor", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        CanTargetNeutralKiller = BooleanOptionItem.Create(Id + 12, "ExecutionerCanTargetNeutralKiller", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        CanTargetNeutralBenign = BooleanOptionItem.Create(Id + 14, "ExecutionerCanTargetNeutralBenign", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        CanTargetNeutralEvil = BooleanOptionItem.Create(Id + 15, "ExecutionerCanTargetNeutralEvil", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        CanTargetNeutralChaos = BooleanOptionItem.Create(Id + 16, "ExecutionerCanTargetNeutralChaos", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        CanTargetNeutralApocalypse = BooleanOptionItem.Create(Id + 17, "ExecutionerCanTargetNeutralApocalypse", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "KnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        ChangeRolesAfterTargetKilled = StringOptionItem.Create(Id + 11, "ExecutionerChangeRolesAfterTargetKilled", EnumHelper.GetAllNames<ChangeRolesSelectList>(), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        RevealExeTargetUponEjection = BooleanOptionItem.Create(Id + 18, "Executioner_RevealTargetUponEject", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        TargetList.Clear();
        TargetId = byte.MaxValue;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        CustomRoleManager.CheckDeadBodyOthers.Add(OnOthersDead);

        var executioner = _Player;
        if (AmongUsClient.Instance.AmHost && executioner.IsAlive())
        {
            List<PlayerControl> targetList = [];
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (!CanTargetImpostor.GetBool() && target.Is(Custom_Team.Impostor)) continue;
                else if (!CanTargetNeutralApocalypse.GetBool() && target.GetCustomRole().IsNA()) continue;
                else if (!CanTargetNeutralKiller.GetBool() && target.GetCustomRole().IsNK()) continue;
                else if (!CanTargetNeutralBenign.GetBool() && target.GetCustomRole().IsNB()) continue;
                else if (!CanTargetNeutralEvil.GetBool() && target.GetCustomRole().IsNE()) continue;
                else if (!CanTargetNeutralChaos.GetBool() && target.GetCustomRole().IsNC()) continue;
                if (target.GetCustomRole() is CustomRoles.GM or CustomRoles.SuperStar or CustomRoles.NiceMini or CustomRoles.EvilMini) continue;
                if (executioner.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }
            if (targetList.Any())
            {
                var selectedTarget = targetList.RandomElement();
                TargetId = selectedTarget.PlayerId;
                TargetList.Add(selectedTarget.PlayerId);

                SendRPC(SetTarget: true);
                Logger.Info($"{executioner?.GetNameWithRole()}:{selectedTarget.GetNameWithRole()}", "Executioner");
            }
            else
            {
                Logger.Warn("Warning! No suitableable target was found for executioner, switching role","Executioner.Add");
                ChangeRole();
            }
        }
    }
    public override void Remove(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            SendRPC(SetTarget: false);
        }
        playerIdList.Remove(playerId);
        TargetList.Remove(TargetId);
        TargetId = byte.MaxValue;
    }
    private void SendRPC(bool SetTarget = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(_Player);
        writer.Write(SetTarget);
        writer.Write(TargetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        bool SetTarget = reader.ReadBoolean();
        byte targetId = reader.ReadByte();
        if (SetTarget)
        {
            TargetId = targetId;
            TargetList.Add(targetId);
        }
        else
        {
            TargetId = byte.MaxValue;
            TargetList.Remove(targetId);
        }
    }

    public bool IsTarget(byte playerId) => TargetId == playerId;
    public byte GetTargetId() => TargetId;

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute)
        => !(ChangeRolesAfterTargetKilled.GetValue() is 6 or 7) && !ForRecompute;

    private void ChangeRole()
    {
        if (!_Player.IsAlive()) return;

        var executioner = _Player;
        var executionerId = _Player.PlayerId;

        SendRPC(SetTarget: false);
        var valueChanger = ChangeRolesAfterTargetKilled.GetValue();
        var newCustomRole = CRoleChangeRoles[valueChanger];

        if (executioner.IsAlive())
            executioner.RpcChangeRoleBasis(newCustomRole);

        executioner.GetRoleClass()?.OnRemove(executionerId);
        executioner.RpcSetCustomRole(newCustomRole);
        executioner.GetRoleClass().OnAdd(executionerId);

        Utils.NotifyRoles(SpecifySeer: executioner);
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (_Player != null && _Player.PlayerId == target.PlayerId)
        {
            SendRPC(SetTarget: false);
            TargetList.Remove(TargetId);
            TargetId = byte.MaxValue;
        }
    }
    private void OnOthersDead(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (IsTarget(target.PlayerId))
            ChangeRole();
    }

    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Executioner) && IsTarget(target.PlayerId);
    }
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if ((!seer.IsAlive() || seer.Is(CustomRoles.Executioner)) && IsTarget(target.PlayerId))
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), "♦");
        }
        return string.Empty;
    }

    public override void CheckExileTarget(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        if (!_Player.IsAlive() || !IsTarget(exiled.PlayerId)) return;

        if (isMeetingHud)
        {
            if (RevealExeTargetUponEjection.GetBool())
            {
                name = string.Format(Translator.GetString("ExiledExeTarget"), Main.LastVotedPlayer, Utils.GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, true));
                DecidedWinner = true;
            }
        }
        else
        {
            ExeWin(_Player.PlayerId, DecidedWinner);
            DecidedWinner = true;
        }
    }
    private static void ExeWin(byte executionerId, bool DecidedWinner)
    {
        if (!DecidedWinner && CustomWinnerHolder.WinnerTeam == CustomWinner.Default)
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(executionerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Executioner);
                CustomWinnerHolder.WinnerIds.Add(executionerId);
            }
        }
        else
        {
            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Executioner);
            CustomWinnerHolder.WinnerIds.Add(executionerId);
        }
    }
}