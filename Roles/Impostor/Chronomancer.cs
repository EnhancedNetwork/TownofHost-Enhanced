using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;
public static class Chronomancer
{
    private static readonly int Id = 900;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, long> firstKill = new();
    public static Dictionary<byte, long> lastCooldownStart = new();
    public static Dictionary<byte, float> ChargedTime = new();

    private static OptionItem KillCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Chronomancer);
        KillCooldown = FloatOptionItem.Create(Id + 10, "ChronomancerKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chronomancer])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static void Init()
    {
        playerIdList = new();
        firstKill = new();
        lastCooldownStart = new();
        ChargedTime = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        long now = Utils.GetTimeStamp();
        playerIdList.Add(playerId);
        firstKill.Add(playerId, -1);
        ChargedTime.Add(playerId, 0);
        lastCooldownStart.Add(playerId, now);
        IsEnable = true;
    }

    public static void SetKillCooldown(byte id)
    {
        long now = Utils.GetTimeStamp();

        if (firstKill[id] == -1)
        {
            Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
            lastCooldownStart[id] = now;
            return;
        }
        if (now - firstKill[id] >= ChargedTime[id])
        {
            firstKill[id] = -1;
            Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
            lastCooldownStart[id] = now;
        }
        else Main.AllPlayerKillCooldown[id] = 0f;
        Logger.Info($"{Utils.GetPlayerById(id).GetNameWithRole()} kill cd set to {Main.AllPlayerKillCooldown[id]}", "Chronomancer");
    }

    public static void AfterMeetingTask()
    {
        if (!IsEnable) return;

        long now = Utils.GetTimeStamp();
        foreach (var playerId in playerIdList)
        {
            if (Utils.GetPlayerById(playerId).IsAlive())
            { 
                firstKill[playerId] =  -1;
                lastCooldownStart[playerId] = now;
                ChargedTime[playerId] = 0;
                SetKillCooldown(playerId);
            }
           
        }
    }

    public static void OnCheckMurder(PlayerControl killer)
    {
        long now = Utils.GetTimeStamp();
        if (firstKill[killer.PlayerId] == -1)
        {
            firstKill[killer.PlayerId] = now;
            ChargedTime[killer.PlayerId] = (firstKill[killer.PlayerId] - lastCooldownStart[killer.PlayerId]) - KillCooldown.GetFloat();
        }
        SetKillCooldown(killer.PlayerId);
    }
}