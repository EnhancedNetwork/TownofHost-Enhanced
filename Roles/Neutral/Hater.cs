using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Hater
{
    private static readonly int Id = 12900;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static OptionItem CanVent;
    public static OptionItem ChooseConverted;
    public static OptionItem MisFireKillTarget;

    public static OptionItem CanKillLovers;
    public static OptionItem CanKillMadmate;
    public static OptionItem CanKillCharmed;
    public static OptionItem CanKillAdmired;
    public static OptionItem CanKillSidekicks;
    public static OptionItem CanKillEgoists;
    public static OptionItem CanKillInfected;
    public static OptionItem CanKillContagious;

    public static bool isWon = false; // There's already a playerIdList, so replaced this with a boolean value
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Hater, zeroOne: false);
        MisFireKillTarget = BooleanOptionItem.Create(Id + 11, "HaterMisFireKillTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hater]);
        ChooseConverted = BooleanOptionItem.Create(Id + 12, "HaterChooseConverted", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hater]);
        CanKillMadmate = BooleanOptionItem.Create(Id + 13, "HaterCanKillMadmate", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillCharmed = BooleanOptionItem.Create(Id + 14, "HaterCanKillCharmed", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillLovers = BooleanOptionItem.Create(Id + 15, "HaterCanKillLovers", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillSidekicks = BooleanOptionItem.Create(Id + 16, "HaterCanKillSidekick", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillEgoists = BooleanOptionItem.Create(Id + 17, "HaterCanKillEgoist", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillInfected = BooleanOptionItem.Create(Id + 18, "HaterCanKillInfected", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillContagious = BooleanOptionItem.Create(Id + 19, "HaterCanKillContagious", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        CanKillAdmired = BooleanOptionItem.Create(Id + 20, "HaterCanKillAdmired", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
    }

    public static void Init()
    {
        playerIdList = [];
        IsEnable = false;
        isWon = false;
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (killer.PlayerId == target.PlayerId) return true;  // Return true to allow suicides

        if (target.GetCustomSubRoles().Any(x => x.IsConverted() || x == CustomRoles.Madmate || x == CustomRoles.Admired)
            || IsConvertedMainRole(target.GetCustomRole()))
        {
            if (!ChooseConverted.GetBool())
            {
                if (killer.RpcCheckAndMurder(target)) isWon = true; // Only win if target can be killed - this kills the target if they can be killed
                Logger.Info($"{killer.GetRealName()} killed right target case 1", "FFF");
                return false;  // The murder is already done if it could be done, so return false to avoid double killing
            }
            else if (
                ((target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Gangster)) && CanKillMadmate.GetBool())
                || ((target.Is(CustomRoles.Charmed) || target.Is(CustomRoles.Succubus)) && CanKillCharmed.GetBool())
                || ((target.Is(CustomRoles.Lovers) || target.Is(CustomRoles.Ntr)) && CanKillLovers.GetBool())
                || ((target.Is(CustomRoles.Romantic) || target.Is(CustomRoles.RuthlessRomantic) || target.Is(CustomRoles.VengefulRomantic)
                    || Romantic.BetPlayer.ContainsValue(target.PlayerId)) && CanKillLovers.GetBool())
                || ((target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit)) && CanKillSidekicks.GetBool())
                || (target.Is(CustomRoles.Egoist) && CanKillEgoists.GetBool())
                || ((target.Is(CustomRoles.Infected) || target.Is(CustomRoles.Infectious)) && CanKillInfected.GetBool())
                || ((target.Is(CustomRoles.Contagious) || target.Is(CustomRoles.Virus)) && CanKillContagious.GetBool())
                || ((target.Is(CustomRoles.Admired) || target.Is(CustomRoles.Admirer)) && CanKillAdmired.GetBool())
                )
            {
                if (killer.RpcCheckAndMurder(target)) isWon = true; // Only win if target can be killed - this kills the target if they can be killed
                Logger.Info($"{killer.GetRealName()} killed right target case 2", "FFF");
                return false;  // The murder is already done if it could be done, so return false to avoid double killing
            }
        }
        //Not return trigger following fail check ---- I'm sorry, what?
        if (MisFireKillTarget.GetBool() && killer.RpcCheckAndMurder(target, true)) // RpcCheckAndMurder checks if the target can be murdered or not (checks for shields and other stuff); the 'true' parameter indicates that we just want a check, and not murder yet.
        {
            target.SetRealKiller(killer);
            target.Data.IsDead = true;
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
            killer.RpcMurderPlayerV3(target); // Murder the target only if the setting is on and the target can be killed

        }
        killer.Data.IsDead = true;
        Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
        killer.RpcMurderPlayerV3(killer);
        Main.PlayerStates[killer.PlayerId].SetDead();
        Logger.Info($"{killer.GetRealName()} killed incorrect target => misfire", "FFF");
        return false;
    }

    private static bool IsConvertedMainRole(CustomRoles role)
    {
        return role switch  // Use the switch expression whenever possible instead of the switch statement to improve performance
        {
            CustomRoles.Gangster or
            CustomRoles.Succubus or
            CustomRoles.Romantic or
            CustomRoles.RuthlessRomantic or
            CustomRoles.VengefulRomantic or
            CustomRoles.Sidekick or
            CustomRoles.Jackal or
            CustomRoles.Virus or
            CustomRoles.Infectious or
            CustomRoles.Admirer
            => true,

            _ => false,
        };
    }
}
