
using Hazel;
using TOHE.Roles.Core;
using InnerNet;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using System;
using UnityEngine;

namespace TOHE.Roles.Coven;

internal class Illusionist : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 30400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Illusionist);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenTrickery;
    //==================================================================\\

    private static OptionItem IllusionCooldown;
    private static OptionItem MaxIllusions;
    public static OptionItem SnitchCanIllusioned;

    private static readonly Dictionary<byte, List<byte>> IllusionedPlayers = [];


    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Illusionist, 1, zeroOne: false);
        IllusionCooldown = FloatOptionItem.Create(Id + 10, "IllusionCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist])
            .SetValueFormat(OptionFormat.Seconds);
        MaxIllusions = IntegerOptionItem.Create(Id + 11, "IllusionistMaxIllusions", new(1, 100, 1), 5, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist])
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
    public void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(player.PlayerId);
        writer.Write(AbilityLimit);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();

        AbilityLimit = reader.ReadSingle();
        IllusionedPlayers[playerId].Add(reader.ReadByte());
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckDoubleTrigger(target, () => { IllusionedPlayers[killer.PlayerId].Add(target.PlayerId); }))
        {
            if (HasNecronomicon(killer) && !target.IsPlayerCoven()) { 
                var randomDeathReason = ChangeRandomDeath();
                Main.PlayerStates[target.PlayerId].deathReason = randomDeathReason;
                Main.PlayerStates[target.PlayerId].SetDead();
                return true;
            }
            return false;
        }
        else
        {
            AbilityLimit--;
            SendRPC(killer, target);
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            return false;
        }
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IllusionCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override string GetProgressText(byte playerId, bool comms)
        => ColorString(AbilityLimit >= 1 ? GetRoleColor(CustomRoles.Illusionist).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    private static PlayerState.DeathReason ChangeRandomDeath()
    {
        PlayerState.DeathReason[] deathReasons = EnumHelper.GetAllValues<PlayerState.DeathReason>().ToArray();
        if (deathReasons.Length == 0 || !deathReasons.Contains(PlayerState.DeathReason.Kill)) deathReasons.AddItem(PlayerState.DeathReason.Kill);
        var random = IRandom.Instance;
        int randomIndex = random.Next(deathReasons.Length);
        return deathReasons[randomIndex];
    }
    // Affects the following roles: Snitch, Witness, Psychic, Inspector, Oracle, Investigator
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
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => IllusionedPlayers[seer.PlayerId].Contains(seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.Illusionist), "�") : string.Empty;
}