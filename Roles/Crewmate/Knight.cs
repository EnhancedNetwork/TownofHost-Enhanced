using AmongUs.GameOptions;
using Hazel;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

internal class Knight : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 10800;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem CanVent;
    private static OptionItem KillCooldown;

    private static readonly HashSet<byte> killed = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Knight);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Knight])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Knight]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        killed.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public static bool CheckCanUseVent(PlayerControl player) => player.Is(CustomRoles.Knight) && CanVent.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CheckCanUseVent(pc);

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsKilled(id) ? 300f : KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc)
        => !IsKilled(pc.PlayerId);

    public override string GetProgressText(byte id, bool comms)
        => Utils.ColorString(!IsKilled(id) ? Utils.GetRoleColor(CustomRoles.Knight).ShadeColor(0.25f) : Color.gray, !IsKilled(id) ? "(1)" : "(0)");
    
    private static bool IsKilled(byte playerId) => killed.Contains(playerId);

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Knight);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte KnightId = reader.ReadByte();
        if (!killed.Contains(KnightId))
            killed.Add(KnightId);
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl banana)
    {
        SendRPC(killer.PlayerId);
        killed.Add(killer.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()} : " + (IsKilled(killer.PlayerId) ? "Kill chance used" : "Kill chance not used"), "Knight");
        killer.ResetKillCooldown();
        Utils.NotifyRoles(SpecifySeer: killer);
        return true;
    }
}