using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
internal class Pixie : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Pixie;
    private const int Id = 25900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pirate);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem PixiePointsToWin;
    private static OptionItem PixieMaxTargets;
    private static OptionItem PixieMarkCD;
    private static OptionItem PixieSuicideOpt;

    private static readonly Dictionary<byte, HashSet<byte>> PixieTargets = [];
    private static readonly Dictionary<byte, int> PixiePoints = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pixie);
        PixiePointsToWin = IntegerOptionItem.Create(Id + 10, "PixiePointsToWin", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pixie])
            .SetValueFormat(OptionFormat.Times);
        PixieMaxTargets = IntegerOptionItem.Create(Id + 11, "MaxTargets", new(1, 14, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pixie])
            .SetValueFormat(OptionFormat.Players);
        PixieMarkCD = FloatOptionItem.Create(Id + 12, "MarkCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pixie])
            .SetValueFormat(OptionFormat.Seconds);
        PixieSuicideOpt = BooleanOptionItem.Create(Id + 13, "PixieSuicide", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pixie]);
    }
    public override void Init()
    {
        PixieTargets.Clear();
        PixiePoints.Clear();
    }

    public override void Add(byte playerId)
    {
        PixieTargets[playerId] = [];
        PixiePoints.Add(playerId, 0);
    }

    public override void Remove(byte playerId)
    {
        PixieTargets.Remove(playerId);
        PixiePoints.Remove(playerId);
    }
    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pixie).ShadeColor(0.25f), PixiePoints.TryGetValue(playerId, out var x) ? $"({x}/{PixiePointsToWin.GetInt()})" : "Invalid");

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = PixieMarkCD.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        HudManager.Instance.KillButton.OverrideText(GetString("PixieButtonText"));
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        string color = string.Empty;
        if (seer.Is(CustomRoles.Pixie) && PixieTargets[seer.PlayerId].Contains(target.PlayerId)) color = Main.roleColors[CustomRoles.Pixie];
        return color;
    }
    public void SendRPC(byte pixieId, bool operate, byte targetId = 0xff)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); //SetPixieTargets
        writer.Write(pixieId);
        writer.Write(operate);
        if (!operate) // false = 0
        {
            writer.Write(targetId);
        }
        else // true = 1
        {
            writer.Write(PixiePoints[pixieId]);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte pixieId = reader.ReadByte();
        bool operate = reader.ReadBoolean();
        if (!operate)
        {
            if (!PixieTargets.ContainsKey(pixieId)) PixieTargets[pixieId] = [];
            byte targetId = reader.ReadByte();
            PixieTargets[pixieId].Add(targetId);
        }
        else
        {
            int pts = reader.ReadInt32();
            if (!PixiePoints.ContainsKey(pixieId)) PixiePoints[pixieId] = 0;
            PixiePoints[pixieId] = pts;
            PixieTargets[pixieId].Clear();
        }
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        byte targetId = target.PlayerId;
        byte killerId = killer.PlayerId;
        if (!PixieTargets.ContainsKey(killerId)) PixieTargets[killerId] = [];
        if (PixieTargets[killerId].Count >= PixieMaxTargets.GetInt())
        {
            killer.Notify(GetString("PixieMaxTargetReached"));
            Logger.Info($"Max targets per round already reached, {PixieTargets[killerId].Count}/{PixieMaxTargets.GetInt()}", "Pixie");
            return false;
        }
        if (PixieTargets[killerId].Contains(targetId))
        {
            killer.Notify(GetString("PixieTargetAlreadySelected"));
            return false;
        }
        PixieTargets[killerId].Add(targetId);
        SendRPC(killerId, false, targetId);
        Utils.NotifyRoles(SpecifySeer: killer, ForceLoop: true);
        if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer);
        SetKillCooldown(killer.PlayerId);
        return false;
    }

    public override void OnPlayerExiled(PlayerControl pc, NetworkedPlayerInfo exiled)
    {
        byte pixieId = pc.PlayerId;
        if (PixieTargets.ContainsKey(pixieId))
        {
            if (exiled != null)
            {
                if (PixieTargets[pixieId].Count == 0) return;
                if (!PixiePoints.ContainsKey(pixieId)) PixiePoints[pixieId] = 0;
                if (PixiePoints[pixieId] >= PixiePointsToWin.GetInt()) return;

                if (PixieTargets[pixieId].Contains(exiled.PlayerId))
                {
                    PixiePoints[pixieId]++;
                }
                else if (PixieSuicideOpt.GetBool()
                    && PixieTargets[pixieId].Any(eid => Utils.GetPlayerById(eid)?.IsAlive() == true))
                {
                    pc.SetRealKiller(pc);
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pixieId);
                    Logger.Info($"{pc.GetNameWithRole()} committed suicide because target not exiled and target(s) were alive during ejection", "Pixie");
                }
            }
            PixieTargets[pixieId].Clear();
            SendRPC(pixieId, true);
        }
    }

    public static void PixieWinCondition(PlayerControl pc)
    {
        if (pc == null) return;
        if (PixiePoints.TryGetValue(pc.PlayerId, out int totalPts))
        {
            if (totalPts >= PixiePointsToWin.GetInt())
            {
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Pixie);
            }
        }
    }
}

