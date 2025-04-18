using AmongUs.GameOptions;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Mercenary : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Mercenary;
    private const int Id = 2000;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem TimeLimit;

    private static readonly Dictionary<byte, float> SuicideTimer = [];

    private static float OptTimeLimit;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Mercenary);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mercenary])
            .SetValueFormat(OptionFormat.Seconds);
        TimeLimit = FloatOptionItem.Create(Id + 11, "SuicideTimer", new(5f, 180f, 5f), 60f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mercenary])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        SuicideTimer.Clear();
    }
    public override void Add(byte serial)
    {
        OptTimeLimit = TimeLimit.GetFloat();
    }

    private static bool HasKilled(PlayerControl pc)
        => pc != null && pc.Is(CustomRoles.Mercenary) && pc.IsAlive() && Main.PlayerStates[pc.PlayerId].GetKillCount(true) > 0;

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = HasKilled(Utils.GetPlayerById(playerId)) ? OptTimeLimit : 255f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        // not should shapeshifted
        resetCooldown = false;
        return false;
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        SuicideTimer.Remove(killer.PlayerId);
        killer.MarkDirtySettings();

        return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        ClearSuicideTimer();
    }

    public static void ClearSuicideTimer() => SuicideTimer.Clear();

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!HasKilled(player))
        {
            SuicideTimer.Remove(player.PlayerId);
            return;
        }

        if (!SuicideTimer.TryGetValue(player.PlayerId, out var timer))
        {
            SuicideTimer[player.PlayerId] = 0f;
            player.RpcResetAbilityCooldown();
        }
        else if (timer >= OptTimeLimit)
        {
            player.SetDeathReason(PlayerState.DeathReason.Suicide);
            player.RpcMurderPlayer(player);
            SuicideTimer.Remove(player.PlayerId);
        }
        else
        {
            SuicideTimer[player.PlayerId] += Time.fixedDeltaTime;
        }
    }

    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Timer");
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("MercenarySuicideButtonText"));
    }

    public override void AfterMeetingTasks()
    {
        foreach (var id in _playerIdList)
        {
            var pc = Utils.GetPlayerById(id);

            if (pc != null && pc.IsAlive())
            {
                pc.RpcResetAbilityCooldown();

                if (HasKilled(pc))
                    SuicideTimer[id] = 0f;
            }
        }
    }
}
