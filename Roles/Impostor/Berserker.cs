using UnityEngine;
using System.Collections.Generic;
using TOHE.Modules;

namespace TOHE.Roles.Impostor;

internal class Berserker : RoleBase
{
    private const int Id = 600;

    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem BerserkerKillCooldown;
    private static OptionItem BerserkerMax;
    private static OptionItem BerserkerOneCanKillCooldown;
    private static OptionItem BerserkerKillCooldownLevel;
    private static OptionItem BerserkerOneKillCooldown;
    private static OptionItem BerserkerTwoCanScavenger;
    private static OptionItem BerserkerScavengerLevel;
    private static OptionItem BerserkerThreeCanBomber;
    private static OptionItem BerserkerBomberLevel;
    //public static OptionItem BerserkerFourCanFlash;
    //public static OptionItem BerserkerSpeed;
    private static OptionItem BerserkerFourCanNotKill;
    private static OptionItem BerserkerImmortalLevel;

    private static Dictionary<byte, int> BerserkerKillMax = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Berserker);
        BerserkerKillCooldown = FloatOptionItem.Create(Id + 2, "BerserkerKillCooldown", new(25f, 250f, 2.5f), 35f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Berserker])
            .SetValueFormat(OptionFormat.Seconds);
        BerserkerMax = IntegerOptionItem.Create(Id + 3, "BerserkerMax", new(1, 10, 1), 4, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Berserker])
            .SetValueFormat(OptionFormat.Level);
        BerserkerOneCanKillCooldown = BooleanOptionItem.Create(Id + 4, "BerserkerOneCanKillCooldown", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerOneKillCooldown = FloatOptionItem.Create(Id + 5, "BerserkerOneKillCooldown", new(10f, 45f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(BerserkerOneCanKillCooldown)
            .SetValueFormat(OptionFormat.Seconds);
        BerserkerKillCooldownLevel = IntegerOptionItem.Create(Id + 6, "BerserkerLevelRequirement", new(1, 10, 1), 1, TabGroup.ImpostorRoles, false).SetParent(BerserkerOneCanKillCooldown)
            .SetValueFormat(OptionFormat.Level);
        BerserkerTwoCanScavenger = BooleanOptionItem.Create(Id + 7, "BerserkerTwoCanScavenger", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerScavengerLevel = IntegerOptionItem.Create(Id + 8, "BerserkerLevelRequirement", new(1, 10, 1), 2, TabGroup.ImpostorRoles, false).SetParent(BerserkerTwoCanScavenger)
            .SetValueFormat(OptionFormat.Level);
        BerserkerThreeCanBomber = BooleanOptionItem.Create(Id + 9, "BerserkerThreeCanBomber", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerBomberLevel = IntegerOptionItem.Create(Id + 10, "BerserkerLevelRequirement", new(1, 10, 1), 3, TabGroup.ImpostorRoles, false).SetParent(BerserkerThreeCanBomber)
            .SetValueFormat(OptionFormat.Level);
        //BerserkerFourCanFlash = BooleanOptionItem.Create(Id + 11, "BerserkerFourCanFlash", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Berserker]);
        //BerserkerSpeed = FloatOptionItem.Create(611, "BerserkerSpeed", new(1.5f, 5f, 0.25f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(BerserkerOneCanKillCooldown)
        //    .SetValueFormat(OptionFormat.Multiplier);
        BerserkerFourCanNotKill = BooleanOptionItem.Create(Id + 12, "BerserkerFourCanNotKill", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerImmortalLevel = IntegerOptionItem.Create(Id + 13, "BerserkerLevelRequirement", new(1, 10, 1), 4, TabGroup.ImpostorRoles, false).SetParent(BerserkerFourCanNotKill)
            .SetValueFormat(OptionFormat.Level);
    }
    public override void Init()
    {
        BerserkerKillMax = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        BerserkerKillMax[playerId] = 0;
        On = true;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = BerserkerKillCooldown.GetFloat();

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (BerserkerKillMax[target.PlayerId] >= BerserkerImmortalLevel.GetInt() && BerserkerFourCanNotKill.GetBool())
        {
            killer.RpcTeleport(target.GetCustomPosition());
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
            killer.SetKillCooldown(target: target, forceAnime: true);
            return false;
        }
        return true;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (BerserkerKillMax[killer.PlayerId] < BerserkerMax.GetInt())
        {
            BerserkerKillMax[killer.PlayerId]++;
            killer.Notify(string.Format(Translator.GetString("BerserkerLevelChanged"), BerserkerKillMax[killer.PlayerId]));
            Logger.Info($"Increased the lvl to {BerserkerKillMax[killer.PlayerId]}", "CULTIVATOR");
        }
        else
        {
            killer.Notify(Translator.GetString("BerserkerMaxReached"));
            Logger.Info($"Max level reached lvl =  {BerserkerKillMax[killer.PlayerId]}", "CULTIVATOR");

        }

        if (BerserkerKillMax[killer.PlayerId] >= BerserkerKillCooldownLevel.GetInt() && BerserkerOneCanKillCooldown.GetBool())
        {
            Main.AllPlayerKillCooldown[killer.PlayerId] = BerserkerOneKillCooldown.GetFloat();
        }

        if (BerserkerKillMax[killer.PlayerId] == BerserkerScavengerLevel.GetInt() && BerserkerTwoCanScavenger.GetBool())
        {
            killer.RpcTeleport(target.GetCustomPosition());
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
            target.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());
            target.SetRealKiller(killer);
            Main.PlayerStates[target.PlayerId].SetDead();
            target.RpcMurderPlayerV3(target);
            killer.SetKillCooldownV2();
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Berserker), Translator.GetString("KilledByBerserker")));
            return false;
        }

        if (BerserkerKillMax[killer.PlayerId] >= BerserkerBomberLevel.GetInt() && BerserkerThreeCanBomber.GetBool())
        {
            Logger.Info("炸弹爆炸了", "Boom");
            CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (!player.IsModClient())
                    player.KillFlash();

                if (player == killer) continue;
                if (player == target) continue;

                if (Vector2.Distance(killer.transform.position, player.transform.position) <= Bomber.BomberRadius.GetFloat())
                {
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    player.SetRealKiller(killer);
                    player.RpcMurderPlayerV3(player);
                }
            }
        }

        return true;
    }
}
