using Hazel;
using System.Text;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

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
    }

    public override void Add(byte playerId)
    {
        PixieTargets[playerId] = [];
        playerId.SetAbilityUseLimit(0);
    }
    public override void Remove(byte playerId)
    {
        PixieTargets.Remove(playerId);
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        Color TextColor = GetRoleColor(CustomRoles.Pixie).ShadeColor(0.25f);

        ProgressText.Append(ColorString(TextColor, $"({playerId.GetAbilityUseLimit()}/{PixiePointsToWin.GetInt()})"));
        return ProgressText.ToString();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = PixieMarkCD.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("MarkButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Mark");

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        string color = string.Empty;
        if (seer.Is(CustomRoles.Pixie) && PixieTargets[seer.PlayerId].Contains(target.PlayerId)) color = Main.roleColors[CustomRoles.Pixie];
        return color;
    }
    public void SendRPC(byte pixieId, byte targetId = 255)
    {
        var writer = MessageWriter.Get(SendOption.Reliable); //SetPixieTargets
        writer.Write(pixieId);
        writer.Write(targetId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte pixieId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        if (targetId != 255)
        {
            PixieTargets[pixieId].Add(targetId);
        }
        else
        {
            PixieTargets[pixieId].Clear();
        }
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        byte targetId = target.PlayerId;
        byte killerId = killer.PlayerId;

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
        SendRPC(killerId, targetId);

        NotifyRoles(SpecifySeer: killer, ForceLoop: true);
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
                if (PixieTargets[pixieId].Count <= 0) return;
                if (pixieId.GetAbilityUseLimit() >= PixiePointsToWin.GetInt()) return;

                if (PixieTargets[pixieId].Contains(exiled.PlayerId))
                {
                    pc.RpcIncreaseAbilityUseLimitBy(1);
                }
                else if (PixieSuicideOpt.GetBool()
                    && PixieTargets[pixieId].Any(eid => eid.GetPlayer()?.IsAlive() == true))
                {
                    pc.SetRealKiller(pc);
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pixieId);
                    Logger.Info($"{pc.GetNameWithRole()} committed suicide because target not exiled and target(s) were alive during ejection", "Pixie");
                }
            }
            PixieTargets[pixieId].Clear();
            SendRPC(pixieId);
        }
    }
    public static void PixieWinCondition(PlayerControl pc)
    {
        if (pc == null) return;
        if (pc.GetAbilityUseLimit() >= PixiePointsToWin.GetInt())
        {
            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Pixie);
        }
    }
}

