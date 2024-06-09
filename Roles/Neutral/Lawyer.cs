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
    private static OptionItem CanTargetCrewmate;
    private static OptionItem CanTargetJester;
    private static OptionItem ShouldChangeRoleAfterTargetDeath;
    private static OptionItem ChangeRolesAfterTargetKilled;
    private static OptionItem KnowTargetRole;
    private static OptionItem TargetKnowsLawyer;

    public static readonly Dictionary<byte, byte> Target = [];
    private enum ChangeRolesSelect
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
        CanTargetCrewmate = BooleanOptionItem.Create(Id + 12, "LawyerCanTargetCrewmate", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetJester = BooleanOptionItem.Create(Id + 13, "LawyerCanTargetJester", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        KnowTargetRole = BooleanOptionItem.Create(Id + 14, "KnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        TargetKnowsLawyer = BooleanOptionItem.Create(Id + 15, "TargetKnowsLawyer", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        ShouldChangeRoleAfterTargetDeath = BooleanOptionItem.Create(Id + 17, "LaywerShouldChangeRoleAfterTargetKilled", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        ChangeRolesAfterTargetKilled = StringOptionItem.Create(Id + 16, "LawyerChangeRolesAfterTargetKilled", EnumHelper.GetAllNames<ChangeRolesSelect>(), 1, TabGroup.NeutralRoles, false).SetParent(ShouldChangeRoleAfterTargetDeath);
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
                else if (!CanTargetNeutralKiller.GetBool() && target.IsNeutralKiller()) continue;
                else if (!CanTargetCrewmate.GetBool() && target.Is(Custom_Team.Crewmate)) continue;
                else if (!CanTargetJester.GetBool() && target.Is(CustomRoles.Jester)) continue;
                else if (target.Is(Custom_Team.Neutral) && !target.IsNeutralKiller() && !target.Is(CustomRoles.Jester)) continue;
                if (target.GetCustomRole() is CustomRoles.GM or CustomRoles.SuperStar or CustomRoles.NiceMini or CustomRoles.EvilMini) continue;
                if (Utils.GetPlayerById(playerId).Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }

            if (!targetList.Any())
            {
                Logger.Info($"Wow, not target for lawyer to select! Changing lawyer role to other", "Lawyer");
                if (ShouldChangeRoleAfterTargetDeath.GetBool())
                {
                    Utils.GetPlayerById(playerId).RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
                }
                else Utils.GetPlayerById(playerId).RpcSetCustomRole(CustomRoles.Opportunist);
                return;
            }
            // Unable to find a target? Try to turn to changerole or opportunist

            var SelectedTarget = targetList.RandomElement();
            Target.Add(playerId, SelectedTarget.PlayerId);
            SendRPC(playerId, SelectedTarget.PlayerId, true);
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Lawyer");
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
            Target[LawyerId] = TargetId;
        }
        else
            Target.Remove(reader.ReadByte());
    }

    public override bool HasTasks(GameData.PlayerInfo player, CustomRoles role, bool ForRecompute)
        => !(ChangeRolesAfterTargetKilled.GetValue() is 1 or 2) && !ForRecompute;

    private void OthersAfterPlayerDeathTask(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (!Target.ContainsValue(target.PlayerId)) return;

        byte Lawyer = 0x73;
        if (!ShouldChangeRoleAfterTargetDeath.GetBool())
        {
            Logger.Info($"Laywer target  dead {target.GetRealName()}, but change role setting is off", "Lawyer");
            return;
        }
        Target.Do(x =>
        {
            if (x.Value == target.PlayerId)
                Lawyer = x.Key;
        });
        Utils.GetPlayerById(Lawyer)?.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
        Target.Remove(Lawyer);
        SendRPC(Lawyer, SetTarget: false);

        if (inMeeting)
        {
            Utils.SendMessage(GetString("LawyerTargetDeadInMeeting"), sendTo: Lawyer, replay: true);
        }
        else
        {
            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(Lawyer));
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

        if (!seer.Is(CustomRoles.Lawyer))
        {
            if (!TargetKnowsLawyer.GetBool()) return string.Empty;
            return (Target.TryGetValue(target.PlayerId, out var x) && seer.PlayerId == x) ?
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦") : string.Empty;
        }
        var GetValue = Target.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦") : string.Empty;
    }
    public override void AfterMeetingTasks()
    {
        Target.Do(x =>
        {
            if (Main.PlayerStates[x.Value].IsDead)
            {
                var lawyer = Utils.GetPlayerById(x.Key);

                if (lawyer != null && lawyer.IsAlive())
                {
                    ChangeRole(lawyer);
                }
            }
        });
    }
    private void ChangeRole(PlayerControl lawyer)
    {
        // Called only in after meeting tasks when target death is impossible to check.
        if (!ShouldChangeRoleAfterTargetDeath.GetBool())
        {
            Logger.Info($"Laywer target dead, but change role setting is off", "Lawyer");
            return;
        }
        lawyer.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
        Target.Remove(lawyer.PlayerId);
        SendRPC(lawyer.PlayerId, SetTarget: false);
        var text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), GetString(""));
        text = string.Format(text, Utils.ColorString(Utils.GetRoleColor(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]), GetString(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()].ToString())));
        lawyer.Notify(text);
    }
    /* 
    // Lonnie moment, makes the lawyer loose after death
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (Target.ContainsKey(target.PlayerId))
        {
            Target.Remove(target.PlayerId);
            SendRPC(target.PlayerId, SetTarget: false);
        }
    }
    */
    /*public static bool CheckExileTarget(GameData.PlayerInfo exiled, bool DecidedWinner, bool Check = false)
    {
        if (!HasEnabled) return false;

        foreach (var kvp in Target.Where(x => x.Value == exiled.PlayerId).ToArray())
        {
            var lawyer = Utils.GetPlayerById(kvp.Key);
            if (lawyer == null || lawyer.Data.Disconnected) continue;
            return true;
        }
        return false;
    }*/
}