using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

internal class Addict : RoleBase
{
    private const int Id = 6300;
    private static List<byte> playerIdList = [];

    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

    public static OptionItem VentCooldown;
    public static OptionItem TimeLimit;
    public static OptionItem ImmortalTimeAfterVent;
    public static OptionItem FreezeTimeAfterImmortal;

    private static Dictionary<byte, float> SuicideTimer = [];
    private static Dictionary<byte, float> ImmortalTimer = [];

    private static float DefaultSpeed = new();


    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Addict);
        VentCooldown = FloatOptionItem.Create(Id + 11, "VentCooldown", new(5f, 180f, 1f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Addict])
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
        playerIdList = [];
        SuicideTimer = [];
        ImmortalTimer = [];
        DefaultSpeed = new();
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SuicideTimer.TryAdd(playerId, -10f);
        ImmortalTimer.TryAdd(playerId, 420f);
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
        On = true;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        SuicideTimer.Remove(playerId);
        ImmortalTimer.Remove(playerId);
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
        if (playerIdList.Count <= 0) On = false;
    }

    public static bool IsImmortal(PlayerControl player) => player.Is(CustomRoles.Addict) && ImmortalTimer[player.PlayerId] <= ImmortalTimeAfterVent.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        return !IsImmortal(target);
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        foreach (var player in playerIdList.ToArray())
        {
            SuicideTimer[player] = -10f;
            ImmortalTimer[player] = 420f;
            Main.AllPlayerSpeed[player] = DefaultSpeed;
        }
    }
    public override void OnFixedUpdateLowLoad(PlayerControl player)
    {
        if (!SuicideTimer.ContainsKey(player.PlayerId) || !player.IsAlive()) return;

        if (SuicideTimer[player.PlayerId] >= TimeLimit.GetFloat())
        {
            Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
            player.RpcMurderPlayerV3(player);
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
        if (!IsEnable) return;
        if (!pc.Is(CustomRoles.Addict)) return;

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
}
