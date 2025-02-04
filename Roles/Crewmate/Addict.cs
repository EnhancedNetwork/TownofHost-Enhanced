using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Addict : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Addict;
    private const int Id = 6300;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    public override bool BlockMoveInVent(PlayerControl pc) => true;
    //==================================================================\\

    private static OptionItem VentCooldown;
    private static OptionItem TimeLimit;
    private static OptionItem ImmortalTimeAfterVent;
    private static OptionItem FreezeTimeAfterImmortal;

    private static readonly Dictionary<byte, float> SuicideTimer = [];
    private static readonly Dictionary<byte, float> ImmortalTimer = [];

    private static float DefaultSpeed = new();

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Addict);
        VentCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.EngineerBase_VentCooldown, new(5f, 180f, 1f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
            .SetValueFormat(OptionFormat.Seconds);
        TimeLimit = FloatOptionItem.Create(Id + 12, "AddictSuicideTimer", new(5f, 180f, 1f), 45f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
            .SetValueFormat(OptionFormat.Seconds);
        ImmortalTimeAfterVent = FloatOptionItem.Create(Id + 13, "AddictInvulnerbilityTimeAfterVent", new(0f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
            .SetValueFormat(OptionFormat.Seconds);
        FreezeTimeAfterImmortal = FloatOptionItem.Create(Id + 15, "AddictFreezeTimeAfterInvulnerbility", new(0f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        SuicideTimer.Clear();
        ImmortalTimer.Clear();
        DefaultSpeed = new();

    }
    public override void Add(byte playerId)
    {
        SuicideTimer.TryAdd(playerId, -10f);
        ImmortalTimer.TryAdd(playerId, 420f);
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
    }
    public override void Remove(byte playerId)
    {
        SuicideTimer.Remove(playerId);
        ImmortalTimer.Remove(playerId);
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = VentCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }

    private static bool IsImmortal(PlayerControl player) => ImmortalTimer[player.PlayerId] <= ImmortalTimeAfterVent.GetFloat();

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        return !IsImmortal(target);
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var player in _playerIdList.ToArray())
        {
            SuicideTimer[player] = -10f;
            ImmortalTimer[player] = 420f;
            Main.AllPlayerSpeed[player] = DefaultSpeed;
        }
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!player.IsAlive() || !SuicideTimer.TryGetValue(player.PlayerId, out var timer)) return;

        if (timer >= TimeLimit.GetFloat())
        {
            player.SetDeathReason(PlayerState.DeathReason.Suicide);
            player.RpcMurderPlayer(player);
            SuicideTimer.Remove(player.PlayerId);
        }
        else
        {
            SuicideTimer[player.PlayerId] += Time.fixedDeltaTime;

            if (IsImmortal(player))
            {
                ImmortalTimer[player.PlayerId] += Time.fixedDeltaTime;
            }
            else
            {
                if (ImmortalTimer[player.PlayerId] != 420f && FreezeTimeAfterImmortal.GetFloat() > 0)
                {
                    AddictGetDown(player);
                    ImmortalTimer[player.PlayerId] = 420f;
                }
            }
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        SuicideTimer[pc.PlayerId] = 0f;
        ImmortalTimer[pc.PlayerId] = 0f;

        //   Main.AllPlayerSpeed[pc.PlayerId] = SpeedWhileImmortal.GetFloat();
        pc.MarkDirtySettings();
    }

    private static void AddictGetDown(PlayerControl addict)
    {
        Main.AllPlayerSpeed[addict.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[addict.PlayerId] = false;
        addict.MarkDirtySettings();

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[addict.PlayerId] = DefaultSpeed;
            ReportDeadBodyPatch.CanReport[addict.PlayerId] = true;
            addict.MarkDirtySettings();
        }, FreezeTimeAfterImmortal.GetFloat(), "AddictGetDown");
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(Translator.GetString("AddictVentButtonText"));
    }
}
