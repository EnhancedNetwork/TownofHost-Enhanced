using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Overseer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Overseer;
    private const int Id = 12200;
    public override bool IsDesyncRole => true;
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
        OverseerTimer.Clear();
        RandomRole.Clear();
        IsRevealed.Clear();
    }
    public override void Add(byte playerId)
    {
        foreach (var ar in Main.AllPlayerControls)
        {
            IsRevealed.Add((playerId, ar.PlayerId), false);
        }

        RandomRole.Add(playerId, GetRandomCrewRoleString());
    }
    public override void Remove(byte playerId)
    {
        OverseerTimer.Remove(playerId);
        RandomRole.Remove(playerId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;

    private static void SendTimerRPC(byte RpcType, byte overseertId, PlayerControl target = null, float timer = 0)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetOverseerTimer, ExtendedPlayerControl.RpcSendOption);
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
        Aware.OnCheckMurder(CustomRoles.Overseer, target);
        killer.SetKillCooldown(OverseerRevealTime.GetFloat());
        if (!IsRevealed[(killer.PlayerId, target.PlayerId)] && !OverseerTimer.ContainsKey(killer.PlayerId))
        {
            OverseerTimer.TryAdd(killer.PlayerId, (target, 0f));
            SendTimerRPC(1, killer.PlayerId, target, 0f);
            target.RpcSetSpecificScanner(killer, true);

            NotifyRoles(SpecifySeer: killer);
        }
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!OverseerTimer.TryGetValue(player.PlayerId, out var data)) return;

        var playerId = player.PlayerId;
        if (!player.IsAlive() || Pelican.IsEaten(playerId))
        {
            data.Item1.RpcSetSpecificScanner(player, false);
            OverseerTimer.Remove(playerId);
            SendTimerRPC(2, playerId);
            NotifyRoles(SpecifySeer: player);
        }
        else
        {
            var (farTarget, farTime) = data;

            if (!farTarget.IsAlive())
            {
                OverseerTimer.Remove(playerId);
                SendTimerRPC(2, playerId);
                farTarget.RpcSetSpecificScanner(player, false);

            }
            else if (farTime >= OverseerRevealTime.GetFloat())
            {
                player.SetKillCooldown();

                OverseerTimer.Remove(playerId);
                SendTimerRPC(2, playerId);
                farTarget.RpcSetSpecificScanner(player, false);

                IsRevealed[(playerId, farTarget.PlayerId)] = true;
                SetRevealtPlayerRPC(player, farTarget, true);

                NotifyRoles(SpecifySeer: player);

                Logger.Info($"Revealed: {player.GetNameWithRole()}", "Overseer");
            }
            else
            {

                float range = NormalGameOptionsV08.KillDistances[Mathf.Clamp(player.Is(Reach.IsReach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                float dis = GetDistance(player.GetCustomPosition(), farTarget.GetCustomPosition());
                if (dis <= range)
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
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (_Player == null) return;
        if (OverseerTimer.TryGetValue(_Player.PlayerId, out var data))
        {
            var farTarget = data.Item1;
            farTarget?.RpcSetSpecificScanner(_Player, false);
        }

        OverseerTimer.Clear();
        SendTimerRPC(0, byte.MaxValue);
    }

    private static string GetRandomCrewRoleString() // Random role for trickster
    {
        var randomRole = randomRolesForTrickster.RandomElement();

        //string roleName = GetRoleName(randomRole);
        string RoleText = ColorString(GetRoleColor(randomRole), GetString(randomRole.ToString()));

        return RoleText;
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
