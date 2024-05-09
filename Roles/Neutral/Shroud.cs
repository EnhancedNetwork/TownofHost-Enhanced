using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Shroud : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 18000;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsEnable => HasEnabled;
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
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Shroud]);
    }
    public override void Init()
    {
        PlayerIds.Clear();
        ShroudList.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        CustomRoleManager.MarkOthers.Add(GetShroudMark);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte shroudId, byte targetId, byte typeId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Shroud); // syncShroud
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

    private static void OnFixedUpdateOthers(PlayerControl shroud)
    {
        if (!ShroudList.ContainsKey(shroud.PlayerId)) return;

        if (!shroud.IsAlive() || Pelican.IsEaten(shroud.PlayerId))
        {
            ShroudList.Remove(shroud.PlayerId);
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
                    dis = Vector2.Distance(shroudPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }
            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = Utils.GetPlayerById(min.Key);
                var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                if (min.Value <= KillRange && shroud.CanMove && target.CanMove)
                {
                    if (shroud.RpcCheckAndMurder(target, true))
                    {
                        var shroudId = ShroudList[shroud.PlayerId];
                        RPC.PlaySoundRPC(shroudId, Sounds.KillSound);
                        target.SetRealKiller(Utils.GetPlayerById(shroudId));
                        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Shrouded;
                        shroud.RpcMurderPlayer(target);
                        Utils.MarkEveryoneDirtySettings();
                        ShroudList.Remove(shroud.PlayerId);
                        SendRPC(byte.MaxValue, shroud.PlayerId, 2);
                        //Utils.NotifyRoles(SpecifySeer: shroud);
                        Utils.NotifyRoles(Utils.GetPlayerById(shroudId), SpecifyTarget: shroud, ForceLoop: true);
                    }
                }
            }
        }
    }

    public override void OnPlayerExiled(PlayerControl shroud, GameData.PlayerInfo exiled)
    {
        if (!shroud.IsAlive())
        {
            ShroudList.Remove(shroud.PlayerId);
            SendRPC(byte.MaxValue, shroud.PlayerId, 2);
            return;
        }

        foreach (var shroudedId in ShroudList.Keys)
        {
            PlayerControl shrouded = Utils.GetPlayerById(shroudedId);
            if (shrouded == null) continue;

            Main.PlayerStates[shrouded.PlayerId].deathReason = PlayerState.DeathReason.Shrouded;
            shrouded.RpcMurderPlayer(shrouded);

            ShroudList.Remove(shrouded.PlayerId);
            SendRPC(byte.MaxValue, shrouded.PlayerId, 2);
        }
    }

    public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
        => target != null && (ShroudList.ContainsValue(seer.PlayerId) && ShroudList.ContainsKey(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shroud), "◈") : string.Empty;
    
    private static string GetShroudMark(PlayerControl seer, PlayerControl target, bool isMeeting)
        => isMeeting && target != null && ShroudList.ContainsKey(target.PlayerId) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Shroud), "◈") : string.Empty;

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText($"{GetString("ShroudButtonText")}");
    }
}
