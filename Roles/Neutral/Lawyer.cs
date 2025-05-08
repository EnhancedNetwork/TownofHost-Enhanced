using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;


namespace TOHE.Roles.Neutral;

internal class Lawyer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Lawyer;
    private const int Id = 13100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Lawyer);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem CanTargetImpostor;
    private static OptionItem CanTargetNeutralKiller;
    private static OptionItem CanTargetNeutralApoc;
    private static OptionItem CanTargetCoven;
    private static OptionItem CanTargetCrewmate;
    private static OptionItem CanTargetJester;
    private static OptionItem ShouldChangeRoleAfterTargetDeath;
    private static OptionItem ChangeRolesAfterTargetKilled;
    private static OptionItem KnowTargetRole;
    private static OptionItem TargetKnowsLawyer;
    private static OptionItem HasImpostorVision;

    public static HashSet<byte> TargetList = [];
    private byte TargetId;

    [Obfuscation(Exclude = true)]
    public static readonly List<string> ChangeRoles = new List<string>();
    public static readonly CustomRoles[] CRoleChangeRoles =
    [
        CustomRoles.CrewmateTOHE,
        CustomRoles.Amnesiac,
        CustomRoles.Jester,
        CustomRoles.Opportunist,
        CustomRoles.Celebrity,
        CustomRoles.Bodyguard,
        CustomRoles.Dictator,
        CustomRoles.Mayor,
        CustomRoles.Doctor,
        CustomRoles.Maverick,
        CustomRoles.Follower,
        CustomRoles.TrackerTOHE,
        CustomRoles.Mechanic,
        CustomRoles.Refugee,
        CustomRoles.Sheriff,
        CustomRoles.Medic,
    ];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Lawyer);
        CanTargetImpostor = BooleanOptionItem.Create(Id + 10, "CanTargetImpostor", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetNeutralKiller = BooleanOptionItem.Create(Id + 11, "CanTargetNeutralKiller", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetNeutralApoc = BooleanOptionItem.Create(Id + 18, "CanTargetNeutralApocalypse", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetCoven = BooleanOptionItem.Create(Id + 19, "CanTargetCoven", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetCrewmate = BooleanOptionItem.Create(Id + 12, "CanTargetCrewmate", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetJester = BooleanOptionItem.Create(Id + 13, "LawyerCanTargetJester", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        KnowTargetRole = BooleanOptionItem.Create(Id + 14, "KnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        TargetKnowsLawyer = BooleanOptionItem.Create(Id + 15, "TargetKnowsLawyer", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 20, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CRoleChangeRoles.ForEach(x => ChangeRoles.Add(x.ToColoredString()));
        ShouldChangeRoleAfterTargetDeath = BooleanOptionItem.Create(Id + 17, "LaywerShouldChangeRoleAfterTargetKilled", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        ChangeRolesAfterTargetKilled = StringOptionItem.Create(Id + 16, "LawyerChangeRolesAfterTargetKilled", ChangeRoles.ToArray(), 1, TabGroup.NeutralRoles, false, useGetString: false).SetParent(ShouldChangeRoleAfterTargetDeath);
    }
    public override void Init()
    {
        TargetId = byte.MaxValue;
        TargetList.Clear();
    }
    public override void Add(byte playerId)
    {
        var lawyer = _Player;
        if (AmongUsClient.Instance.AmHost && lawyer.IsAlive())
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(OthersAfterPlayerDeathTask);

            List<PlayerControl> targetList = [];
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (TargetList.Contains(target.PlayerId)) continue;
                else if (!CanTargetImpostor.GetBool() && target.Is(Custom_Team.Impostor)) continue;
                else if (!CanTargetNeutralApoc.GetBool() && target.IsNeutralApocalypse()) continue;
                else if (!CanTargetNeutralKiller.GetBool() && target.IsNeutralKiller()) continue;
                else if (!CanTargetCoven.GetBool() && target.Is(Custom_Team.Coven)) continue;
                else if (!CanTargetCrewmate.GetBool() && target.Is(Custom_Team.Crewmate)) continue;
                else if (!CanTargetJester.GetBool() && target.Is(CustomRoles.Jester)) continue;
                else if (target.Is(Custom_Team.Neutral) && !target.IsNeutralKiller() && !target.Is(CustomRoles.Jester) && !target.IsNeutralApocalypse()) continue;
                if (target.GetCustomRole() is CustomRoles.GM or CustomRoles.SuperStar or CustomRoles.NiceMini or CustomRoles.EvilMini) continue;
                if (lawyer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }

            if (targetList.Any())
            {
                var selectedTarget = targetList.RandomElement();
                TargetId = selectedTarget.PlayerId;
                TargetList.Add(selectedTarget.PlayerId);

                SendRPC(SetTarget: true);
                Logger.Info($"{lawyer?.GetNameWithRole()}:{selectedTarget.GetNameWithRole()}", "Lawyer");
            }
            else
            {
                Logger.Info($"Wow, not target for lawyer to select! Changing lawyer role to other", "Lawyer");

                // Unable to find a target? Try to turn to changerole or opportunist
                var changedRole = ShouldChangeRoleAfterTargetDeath.GetBool() ? CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()] : CustomRoles.Opportunist;

                lawyer.GetRoleClass()?.OnRemove(playerId);
                lawyer.RpcSetCustomRole(changedRole);
                lawyer.GetRoleClass()?.OnAdd(playerId);
            }
        }
    }
    public override void Remove(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            SendRPC(SetTarget: false);
        }
        TargetList.Remove(TargetId);
        TargetId = byte.MaxValue;
        CustomRoleManager.CheckDeadBodyOthers.Remove(OthersAfterPlayerDeathTask);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(HasImpostorVision.GetBool());
    private void SendRPC(bool SetTarget = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(_Player);
        writer.Write(SetTarget);
        writer.Write(TargetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
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

    private bool IsTarget(byte playerId) => TargetId == playerId;
    public byte GetTargetId() => TargetId;

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute)
        => !ForRecompute;

    private void OthersAfterPlayerDeathTask(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (_Player == null || !IsTarget(target.PlayerId)) return;
        ChangeRole(inMeeting);
    }
    public static bool TargetKnowLawyer => TargetKnowsLawyer.GetBool();
    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Lawyer) && IsTarget(target.PlayerId);
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (TargetId == byte.MaxValue) return string.Empty;

        if ((!seer.IsAlive() || seer.Is(CustomRoles.Lawyer)) && IsTarget(target.PlayerId))
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦");
        }
        else if (seer.IsAlive() && TargetKnowsLawyer.GetBool() && IsTarget(seer.PlayerId) && _state.PlayerId == target.PlayerId)
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦");
        }

        return string.Empty;
    }
    private void ChangeRole(bool inMeeting)
    {
        if (!ShouldChangeRoleAfterTargetDeath.GetBool())
        {
            Logger.Info($"Laywer target dead, but change role setting is off", "Lawyer");
            return;
        }

        var lawyer = _Player;
        var valueChanger = ChangeRolesAfterTargetKilled.GetValue();
        var newCustomRole = CRoleChangeRoles[valueChanger];

        if (lawyer.IsAlive())
            lawyer.RpcChangeRoleBasis(newCustomRole);

        lawyer.GetRoleClass()?.OnRemove(lawyer.PlayerId);
        lawyer.RpcSetCustomRole(newCustomRole);
        lawyer.GetRoleClass()?.OnAdd(lawyer.PlayerId);

        switch (newCustomRole)
        {
            case CustomRoles.Amnesiac:
                Main.PlayerStates[lawyer.PlayerId].RemoveSubRole(CustomRoles.Oblivious);
                break;
            case CustomRoles.Celebrity:
                Main.PlayerStates[lawyer.PlayerId].RemoveSubRole(CustomRoles.Cyber);
                break;
            case CustomRoles.Dictator:
                new[] { CustomRoles.Tiebreaker, CustomRoles.Paranoia, CustomRoles.Knighted, CustomRoles.VoidBallot, CustomRoles.Silent, CustomRoles.Influenced }.Do(x => Main.PlayerStates[lawyer.PlayerId].RemoveSubRole(x));
                break;
            case CustomRoles.Mayor:
                new[] { CustomRoles.Knighted, CustomRoles.VoidBallot }.Do(x => Main.PlayerStates[lawyer.PlayerId].RemoveSubRole(x));
                break;
            case CustomRoles.Doctor:
                new[] { CustomRoles.Autopsy, CustomRoles.Necroview }.Do(x => Main.PlayerStates[lawyer.PlayerId].RemoveSubRole(x));
                break;
            case CustomRoles.Jester:
                new[] { CustomRoles.Rebirth, CustomRoles.Susceptible }.Do(x => Main.PlayerStates[lawyer.PlayerId].RemoveSubRole(x));
                break;
            case CustomRoles.Opportunist when Opportunist.OppoImmuneToAttacksWhenTasksDone.GetBool():
            case CustomRoles.Medic:
                Main.PlayerStates[lawyer.PlayerId].RemoveSubRole(CustomRoles.Fragile);
                break;
            case CustomRoles.Mechanic:
                Main.PlayerStates[lawyer.PlayerId].RemoveSubRole(CustomRoles.Fool);
                break;
            case CustomRoles.Refugee:
                Main.PlayerStates[lawyer.PlayerId].RemoveSubRole(CustomRoles.Madmate);
                break;
        }

        if (inMeeting)
        {
            Utils.SendMessage(GetString("LawyerTargetDeadInMeeting"), sendTo: lawyer.PlayerId);
        }
        else
        {
            Utils.NotifyRoles(SpecifySeer: lawyer);
        }
    }
}
