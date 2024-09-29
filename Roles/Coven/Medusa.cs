using AmongUs.GameOptions;
using static TOHE.Translator;
using static TOHE.Options;
using static TOHE.Utils;
using TOHE.Roles.Core;
using static UnityEngine.GraphicsBuffer;
using Hazel;
using InnerNet;

namespace TOHE.Roles.Coven;

internal class Medusa : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 17000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Medusa);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenUtility;
    //==================================================================\\

    private static OptionItem StoneCooldown;
    private static OptionItem StoneDuration;
    private static OptionItem StoneVision;
    //private static OptionItem KillCooldownAfterStoneGazing;
    //private static OptionItem CanVent;
    //private static OptionItem HasImpostorVision;

    private static readonly Dictionary<byte, List<byte>> StonedPlayers = [];
    private static readonly Dictionary<byte, List<byte>> PreStonedPlayers = [];
    private static readonly Dictionary<byte, float> originalSpeed = [];



    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Medusa, 1, zeroOne: false);
        StoneCooldown = FloatOptionItem.Create(Id + 12, "MedusaStoneCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa])
            .SetValueFormat(OptionFormat.Seconds);
        StoneDuration = FloatOptionItem.Create(Id + 14, "MedusaStoneDuration", new(0f, 180f, 2.5f), 15f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa])
            .SetValueFormat(OptionFormat.Seconds);
        StoneVision = FloatOptionItem.Create(Id + 16, "MedusaStoneVision", new(0f, 5f, 0.25f), 0.5f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa])
            .SetValueFormat(OptionFormat.Multiplier);
        /*
        KillCooldownAfterStoneGazing = FloatOptionItem.Create(Id + 15, "KillCooldownAfterStoneGazing", new(0f, 180f, 2.5f), 40f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Medusa]);
        */
    }
    public override void Init()
    {
        StonedPlayers.Clear();
        originalSpeed.Clear();
        PreStonedPlayers.Clear();
    }
    public override void Add(byte playerId)
    {
        StonedPlayers[playerId] = [];
        PreStonedPlayers[playerId] = [];
    }

    public void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(player.PlayerId);
        writer.Write(AbilityLimit);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();

        AbilityLimit = reader.ReadSingle();
        PreStonedPlayers[playerId].Add(reader.ReadByte());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = StoneCooldown.GetFloat();
    //public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    //public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    /*
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, PlayerControl killer)
    {
        if (reporter.Is(CustomRoles.Medusa))
        {
            Main.UnreportableBodies.Add(target.PlayerId);
            reporter.Notify(GetString("MedusaStoneBody"));

            reporter.SetKillCooldownV3(KillCooldownAfterStoneGazing.GetFloat(), forceAnime: true);
            Logger.Info($"{reporter.GetRealName()} stoned {target.PlayerName} body", "Medusa");
            return false;
        }
        return true;
    }
    */
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (HasNecronomicon(killer) || !target.IsPlayerCoven()) {
            killer.RpcMurderPlayer(target);
            killer.ResetKillCooldown();
            Main.UnreportableBodies.Add(target.PlayerId);
            return false; 
        }
        else
        {
            PreStonedPlayers[killer.PlayerId].Add(target.PlayerId);
            killer.Notify(GetString("MedusaStonedPlayer"));
            return false;
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        foreach (var player in PreStonedPlayers[pc.PlayerId]) 
        {
            StonedPlayers[pc.PlayerId].Add(player);
            PreStonedPlayers[pc.PlayerId].Remove(player);   
        }
        foreach (var player in StonedPlayers[pc.PlayerId])
        {
            originalSpeed.Remove(player);
            originalSpeed.Add(player, Main.AllPlayerSpeed[player]);
            Main.AllPlayerSpeed[player] = 0f;
            ReportDeadBodyPatch.CanReport[player] = false;
            GetPlayerById(player).MarkDirtySettings();
            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[player] = originalSpeed[player];
                GetPlayerById(player).SyncSettings();
                originalSpeed.Remove(player);
                StonedPlayers[pc.PlayerId].Remove(player);
            }, StoneDuration.GetFloat(), "Medusa Revert Stone");
        }
    }
    public static void SetStoned(PlayerControl player, IGameOptions opt)
    {
        if (StonedPlayers.Any(a => a.Value.Contains(player.PlayerId) &&
           Main.AllAlivePlayerControls.Any(b => b.PlayerId == a.Key)))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, StoneDuration.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, StoneDuration.GetFloat());
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => PreStonedPlayers[seer.PlayerId].Contains(seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.Medusa), "♻") : string.Empty;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("MedusaReportButtonText"));
    }
}
