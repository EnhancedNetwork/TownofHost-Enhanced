using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Fireworker : RoleBase
{
    private enum FireworkerState
    {
        Initial = 1,
        SettingFireworker = 2,
        WaitTime = 4,
        ReadyFire = 8,
        FireEnd = 16,
        CanUseKill = Initial | FireEnd
    }
    //===========================SETUP================================\\
    private const int Id = 3200;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => nowFireworkerCount[player.PlayerId] == 0 ? CustomButton.Get("FireworkD") : CustomButton.Get("FireworkP");

    private static OptionItem FireworkerCount;
    private static OptionItem FireworkerRadius;
    private static OptionItem CanKill;

    private static readonly Dictionary<byte, int> nowFireworkerCount = [];
    private static readonly Dictionary<byte, HashSet<Vector3>> FireworkerPosition = [];
    private static readonly Dictionary<byte, FireworkerState> state = [];
    private static readonly Dictionary<byte, int> FireworkerBombKill = [];
    private static int fireworkerCount = 1;
    private static float fireworkerRadius = 1;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Fireworker);
        FireworkerCount = IntegerOptionItem.Create(Id + 10, "FireworkerMaxCount", new(1, 20, 1), 3, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fireworker])
            .SetValueFormat(OptionFormat.Pieces);
        FireworkerRadius = FloatOptionItem.Create(Id + 11, "FireworkerRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fireworker])
            .SetValueFormat(OptionFormat.Multiplier);
        CanKill = BooleanOptionItem.Create(Id + 12, GeneralOption.CanKill, false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fireworker]);
    }

    public override void Init()
    {
        PlayerIds.Clear();
        nowFireworkerCount.Clear();
        FireworkerPosition.Clear();
        state.Clear();
        FireworkerBombKill.Clear();

        fireworkerCount = FireworkerCount.GetInt();
        fireworkerRadius = FireworkerRadius.GetFloat();
    }

    public override void Add(byte playerId)
    {
        nowFireworkerCount[playerId] = fireworkerCount;
        FireworkerPosition[playerId] = [];
        state.TryAdd(playerId, FireworkerState.Initial);
        FireworkerBombKill[playerId] = 0;
        PlayerIds.Add(playerId);
    }

    private static void SendRPC(byte playerId)
    {
        Logger.Info($"Player{playerId}:SendRPC", "Fireworker");
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendFireworkerState, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(nowFireworkerCount[playerId]);
        writer.Write((int)state[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader msg)
    {
        var playerId = msg.ReadByte();
        nowFireworkerCount[playerId] = msg.ReadInt32();
        state[playerId] = (FireworkerState)msg.ReadInt32();
        Logger.Info($"Player{playerId}:ReceiveRPC", "Fireworker");
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterDuration = state[playerId] != FireworkerState.FireEnd ? 1f : 30f;
        AURoleOptions.ShapeshifterLeaveSkin = true;
    }

    public override bool CanUseKillButton(PlayerControl pc)
    {
        if (!state.ContainsKey(pc.PlayerId) || !pc.IsAlive()) return false;

        var canUse = false;
        if ((state[pc.PlayerId] & FireworkerState.CanUseKill) != 0)
        {
            canUse = true;
        }
        if (CanKill.GetBool())
        {
            canUse = true;
        }
        return canUse;
    }

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        Logger.Info($"Fireworker ShapeShift", "Fireworker");
        if (shapeshifter.PlayerId == target.PlayerId) return false;

        var shapeshifterId = shapeshifter.PlayerId;
        switch (state[shapeshifterId])
        {
            case FireworkerState.Initial:
            case FireworkerState.SettingFireworker:
                Logger.Info("One firework set up", "Fireworker");

                FireworkerPosition[shapeshifterId].Add(shapeshifter.transform.position);
                nowFireworkerCount[shapeshifterId]--;
                state[shapeshifterId] = nowFireworkerCount[shapeshifterId] == 0
                    ? Main.AliveImpostorCount <= 1 ? FireworkerState.ReadyFire : FireworkerState.WaitTime
                    : FireworkerState.SettingFireworker;

                shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
                break;

            case FireworkerState.ReadyFire:
                Logger.Info("Blowing up fireworks", "Fireworker");
                bool suicide = false;
                foreach (var player in Main.AllAlivePlayerControls)
                {
                    foreach (var pos in FireworkerPosition[shapeshifterId].ToArray())
                    {
                        var dis = Utils.GetDistance(pos, player.transform.position);
                        if (dis > fireworkerRadius) continue;

                        if (player == shapeshifter)
                        {
                            suicide = true;
                        }
                        else
                        {
                            player.SetDeathReason(PlayerState.DeathReason.Bombed);
                            player.RpcMurderPlayer(player);
                            player.SetRealKiller(shapeshifter);
                        }
                    }
                }
                if (suicide)
                {
                    var totalAlive = Main.AllAlivePlayerControls.Length;
                    if (totalAlive != 1)
                    {
                        shapeshifterId.SetDeathReason(PlayerState.DeathReason.Misfire);
                        shapeshifter.RpcMurderPlayer(shapeshifter);
                    }
                    shapeshifter.MarkDirtySettings();
                }
                state[shapeshifterId] = FireworkerState.FireEnd;
                break;
        }
        SendRPC(shapeshifterId);
        Utils.NotifyRoles(ForceLoop: true);

        return false;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        string retText = string.Empty;
        var seerId = seer.PlayerId;
        if (seer == null || !seer.IsAlive()) return retText;
        if (!state.ContainsKey(seerId)) return retText;

        if (state[seer.PlayerId] == FireworkerState.WaitTime && Main.AliveImpostorCount <= 1)
        {
            Logger.Info("Ready to blow up", "Fireworker");
            state[seerId] = FireworkerState.ReadyFire;
            SendRPC(seerId);
            Utils.NotifyRoles(SpecifySeer: seer);
        }
        switch (state[seerId])
        {
            case FireworkerState.Initial:
            case FireworkerState.SettingFireworker:
                retText = string.Format(GetString("FireworkerPutPhase"), nowFireworkerCount[seerId]);
                break;
            case FireworkerState.WaitTime:
                retText = GetString("FireworkerWaitPhase");
                break;
            case FireworkerState.ReadyFire:
                retText = GetString("FireworkerReadyFirePhase");
                break;
            case FireworkerState.FireEnd:
                break;
        }
        return retText;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        if (state[id] == FireworkerState.ReadyFire)
            hud.AbilityButton.OverrideText(GetString("FireworkerExplosionButtonText"));
        else
            hud.AbilityButton.OverrideText(GetString("FireworkerInstallAtionButtonText"));
    }
}
