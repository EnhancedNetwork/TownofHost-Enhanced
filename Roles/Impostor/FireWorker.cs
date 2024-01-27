using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Fireworker
{
    public enum FireworkerState
    {
        Initial = 1,
        SettingFireworker = 2,
        WaitTime = 4,
        ReadyFire = 8,
        FireEnd = 16,
        CanUseKill = Initial | FireEnd
    }

    private static readonly int Id = 3200;
    public static bool IsEnable = false;

    private static OptionItem FireworkerCount;
    private static OptionItem FireworkerRadius;
    public static OptionItem CanKill;

    public static Dictionary<byte, int> nowFireworkerCount = [];
    private static Dictionary<byte, List<Vector3>> FireworkerPosition = [];
    private static Dictionary<byte, FireworkerState> state = [];
    private static Dictionary<byte, int> FireworkerBombKill = [];
    private static int FireworkerCount = 1;
    private static float FireworkerRadius = 1;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Fireworker);
        FireworkerCount = IntegerOptionItem.Create(Id + 10, "FireworkerMaxCount", new(1, 20, 1), 3, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fireworker])
            .SetValueFormat(OptionFormat.Pieces);
        FireworkerRadius = FloatOptionItem.Create(Id + 11, "FireworkerRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fireworker])
            .SetValueFormat(OptionFormat.Multiplier);
        CanKill = BooleanOptionItem.Create(Id + 12, "CanKill", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Fireworker]);
    }

    public static void Init()
    {
        IsEnable = false;
        nowFireworkerCount = [];
        FireworkerPosition = [];
        state = [];
        FireworkerBombKill = [];
        FireworkerCount = FireworkerCount.GetInt();
        FireworkerRadius = FireworkerRadius.GetFloat();
    }

    public static void Add(byte playerId)
    {
        nowFireworkerCount[playerId] = FireworkerCount;
        FireworkerPosition[playerId] = [];
        state[playerId] = FireworkerState.Initial;
        FireworkerBombKill[playerId] = 0;
        IsEnable = true;
    }

    public static void SendRPC(byte playerId)
    {
        Logger.Info($"Player{playerId}:SendRPC", "Fireworker");
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendFireworkerState, Hazel.SendOption.Reliable, -1);
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

    public static bool CanUseKillButton(PlayerControl pc)
    {
        //            Logger.Info($"Fireworker CanUseKillButton", "Fireworker");
        if (pc.Data.IsDead) return false;
        var canUse = false;
        if ((state[pc.PlayerId] & FireworkerState.CanUseKill) != 0)
        {
            canUse = true;
        }
        if (CanKill.GetBool())
        {
            canUse = true;
        }
        //            Logger.Info($"CanUseKillButton:{canUse}", "Fireworker");
        return canUse;
    }

    public static void ShapeShiftState(PlayerControl pc, bool shapeshifting)
    {
        Logger.Info($"Fireworker ShapeShift", "Fireworker");
        if (pc == null || pc.Data.IsDead || !shapeshifting || Pelican.IsEaten(pc.PlayerId) || Medic.ProtectList.Contains(pc.PlayerId)) return;
        switch (state[pc.PlayerId])
        {
            case FireworkerState.Initial:
            case FireworkerState.SettingFireworker:
                Logger.Info("花火を一個設置", "Fireworker");
                FireworkerPosition[pc.PlayerId].Add(pc.transform.position);
                nowFireworkerCount[pc.PlayerId]--;
                state[pc.PlayerId] = nowFireworkerCount[pc.PlayerId] == 0
                    ? Main.AliveImpostorCount <= 1 ? FireworkerState.ReadyFire : FireworkerState.WaitTime
                    : FireworkerState.SettingFireworker;
                break;
            case FireworkerState.ReadyFire:
                Logger.Info("花火を爆破", "Fireworker");
                bool suicide = false;
                foreach (var target in Main.AllAlivePlayerControls)
                {
                    foreach (var pos in FireworkerPosition[pc.PlayerId])
                    {
                        var dis = Vector2.Distance(pos, target.transform.position);
                        if (dis > FireworkerRadius) continue;

                        if (target == pc)
                        {
                            //自分は後回し
                            suicide = true;
                        }
                        else
                        {
                            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                            target.SetRealKiller(pc);
                            target.RpcMurderPlayerV3(target);
                            Medic.IsDead(target);
                        }
                    }
                }
                if (suicide)
                {
                    var totalAlive = Main.AllAlivePlayerControls.Length;
                    //自分が最後の生き残りの場合は勝利のために死なない
                    if (totalAlive != 1)
                    {
                        Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                        pc.RpcMurderPlayerV3(pc);
                    }
                }
                state[pc.PlayerId] = FireworkerState.FireEnd;
                break;
            default:
                break;
        }
        SendRPC(pc.PlayerId);
        Utils.NotifyRoles(ForceLoop: true);
    }

    public static string GetStateText(PlayerControl pc)
    {
        string retText = "";
        if (pc == null || pc.Data.IsDead) return retText;

        if (state[pc.PlayerId] == FireworkerState.WaitTime && Main.AliveImpostorCount <= 1)
        {
            Logger.Info("爆破準備OK", "Fireworker");
            state[pc.PlayerId] = FireworkerState.ReadyFire;
            SendRPC(pc.PlayerId);
            Utils.NotifyRoles(SpecifySeer: pc);
        }
        switch (state[pc.PlayerId])
        {
            case FireworkerState.Initial:
            case FireworkerState.SettingFireworker:
                retText = string.Format(GetString("FireworkerPutPhase"), nowFireworkerCount[pc.PlayerId]);
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
}