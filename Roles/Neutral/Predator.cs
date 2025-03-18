using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.CustomWinnerHolder;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
internal class Predator : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Predator;
    private const int Id = 34200;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    public static OptionItem KillCooldown;
    public static OptionItem HuntCooldown;

    public static HashSet<byte> TargetList = [];
    public static HashSet<byte> RevealedList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Predator);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Predator])
            .SetValueFormat(OptionFormat.Seconds);
        HuntCooldown = FloatOptionItem.Create(Id + 11, "HuntCooldown342", new(0f, 20f, 1f), 5f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Predator])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;

    public override void Add(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Add(OthersAfterPlayerDeathTask);
        var pred = _Player;
        if (AmongUsClient.Instance.AmHost && pred.IsAlive())
        {
            List<PlayerControl> targetList = [];
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (target != pred)
                {
                    targetList.Add(target);
                }
            }

            if (targetList.Any())
            {
                var selectedTarget1 = targetList.RandomElement();
                TargetList.Add(selectedTarget1.PlayerId);
                targetList.Remove(selectedTarget1);

                Logger.Info($"{pred?.GetNameWithRole()}:{selectedTarget1.GetNameWithRole()}", "Predator");
            }
            else
            {
                Logger.Info($"Wow, no targets for Predator to select! Changing predator role to other", "Predator");

                pred.GetRoleClass()?.OnRemove(playerId);
                pred.RpcSetCustomRole(CustomRoles.Opportunist);
                pred.GetRoleClass()?.OnAdd(playerId);
            }

            if (targetList.Any())
            {

                var selectedTarget2 = targetList.RandomElement();
                TargetList.Add(selectedTarget2.PlayerId);
                targetList.Remove(selectedTarget2);

                Logger.Info($"{pred?.GetNameWithRole()}:{selectedTarget2.GetNameWithRole()}", "Predator");
            }

            if (targetList.Any())
            {
                var selectedTarget3 = targetList.RandomElement();
                TargetList.Add(selectedTarget3.PlayerId);
                targetList.Remove(selectedTarget3);

                Logger.Info($"{pred?.GetNameWithRole()}:{selectedTarget3.GetNameWithRole()}", "Predator");
            }
        }
    }

    public void OthersAfterPlayerDeathTask(PlayerControl killer, PlayerControl target, bool InMeeting)
    {
        List<PlayerControl> AlivePrey = [];
        foreach (var preyID in TargetList)
        {
            var prey = GetPlayerById(preyID);
            if (prey.IsAlive())
            {
                AlivePrey.Add(prey);
            }
        }
        if (AlivePrey.Count == 0)
        {
            if (!CheckForConvertedWinner(_Player.PlayerId))
            {
                ResetAndSetWinner(CustomWinner.Predator);
                WinnerIds.Add(_Player.PlayerId);
            }
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (RevealedList.Contains(target.PlayerId))
        {
            return true;
        }
        if (TargetList.Contains(target.PlayerId))
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Predator), "Target is prey"));
            RevealedList.Add(target.PlayerId);
            killer.RpcGuardAndKill(killer);
            killer.SetKillCooldown(time: HuntCooldown.GetFloat());
        }
        else
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Predator), "Target is not prey"));
            killer.RpcGuardAndKill(killer);
            killer.SetKillCooldown(time: HuntCooldown.GetFloat());
        }
        return false;
    }
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        string color = string.Empty;
        if (seer.Is(CustomRoles.Predator) && RevealedList.Contains(target.PlayerId)) color = Main.roleColors[CustomRoles.Predator];
        return color;
    }
}
