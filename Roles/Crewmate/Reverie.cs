using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE;

internal class Reverie : RoleBase
{
    private const int Id = 11100;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Retributionist.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem IncreaseKillCooldown;
    private static OptionItem MinKillCooldown;
    private static OptionItem MaxKillCooldown;
    private static OptionItem MisfireSuicide;
    private static OptionItem ResetCooldownMeeting;
    private static OptionItem ConvertedReverieRogue;

    private static List<byte> playerIdList = [];
    private static Dictionary<byte, float> NowCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Reverie);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "ArroganceDefaultKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "ArroganceReduceKillCooldown", new(0f, 180f, 2.5f), 7.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, "ArroganceMinKillCooldown", new(0f, 180f, 2.5f), 2.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        IncreaseKillCooldown = FloatOptionItem.Create(Id + 13, "ReverieIncreaseKillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        MaxKillCooldown = FloatOptionItem.Create(Id + 14, "ReverieMaxKillCooldown", new(0f, 180f, 2.5f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        MisfireSuicide =  BooleanOptionItem.Create(Id + 15, "ReverieMisfireSuicide", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie]);
        ResetCooldownMeeting =  BooleanOptionItem.Create(Id + 16, "ReverieResetCooldownMeeting", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie]);
        ConvertedReverieRogue = BooleanOptionItem.Create(Id + 17, "ConvertedReverieKillAll", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie]);
    }
    public override void Init()
    {
        playerIdList = [];
        NowCooldown = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
        On = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        NowCooldown.Remove(playerId);
    }
    public override void OnReportDeadBody(PlayerControl HES, PlayerControl HIM)
    {
        foreach(var playerId in NowCooldown.Keys)
        {
            if (ResetCooldownMeeting.GetBool())
            {
                NowCooldown[playerId] = DefaultKillCooldown.GetFloat();
            }
        }
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool OnCheckMurderAsKiller(PlayerControl killer,PlayerControl target)
    {
        if (killer == null || target == null) return true;
        if (!HasEnabled || !killer.Is(CustomRoles.Reverie)) return true;
        float kcd;
        if ((!target.GetCustomRole().IsCrewmate() && !target.Is(CustomRoles.Trickster)) || (ConvertedReverieRogue.GetBool() && killer.GetCustomSubRoles().Any(subrole => subrole.IsConverted() || subrole == CustomRoles.Madmate))) // if killed non crew or if converted
                kcd = NowCooldown[killer.PlayerId] - ReduceKillCooldown.GetFloat();
        else kcd = NowCooldown[killer.PlayerId] + IncreaseKillCooldown.GetFloat();
        NowCooldown[killer.PlayerId] = Math.Clamp(kcd, MinKillCooldown.GetFloat(), MaxKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();
        if (NowCooldown[killer.PlayerId] >= MaxKillCooldown.GetFloat() && MisfireSuicide.GetBool())
        {
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
            killer.RpcMurderPlayerV3(killer);
        }
        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.SabotageButton.ToggleVisible(false);
        hud.AbilityButton.ToggleVisible(false);
        hud.ImpostorVentButton.ToggleVisible(false);
    }
}