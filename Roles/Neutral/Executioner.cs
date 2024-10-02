using Hazel;
using System;
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
    private static OptionItem KnowTargetRole;
    private static OptionItem ChangeRolesAfterTargetKilled;
    public static OptionItem RevealExeTargetUponEjection;

    public static readonly Dictionary<byte, byte> Target = [];
    
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
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "KnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        ChangeRolesAfterTargetKilled = StringOptionItem.Create(Id + 11, "ExecutionerChangeRolesAfterTargetKilled", EnumHelper.GetAllNames<ChangeRolesSelectList>(), 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
        RevealExeTargetUponEjection = BooleanOptionItem.Create(Id + 17, "Executioner_RevealTargetUponEject", true, TabGroup.NeutralRoles, false) .SetParent(CustomRoleSpawnChances[CustomRoles.Executioner]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        Target.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(OnOthersDead);

            List<PlayerControl> targetList = [];
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (!CanTargetImpostor.GetBool() && target.Is(Custom_Team.Impostor)) continue;
                else if (!CanTargetNeutralKiller.GetBool() && target.GetCustomRole().IsNK()) continue;
                else if (!CanTargetNeutralBenign.GetBool() && target.GetCustomRole().IsNB()) continue;
                else if (!CanTargetNeutralEvil.GetBool() && target.GetCustomRole().IsNE()) continue;
                else if (!CanTargetNeutralChaos.GetBool() && target.GetCustomRole().IsNC()) continue;
                if (target.GetCustomRole() is CustomRoles.GM or CustomRoles.SuperStar or CustomRoles.NiceMini or CustomRoles.EvilMini) continue;
                if (Utils.GetPlayerById(playerId).Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }
            if (targetList.Any())
            {
                var SelectedTarget = targetList.RandomElement();
                Target.Add(playerId, SelectedTarget.PlayerId);
                SendRPC(playerId, SelectedTarget.PlayerId, "SetTarget");
                Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Executioner");
            }
            else
            {
                Logger.Warn(" Warning! No suitableable target was found for executioner, switching role","Executioner.Add");
                ChangeRole(Utils.GetPlayerById(playerId));
            }
        }
    }
    public static void SendRPC(byte executionerId, byte targetId = 0x73, string Progress = "")
    {
        MessageWriter writer;
        switch (Progress)
        {
            case "SetTarget":
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetExecutionerTarget, SendOption.Reliable);
                writer.Write(executionerId);
                writer.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                break;
            case "":
                if (!AmongUsClient.Instance.AmHost) return;
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveExecutionerTarget, SendOption.Reliable);
                writer.Write(executionerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                break;
            case "WinCheck":
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) break;
                if (!CustomWinnerHolder.CheckForConvertedWinner(executionerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Executioner);
                    CustomWinnerHolder.WinnerIds.Add(executionerId);
                }
                break;
        }
    }
    public static void ReceiveRPC(MessageReader reader, bool SetTarget)
    {
        if (SetTarget)
        {
            byte ExecutionerId = reader.ReadByte();
            byte TargetId = reader.ReadByte();
            Target[ExecutionerId] = TargetId;
        }
        else
            Target.Remove(reader.ReadByte());
    }
    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute)
        => !(ChangeRolesAfterTargetKilled.GetValue() is 6 or 7) && !ForRecompute;

    public static void ChangeRoleByTarget(PlayerControl target)
    {
        byte ExecutionerId = 0x73;
        Target.Do(x =>
        {
            if (x.Value == target.PlayerId)
                ExecutionerId = x.Key;
        });
        
        var Executioner = Utils.GetPlayerById(ExecutionerId);
        Executioner.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);

        playerIdList.Remove(ExecutionerId);
        Target.Remove(ExecutionerId);
        SendRPC(ExecutionerId);

        Executioner.GetRoleClass().OnAdd(ExecutionerId);
        Utils.NotifyRoles(SpecifySeer: Executioner);
    }
    public static void ChangeRole(PlayerControl executioner)
    {
        executioner.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]);

        playerIdList.Remove(executioner.PlayerId);
        Target.Remove(executioner.PlayerId);
        SendRPC(executioner.PlayerId);
        
        var text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), Translator.GetString(""));
        text = string.Format(text, Utils.ColorString(Utils.GetRoleColor(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()]), Translator.GetString(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetValue()].ToString())));
        executioner.Notify(text);
        
        try { executioner.GetRoleClass().OnAdd(executioner.PlayerId); } 
        catch (Exception err) 
        { Logger.Warn($"Error after attempting to RoleCLass.Add({executioner.GetCustomRole().ToString().RemoveHtmlTags() + ", " + executioner.GetRealName()}.PlayerId): {err}", "Executioner.ChangeRole.Add"); }
    }

    public static bool CheckTarget(byte targetId) => Target.ContainsValue(targetId);
    public static bool IsTarget(byte executionerId, byte targetId) => Target.TryGetValue(executionerId, out var exeTargetId) && exeTargetId == targetId;

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (Target.ContainsKey(target.PlayerId))
        {
            Target.Remove(target.PlayerId);
            SendRPC(target.PlayerId);
        }
    }
    private void OnOthersDead(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (CheckTarget(target.PlayerId))
            ChangeRoleByTarget(target);
    }

    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Executioner) && Target.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }

    public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (target == null || !seer.IsAlive()) return string.Empty;

        var GetValue = Target.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), "♦") : string.Empty;
    }

    public override void CheckExileTarget(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        foreach (var kvp in Target.Where(x => x.Value == exiled.PlayerId))
        {
            var executioner = Utils.GetPlayerById(kvp.Key);
            if (executioner == null || !executioner.IsAlive() || executioner.Data.Disconnected) continue;

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
                ExeWin(kvp.Key, DecidedWinner);
                DecidedWinner = true;
            }
        }
    }
    private static void ExeWin(byte playerId, bool DecidedWinner)
    {
        if (!DecidedWinner)
        {
            SendRPC(playerId, Progress: "WinCheck");
        }
        else
        {
            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Executioner);
            CustomWinnerHolder.WinnerIds.Add(playerId);
        }
    }
}
