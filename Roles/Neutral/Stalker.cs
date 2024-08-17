using AmongUs.GameOptions;
using Hazel;
using InnerNet;

namespace TOHE.Roles.Neutral;

// 来源：https://github.com/Yumenopai/TownOfHost_Y
internal class Stalker : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 18100;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanVent;
    private static OptionItem CanCountNeutralKiller;
    public static OptionItem SnatchesWin;

    private static readonly Dictionary<byte, float> CurrentKillCooldown = [];
    public static readonly Dictionary<byte, bool> IsWinKill = [];

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Stalker, 1);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker])
            .SetValueFormat(OptionFormat.Seconds);
        HasImpostorVision = BooleanOptionItem.Create(Id + 11, GeneralOption.ImpostorVision, false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker]);
        CanVent = BooleanOptionItem.Create(Id + 14, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker]);
        CanCountNeutralKiller = BooleanOptionItem.Create(Id + 12, "CanCountNeutralKiller", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker]);
        SnatchesWin = BooleanOptionItem.Create(Id + 13, GeneralOption.SnatchesWin, false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Stalker]);

    }
    public override void Init()
    {
        playerIdList.Clear();
        CurrentKillCooldown.Clear();
        IsWinKill.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());
        IsWinKill[playerId] = false;

        DRpcSetKillCount(Utils.GetPlayerById(playerId));
    }

    public static void ReceiveRPC(MessageReader msg)
    {
        byte StalkerrId = msg.ReadByte();
        bool IsKillerKill = msg.ReadBoolean();
        if (IsWinKill.ContainsKey(StalkerrId))
            IsWinKill[StalkerrId] = IsKillerKill;
        else
            IsWinKill.Add(StalkerrId, false);
        Logger.Info($"Player{StalkerrId}:ReceiveRPC", "Stalker");
    }
    private static void DRpcSetKillCount(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetStalkerrKillCount, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(IsWinKill[player.PlayerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CurrentKillCooldown[id];
    public override bool CanUseKillButton(PlayerControl player) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl Ktarget)
    {
        var targetRole = Ktarget.GetCustomRole();
        var succeeded = targetRole.IsImpostor();
        if (CanCountNeutralKiller.GetBool() && !Ktarget.Is(CustomRoles.Arsonist) && !Ktarget.Is(CustomRoles.Revolutionist))
        {
            succeeded = succeeded || Ktarget.IsNeutralKiller();
        }
        if (succeeded && SnatchesWin.GetBool())
            IsWinKill[killer.PlayerId] = true;

        DRpcSetKillCount(killer);
        MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, killer.GetClientId());
        SabotageFixWriter.Write((byte)SystemTypes.Electrical);
        MessageExtensions.WriteNetObject(SabotageFixWriter, killer);
        AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);

        foreach (var target in Main.AllPlayerControls)
        {
            if (target.PlayerId == killer.PlayerId || target.Data.Disconnected) continue;
            SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, target.GetClientId());
            SabotageFixWriter.Write((byte)SystemTypes.Electrical);
            MessageExtensions.WriteNetObject(SabotageFixWriter, target);
            AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
        }
        return true;
    }
}
