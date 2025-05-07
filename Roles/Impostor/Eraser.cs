using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using static TOHE.Translator;

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

    private static OptionItem EraseLimitOpt;
    private static OptionItem CanGuessErasedPlayer;

    private static readonly HashSet<byte> didVote = [];
    private static readonly HashSet<byte> PlayerToErase = [];
    public static readonly Dictionary<byte, CustomRoles> ErasedRoleStorage = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Eraser);
        EraseLimitOpt = IntegerOptionItem.Create(Id + 10, "EraseLimit", new(1, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Eraser])
            .SetValueFormat(OptionFormat.Times);
        CanGuessErasedPlayer = BooleanOptionItem.Create(Id + 11, "EraserCanGuessErasedPlayer", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Eraser]);
    }
    public override void Init()
    {
        PlayerToErase.Clear();
        didVote.Clear();
        ErasedRoleStorage.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(EraseLimitOpt.GetInt());
    }
    public override bool CheckVote(PlayerControl player, PlayerControl target)
    {
        if (!HasEnabled) return true;
        if (player == null || target == null) return true;
        if (target.Is(CustomRoles.Eraser)) return true;
        if (player.GetAbilityUseLimit() < 1) return true;

        if (didVote.Contains(player.PlayerId)) return true;
        didVote.Add(player.PlayerId);

        Logger.Info($"{player.GetCustomRole()} votes for {target.GetCustomRole()}", "Vote Eraser");

        if (target.PlayerId == player.PlayerId)
        {
            Utils.SendMessage(GetString("EraserEraseSelf"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("Eraser").ToUpper()));
            return true;
        }

        var targetRole = target.GetCustomRole();
        if (targetRole.IsNeutral() || targetRole.IsCoven() || CopyCat.playerIdList.Contains(target.PlayerId) || target.Is(CustomRoles.Stubborn))
        {
            Logger.Info($"Cannot erase role because is Impostor Based or Neutral or ect", "Eraser");
            Utils.SendMessage(string.Format(GetString("EraserEraseBaseImpostorOrNeutralRoleNotice"), target.GetRealName()), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("Eraser").ToUpper()));
            return true;
        }

        player.RpcRemoveAbilityUse();

        if (!PlayerToErase.Contains(target.PlayerId))
            PlayerToErase.Add(target.PlayerId);

        Utils.SendMessage(string.Format(GetString("EraserEraseNotice"), target.GetRealName()), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("Eraser").ToUpper()));

        Utils.NotifyRoles(SpecifySeer: player);
        return false;
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
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        didVote.Clear();
    }
    public override void NotifyAfterMeeting()
    {
        foreach (var pc in PlayerToErase.ToArray())
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null) continue;

            player.RPCPlayCustomSound("Oiiai");
            player.Notify(GetString("LostRoleByEraser"));
        }
    }
    public override void AfterMeetingTasks()
    {
        foreach (var pc in PlayerToErase.ToArray())
        {
            var player = Utils.GetPlayerById(pc);
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
            player.GetRoleClass()?.OnRemove(player.PlayerId);
            player.RpcChangeRoleBasis(GetErasedRole(player.GetCustomRole().GetRoleTypes(), player.GetCustomRole()));
            player.RpcSetCustomRole(GetErasedRole(player.GetCustomRole().GetRoleTypes(), player.GetCustomRole()));
            Main.DesyncPlayerList.Remove(player.PlayerId);
            player.GetRoleClass()?.OnAdd(player.PlayerId);
            player.ResetKillCooldown();
            player.SetKillCooldown();
            Logger.Info($"{player.GetNameWithRole()} Erase by Eraser", "Eraser");
        }
        Utils.MarkEveryoneDirtySettings();
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
