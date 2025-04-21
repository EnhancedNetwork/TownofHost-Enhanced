using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Medusa : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Medusa;
    private const int Id = 17000;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenUtility;
    //==================================================================\\

    private static OptionItem StoneCooldown;
    private static OptionItem StoneDuration;
    private static OptionItem StoneVision;
    //private static OptionItem KillCooldownAfterStoneGazing;
    //private static OptionItem CanVent;
    //private static OptionItem HasImpostorVision;

    private static readonly Dictionary<byte, List<byte>> StonedPlayers = [];
    private static readonly Dictionary<byte, float> originalSpeed = [];
    private static bool isStoning;



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
    }
    public override void Add(byte playerId)
    {
        StonedPlayers[playerId] = [];
        isStoning = false;
        GetPlayerById(playerId)?.AddDoubleTrigger();
    }

    public void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();
        StonedPlayers[playerId].Add(reader.ReadByte());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = StoneCooldown.GetFloat();
    //public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
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
        if (killer.CheckDoubleTrigger(target, () => { SetStoned(killer, target); }))
        {
            if (HasNecronomicon(killer) && !target.GetCustomRole().IsCovenTeam())
            {
                killer.RpcMurderPlayer(target);
                killer.ResetKillCooldown();
                killer.SetKillCooldown();
                Main.UnreportableBodies.Add(target.PlayerId);
                return false;
            }
            killer.Notify(GetString("CovenDontKillOtherCoven"));
        }
        return false;
    }
    private void SetStoned(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return;
        StonedPlayers[killer.PlayerId].Add(target.PlayerId);
        SendRPC(killer, target);
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.Notify(string.Format(GetString("MedusaStonedPlayer"), target.GetRealName()));
    }
    public override void UnShapeShiftButton(PlayerControl dusa)
    {
        foreach (var player in StonedPlayers[dusa.PlayerId])
        {
            dusa.Notify(GetString("MedusaStoningStart"), StoneDuration.GetFloat());
            isStoning = true;
            originalSpeed.Add(player, Main.AllPlayerSpeed[player]);
            Main.AllPlayerSpeed[player] = 0f;
            ReportDeadBodyPatch.CanReport[player] = false;
            GetPlayerById(player).MarkDirtySettings();
            _ = new LateTask(() =>
            {
                dusa.Notify(GetString("MedusaStoningEnd"));
                isStoning = false;
                // sometimes it doesn't contain the player for some stupid reason
                if (originalSpeed.ContainsKey(player)) Main.AllPlayerSpeed[player] = originalSpeed[player];
                else Main.AllPlayerSpeed[player] = AURoleOptions.PlayerSpeedMod;
                GetPlayerById(player).SyncSettings();
                if (originalSpeed.ContainsKey(player)) originalSpeed.Remove(player);
                StonedPlayers[dusa.PlayerId].Remove(player);
            }, StoneDuration.GetFloat(), "Medusa Revert Stone");
        }
    }
    public static void SetStoned(PlayerControl player, IGameOptions opt)
    {
        if (StonedPlayers.Any(a => a.Value.Contains(player.PlayerId) &&
           Main.AllAlivePlayerControls.Any(b => b.PlayerId == a.Key)) && isStoning)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, StoneVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, StoneVision.GetFloat());
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => IsStoned(seer.PlayerId, seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.Medusa), "♻") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (_Player == null) return string.Empty;
        if (IsStoned(seer.PlayerId, target.PlayerId) && ((seer.GetCustomRole().IsCovenTeam() && seer.PlayerId != _Player.PlayerId) || !seer.IsAlive()))
        {
            return ColorString(GetRoleColor(CustomRoles.Medusa), "♻");
        }
        return string.Empty;
    }
    public static bool IsStoned(byte pc, byte target) => StonedPlayers.TryGetValue(pc, out var stoneds) && stoneds.Contains(target);

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.ReportButton.OverrideText(GetString("MedusaReportButtonText"));
    }
}
