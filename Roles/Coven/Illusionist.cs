
using Hazel;
using TOHE.Roles.Core;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Modules;
using InnerNet;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using System.Text.RegularExpressions;
using System;
using TOHE.Modules.ChatManager;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using AmongUs.GameOptions;

namespace TOHE.Roles.Coven;

internal class Illusionist : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 30400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Illusionist);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenTrickery;
    //==================================================================\\

    private static OptionItem IllusionCooldown;
    private static OptionItem MaxIllusions;
    private static OptionItem SnitchCanIllusioned;

    private static readonly Dictionary<byte, List<byte>> IllusionedPlayers = [];


    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Illusionist, 1, zeroOne: false);
        IllusionCooldown = FloatOptionItem.Create(Id + 10, "IllusionCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist])
            .SetValueFormat(OptionFormat.Seconds);
        MaxIllusions = IntegerOptionItem.Create(Id + 11, "IllusionistMaxIllusions", new(1, 15, 1), 5, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist])
            .SetValueFormat(OptionFormat.Times);
        SnitchCanIllusioned = BooleanOptionItem.Create(Id + 12, "IllusionistSnitchAffected", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist]);
    }

    public override void Init()
    {
        IllusionedPlayers.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = MaxIllusions.GetInt();
        IllusionedPlayers[playerId] = [];
        GetPlayerById(playerId)?.AddDoubleTrigger();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckDoubleTrigger(target, () =>
            {
                killer.SetKillCooldown();

                if (!IllusionedPlayers[killer.PlayerId].Contains(target.PlayerId))
                {
                    IllusionedPlayers[killer.PlayerId].Add(target.PlayerId);
                }
            })
        ) //this looks so ugly
        {
            var randomDeathReason = ChangeRandomDeath();
            Main.PlayerStates[target.PlayerId].deathReason = randomDeathReason;
            Main.PlayerStates[target.PlayerId].SetDead();
            return true;
        }
        return false;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IllusionCooldown.GetFloat();
    private static PlayerState.DeathReason ChangeRandomDeath()
    {
        PlayerState.DeathReason[] deathReasons = EnumHelper.GetAllValues<PlayerState.DeathReason>().ToArray();
        if (deathReasons.Length == 0 || !deathReasons.Contains(PlayerState.DeathReason.Kill)) deathReasons.AddItem(PlayerState.DeathReason.Kill);
        var random = IRandom.Instance;
        int randomIndex = random.Next(deathReasons.Length);
        return deathReasons[randomIndex];
    }
    public static bool IsNonCovIllusioned(byte target)
    {
        byte pc = Utils.GetPlayerListByRole(CustomRoles.Illusionist).First().PlayerId;
        return IllusionedPlayers[pc].Contains(target) && !GetPlayerById(target).IsPlayerCoven();
    }
    public static bool IsCovIllusioned(byte target)
    {
        byte pc = Utils.GetPlayerListByRole(CustomRoles.Illusionist).First().PlayerId;
        return IllusionedPlayers[pc].Contains(target) && GetPlayerById(target).IsPlayerCoven();
    }
    public override void AfterMeetingTasks()
    {
        IllusionedPlayers.Clear();
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => IllusionedPlayers[seer.PlayerId].Contains(seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.Illusionist), "ø") : string.Empty;
}