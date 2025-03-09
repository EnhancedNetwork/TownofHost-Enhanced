using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;


namespace TOHE.Roles.Neutral;

internal class Shroud : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Shroud;
    private const int Id = 18000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Shroud);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem ShroudCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    private static readonly Dictionary<byte, byte> ShroudList = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Shroud, 1, zeroOne: false);
        ShroudCooldown = FloatOptionItem.Create(Id + 10, "ShroudCooldown", new(0f, 180f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud]);
    }
    public override void Init()
    {
        ShroudList.Clear();
    }
    public override void Add(byte playerId)
    {
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }
    private void SendRPC(byte shroudId, byte targetId, byte typeId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); // syncShroud
        writer.Write(typeId);
        writer.Write(shroudId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        var typeId = reader.ReadByte();
        var shroudId = reader.ReadByte();
        var targetId = reader.ReadByte();

        switch (typeId)
        {
            case 0:
                ShroudList.Clear();
                break;
            case 1:
                ShroudList[targetId] = shroudId;
                break;
            case 2:
                ShroudList.Remove(targetId);
                break;
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ShroudCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public static bool ShroudIsActive(byte playerId) => ShroudList.ContainsKey(playerId);

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (ShroudList.ContainsKey(target.PlayerId)) return false;
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceMini), GetString("CantShroud")));
            return false;
        }

        ShroudList[target.PlayerId] = killer.PlayerId;
        SendRPC(killer.PlayerId, target.PlayerId, 1);

        killer.SetKillCooldown();

        Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        return false;
    }

    private void OnFixedUpdateOthers(PlayerControl shroud, bool lowLoad, long nowTime)
    {
        if (lowLoad || !ShroudList.TryGetValue(shroud.PlayerId, out var shroudId)) return;

        if (!shroud.IsAlive() || Pelican.IsEaten(shroud.PlayerId))
        {
            ShroudList.Remove(shroud.PlayerId);
            SendRPC(byte.MaxValue, shroud.PlayerId, 2);
        }
        else
        {
            var shroudPos = shroud.transform.position;
            Dictionary<byte, float> targetDistance = [];
            float dis;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.PlayerId != shroud.PlayerId && !target.Is(CustomRoles.Shroud) && !target.IsTransformedNeutralApocalypse())
                {
                    dis = Utils.GetDistance(shroudPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }
            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = min.Key.GetPlayer();
                var KillRange = NormalGameOptionsV08.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                if (min.Value <= KillRange && !shroud.inVent && !shroud.inMovingPlat && !target.inVent && !target.inMovingPlat)
                {
                    if (shroud.RpcCheckAndMurder(target, true))
                    {
                        RPC.PlaySoundRPC(shroudId, Sounds.KillSound);
                        target.SetDeathReason(PlayerState.DeathReason.Shrouded);
                        shroud.RpcMurderPlayer(target);
                        target.SetRealKiller(Utils.GetPlayerById(shroudId));
                        Utils.MarkEveryoneDirtySettings();
                        ShroudList.Remove(shroud.PlayerId);
                        SendRPC(byte.MaxValue, shroud.PlayerId, 2);
                        Utils.NotifyRoles(SpecifySeer: shroudId.GetPlayer(), SpecifyTarget: shroud, ForceLoop: true);
                    }
                }
            }
        }
    }

    public override void OnPlayerExiled(PlayerControl shroud, NetworkedPlayerInfo exiled)
    {
        if (!shroud.IsAlive() || (exiled != null && exiled.PlayerId == shroud.PlayerId))
        {
            ShroudList.Clear();
            SendRPC(byte.MaxValue, byte.MaxValue, 0);
        }
    }
    public override void AfterMeetingTasks()
    {
        if (_Player == null || !_Player.IsAlive()) return;

        foreach (var shroudedId in ShroudList.Keys)
        {
            PlayerControl shrouded = shroudedId.GetPlayer();
            if (!shrouded.IsAlive()) continue;
            if (shrouded.IsTransformedNeutralApocalypse()) continue;

            shrouded.SetDeathReason(PlayerState.DeathReason.Shrouded);
            shrouded.RpcMurderPlayer(shrouded);
            shrouded.SetRealKiller(_Player);

            SendRPC(byte.MaxValue, shrouded.PlayerId, 2);
            ShroudList.Remove(shrouded.PlayerId);
        }
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isMeeting = false)
        => isMeeting && ShroudList.ContainsKey(target.PlayerId) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shroud), "◈") : string.Empty;

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText($"{GetString("ShroudButtonText")}");
    }
}
