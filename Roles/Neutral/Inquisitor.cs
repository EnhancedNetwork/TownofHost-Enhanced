using TOHE.Roles.Neutral;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;

using static TOHE.Translator;
using Hazel;
using UnityEngine;
using System;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;

namespace TOHE.Roles.Neutral;

internal class Inquisitor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Inquisitor;
    private const int Id = 32000;
    public override bool IsExperimental => true;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem OptionHereticCount;
    private static OptionItem OptionDiesOnMiss;

    private static int hereticCount;
    private static bool diesOnMiss;

    private static readonly Dictionary<byte, HashSet<byte>> Targets = [];
    private static readonly Dictionary<byte, HashSet<byte>> InquiredList = [];
    private static readonly Dictionary<byte, CustomRoles> TargetRoles = [];
    private static readonly Dictionary<byte, int> AliveHeretics = [];
    private static readonly HashSet<byte> HaveMissed = [];

    private float CurrentKillCooldown;

    public static readonly CustomRoles[] NotTargetable = [
        CustomRoles.Sunnyboy,
        CustomRoles.PunchingBag,
        CustomRoles.Solsticer,
        CustomRoles.GM,
        CustomRoles.SuperStar,
        CustomRoles.NiceMini,
        CustomRoles.EvilMini
    ];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Inquisitor);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 2.5f), 15f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Inquisitor])
            .SetValueFormat(OptionFormat.Seconds);
        OptionHereticCount = IntegerOptionItem.Create(Id + 11, "InquisitorHereticCount", new(1, 5, 1), 3, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Inquisitor]);
        OptionDiesOnMiss = BooleanOptionItem.Create(Id + 12, "InquisitorDiesOnMiss", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Inquisitor]);
    }

    public override void Init()
    {
        Targets.Clear();
        InquiredList.Clear();
        TargetRoles.Clear();
        AliveHeretics.Clear();
        HaveMissed.Clear();
    }
    public override void Add(byte playerId)
    {
        CurrentKillCooldown = KillCooldown.GetFloat();
        hereticCount = OptionHereticCount.GetInt();
        diesOnMiss = OptionDiesOnMiss.GetBool();

        var inquisitor = _Player;
        Targets[inquisitor.PlayerId] = [];
        InquiredList[inquisitor.PlayerId] = [];

        inquisitor.AddDoubleTrigger();

        if (AmongUsClient.Instance.AmHost && inquisitor.IsAlive())
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(OthersAfterPlayerDeathTask);

            List<PlayerControl> targetList = [];
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (Targets[inquisitor.PlayerId].Contains(target.PlayerId)) continue;
                if (NotTargetable.Contains(target.GetCustomRole())) continue;
                if (inquisitor.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;

                targetList.Add(target);
            }

            if (targetList.Any())
            {
                var selectedTarget = targetList.RandomElement();
                Targets[inquisitor.PlayerId].Add(selectedTarget.PlayerId);
                TargetRoles[selectedTarget.PlayerId] = selectedTarget.GetCustomRole();

                HashSet<Custom_Team> teams = [selectedTarget.GetCustomRole().GetCustomRoleTeam()];
                int teamFound = 0;
                for (int i = 1; i < hereticCount; i++)
                {
                    if (targetList.Count + teamFound <= i) break;

                    selectedTarget = targetList.RandomElement();

                    if (Targets[inquisitor.PlayerId].Contains(selectedTarget.PlayerId))
                    {
                        i--;
                        continue;
                    }

                    if (teams.Count == 1 && teams.Contains(selectedTarget.GetCustomRole().GetCustomRoleTeam()))
                    {
                        teamFound += 1;
                        i--;
                        continue;
                    }

                    Targets[inquisitor.PlayerId].Add(selectedTarget.PlayerId);
                    TargetRoles[selectedTarget.PlayerId] = selectedTarget.GetCustomRole();

                    teams.Add(selectedTarget.GetCustomRole().GetCustomRoleTeam());

                    if (teams.Count > 1) teamFound = 0;
                }

                AliveHeretics[inquisitor.PlayerId] = Targets[inquisitor.PlayerId].Count;

                Logger.Info($"{inquisitor?.GetNameWithRole()}:{Targets[inquisitor.PlayerId].Select(id => TargetRoles[id])}", "Inquisitor");
            }
            else
            {
                Logger.Info($"Wow, no targets for inquisitor to select! Changing inquisitor role to other", "Inquisitor");

                // Unable to find a target? Try to turn to opportunist
                var changedRole = CustomRoles.Opportunist;

                inquisitor.GetRoleClass()?.OnRemove(playerId);
                inquisitor.RpcSetCustomRole(changedRole);
                inquisitor.GetRoleClass()?.OnAdd(playerId);
            }
        }
    }
    public override void Remove(byte playerId)
    {
        Targets.Remove(_Player.PlayerId);
        CustomRoleManager.CheckDeadBodyOthers.Remove(OthersAfterPlayerDeathTask);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsUseKillButton(Utils.GetPlayerById(id)) ? CurrentKillCooldown : 300f;

    public override bool CanUseKillButton(PlayerControl pc) => IsUseKillButton(pc);
    public static bool IsUseKillButton(PlayerControl pc)
        => !HaveMissed.Contains(pc.PlayerId);

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckDoubleTrigger(target, () => { CheckIfHeretic(killer, target); }))
        {
            if (IsTarget(target))
            {
                killer.SetKillCooldown();
                target.SetDeathReason(PlayerState.DeathReason.Torched);
                return true;
            }

            if (diesOnMiss)
            {
                killer.SetDeathReason(PlayerState.DeathReason.Misfire);
                killer.RpcMurderPlayer(killer);
                return true;
            }
            else
            {
                HaveMissed.Add(killer.PlayerId);
                killer.SetKillCooldown();
                return true;
            }
        }
        else return false;
    }

    public static bool CanSeeIsHeretic(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return false;
        if (!InquiredList.TryGetValue(player.PlayerId, out var targetList)) return false;
        if (targetList.Contains(target.PlayerId)) return true;

        return false;
    }

    public static void CheckIfHeretic(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return;
        if (!InquiredList.TryGetValue(player.PlayerId, out var targetList)) return;
        if (targetList.Contains(target.PlayerId)) return;

        InquiredList[player.PlayerId].Add(target.PlayerId);

        SendRPC(player.PlayerId, target.PlayerId);

        Logger.Info($"{player.GetNameWithRole()}：{target.GetNameWithRole()}", "Inquisitor");
        Utils.NotifyRoles(SpecifySeer: player, SpecifyTarget: target, ForceLoop: true);

        player.SetKillCooldown();
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        if (seer == null || target == null) return string.Empty;
        if (!InquiredList.TryGetValue(seer.PlayerId, out var targetList)) return string.Empty;
        if (!targetList.Contains(target.PlayerId)) return string.Empty;

        if (Illusionist.IsCovIllusioned(target.PlayerId)) return "#8CFFFF";
        if (Illusionist.IsNonCovIllusioned(target.PlayerId) || IsTarget(target)) return "#a21b16ff";
        else return "#8CFFFF";
    }

    private static void SendRPC(byte playerId, byte targetId)
    {
        var msg = new RpcSetInquisitor(PlayerControl.LocalPlayer.NetId, playerId, targetId);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();

        if (InquiredList.ContainsKey(playerId))
            InquiredList[playerId].Add(reader.ReadByte());
        else
            InquiredList.Add(playerId, []);
    }

    private bool IsTarget(PlayerControl player) => IsTarget(player.PlayerId);
    private bool IsTarget(byte playerId) => Targets[_Player.PlayerId].Contains(playerId);
    public HashSet<byte> GetTargetIds() => Targets[_Player.PlayerId];

    private void OthersAfterPlayerDeathTask(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (_Player == null || !IsTarget(target.PlayerId)) return;

        AliveHeretics[_Player.PlayerId]--;

        CheckWin();

        if (inMeeting)
        {
            Utils.SendMessage(GetString("InquisitorHereticDeadInMeeting"), sendTo: _Player.PlayerId);
        }
        else
        {
            Utils.NotifyRoles(SpecifySeer: _Player);
        }
    }

    private void CheckWin()
    {
        var inquisitorId = _Player.PlayerId;

        if (AliveHeretics[_Player.PlayerId] <= 0)
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(inquisitorId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Inquisitor);
                CustomWinnerHolder.WinnerIds.Add(inquisitorId);
            }
        }
    }

    private static List<string> GetTargetsRoles(PlayerControl player)
    {
        if (player == null) return [];

        return [.. Targets[player.PlayerId].Select(target => TargetRoles[target].GetColoredTextByRole(TargetRoles[target].GetActualRoleName()))];
    }
    private static int GetHereticsLeft(PlayerControl player)
    {
        if (player == null) return hereticCount;

        return AliveHeretics[player.PlayerId];
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting) return string.Empty;

        string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";

        List<string> targetRoles = GetTargetsRoles(seer);
        int hereticsLeft = GetHereticsLeft(seer);
        return targetRoles.Count > 0 ? CustomRoles.Inquisitor.GetColoredTextByRole($"{string.Format(GetString("InquisitorTargets"), string.Join(separator, targetRoles))}: {string.Format(GetString("InquisitorTargetsLeft"), hereticsLeft, hereticCount)}") : string.Empty;
    }

    public static void CheckHereticRevived()
    {
        // TODO: This is for the edge case where a heretic is revived.
    }
}