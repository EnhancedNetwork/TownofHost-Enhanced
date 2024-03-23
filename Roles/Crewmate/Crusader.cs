using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Crusader : RoleBase
{
    private const int Id = 10400;
    private static readonly HashSet<byte> playerIdList = [];
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Crusader.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem SkillLimitOpt;
    private static OptionItem SkillCooldown;

    private static readonly HashSet<byte> ForCrusade = [];
    private static readonly Dictionary<byte, int> CrusaderLimit = [];
    private static readonly Dictionary<byte, float> CurrentKillCooldown = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Crusader);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "CrusaderSkillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Crusader])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = IntegerOptionItem.Create(Id + 11, "CrusaderSkillLimit", new(1, 15, 1), 5, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Crusader])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList.Clear();
        ForCrusade.Clear();
        CrusaderLimit.Clear();
        CurrentKillCooldown.Clear();
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CrusaderLimit.Add(playerId, SkillLimitOpt.GetInt());
        CurrentKillCooldown.Add(playerId, SkillCooldown.GetFloat());
        On = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        CrusaderLimit.Remove(playerId);
        CurrentKillCooldown.Remove(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Crusader);
        writer.Write(playerId);
        writer.Write(CrusaderLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (CrusaderLimit.ContainsKey(PlayerId))
            CrusaderLimit[PlayerId] = Limit;
        else
            CrusaderLimit.Add(PlayerId, Limit);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(Utils.GetPlayerById(id)) ? CurrentKillCooldown[id] : 300f;

    public override bool CanUseKillButton(PlayerControl pc)
        => !Main.PlayerStates[pc.PlayerId].IsDead
        && (CrusaderLimit.TryGetValue(pc.PlayerId, out var x) ? x : 1) >= 1;
    
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    
    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(CanUseKillButton(Utils.GetPlayerById(playerId)) ? Utils.GetRoleColor(CustomRoles.Crusader).ShadeColor(0.25f) : Color.gray, CrusaderLimit.TryGetValue(playerId, out var constableLimit) ? $"({constableLimit})" : "Invalid");
    
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (CrusaderLimit[killer.PlayerId] <= 0) return false;

        ForCrusade.Remove(target.PlayerId);
        ForCrusade.Add(target.PlayerId);
        CrusaderLimit[killer.PlayerId]--;
        SendRPC(killer.PlayerId);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        
        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(killer);
        
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (ForCrusade.Contains(target.PlayerId)) return true;

        foreach (var player in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Crusader)).ToArray())
        {
            if (!killer.Is(CustomRoles.Pestilence) && !killer.Is(CustomRoles.KillingMachine))
            {
                player.RpcMurderPlayerV3(killer);
                ForCrusade.Remove(target.PlayerId);
                killer.RpcGuardAndKill(target);
                return false;
            }

            if (killer.Is(CustomRoles.Pestilence))
            {
                Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
                killer.RpcMurderPlayerV3(player);
                ForCrusade.Remove(target.PlayerId);
                target.RpcGuardAndKill(killer);

                return false;
            }
        }

        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("CrusaderKillButtonText"));
    }
}