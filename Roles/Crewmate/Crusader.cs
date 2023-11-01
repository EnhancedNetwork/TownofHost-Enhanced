using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

public static class Crusader
{
    private static readonly int Id = 20050;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, int> CrusaderLimit = new();
    public static OptionItem SkillLimitOpt;
    public static OptionItem SkillCooldown;
    public static Dictionary<byte, float> CurrentKillCooldown = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Crusader);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "CrusaderSkillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Crusader])
            .SetValueFormat(OptionFormat.Seconds);
        SkillLimitOpt = IntegerOptionItem.Create(Id + 11, "CrusaderSkillLimit", new(1, 15, 1), 5, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Crusader])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        CrusaderLimit = new();
        CurrentKillCooldown = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CrusaderLimit.Add(playerId, SkillLimitOpt.GetInt());
        CurrentKillCooldown.Add(playerId, SkillCooldown.GetFloat());
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (CrusaderLimit.ContainsKey(PlayerId))
            CrusaderLimit[PlayerId] = Limit;
        else
            CrusaderLimit.Add(PlayerId, SkillLimitOpt.GetInt());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (CrusaderLimit.TryGetValue(playerId, out var x) ? x : 1) >= 1;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? CurrentKillCooldown[id] : 300f;
    public static string GetSkillLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Crusader).ShadeColor(0.25f) : Color.gray, CrusaderLimit.TryGetValue(playerId, out var constableLimit) ? $"({constableLimit})" : "Invalid");
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (CrusaderLimit[killer.PlayerId] <= 0) return false;
        Main.ForCrusade.Remove(target.PlayerId);
        Main.ForCrusade.Add(target.PlayerId);
        CrusaderLimit[killer.PlayerId]--;
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(killer);
        return false;
    }
}