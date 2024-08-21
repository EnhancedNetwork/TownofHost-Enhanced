﻿using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Spiritcaller : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 25200;
    public static bool HasEnabled = CustomRoleManager.HasEnabled(CustomRoles.Spiritcaller);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem ImpostorVision;
    private static OptionItem SpiritMax;
    public static OptionItem SpiritAbilityCooldown;
    private static OptionItem SpiritFreezeTime;
    private static OptionItem SpiritProtectTime;
    private static OptionItem SpiritCauseVision;
    private static OptionItem SpiritCauseVisionTime;

    private static readonly Dictionary<byte, long> PlayersHaunted = [];

    private long? ProtectTimeStamp = new();

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Spiritcaller, 1);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 1f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller]);
        ImpostorVision = BooleanOptionItem.Create(Id + 12, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller]);
        SpiritMax = IntegerOptionItem.Create(Id + 13, "SpiritcallerSpiritMax", new(1, 15, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
            .SetValueFormat(OptionFormat.Times);
        SpiritAbilityCooldown = FloatOptionItem.Create(Id + 14, "SpiritcallerSpiritAbilityCooldown", new(5f, 90f, 1f), 35f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
            .SetValueFormat(OptionFormat.Seconds);
        SpiritFreezeTime = FloatOptionItem.Create(Id + 15, "SpiritcallerFreezeTime", new(0f, 30f, 1f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
            .SetValueFormat(OptionFormat.Seconds);
        SpiritProtectTime = FloatOptionItem.Create(Id + 16, "SpiritcallerProtectTime", new(0f, 30f, 1f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
            .SetValueFormat(OptionFormat.Seconds);
        SpiritCauseVision = FloatOptionItem.Create(Id + 17, "SpiritcallerCauseVision", new(0f, 5f, 0.05f), 0.2f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
            .SetValueFormat(OptionFormat.Multiplier);
        SpiritCauseVisionTime = FloatOptionItem.Create(Id + 18, "SpiritcallerCauseVisionTime", new(0f, 45f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Spiritcaller])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        PlayersHaunted.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = SpiritMax.GetInt();
        ProtectTimeStamp = 0;

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateLowLoadOthers.Add(OnFixedUpdateOthers);
        }
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    private bool InProtect(PlayerControl player) => player.Is(CustomRoles.Spiritcaller) && ProtectTimeStamp > Utils.GetTimeStamp();

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(ImpostorVision.GetBool());

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!target.GetCustomRole().IsAbleToBeSidekicked() && !target.GetCustomRole().IsImpostor())
        {
            if (AbilityLimit < 1) return true;

            AbilityLimit--;
            SendSkillRPC();

            target.RpcSetCustomRole(CustomRoles.EvilSpirit);

            var writer = CustomRpcSender.Create("SpiritCallerSendMessage", SendOption.None);
            writer.StartMessage(target.GetClientId());
            writer.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                .Write(target.Data.NetId)
                .Write(GetString("SpiritcallerNoticeTitle"))
                .EndRpc();
            writer.StartRpc(target.NetId, (byte)RpcCalls.SendChat)
                .Write(GetString("SpiritcallerNoticeMessage"))
                .EndRpc();
            writer.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                .Write(target.Data.NetId)
                .Write(target.Data.PlayerName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }
        return true;
    }

    private void OnFixedUpdateOthers(PlayerControl pc)
    {
        if (pc.Is(CustomRoles.Spiritcaller))
        {
            if (ProtectTimeStamp < Utils.GetTimeStamp() && ProtectTimeStamp != 0)
            {
                ProtectTimeStamp = 0;
            }
        }
        else if (PlayersHaunted.ContainsKey(pc.PlayerId) && PlayersHaunted[pc.PlayerId] < Utils.GetTimeStamp())
        {
            PlayersHaunted.Remove(pc.PlayerId);
            pc.MarkDirtySettings();
        }
    }

    public override string GetProgressText(byte PlayerId, bool cooooms) => Utils.ColorString(AbilityLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Spiritcaller) : Color.gray, $"({AbilityLimit})");

    public static void HauntPlayer(PlayerControl target)
    {
        if (SpiritCauseVisionTime.GetFloat() > 0 || SpiritFreezeTime.GetFloat() > 0)
        {
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Spiritcaller), GetString("HauntedByEvilSpirit")));
        }

        if (SpiritCauseVisionTime.GetFloat() > 0 && !PlayersHaunted.ContainsKey(target.PlayerId))
        {
            long time = Utils.GetTimeStamp() + (long)SpiritCauseVisionTime.GetFloat();
            PlayersHaunted.Add(target.PlayerId, time);
        }

        if (SpiritFreezeTime.GetFloat() > 0)
        {
            var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
            Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
            target.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
                target.MarkDirtySettings();
                RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
            }, SpiritFreezeTime.GetFloat(), "Spirit UnFreeze");
        }
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (InProtect(target))
        {
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill();
            return false;
        }
        return true;
    }

    public static void ReduceVision(IGameOptions opt, PlayerControl target)
    {
        if (PlayersHaunted.ContainsKey(target.PlayerId))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, SpiritCauseVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, SpiritCauseVision.GetFloat());
        }
    }

    public void ProtectSpiritcaller()
    {
        ProtectTimeStamp = Utils.GetTimeStamp() + (long)SpiritProtectTime.GetFloat();
    }
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
        => seer.Is(CustomRoles.Spiritcaller) && target.Is(CustomRoles.EvilSpirit) ? Main.roleColors[CustomRoles.EvilSpirit] : "";

}