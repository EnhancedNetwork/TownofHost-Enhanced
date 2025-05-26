using AmongUs.GameOptions;
using Hazel;
using System;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;

namespace TOHE.Roles.Impostor;

internal class QuickShooter : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.QuickShooter;
    private const int Id = 2200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.QuickShooter);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem MeetingReserved;
    private static OptionItem LimitBySSCoolDown;
    private static OptionItem SSCoolDown;

    private bool Storaging = false;
    private readonly Dictionary<byte, int> NewSL = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.QuickShooter);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 35f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        LimitBySSCoolDown = BooleanOptionItem.Create(Id + 11, "QucikShooterLimitBySS", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter]);
        SSCoolDown = FloatOptionItem.Create(Id + 12, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(LimitBySSCoolDown)
            .SetValueFormat(OptionFormat.Seconds);
        MeetingReserved = IntegerOptionItem.Create(Id + 14, "MeetingReserved", new(0, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Pieces);
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(0);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = LimitBySSCoolDown.GetBool() ? SSCoolDown.GetFloat() : 0.01f;
    }

    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = (Storaging || id.GetAbilityUseLimit() < 1) ? KillCooldown.GetFloat() : 0.1f;
        Storaging = false;
    }

    public void SendRPC(bool timer = false)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);

        if (_Player == null) { timer = false; }
        writer.Write(timer);
        if (timer)
            writer.Write(_Player.GetKillTimer());
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var shouldtime = reader.ReadBoolean();
        float timer = 0f;
        if (shouldtime)
        {
            timer = reader.ReadSingle();
        }

        if (pc.AmOwner && shouldtime)
            DestroyableSingleton<HudManager>.Instance.AbilityButton.SetCoolDown(timer, 0.01f);
    }

    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        var killTimer = shapeshifter.GetKillTimer();
        Logger.Info($"Kill Timer: {killTimer}", "QuickShooter");

        if (killTimer <= 0)
        {
            shapeshifter.RpcIncreaseAbilityUseLimitBy(1);

            Storaging = true;
            shapeshifter.ResetKillCooldown();
            shapeshifter.SetKillCooldown();

            shapeshifter.Notify(Translator.GetString("QuickShooterStoraging"));
        }
        else
        {
            shapeshifter.Notify(Translator.GetString("QuickShooterFailed"));
            SendRPC(true);
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (_Player == null) return;

        NewSL[_Player.PlayerId] = Math.Clamp((int)_Player.GetAbilityUseLimit(), 0, MeetingReserved.GetInt());
        _Player.SetAbilityUseLimit(NewSL[_state.PlayerId]);
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0)
        {
            killer.RpcRemoveAbilityUse();
        }
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton?.OverrideText(Translator.GetString("QuickShooterShapeshiftText"));
        hud.AbilityButton?.SetUsesRemaining((int)playerId.GetAbilityUseLimit());
    }
}
