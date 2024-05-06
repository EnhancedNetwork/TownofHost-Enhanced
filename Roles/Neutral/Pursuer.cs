using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TOHE.Modules;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Pursuer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13400;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem PursuerSkillCooldown;
    private static OptionItem PursuerSkillLimitTimes;

    private static readonly HashSet<byte> notActiveList = [];
    public static readonly Dictionary<byte, int> SeelLimit = [];
    private static readonly Dictionary<byte, List<byte>> clientList = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pursuer);
        PursuerSkillCooldown = FloatOptionItem.Create(Id + 10, "PursuerSkillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pursuer])
            .SetValueFormat(OptionFormat.Seconds);
        PursuerSkillLimitTimes = IntegerOptionItem.Create(Id + 11, "PursuerSkillLimitTimes", new(1, 20, 1), 2, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pursuer])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList.Clear();
        clientList.Clear();
        notActiveList.Clear();
        SeelLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SeelLimit.Add(playerId, PursuerSkillLimitTimes.GetInt());

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Pursuer); //SetPursuerSellLimit
        writer.Write(playerId);
        writer.Write(SeelLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (SeelLimit.ContainsKey(PlayerId))
            SeelLimit[PlayerId] = Limit;
        else
            SeelLimit.Add(PlayerId, PursuerSkillLimitTimes.GetInt());
    }
    public override bool CanUseKillButton(PlayerControl pc) => CanUseKillButton(pc.PlayerId);
    
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && SeelLimit.TryGetValue(playerId, out var x) && x >= 1;
    public override string GetProgressText(byte playerId, bool cooms) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Pursuer) : Color.gray, SeelLimit.TryGetValue(playerId, out var x) ? $"({x})" : "Invalid");
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? PursuerSkillCooldown.GetFloat() : 300f;
    public static bool IsClient(byte playerId)
    {
        foreach (var pc in clientList)
            if (pc.Value.Contains(playerId)) return true;
        return false;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);
    public static bool CanBeClient(PlayerControl pc) => pc != null && pc.IsAlive() && !GameStates.IsMeeting && !IsClient(pc.PlayerId);
    public static bool CanSeel(byte playerId) => playerIdList.Contains(playerId) && SeelLimit.TryGetValue(playerId, out int x) && x > 0;
    public override bool OnCheckMurderAsKiller(PlayerControl pc, PlayerControl target)
    {
        if (pc == null || target == null || !pc.Is(CustomRoles.Pursuer)) return true;
        if (target.Is(CustomRoles.Pestilence) || target.Is(CustomRoles.SerialKiller)) return false;
        if (!(CanBeClient(target) && CanSeel(pc.PlayerId))) return false;

        SeelLimit[pc.PlayerId]--;
        SendRPC(pc.PlayerId);
        if (target.Is(CustomRoles.KillingMachine)) 
        {
            Logger.Info("target is Killing Machine, ability used count reduced, but target will not die", "Purser");
            return false; 
        }
        if (!clientList.ContainsKey(pc.PlayerId))
            clientList.Add(pc.PlayerId, []);

        clientList[pc.PlayerId].Add(target.PlayerId);

        if (!Options.DisableShieldAnimations.GetBool())
            pc.RpcGuardAndKill(pc);

        notActiveList.Add(pc.PlayerId);

        pc.SetKillCooldown();
        pc.RPCPlayCustomSound("Bet");

        Utils.NotifyRoles(SpecifySeer: pc);
        Logger.Info($"Counterfeiters {pc.GetRealName()} sell counterfeits to {target.GetRealName()}", "Pursuer");
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl pc, PlayerControl _)  // Target of Pursuer attempt to murder someone
    {
        if (!IsClient(pc.PlayerId) || notActiveList.Contains(pc.PlayerId)) return false;
        
        byte cfId = byte.MaxValue;
        foreach (var cf in clientList)
            if (cf.Value.Contains(pc.PlayerId)) cfId = cf.Key;
        
        if (cfId == byte.MaxValue) return false;
        
        var killer = Utils.GetPlayerById(cfId);
        var target = pc;
        if (killer == null) return false;
        
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
        target.SetRealKiller(killer);
        target.RpcMurderPlayer(target);
        
        Logger.Info($"赝品商 {pc.GetRealName()} 的客户 {target.GetRealName()} 因使用赝品走火自杀", "Pursuer");
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("PursuerButtonText"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Pursuer");
}