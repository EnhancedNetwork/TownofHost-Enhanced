using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Neutral;
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

    private static readonly int Id = 3200;
    public static bool On;
    public override bool IsEnable => On;

    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => nowFireworkerCount[player.PlayerId] == 0 ? CustomButton.Get("FireworkD") : CustomButton.Get("FireworkP");

    private static OptionItem FireworkerCount;
    private static OptionItem FireworkerRadius;
    private static OptionItem CanKill;

    private static Dictionary<byte, int> nowFireworkerCount = [];
    private static Dictionary<byte, List<Vector3>> FireworkerPosition = [];
    private static Dictionary<byte, FireworkerState> state = [];
    private static Dictionary<byte, int> FireworkerBombKill = [];
    private static int fireworkerCount = 1;
    private static float fireworkerRadius = 1;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Fireworker);
        FireworkerCount = IntegerOptionItem.Create(Id + 10, "FireworkerMaxCount", new(1, 20, 1), 3, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fireworker])
            .SetValueFormat(OptionFormat.Pieces);
        FireworkerRadius = FloatOptionItem.Create(Id + 11, "FireworkerRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fireworker])
            .SetValueFormat(OptionFormat.Multiplier);
        CanKill = BooleanOptionItem.Create(Id + 12, "CanKill", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fireworker]);
    }

    public override void Init()
    {
        On = false;
        nowFireworkerCount = [];
        FireworkerPosition = [];
        state = [];
        FireworkerBombKill = [];

        fireworkerCount = FireworkerCount.GetInt();
        fireworkerRadius = FireworkerRadius.GetFloat();
    }

    public override void Add(byte playerId)
    {
        nowFireworkerCount[playerId] = fireworkerCount;
        FireworkerPosition[playerId] = [];
        state.TryAdd(playerId, FireworkerState.Initial);
        FireworkerBombKill[playerId] = 0;
        On = true;
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

    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        Logger.Info($"Fireworker ShapeShift", "Fireworker");
        if (!shapeshifting && !shapeshiftIsHidden) return;
        if (shapeshifter == null || shapeshifter.Data.IsDead || !shapeshifting || Pelican.IsEaten(shapeshifter.PlayerId)) return;

        var shapeshifterId = shapeshifter.PlayerId;
        switch (state[shapeshifterId])
        {
            case FireworkerState.Initial:
            case FireworkerState.SettingFireworker:
                Logger.Info("One firework set up", "Fireworker");

                if (shapeshiftIsHidden)
                    shapeshifter.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);

                FireworkerPosition[shapeshifterId].Add(shapeshifter.transform.position);
                nowFireworkerCount[shapeshifterId]--;
                state[shapeshifterId] = nowFireworkerCount[shapeshifterId] == 0
                    ? Main.AliveImpostorCount <= 1 ? FireworkerState.ReadyFire : FireworkerState.WaitTime
                    : FireworkerState.SettingFireworker;
                break;
            case FireworkerState.ReadyFire:
                Logger.Info("Blowing up fireworks", "Fireworker");
                bool suicide = false;
                foreach (var player in Main.AllAlivePlayerControls)
                {
                    foreach (var pos in FireworkerPosition[shapeshifterId].ToArray())
                    {
                        var dis = Vector2.Distance(pos, player.transform.position);
                        if (dis > fireworkerRadius) continue;

                        if (player == shapeshifter)
                        {
                            suicide = true;
                        }
                        else
                        {
                            Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                            player.SetRealKiller(shapeshifter);
                            player.RpcMurderPlayerV3(player);
                        }
                    }
                }
                if (suicide)
                {
                    var totalAlive = Main.AllAlivePlayerControls.Length;
                    if (totalAlive != 1)
                    {
                        Main.PlayerStates[shapeshifterId].deathReason = PlayerState.DeathReason.Misfire;
                        shapeshifter.RpcMurderPlayerV3(shapeshifter);
                    }

                    if (shapeshiftIsHidden)
                        shapeshifter.SyncSettings();
                    else
                        shapeshifter.MarkDirtySettings();
                }
                state[shapeshifterId] = FireworkerState.FireEnd;
                break;
            default:
                break;
        }
        SendRPC(shapeshifterId);
        Utils.NotifyRoles(ForceLoop: true);
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
            hud.AbilityButton.OverrideText(GetString("FireWorksExplosionButtonText"));
        else
            hud.AbilityButton.OverrideText(GetString("FireWorksInstallAtionButtonText"));
    }
}