﻿using AmongUs.GameOptions;
using System;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles._Ghosts_.Crewmate;

internal class Hawk : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 28000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Hawk);
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateGhosts;
    //==================================================================\\

    public static OptionItem KillCooldown;
    public static OptionItem HawkCanKillNum;
    public static OptionItem MinimumPlayersAliveToKill;
    public static OptionItem MissChance;
    public static OptionItem IncreaseByOneIfConvert;
    
    public readonly Dictionary<byte, float> KillerChanceMiss = [];
    public int KeepCount = 0;
    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Hawk);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 120f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Seconds);
        HawkCanKillNum = IntegerOptionItem.Create(Id + 11, "HawkCanKillNum", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Players);
        MissChance = FloatOptionItem.Create(Id + 12, "MissChance", new(0f, 97.5f, 2.5f), 85f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Percent);
        MinimumPlayersAliveToKill = IntegerOptionItem.Create(Id + 13, "MinimumPlayersAliveToKill", new(0, 15, 1), 4, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk])
            .SetValueFormat(OptionFormat.Players);
        IncreaseByOneIfConvert = BooleanOptionItem.Create(Id + 14, "IncreaseByOneIfConvert", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hawk]);
    }

    public override void Add(byte PlayerId)
    {
        AbilityLimit = HawkCanKillNum.GetInt();

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.IsAnySubRole(x => x.IsConverted()))
            {
                KeepCount++;
            }
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        int ThisCount = 0;
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.IsAnySubRole(x => x.IsConverted()))
            {
                ThisCount++;
            }
        }
        if (ThisCount > KeepCount && IncreaseByOneIfConvert.GetBool())
        {
            KeepCount++;
            AbilityLimit += ThisCount - KeepCount;
        }

    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = KillCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (!KillerChanceMiss.ContainsKey(target.PlayerId))
            KillerChanceMiss.Add(target.PlayerId, MissChance.GetFloat());

        if (CheckRetriConflicts(target) && killer.RpcCheckAndMurder(target, true))
        {
            target.SetDeathReason(PlayerState.DeathReason.Slice);
            killer.RpcMurderPlayer(target);
            killer.RpcResetAbilityCooldown();
            AbilityLimit--;
            SendSkillRPC();
        }
        else if (AbilityLimit <= 0) killer.Notify(GetString("HawkKillMax"));
        else if (Main.AllAlivePlayerControls.Length < MinimumPlayersAliveToKill.GetInt()) killer.Notify(GetString("HawkKillTooManyDead"));
        else
        {
            killer.RpcResetAbilityCooldown();
            AbilityLimit--;
            SendSkillRPC();
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Hawk), GetString("HawkMissed")));
        }

        Logger.Info($" {target.GetRealName()}'s DieChance is :{100f - KillerChanceMiss[target.PlayerId]}%", "Hawk");
        KillerChanceMiss[target.PlayerId] -= Math.Clamp(35, 0, KillerChanceMiss[target.PlayerId] - 10);
        return false;
    }

    private bool CheckRetriConflicts(PlayerControl target)
    {
        var rnd = IRandom.Instance;

        return target != null && Main.AllAlivePlayerControls.Length >= MinimumPlayersAliveToKill.GetInt()
            && AbilityLimit > 0
            && rnd.Next(100) >= KillerChanceMiss[target.PlayerId]
            && !target.IsNeutralApocalypse()
            && !target.Is(CustomRoles.Jinx)
            && !target.Is(CustomRoles.CursedWolf)
            && (!target.Is(CustomRoles.NiceMini) || Mini.Age > 18);
    }
    public override string GetProgressText(byte playerId, bool coms) 
        => ColorString(AbilityLimit > 0 ? GetRoleColor(CustomRoles.Hawk).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");

}


