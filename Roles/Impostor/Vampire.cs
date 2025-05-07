using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Vampire : RoleBase
{
    private class BittenInfo(byte vampierId, float killTimer)
    {
        public byte VampireId = vampierId;
        public float KillTimer = killTimer;
    }

    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Vampire;
    private const int Id = 5000;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem OptionKillDelay;
    private static OptionItem CanVent;
    private static OptionItem ActionModeOpt;

    [Obfuscation(Exclude = true)]
    private enum ActionModeList
    {
        Vampire_OnlyBites,
        TriggerDouble
    }
    private static ActionModeList NowActionMode;

    private static float KillDelay = new();
    private static readonly Dictionary<byte, BittenInfo> BittenPlayers = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Vampire);
        OptionKillDelay = FloatOptionItem.Create(Id + 10, "VampireKillDelay", new(1f, 60f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampire])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampire]);
        ActionModeOpt = StringOptionItem.Create(Id + 12, "VampireActionMode", EnumHelper.GetAllNames<ActionModeList>(), 2, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vampire]);
    }
    public override void Init()
    {
        BittenPlayers.Clear();

        KillDelay = OptionKillDelay.GetFloat();
        NowActionMode = (ActionModeList)ActionModeOpt.GetValue();
    }
    public override void Add(byte playerId)
    {
        if (NowActionMode == ActionModeList.TriggerDouble)
        {
            Utils.GetPlayerById(playerId)?.AddDoubleTrigger();
        }
    }

    public static bool CheckCanUseVent() => CanVent.GetBool();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CheckCanUseVent();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Bait)) return true;

        if (NowActionMode == ActionModeList.Vampire_OnlyBites)
        {
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Bite");

            if (!BittenPlayers.ContainsKey(target.PlayerId))
            {
                BittenPlayers.Add(target.PlayerId, new(killer.PlayerId, 0f));
            }
        }
        else if (NowActionMode == ActionModeList.TriggerDouble)
        {
            return killer.CheckDoubleTrigger(target, () =>
            {
                killer.SetKillCooldown();
                killer.RPCPlayCustomSound("Bite");

                if (!BittenPlayers.ContainsKey(target.PlayerId))
                {
                    BittenPlayers.Add(target.PlayerId, new(killer.PlayerId, 0f));
                }
            });
        }
        return false;
    }

    public override void OnFixedUpdate(PlayerControl vampire, bool lowLoad, long nowTime, int timerLowLoad)
    {
        var vampireId = vampire.PlayerId;
        List<byte> targetList = new(BittenPlayers.Where(b => b.Value.VampireId == vampireId).Select(b => b.Key));

        foreach (var targetId in targetList)
        {
            var bitten = BittenPlayers[targetId];

            if (bitten.KillTimer >= KillDelay)
            {
                Logger.Info("KillTimer >= KillDelay", "Vampire");

                var target = targetId.GetPlayer();
                KillBitten(vampire, target);
                BittenPlayers.Remove(targetId);
            }
            else
            {
                bitten.KillTimer += Time.fixedDeltaTime;
                BittenPlayers[targetId] = bitten;
            }
        }
    }
    private static void KillBitten(PlayerControl vampire, PlayerControl target)
    {
        if (target.Data.Disconnected) return;
        if (target.IsTransformedNeutralApocalypse()) return;

        if (target.IsAlive())
        {
            target.SetDeathReason(PlayerState.DeathReason.Bite);
            target.RpcMurderPlayer(target);
            target.SetRealKiller(vampire);

            Logger.Info($"{target.name} self-kill while being bitten by Vampire.", "Vampire");
            if (vampire.IsAlive())
            {
                RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);

                if (target.Is(CustomRoles.Trapper))
                    vampire.TrapperKilled(target);

                vampire.Notify(GetString("VampireTargetDead"));
                vampire.SetKillCooldown();
            }
        }
        else
        {
            Logger.Info($"{target.name} was dead after being bitten by Vampire", "Vampire");
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo deadBody)
    {
        foreach (var targetId in BittenPlayers.Keys)
        {
            var target = Utils.GetPlayerById(targetId);
            var vampire = Utils.GetPlayerById(BittenPlayers[targetId].VampireId);
            KillBitten(vampire, target);
        }
        BittenPlayers.Clear();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText(GetString("VampireBiteButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Bite");
}
