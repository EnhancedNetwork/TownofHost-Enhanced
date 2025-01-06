﻿using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Utils;


namespace TOHE.Roles._Ghosts_.Impostor;

internal class Minion : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 27900;
    private static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorGhosts;
    //==================================================================\\

    public static OptionItem AbilityCooldown;
    public static OptionItem AbilityTime;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Minion);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(2.5f, 120f, 2.5f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Minion])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityTime = FloatOptionItem.Create(Id + 11, "MinionAbilityTime", new(1f, 10f, 1f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Minion])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        Playerids.Clear();
    }
    public override void Add(byte playerId)
    {
        Playerids.Add(playerId);
    }
    // EAC bans players when GA uses sabotage
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override string GetLowerTextOthers(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false, bool isForHud = false)
    {
        if ((seer.GetCustomRole().IsImpostorTeam()) && Main.PlayerStates[target.PlayerId].IsBlackOut && !isForMeeting)
        {
            var blinded = Translator.GetString("Minion_Blind");
            return ColorString(GetRoleColor(CustomRoles.Minion), $"<size=75%><alpha=#CC>『{blinded}』</size>");
        }
        return string.Empty;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        var ImpPVC = target.GetCustomRole().IsImpostor();
        if (!ImpPVC || killer.IsAnySubRole(x => x.IsConverted() && !killer.Is(CustomRoles.Madmate)))
        {
            Main.PlayerStates[target.PlayerId].IsBlackOut = true;
            target.MarkDirtySettings();
            
            _ = new LateTask(() =>
            {
                Main.PlayerStates[target.PlayerId].IsBlackOut = false;
                target.MarkDirtySettings();
            }, AbilityTime.GetFloat(), "Minion: return vision");
            killer.RpcResetAbilityCooldown();
        }
        return false;
    }
}

