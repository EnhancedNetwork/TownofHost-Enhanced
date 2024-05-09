using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Crusader : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 10400;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem SkillLimitOpt;
    private static OptionItem SkillCooldown;

    private static readonly HashSet<byte> ForCrusade = [];
    private static readonly Dictionary<byte, int> CrusaderLimit = [];
    private static readonly Dictionary<byte, float> CurrentKillCooldown = [];

    public override void SetupCustomOption()
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
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CrusaderLimit.Add(playerId, SkillLimitOpt.GetInt());
        CurrentKillCooldown.Add(playerId, SkillCooldown.GetFloat());

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
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
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
        => (CrusaderLimit.TryGetValue(pc.PlayerId, out var x) ? x : 1) >= 1;
    
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    
    public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(CanUseKillButton(Utils.GetPlayerById(playerId)) ? Utils.GetRoleColor(CustomRoles.Crusader).ShadeColor(0.25f) : Color.gray, CrusaderLimit.TryGetValue(playerId, out var constableLimit) ? $"({constableLimit})" : "Invalid");
    
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (ForCrusade.Contains(target.PlayerId) || CrusaderLimit[killer.PlayerId] <= 0) return false;

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
        if (!ForCrusade.Contains(target.PlayerId)) return false;

        foreach (var crusader in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Crusader)).ToArray())
        {
            if (!killer.Is(CustomRoles.Pestilence) && !killer.Is(CustomRoles.KillingMachine)
                && killer.CheckForInvalidMurdering(target) && crusader.RpcCheckAndMurder(killer, true))
            {
                crusader.RpcMurderPlayer(killer);
                ForCrusade.Remove(target.PlayerId);
                killer.RpcGuardAndKill(target);
                return true;
            }

            if (killer.Is(CustomRoles.Pestilence))
            {
                Main.PlayerStates[crusader.PlayerId].deathReason = PlayerState.DeathReason.PissedOff;
                killer.RpcMurderPlayer(crusader);
                ForCrusade.Remove(target.PlayerId);
                target.RpcGuardAndKill(killer);

                return true;
            }
        }

        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("CrusaderKillButtonText"));
    }
}