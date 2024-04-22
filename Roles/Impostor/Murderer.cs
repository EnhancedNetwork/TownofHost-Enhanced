using Hazel;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Murderer : RoleBase
{
    private readonly static GameData.PlayerOutfit UnidentifiableOutfit = new GameData.PlayerOutfit().Set("Body", 0, "", "", "visor_Dirty", "", "");
    public static readonly Dictionary<byte, string> MurdererVictim = [];
    private static float KillerOriginalSpeed = 0;
    //===========================SETUP================================\\
    private const int Id = 28600;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Murderer);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Murderer])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        playerIdList.Clear();
        MurdererVictim.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        if (playerId == PlayerControl.LocalPlayer.PlayerId && Main.nickName.Length != 0) MurdererVictim[playerId] = Main.nickName;
        else MurdererVictim[playerId] = Utils.GetPlayerById(playerId).Data.PlayerName;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Murderer);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static bool CheckMurdererVictim(byte playerId) => MurdererVictim.ContainsKey(playerId);

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DefaultKillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    public static void RpcChangeSkin(PlayerControl pc, GameData.PlayerOutfit newOutfit, uint level)
    {
        var sender = CustomRpcSender.Create(name: $"Doppelganger.RpcChangeSkin({pc.Data.PlayerName})");

        pc.SetName(newOutfit.PlayerName);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetName)
            .Write(newOutfit.PlayerName)
        .EndRpc();

        Main.AllPlayerNames[pc.PlayerId] = newOutfit.PlayerName;

        pc.SetColor(newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetColor)
            .Write(newOutfit.ColorId)
        .EndRpc();

        pc.SetHat(newOutfit.HatId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetHatStr)
            .Write(newOutfit.HatId)
        .EndRpc();

        pc.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(newOutfit.SkinId)
        .EndRpc();

        pc.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(newOutfit.VisorId)
        .EndRpc();

        pc.SetNamePlate(newOutfit.NamePlateId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetNamePlateStr)
            .Write(newOutfit.NamePlateId)
            .EndRpc();
        
        pc.SetLevel(level);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetLevel)
            .Write(level)
            .EndRpc();

        sender.SendMessage();
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;

        MurdererVictim[target.PlayerId] = target.GetRealName();
        RpcChangeSkin(target, UnidentifiableOutfit, 0);
        Logger.Info("Changed target skin", "Murderer");

        SendRPC(killer.PlayerId);
        Utils.NotifyRoles(ForceLoop: true, NoCache: true);
        RPC.SyncAllPlayerNames();

        KillerOriginalSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
        killer.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = KillerOriginalSpeed;
            killer.MarkDirtySettings();
        }, 1.5f, "Reset player speed");
        return true;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("MurdererKillButtonText"));
    }
}
