using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using static Il2CppSystem.Globalization.CultureInfo;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Lawyer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13100;
    private static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\

    private static OptionItem CanTargetImpostor;
    private static OptionItem CanTargetNeutralKiller;
    private static OptionItem CanTargetCrewmate;
    private static OptionItem CanTargetJester;
    public static OptionItem ShouldChangeRoleAfterTargetDeath;
    public static OptionItem ChangeRolesAfterTargetKilled;
    public static OptionItem KnowTargetRole;
    public static OptionItem TargetKnowsLawyer;

    public static Dictionary<byte, byte> Target = [];
    public static readonly string[] ChangeRoles =
    [
        "Role.Crewmate",
        "Role.Jester",
        "Role.Opportunist",
        "Role.Convict",
        "Role.Celebrity",
        "Role.Bodyguard",
        "Role.Dictator",
        "Role.Mayor",
        "Role.Doctor",
    ];
    public static readonly CustomRoles[] CRoleChangeRoles =
    [
        CustomRoles.CrewmateTOHE, CustomRoles.Jester, CustomRoles.Opportunist, CustomRoles.Convict, CustomRoles.Celebrity, CustomRoles.Bodyguard, CustomRoles.Dictator, CustomRoles.Mayor, CustomRoles.Doctor,
    ];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Lawyer);
        CanTargetImpostor = BooleanOptionItem.Create(Id + 10, "LawyerCanTargetImpostor", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetNeutralKiller = BooleanOptionItem.Create(Id + 11, "LawyerCanTargetNeutralKiller", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetCrewmate = BooleanOptionItem.Create(Id + 12, "LawyerCanTargetCrewmate", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        CanTargetJester = BooleanOptionItem.Create(Id + 13, "LawyerCanTargetJester", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        KnowTargetRole = BooleanOptionItem.Create(Id + 14, "KnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        TargetKnowsLawyer = BooleanOptionItem.Create(Id + 15, "TargetKnowsLawyer", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        ShouldChangeRoleAfterTargetDeath = BooleanOptionItem.Create(Id + 17, "LaywerShouldChangeRoleAfterTargetKilled", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lawyer]);
        ChangeRolesAfterTargetKilled = StringOptionItem.Create(Id + 16, "LawyerChangeRolesAfterTargetKilled", ChangeRoles, 1, TabGroup.NeutralRoles, false).SetParent(ShouldChangeRoleAfterTargetDeath);
    }
    public override void Init()
    {
        playerIdList.Clear();
        Target.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CustomRoleManager.MarkOthers.Add(LawyerMark);
        CustomRoleManager.OthersAfterDeathTask.Add(ChangeRoleByTarget);

        if (AmongUsClient.Instance.AmHost)
        {
            List<PlayerControl> targetList = [];
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (!CanTargetImpostor.GetBool() && target.Is(CustomRoleTypes.Impostor)) continue;
                else if (!CanTargetNeutralKiller.GetBool() && target.IsNeutralKiller()) continue;
                else if (!CanTargetCrewmate.GetBool() && target.Is(CustomRoleTypes.Crewmate)) continue;
                else if (!CanTargetJester.GetBool() && target.Is(CustomRoles.Jester)) continue;
                else if (target.Is(CustomRoleTypes.Neutral) && !target.IsNeutralKiller() && !target.Is(CustomRoles.Jester)) continue;
                if (target.GetCustomRole() is CustomRoles.GM or CustomRoles.SuperStar or CustomRoles.NiceMini or CustomRoles.EvilMini) continue;
                if (Utils.GetPlayerById(playerId).Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }

            if (targetList.Count < 1)
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

            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            Target.Add(playerId, SelectedTarget.PlayerId);
            SendRPC(playerId, SelectedTarget.PlayerId, true);
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Lawyer");
        }
    }
    public static void SendRPC(byte lawyerId, byte targetId = 0x73, bool SetTarget = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WritePacked((int)CustomRoles.Lawyer);
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
    public static void ReceiveRPC(MessageReader reader)
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
    public static void ChangeRoleByTarget(PlayerControl target)
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
        Utils.GetPlayerById(Lawyer).RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);
        Target.Remove(Lawyer);
        SendRPC(Lawyer, SetTarget: false);

        if (GameStates.IsMeeting)
        {
            Utils.SendMessage(GetString("LawyerTargetDeadInMeeting"), sendTo: Lawyer, replay: true);
        }
        else
        {
            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(Lawyer));
        }
    }
    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Lawyer) && Target.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }
    private static string LawyerMark(PlayerControl seer, PlayerControl target, bool IsForMeeting = false)
    {
        if (!seer.Is(CustomRoles.Lawyer))
        {
            if (!TargetKnowsLawyer.GetBool()) return "";
            return (Target.TryGetValue(target.PlayerId, out var x) && seer.PlayerId == x) ?
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦") : "";
        }
        var GetValue = Target.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lawyer), "♦") : "";
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
    private static void ChangeRole(PlayerControl lawyer)
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
        text = string.Format(text, Utils.ColorString(Utils.GetRoleColor(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]), Translator.GetString(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()].ToString())));
        lawyer.Notify(text);
    }
    public override void AfterPlayerDeathTask(PlayerControl target)
    {
        if (Target.ContainsKey(target.PlayerId))
        {
            Target.Remove(target.PlayerId);
            SendRPC(target.PlayerId, SetTarget: false);
        }
    }
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