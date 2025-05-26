using TOHE.Roles.AddOns.Common;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Poisoner : CovenManager
{
    private class PoisonedInfo(byte poisonerId, float killTimer)
    {
        public byte PoisonerId = poisonerId;
        public float KillTimer = killTimer;
    }
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Poisoner;
    private const int Id = 17500;

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenTrickery;
    //==================================================================\\

    private static OptionItem OptionKillDelay;
    //private static OptionItem CanVent;
    public static OptionItem KillCooldown;
    //private static OptionItem HasImpostorVision;

    private static readonly Dictionary<byte, PoisonedInfo> PoisonedPlayers = [];
    private static readonly Dictionary<byte, List<byte>> RoleblockedPlayers = [];


    private static float KillDelay;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Poisoner, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "PoisonCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.CovenRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner])
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillDelay = FloatOptionItem.Create(Id + 11, "PoisonerKillDelay", new(1f, 60f, 1f), 10f, TabGroup.CovenRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner])
            .SetValueFormat(OptionFormat.Seconds);
        //CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.CovenRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner]);
        //HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.CovenRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Poisoner]);
    }

    public override void Init()
    {
        PoisonedPlayers.Clear();
        RoleblockedPlayers.Clear();

        KillDelay = OptionKillDelay.GetFloat();
    }
    public override void Add(byte playerId)
    {
        RoleblockedPlayers[playerId] = [];
        GetPlayerById(playerId)?.AddDoubleTrigger();

    }
    //public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    //public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();


    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckDoubleTrigger(target, () => { SetPoisoned(killer, target); }))
        {
            if (HasNecronomicon(killer))
            {
                if (target.GetCustomRole().IsCovenTeam())
                {
                    killer.Notify(GetString("CovenDontKillOtherCoven"));
                    return false;
                }
                if (target.Is(CustomRoles.Bait)) return true;

                killer.SetKillCooldown();

                if (!PoisonedPlayers.ContainsKey(target.PlayerId))
                {
                    PoisonedPlayers.Add(target.PlayerId, new(killer.PlayerId, 0f));
                }
            }
        }
        return false;
    }
    private static void SetPoisoned(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return;
        RoleblockedPlayers[killer.PlayerId].Add(target.PlayerId);
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
    }

    public override void OnFixedUpdate(PlayerControl poisoner, bool lowLoad, long nowTime, int timerLowLoad)
    {
        var poisonerID = poisoner.PlayerId;
        List<byte> targetList = new(PoisonedPlayers.Where(b => b.Value.PoisonerId == poisonerID).Select(b => b.Key));

        foreach (var targetId in targetList)
        {
            var poisonedPoisoner = PoisonedPlayers[targetId];

            if (poisonedPoisoner.KillTimer >= KillDelay)
            {
                var target = targetId.GetPlayer();
                KillPoisoned(poisoner, target);
                PoisonedPlayers.Remove(targetId);
            }
            else
            {
                poisonedPoisoner.KillTimer += Time.fixedDeltaTime;
                PoisonedPlayers[targetId] = poisonedPoisoner;
            }
        }
    }
    private static void KillPoisoned(PlayerControl poisoner, PlayerControl target, bool isButton = false)
    {
        if (poisoner == null || target == null || target.Data.Disconnected) return;
        if (target.IsAlive() && !target.IsTransformedNeutralApocalypse())
        {
            target.SetDeathReason(PlayerState.DeathReason.Poison);
            target.RpcMurderPlayer(target);
            target.SetRealKiller(poisoner);
            Logger.Info($"{target.GetRealName()} Died by Poison", "Poisoner");
            if (!isButton && poisoner.IsAlive())
            {
                RPC.PlaySoundRPC(Sounds.KillSound, poisoner.PlayerId);
                if (target.Is(CustomRoles.Trapper))
                    poisoner.TrapperKilled(target);
                poisoner.Notify(GetString("PoisonerTargetDead"));
                poisoner.SetKillCooldown();
            }
        }
        else
        {
            Logger.Info($"{target.GetRealName()} was in an unkillable state, poison was canceled", "Poisoner");
        }
    }
    public override void OnReportDeadBody(PlayerControl sans, NetworkedPlayerInfo bateman)
    {
        foreach (var targetId in PoisonedPlayers.Keys)
        {
            var target = GetPlayerById(targetId);
            var poisoner = GetPlayerById(PoisonedPlayers[targetId].PoisonerId);
            KillPoisoned(poisoner, target);
        }
        PoisonedPlayers.Clear();
        foreach (var poisoner in RoleblockedPlayers.Keys)
        {
            RoleblockedPlayers[poisoner].Clear();
        }
    }
    public static bool IsRoleblocked(byte target)
    {
        if (RoleblockedPlayers.Count < 1) return false;
        foreach (var player in RoleblockedPlayers.Keys)
        {
            if (RoleblockedPlayers[player].Contains(target)) return true;
        }
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl pc, PlayerControl _)  // Target of Pursuer attempt to murder someone
    {
        if (pc == null) return false;
        if (IsRoleblocked(pc.PlayerId))
        {
            if (pc.GetCustomRole() is
                CustomRoles.SerialKiller or
                CustomRoles.Pursuer or
                CustomRoles.Deputy or
                CustomRoles.Deceiver or
                CustomRoles.Poisoner)
                return false;

            pc.ResetKillCooldown();
            pc.SetKillCooldown();

            Logger.Info($"{pc.GetRealName()} fail ability because roleblocked", "Poisoner");
            return true;
        }
        return false;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("PoisonerPoisonButtonText"));
    }
}
