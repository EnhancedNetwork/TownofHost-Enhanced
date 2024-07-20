using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;


namespace TOHE.Roles.Neutral;

internal class Lawyer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Lawyer);
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem CanTargetImpostor;
    private static OptionItem CanTargetNeutralKiller;
    private static OptionItem CanTargetNeutralApoc;
    private static OptionItem CanTargetCrewmate;
    private static OptionItem CanTargetJester;
    private static OptionItem ShouldChangeRoleAfterTargetDeath;
    private static OptionItem ChangeRolesAfterTargetKilled;
    private static OptionItem KnowTargetRole;
    private static OptionItem TargetKnowsLawyer;

    public static readonly Dictionary<byte, byte> Target = [];
    private enum ChangeRolesSelectList
    {
        Role_Crewmate,
        Role_Jester,
        Role_Opportunist,
        Role_Celebrity,
        Role_Bodyguard,
        Role_Dictator,
        Role_Mayor,
        Role_Doctor
    }
    public static readonly CustomRoles[] CRoleChangeRoles =
    [
        CustomRoles.CrewmateTOHE,
        CustomRoles.Jester,
        CustomRoles.Opportunist,
        CustomRoles.Celebrity,
        CustomRoles.Bodyguard,
        CustomRoles.Dictator,
        CustomRoles.Mayor,
        CustomRoles.Doctor,
    ];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Lawyer);
        CanTargetImpostor = BooleanOptionItem.Create(Id + 10, "LawyerCanTargetImpostor", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetNeutralKiller = BooleanOptionItem.Create(Id + 11, "LawyerCanTargetNeutralKiller", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetNeutralApoc = BooleanOptionItem.Create(Id + 18, "LawyerCanTargetNeutralApocalypse", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetCrewmate = BooleanOptionItem.Create(Id + 12, "LawyerCanTargetCrewmate", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetJester = BooleanOptionItem.Create(Id + 13, "LawyerCanTargetJester", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        KnowTargetRole = BooleanOptionItem.Create(Id + 14, "KnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        TargetKnowsLawyer = BooleanOptionItem.Create(Id + 15, "TargetKnowsLawyer", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        ShouldChangeRoleAfterTargetDeath = BooleanOptionItem.Create(Id + 17, "LaywerShouldChangeRoleAfterTargetKilled", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        ChangeRolesAfterTargetKilled = StringOptionItem.Create(Id + 16, "LawyerChangeRolesAfterTargetKilled", EnumHelper.GetAllNames<ChangeRolesSelectList>(), 1, TabGroup.NeutralRoles, false).SetParent(ShouldChangeRoleAfterTargetDeath);
    }
    public override void Init()
    {
        Target.Clear();
    }
    public override void Add(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(OthersAfterPlayerDeathTask);

            List<PlayerControl> targetList = [];
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (!CanTargetImpostor.GetBool() && target.Is(Custom_Team.Impostor)) continue;
                else if (!CanTargetNeutralApoc.GetBool() && target.IsNeutralApocalypse()) continue;
                else if (!CanTargetNeutralKiller.GetBool() && target.IsNeutralKiller()) continue;
                else if (!CanTargetCrewmate.GetBool() && target.Is(Custom_Team.Crewmate)) continue;
                else if (!CanTargetJester.GetBool() && target.Is(CustomRoles.Jester)) continue;
                else if (target.Is(Custom_Team.Neutral) && !target.IsNeutralKiller() && !target.Is(CustomRoles.Jester) && !target.IsNeutralApocalypse()) continue;
                if (target.GetCustomRole() is CustomRoles.GM or CustomRoles.SuperStar or CustomRoles.NiceMini or CustomRoles.EvilMini) continue;
                if (Utils.GetPlayerById(playerId).Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }

            if (!targetList.Any())
            {
                Logger.Info($"Wow, not target for lawyer to select! Changing lawyer role to other", "Lawyer");
                // Unable to find a target? Try to turn to changerole or opportunist
                var changedRole = ShouldChangeRoleAfterTargetDeath.GetBool() ? CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()] : CustomRoles.Opportunist;
                var lawyer = Utils.GetPlayerById(playerId);
                if (lawyer.IsAlive())
                {
                    lawyer.GetRoleClass()?.OnRemove(playerId);
                    lawyer.RpcSetCustomRole(changedRole);
                    lawyer.GetRoleClass()?.OnAdd(playerId);
                }
                return;
            }

            var SelectedTarget = targetList.RandomElement();
            Target.Add(playerId, SelectedTarget.PlayerId);
            SendRPC(playerId, SelectedTarget.PlayerId, true);
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Lawyer");
        }
    }
    public override void Remove(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Remove(OthersAfterPlayerDeathTask);
            Target.Remove(playerId);
            SendRPC(playerId, SetTarget: false);
        }
    }
    private void SendRPC(byte lawyerId, byte targetId = 0x73, bool SetTarget = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(_Player);
        writer.Write(SetTarget);

        if (SetTarget)
        {
            writer.Write(lawyerId);
            writer.Write(targetId);
        }
        else
        {
            writer.Write(lawyerId);
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        bool SetTarget = reader.ReadBoolean();
        if (SetTarget)
        {
            byte LawyerId = reader.ReadByte();
            byte TargetId = reader.ReadByte();
            Target.Add(LawyerId, TargetId);
        }
        else
            Target.Remove(reader.ReadByte());
    }

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute)
        => ChangeRolesAfterTargetKilled.GetValue() is not 1 && !ForRecompute;

    private void OthersAfterPlayerDeathTask(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (!Target.ContainsValue(target.PlayerId)) return;

        byte lawyerId = 0x73;
        if (!ShouldChangeRoleAfterTargetDeath.GetBool())
        {
            Logger.Info($"Laywer target  dead {target.GetRealName()}, but change role setting is off", "Lawyer");
            return;
        }
        Target.Do(x =>
        {
            if (x.Value == target.PlayerId)
                lawyerId = x.Key;
        });

        if (lawyerId == 0x73) return;
        var lawyer = Utils.GetPlayerById(lawyerId);
        if (lawyer == null) return;

        // Change role
        lawyer.GetRoleClass()?.OnRemove(lawyerId);
        lawyer.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
        lawyer.GetRoleClass()?.OnAdd(lawyerId);

        if (inMeeting)
        {
            Utils.SendMessage(GetString("LawyerTargetDeadInMeeting"), sendTo: lawyerId);
        }
        else
        {
            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(lawyerId));
        }
    }
    public static bool TargetKnowLawyer => TargetKnowsLawyer.GetBool();
    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Lawyer) && Target.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (seer == null || target == null) return string.Empty;

        if (seer.Is(CustomRoles.Lawyer))
        {
            return Target.TryGetValue(seer.PlayerId, out var targetId) && targetId == target.PlayerId
                ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦") : string.Empty;
        }
        else if (seer.IsAlive() && TargetKnowsLawyer.GetBool())
        {
            return (Target.TryGetValue(target.PlayerId, out var x) && seer.PlayerId == x) ?
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦") : string.Empty;
        }
        else if (!seer.IsAlive() && Target.ContainsValue(target.PlayerId))
        {
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦");
        }

        return string.Empty;
    }
    public override void AfterMeetingTasks()
    {
        Target.Do(x =>
        {
            if (Main.PlayerStates[x.Value].IsDead)
            {
                var lawyer = Utils.GetPlayerById(x.Key);

                if (lawyer.IsAlive())
                {
                    ChangeRole(lawyer);
                }
            }
        });
    }
    private static void ChangeRole(PlayerControl lawyer)
    {
        // Called only in after meeting tasks when target death is impossible to check.
        if (!ShouldChangeRoleAfterTargetDeath.GetBool())
        {
            Logger.Info($"Laywer target dead, but change role setting is off", "Lawyer");
            return;
        }
        lawyer.GetRoleClass()?.OnRemove(lawyer.PlayerId);
        lawyer.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
        lawyer.GetRoleClass()?.OnAdd(lawyer.PlayerId);
        var text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), GetString(""));
        text = string.Format(text, Utils.ColorString(Utils.GetRoleColor(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]), GetString(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()].ToString())));
        lawyer.Notify(text);
    }
}