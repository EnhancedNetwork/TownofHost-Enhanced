﻿using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using UnityEngine;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Overseer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 12200;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("prophecies");

    private static readonly Dictionary<byte, string> RandomRole = [];
    private static readonly Dictionary<byte, (PlayerControl, float)> OverseerTimer = [];
    public static readonly Dictionary<(byte, byte), bool> IsRevealed = [];

    private static OptionItem OverseerCooldown;
    private static OptionItem OverseerRevealTime;
    private static OptionItem Vision;

    //private static byte CurrentRevealTarget = byte.MaxValue;

    private static readonly List<CustomRoles> randomRolesForTrickster =
    [
        CustomRoles.Snitch,
        CustomRoles.LazyGuy,
        CustomRoles.SuperStar,
        CustomRoles.Celebrity,
        CustomRoles.TaskManager,
        CustomRoles.Mayor,
        CustomRoles.Psychic,
        CustomRoles.Mechanic,
        CustomRoles.Snitch,
        CustomRoles.Marshall,
        CustomRoles.Inspector,
        CustomRoles.Bastion,
        CustomRoles.Dictator,
        CustomRoles.Doctor,
        CustomRoles.Detective,
        CustomRoles.Lookout,
        CustomRoles.Telecommunication,
        CustomRoles.NiceGuesser,
        CustomRoles.Transporter,
        CustomRoles.TimeManager,
        CustomRoles.Veteran,
        CustomRoles.Bodyguard,
        CustomRoles.Grenadier,
        CustomRoles.Lighter,
        CustomRoles.FortuneTeller,
        CustomRoles.Oracle,
        CustomRoles.Tracefinder,
  //      CustomRoles.Glitch,
        CustomRoles.Judge,
        CustomRoles.Mortician,
        CustomRoles.Medium,
        CustomRoles.Observer,
        CustomRoles.Pacifist,
        CustomRoles.Coroner,
        CustomRoles.Retributionist,
        CustomRoles.Guardian,
        CustomRoles.Spiritualist,
        //CustomRoles.Tracker,
    ];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Overseer);
        OverseerCooldown = FloatOptionItem.Create(Id + 10, "OverseerRevealCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Overseer])
            .SetValueFormat(OptionFormat.Seconds);
        OverseerRevealTime = FloatOptionItem.Create(Id + 11, "OverseerRevealTime", new(0f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Overseer])
            .SetValueFormat(OptionFormat.Seconds);
        Vision = FloatOptionItem.Create(Id + 12, "OverseerVision", new(0f, 5f, 0.05f), 0.25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Overseer])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Init()
    {
        playerIdList.Clear();
        OverseerTimer.Clear();
        RandomRole.Clear();
        IsRevealed.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        foreach (var ar in Main.AllPlayerControls)
        {
            IsRevealed.Add((playerId, ar.PlayerId), false);
        }

        RandomRole.Add(playerId, GetRandomCrewRoleString());

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        OverseerTimer.Remove(playerId);
        RandomRole.Remove(playerId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;

    private static void SendTimerRPC(byte RpcType, byte overseertId, PlayerControl target = null, float timer = 0)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetOverseerTimer, SendOption.Reliable, -1);
        writer.Write(RpcType);
        writer.Write(overseertId);
        if (target != null && RpcType == 1)
        {
            writer.WriteNetObject(target);
            writer.Write(timer);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveTimerRPC(MessageReader reader)
    {
        var RpcType = reader.ReadByte();
        var overseertId = reader.ReadByte();

        switch (RpcType)
        {
            case 0:
                OverseerTimer.Clear();
                break;
            case 1:
                var target = reader.ReadNetObject<PlayerControl>();
                var timer = reader.ReadSingle();
                OverseerTimer.TryAdd(overseertId, (target, timer));
                break;
            case 2:
                OverseerTimer.Remove(overseertId);
                break;
        }
    }
    private static void SetRevealtPlayerRPC(PlayerControl player, PlayerControl target, bool isRevealed)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetOverseerRevealedPlayer, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isRevealed);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveSetRevealedPlayerRPC(MessageReader reader)
    {
        byte OverseerId = reader.ReadByte();
        byte RevealId = reader.ReadByte();
        bool revealed = reader.ReadBoolean();

        IsRevealed[(OverseerId, RevealId)] = revealed;
    }

    public static bool IsRevealedPlayer(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null || IsRevealed == null) return false;
        IsRevealed.TryGetValue((player.PlayerId, target.PlayerId), out bool isRevealed);
        return isRevealed;
    }

    public static string GetRandomRole(byte playerId) => RandomRole[playerId];
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, Vision.GetFloat());
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, Vision.GetFloat());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = OverseerCooldown.GetFloat();
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        var revealTime = OverseerRevealTime.GetFloat();
        var killerId = killer.PlayerId;
        killer.SetKillCooldown(revealTime);

        if (!IsRevealed.TryGetValue((killerId, target.PlayerId), out _) && !OverseerTimer.ContainsKey(killerId))
        {
            OverseerTimer[killerId] = (target, 0f);
            SendTimerRPC(1, killerId, target, 0f);

            target.RpcSetSpecificScanner(killer, true);
            NotifyRoles(SpecifySeer: killer);
        }

        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        var playerId = player.PlayerId;

        if (!OverseerTimer.TryGetValue(playerId, out var timerData)) return;

        var (farTarget, farTime) = timerData;

        if (!player.IsAlive() || Pelican.IsEaten(playerId))
        {
            farTarget.RpcSetSpecificScanner(player, false);
            OverseerTimer.Remove(playerId);
            SendTimerRPC(2, playerId);
            NotifyRoles(SpecifySeer: player);
            return;
        }

        if (!farTarget.IsAlive())
        {
            OverseerTimer.Remove(playerId);
            SendTimerRPC(2, playerId);
            farTarget.RpcSetSpecificScanner(player, false);
            return;
        }

        var revealTime = OverseerRevealTime.GetFloat();
        if (farTime >= revealTime)
        {
            player.SetKillCooldown();
            OverseerTimer.Remove(playerId);
            SendTimerRPC(2, playerId);
            farTarget.RpcSetSpecificScanner(player, false);

            IsRevealed[(playerId, farTarget.PlayerId)] = true;
            SetRevealtPlayerRPC(player, farTarget, true);
            NotifyRoles(SpecifySeer: player);
            return;
        }

        var killDistance = NormalGameOptionsV08.KillDistances[Mathf.Clamp(player.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
        var playerPosition = player.GetCustomPosition();
        var targetPosition = farTarget.GetCustomPosition();
        var dis = Vector2.Distance(playerPosition, targetPosition);

        if (dis <= killDistance)
        {
            OverseerTimer[playerId] = (farTarget, farTime + Time.fixedDeltaTime);
            //SendTimerRPC(1, playerId, farTarget, farTime + Time.fixedDeltaTime);
        }
        else
        {
            OverseerTimer.Remove(playerId);
            SendTimerRPC(2, playerId);
            farTarget.RpcSetSpecificScanner(player, false);

            NotifyRoles(SpecifySeer: player, SpecifyTarget: farTarget, ForceLoop: true);
            Logger.Info($"Canceled: {player.GetNameWithRole()}", "Overseer");
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        OverseerTimer.Clear();
        SendTimerRPC(0, byte.MaxValue);
    }

    private static string GetRandomCrewRoleString() // Random role for trickster
    {
        var randomRole = randomRolesForTrickster.RandomElement();

        //string roleName = GetRoleName(randomRole);
        string RoleText = ColorString(GetRoleColor(randomRole), GetString(randomRole.ToString()));

        return $"<size={1.5}>{RoleText}</size>";
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (OverseerTimer.TryGetValue(seer.PlayerId, out var fa_kvp) && fa_kvp.Item1 == seen)
            return $"<color={GetRoleColorCode(CustomRoles.Overseer)}>○</color>";

        return string.Empty;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("OverseerKillButtonText"));
    }
}
