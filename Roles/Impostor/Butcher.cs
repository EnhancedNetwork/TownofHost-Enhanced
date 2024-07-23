using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Butcher : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 24300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Butcher);

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private readonly Dictionary<byte, (int, int, Vector2)> MurderTargetLateTask = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Butcher);
    }
    public override void Init()
    {
        MurderTargetLateTask.Clear();;
    }
    public override void Add(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        }
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId) => hud.KillButton.OverrideText(Translator.GetString("ButcherButtonText"));

    public override void OnMurderPlayerAsKiller(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;
        if (target == null) return;

        var targetId = target.PlayerId;

        target.SetRealKiller(killer);
        target.SetDeathReason(PlayerState.DeathReason.Dismembered);
        Main.PlayerStates[targetId].SetDead();

        Main.OverDeadPlayerList.Add(targetId);

        if (target.Is(CustomRoles.Avanger))
        {
            CustomSoundsManager.RPCPlayCustomSoundAll("Congrats");
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);

            var alivePlayers = Main.AllAlivePlayerControls.Where(x => x.PlayerId != targetId).ToArray(); // Ensure enumeration only once
            foreach (var player in alivePlayers)
            {
                player.SetDeathReason(PlayerState.DeathReason.Revenge);
                target.RpcSpecificMurderPlayer(player, player);
                player.SetRealKiller(target);
                Main.PlayerStates[player.PlayerId].SetDead();
            }
            return;
        }

        _ = new LateTask(() =>
        {
            for (int i = 0; i <= 19; i++)
            {
                if (GameStates.IsMeeting) break;
                if (!target.AmOwner)
                {
                    target.MurderPlayer(target, ExtendedPlayerControl.ResultFlags);
                }
                foreach (var player in Main.AllAlivePlayerControls.Where(x => x.PlayerId != targetId && !x.AmOwner).ToArray())
                {
                    target.RpcSpecificMurderPlayer(target, player);
                }
            }
        }, 0.2f, "Butcher Show Bodies");

        _ = new LateTask(() =>
        {
            if (!MurderTargetLateTask.ContainsKey(targetId))
            {
                MurderTargetLateTask[targetId] = (0, 0, target.GetCustomPosition());
            }
        }, 0.6f, "Butcher Late Kill");
    }

    public override void AfterMeetingTasks() => MurderTargetLateTask.Clear();
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => MurderTargetLateTask.Clear();

    private void OnFixedUpdateOthers(PlayerControl target)
    {
        if (!MurderTargetLateTask.TryGetValue(target.PlayerId, out var taskData)) return;
        if (target == null || target.IsAlive()) return;

        var (updateCount, retryCount, ops) = taskData;

        if (updateCount > 19) //on fix update updates 30 times per second
        {
            if (retryCount < 5)
            {
                var rd = IRandom.Instance;

                Vector2 location = new(
                    ops.x + ((float)(rd.Next(1, 200) - 100) / 100),
                    ops.y + ((float)(rd.Next(1, 200) - 100) / 100));

                target.RpcTeleport(location);
                target.RpcMurderPlayer(target);
                target.SetRealKiller(_Player, true);
                MurderTargetLateTask[target.PlayerId] = (0, retryCount + 1, ops);
            }
            else
            {
                MurderTargetLateTask.Remove(target.PlayerId);
            }
        }
        else
        {
            MurderTargetLateTask[target.PlayerId] = (updateCount + 1, retryCount, ops);
        }
    }

}