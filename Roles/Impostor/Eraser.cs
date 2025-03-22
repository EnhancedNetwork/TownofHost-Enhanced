using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Eraser : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Eraser;
    private const int Id = 24200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Eraser);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem EraseCooldown;
    private static OptionItem EraseLimitOpt;
    private static OptionItem CanGuessErasedPlayer;
    private static OptionItem CanEraseNeutral;
    private static OptionItem CanEraseCoven;
    private static OptionItem ChangeNeutralRole;

    [Obfuscation(Exclude = true)]
    private enum ChangeRolesSelectList
    {
        Role_Amnesiac,
        Role_Imitator
    }

    public static readonly CustomRoles[] NRoleChangeRoles =
    [
        CustomRoles.Amnesiac,
        CustomRoles.Imitator,
    ];

    private static readonly HashSet<byte> PlayerToErase = [];
    public static readonly Dictionary<byte, CustomRoles> ErasedRoleStorage = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Eraser);
        EraseCooldown = FloatOptionItem.Create(Id + 10, "EraserEraseCooldown", new(0f, 180f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eraser])
            .SetValueFormat(OptionFormat.Seconds);
        EraseLimitOpt = IntegerOptionItem.Create(Id + 11, "EraseLimit", new(1, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eraser])
            .SetValueFormat(OptionFormat.Times);
        CanEraseNeutral = BooleanOptionItem.Create(Id + 12, "EraserCanEraseNeutral", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eraser]);
        ChangeNeutralRole = StringOptionItem.Create(Id + 13, "NeutralChangeRolesForOiiai", EnumHelper.GetAllNames<ChangeRolesSelectList>(), 0, TabGroup.ImpostorRoles, false).SetParent(CanEraseNeutral);
        CanEraseCoven = BooleanOptionItem.Create(Id + 14, "EraserCanEraseCoven", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eraser]);
        CanGuessErasedPlayer = BooleanOptionItem.Create(Id + 15, "EraserCanGuessErasedPlayer", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Eraser]);
    }
    public override void Init()
    {
        PlayerToErase.Clear();
        ErasedRoleStorage.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(EraseLimitOpt.GetInt());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = id.GetAbilityUseLimit() >= 1 ? EraseCooldown.GetFloat() : DefaultKillCooldown;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (playerId.GetAbilityUseLimit() >= 1)
            HudManager.Instance.KillButton.OverrideText(GetString("EraserButtonText"));
        else
            HudManager.Instance.KillButton.OverrideText(GetString("KillButtonText"));
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (killer.GetAbilityUseLimit() < 1) return true;

        var targetRole = target.GetCustomRole();
        if ((targetRole.IsNeutral() && !CanEraseNeutral.GetBool()) || (targetRole.IsCoven() && (!CanEraseCoven.GetBool() || CovenManager.HasNecronomicon(target))) || CopyCat.playerIdList.Contains(target.PlayerId) || target.Is(CustomRoles.Stubborn))
        {
            Logger.Info($"Cannot erase role because is Neutral or ect", "Eraser");
            killer.Notify(GetString("EraserEraseRoleNotice"));
            return true;
        }

        if (target.IsTransformedNeutralApocalypse()) return false;

        return killer.CheckDoubleTrigger(target, () =>
        {
            killer.RpcRemoveAbilityUse();
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.Notify(GetString("EraserEraseNotice"));
            if (!PlayerToErase.Contains(target.PlayerId))
               PlayerToErase.Add(target.PlayerId);
        });
    }
    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (PlayerToErase.Contains(target.PlayerId) && CanGuessErasedPlayer.GetBool() && !role.IsAdditionRole())
        {
            guesser.ShowInfoMessage(isUI, GetString("EraserTryingGuessErasedPlayer"));
            return true;
        }
        return false;
    }
    public override void NotifyAfterMeeting()
    {
        foreach (var pc in PlayerToErase.ToArray())
        {
            var player = pc.GetPlayer();
            if (player == null) continue;

            player.RPCPlayCustomSound("Oiiai");
            player.Notify(GetString("LostRoleByEraser"));
        }
    }
    public override void AfterMeetingTasks()
    {
        foreach (var pc in PlayerToErase.ToArray())
        {
            var player = pc.GetPlayer();
            CustomRoles role = player.GetCustomRole();
            var readyRole = GetErasedRole(role.GetRoleTypes(), role);
            if (player == null) continue;
            if (!ErasedRoleStorage.ContainsKey(player.PlayerId))
            {
                ErasedRoleStorage.Add(player.PlayerId, player.GetCustomRole());
                Logger.Info($"Added {player.GetNameWithRole()} to ErasedRoleStorage", "Eraser");
            }
            else
            {
                Logger.Info($"Canceled {player.GetNameWithRole()} Eraser bcz already erased.", "Eraser");
                return;
            }

            if (player.HasGhostRole())
            {
                Logger.Info($"Canceled {player.GetNameWithRole()} because player have ghost role", "Eraser");
                return;
            }
            if (role.IsMadmate())
            {
                player.GetRoleClass().OnRemove(player.PlayerId);
                player.RpcChangeRoleBasis(CustomRoles.Amnesiac);
                player.RpcSetCustomRole(CustomRoles.Amnesiac);
                Main.DesyncPlayerList.Remove(player.PlayerId);
                player.GetRoleClass().OnAdd(player.PlayerId);
                player.RpcSetCustomRole(CustomRoles.Madmate);
                player.AddInSwitchAddons(player, CustomRoles.Madmate);
            }
            else if (role.IsCoven() && !CovenManager.HasNecronomicon(player) && CanEraseCoven.GetBool())
            {
                player.GetRoleClass().OnRemove(player.PlayerId);
                player.RpcChangeRoleBasis(CustomRoles.Amnesiac);
                player.RpcSetCustomRole(CustomRoles.Amnesiac);
                Main.DesyncPlayerList.Remove(player.PlayerId);
                player.GetRoleClass().OnAdd(player.PlayerId);
                player.RpcSetCustomRole(CustomRoles.Enchanted);
                player.AddInSwitchAddons(player, CustomRoles.Enchanted);
            }
            else if (role.IsNeutral() && !role.IsTNA() && CanEraseNeutral.GetBool())
            {
                if (player.Is(CustomRoles.Sidekick))
                {
                    player.GetRoleClass().OnRemove(player.PlayerId);
                    player.RpcChangeRoleBasis(CustomRoles.Amnesiac);
                    player.RpcSetCustomRole(CustomRoles.Amnesiac);
                    Main.DesyncPlayerList.Remove(player.PlayerId);
                    player.GetRoleClass().OnAdd(player.PlayerId);
                    player.RpcSetCustomRole(CustomRoles.Recruit);
                    player.AddInSwitchAddons(player, CustomRoles.Recruit);
                }
                else
                {
                    int changeValue = ChangeNeutralRole.GetValue();

                    player.GetRoleClass().OnRemove(player.PlayerId);
                    player.RpcChangeRoleBasis(NRoleChangeRoles[changeValue]);
                    player.RpcSetCustomRole(NRoleChangeRoles[changeValue]);
                    Main.DesyncPlayerList.Remove(player.PlayerId);
                    player.GetRoleClass().OnAdd(player.PlayerId);

                    player.SyncSettings();
                }
            }
            else
            {
                player.GetRoleClass()?.OnRemove(player.PlayerId);
                player.RpcChangeRoleBasis(readyRole);
                player.RpcSetCustomRole(readyRole);
                Main.DesyncPlayerList.Remove(player.PlayerId);
                player.GetRoleClass()?.OnAdd(player.PlayerId);
            }
            player.ResetKillCooldown();
            player.SetKillCooldown();
            Logger.Info($"{player.GetNameWithRole()} Erase by Eraser", "Eraser");
        }
        MarkEveryoneDirtySettings();
    }

    // Erased RoleType - Impostor, Shapeshifter, Crewmate, Engineer, Scientist (Not Neutrals)
    public static CustomRoles GetErasedRole(RoleTypes roleType, CustomRoles role)
    {
        return role.IsVanilla()
            ? role
            : roleType switch
            {
                RoleTypes.Crewmate => CustomRoles.CrewmateTOHE,
                RoleTypes.Scientist => CustomRoles.ScientistTOHE,
                RoleTypes.Tracker => CustomRoles.TrackerTOHE,
                RoleTypes.Noisemaker => CustomRoles.NoisemakerTOHE,
                RoleTypes.Engineer => CustomRoles.EngineerTOHE,
                RoleTypes.Impostor when role.IsCrewmate() => CustomRoles.CrewmateTOHE,
                RoleTypes.Impostor => CustomRoles.ImpostorTOHE,
                RoleTypes.Shapeshifter => CustomRoles.ShapeshifterTOHE,
                RoleTypes.Phantom => CustomRoles.PhantomTOHE,
                _ => role,
            };
    }
}
