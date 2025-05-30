
using Hazel;
using System;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Illusionist : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Illusionist;
    private const int Id = 30400;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenTrickery;
    //==================================================================\\

    private static OptionItem IllusionCooldown;
    private static OptionItem MaxIllusions;
    public static OptionItem SnitchCanIllusioned;
    private static OptionItem ResetIllusionsPerRound;
    private static OptionItem ClearIllusionsWhenDead;



    private static readonly Dictionary<byte, HashSet<byte>> IllusionedPlayers = [];


    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Illusionist, 1, zeroOne: false);
        IllusionCooldown = FloatOptionItem.Create(Id + 10, "IllusionCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist])
            .SetValueFormat(OptionFormat.Seconds);
        MaxIllusions = IntegerOptionItem.Create(Id + 11, "IllusionistMaxIllusions", new(1, 100, 1), 5, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist])
            .SetValueFormat(OptionFormat.Times);
        SnitchCanIllusioned = BooleanOptionItem.Create(Id + 12, "IllusionistSnitchAffected", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist]);
        ResetIllusionsPerRound = BooleanOptionItem.Create(Id + 13, "IllusionistResetIllusionsPerRound", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist]);
        ClearIllusionsWhenDead = BooleanOptionItem.Create(Id + 14, "IllusionistClearIllusionsWhenDead", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Illusionist]);
    }

    public override void Init()
    {
        IllusionedPlayers.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(MaxIllusions.GetInt());
        IllusionedPlayers[playerId] = [];
        GetPlayerById(playerId)?.AddDoubleTrigger();
        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }
    public void SendRPC(PlayerControl player, PlayerControl target)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();

        IllusionedPlayers[playerId].Add(reader.ReadByte());
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckDoubleTrigger(target, () => { SetIllusioned(killer, target); }))
        {
            if (HasNecronomicon(killer) && !target.GetCustomRole().IsCovenTeam())
            {
                var randomDeathReason = ChangeRandomDeath();
                Main.PlayerStates[target.PlayerId].deathReason = randomDeathReason;
                Main.PlayerStates[target.PlayerId].SetDead();
                return true;
            }
            killer.Notify(GetString("CovenDontKillOtherCoven"));
        }
        return false;
    }
    private void SetIllusioned(PlayerControl killer, PlayerControl target)
    {
        IllusionedPlayers[killer.PlayerId].Add(target.PlayerId);
        killer.RpcRemoveAbilityUse();
        SendRPC(killer, target);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IllusionCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;

    private static PlayerState.DeathReason ChangeRandomDeath()
    {
        PlayerState.DeathReason[] deathReasons = [.. EnumHelper.GetAllValues<PlayerState.DeathReason>()];
        if (deathReasons.Length == 0 || !deathReasons.Contains(PlayerState.DeathReason.Kill)) deathReasons.AddItem(PlayerState.DeathReason.Kill);
        var random = IRandom.Instance;
        int randomIndex = random.Next(deathReasons.Length);
        return deathReasons[randomIndex];
    }
    // Affects the following roles: Snitch, Witness, Psychic, Inspector, Oracle, Investigator
    public static bool IsNonCovIllusioned(byte target)
    {
        if (IllusionedPlayers.Count < 1) return false;
        bool result = false;
        foreach (var player in IllusionedPlayers.Keys)
        {
            if (IllusionedPlayers[player].Contains(target) && !GetPlayerById(target).GetCustomRole().IsCovenTeam()) result = true;
        }
        return result;
    }
    public static bool IsCovIllusioned(byte target)
    {
        if (IllusionedPlayers.Count < 1) return false;
        bool result = false;
        foreach (var player in IllusionedPlayers.Keys)
        {
            if (IllusionedPlayers[player].Contains(target) && GetPlayerById(target).GetCustomRole().IsCovenTeam()) result = true;
        }
        return result;
    }
    public override void AfterMeetingTasks()
    {
        if (ResetIllusionsPerRound.GetBool())
            IllusionedPlayers.Clear();
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        if (!ClearIllusionsWhenDead.GetBool()) return;
        foreach (var player in IllusionedPlayers.Keys)
        {
            if (deadPlayer.PlayerId == player) IllusionedPlayers[player].Clear();
        }
    }


    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => (IllusionedPlayers.TryGetValue(seer.PlayerId, out var Targets) && Targets.Contains(seen.PlayerId)) ? ColorString(GetRoleColor(CustomRoles.Illusionist), "ø") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (_Player == null) return string.Empty;
        if ((IsCovIllusioned(target.PlayerId) || IsNonCovIllusioned(target.PlayerId)) && ((seer.GetCustomRole().IsCovenTeam() && seer.PlayerId != _Player.PlayerId) || !seer.IsAlive()))
        {
            return ColorString(GetRoleColor(CustomRoles.Illusionist), "ø");
        }
        return string.Empty;
    }
}
