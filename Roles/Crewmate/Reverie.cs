using AmongUs.GameOptions;
using System;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Reverie : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Reverie;
    private const int Id = 11100;

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem IncreaseKillCooldown;
    private static OptionItem MinKillCooldown;
    private static OptionItem MaxKillCooldown;
    private static OptionItem MisfireSuicide;
    private static OptionItem ResetCooldownMeeting;
    private static OptionItem ConvertedReverieRogue;

    private static readonly Dictionary<byte, float> NowCooldown = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Reverie);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.DefaultKillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.ReduceKillCooldown, new(0f, 180f, 2.5f), 7.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, GeneralOption.MinKillCooldown, new(0f, 180f, 2.5f), 2.5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        IncreaseKillCooldown = FloatOptionItem.Create(Id + 13, "ReverieIncreaseKillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        MaxKillCooldown = FloatOptionItem.Create(Id + 14, "ReverieMaxKillCooldown", new(0f, 180f, 2.5f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie])
            .SetValueFormat(OptionFormat.Seconds);
        MisfireSuicide = BooleanOptionItem.Create(Id + 15, "ReverieMisfireSuicide", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie]);
        ResetCooldownMeeting = BooleanOptionItem.Create(Id + 16, "ReverieResetCooldownMeeting", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie]);
        ConvertedReverieRogue = BooleanOptionItem.Create(Id + 17, "ConvertedReverieKillAll", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Reverie]);
    }
    public override void Init()
    {
        NowCooldown.Clear();
    }
    public override void Add(byte playerId)
    {
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
    }
    public override void Remove(byte playerId)
    {
        NowCooldown.Remove(playerId);
    }
    public override void OnReportDeadBody(PlayerControl HES, NetworkedPlayerInfo HIM)
    {
        foreach (var playerId in NowCooldown.Keys)
        {
            if (ResetCooldownMeeting.GetBool())
            {
                NowCooldown[playerId] = DefaultKillCooldown.GetFloat();
            }
        }
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => false;

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return true;

        float kcd;
        if ((!target.GetCustomRole().IsCrewmate() && !target.Is(CustomRoles.Trickster)) || (ConvertedReverieRogue.GetBool() && killer.GetCustomSubRoles().Any(subrole => subrole.IsConverted() || subrole == CustomRoles.Madmate))) // if killed non crew or if converted
            kcd = NowCooldown[killer.PlayerId] - ReduceKillCooldown.GetFloat();
        else kcd = NowCooldown[killer.PlayerId] + IncreaseKillCooldown.GetFloat();
        NowCooldown[killer.PlayerId] = Math.Clamp(kcd, MinKillCooldown.GetFloat(), MaxKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();
        if (NowCooldown[killer.PlayerId] >= MaxKillCooldown.GetFloat() && MisfireSuicide.GetBool())
        {
            killer.SetDeathReason(PlayerState.DeathReason.Misfire);
            killer.RpcMurderPlayer(killer);
        }
        return true;
    }
}
