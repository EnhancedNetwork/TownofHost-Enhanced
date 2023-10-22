using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class SerialKiller
{
    private static readonly int Id = 1700;
    public static List<byte> playerIdList = new();

    public static bool IsEnable = false;
    private static float OptTimeLimit;

    private static OptionItem KillCooldown;
    private static OptionItem TimeLimit;

    private static Dictionary<byte, float> SuicideTimer = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.SerialKiller);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SerialKiller])
            .SetValueFormat(OptionFormat.Seconds);
        TimeLimit = FloatOptionItem.Create(Id + 11, "SerialKillerLimit", new(5f, 180f, 5f), 60f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SerialKiller])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        SuicideTimer = new();
        IsEnable = false;
    }
    public static void Add(byte serial)
    {
        playerIdList.Add(serial);
        IsEnable = true;
        OptTimeLimit = TimeLimit.GetFloat();
    }
    public static void ApplyKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions(PlayerControl pc)
    {
        AURoleOptions.ShapeshifterCooldown = HasKilled(pc) ? OptTimeLimit : 255f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    ///<summary>
    ///シリアルキラー＋生存＋一人以上キルしている
    ///</summary>
    public static bool HasKilled(PlayerControl pc)
        => pc != null && pc.Is(CustomRoles.SerialKiller) && pc.IsAlive() && Main.PlayerStates[pc.PlayerId].GetKillCount(true) > 0;
    public static void OnCheckMurder(PlayerControl killer, bool CanMurder = true)
    {
        if (!killer.Is(CustomRoles.SerialKiller)) return;
        
        SuicideTimer.Remove(killer.PlayerId);
        if (CanMurder)
            killer.MarkDirtySettings();
    }
    public static void OnReportDeadBody()
    {
        SuicideTimer.Clear();
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!HasKilled(player))
        {
            SuicideTimer.Remove(player.PlayerId);
            return;
        }

        if (!SuicideTimer.TryGetValue(player.PlayerId, out var timer))
        {
            SuicideTimer[player.PlayerId] = 0f;
            player.RpcResetAbilityCooldown();
        }
        else if (timer >= OptTimeLimit)
        {
            Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
            player.RpcMurderPlayerV3(player);
            SuicideTimer.Remove(player.PlayerId);
        }
        else
        {
            SuicideTimer[player.PlayerId] += Time.fixedDeltaTime;
        }
    }
    public static void GetAbilityButtonText(HudManager __instance, PlayerControl pc)
    {
        __instance.AbilityButton.ToggleVisible(pc.IsAlive() && HasKilled(pc));
        __instance.AbilityButton.OverrideText(GetString("SerialKillerSuicideButtonText"));
    }
    public static void AfterMeetingTasks()
    {
        if (!IsEnable) return;

        foreach (var id in playerIdList)
        {
            if (!Main.PlayerStates[id].IsDead)
            {
                var pc = Utils.GetPlayerById(id);
                pc?.RpcResetAbilityCooldown();
                if (HasKilled(pc))
                    SuicideTimer[id] = 0f;
            }
        }
    }
}