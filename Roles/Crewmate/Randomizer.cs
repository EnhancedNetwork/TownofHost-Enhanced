using System;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Randomizer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Randomizer;
    private const int Id = 7500;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    public static OptionItem BecomeBaitDelayNotify;
    public static OptionItem BecomeBaitDelayMin;
    public static OptionItem BecomeBaitDelayMax;
    public static OptionItem BecomeTrapperBlockMoveTime;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Randomizer);
        BecomeBaitDelayNotify = BooleanOptionItem.Create(Id + 10, "BaitDelayNotify", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer]);
        BecomeBaitDelayMin = FloatOptionItem.Create(Id + 11, "BaitDelayMin", new(0f, 5f, 1f), 0f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
            .SetValueFormat(OptionFormat.Seconds);
        BecomeBaitDelayMax = FloatOptionItem.Create(Id + 12, "BaitDelayMax", new(0f, 10f, 1f), 0f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
            .SetValueFormat(OptionFormat.Seconds);
        BecomeTrapperBlockMoveTime = FloatOptionItem.Create(Id + 13, "FreezeTime", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Randomizer])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || isSuicide) return;

        var Fg = IRandom.Instance;
        int Randomizer = Fg.Next(1, 5);

        if (Randomizer == 1)
        {
            if (isSuicide)
            {
                if (target.GetRealKiller() != null)
                {
                    if (!target.GetRealKiller().IsAlive()) return;
                    killer = target.GetRealKiller();
                }
            }

            if (killer.PlayerId == target.PlayerId) return;

            if (killer.Is(CustomRoles.KillingMachine)
                || (killer.Is(CustomRoles.Oblivious) && Oblivious.ObliviousBaitImmune.GetBool()))
                return;

            if (!isSuicide || (target.GetRealKiller()?.GetCustomRole() is CustomRoles.Swooper or CustomRoles.Wraith) || !killer.Is(CustomRoles.KillingMachine) || !killer.Is(CustomRoles.Oblivious) || (killer.Is(CustomRoles.Oblivious) && !Oblivious.ObliviousBaitImmune.GetBool()))
            {
                killer.RPCPlayCustomSound("Congrats");
                target.RPCPlayCustomSound("Congrats");

                float delay;
                if (BecomeBaitDelayMax.GetFloat() < BecomeBaitDelayMin.GetFloat())
                {
                    delay = 0f;
                }
                else
                {
                    delay = IRandom.Instance.Next((int)BecomeBaitDelayMin.GetFloat(), (int)BecomeBaitDelayMax.GetFloat() + 1);
                }
                delay = Math.Max(delay, 0.15f);
                if (delay > 0.15f && BecomeBaitDelayNotify.GetBool())
                {
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(GetString("KillBaitNotify"), (int)delay)), delay);
                }

                Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发自动报告 => {target.GetNameWithRole()}", "Randomizer");

                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer1")));

                _ = new LateTask(() =>
                {
                    if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data);
                }, delay, "Bait Self Report");
            }
        }
        else if (Randomizer == 2)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发暂时无法移动 => {target.GetNameWithRole()}", "Randomizer");

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer2")));
            var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
            Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
            killer.MarkDirtySettings();

            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                killer.MarkDirtySettings();
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
            }, BecomeTrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
        }
        else if (Randomizer == 3)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发凶手CD变成600 => {target.GetNameWithRole()}", "Randomizer");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer3")));
            Main.AllPlayerKillCooldown[killer.PlayerId] = 600f;
            killer.SyncSettings();
        }
        else if (Randomizer == 4)
        {
            Logger.Info($"{killer.GetNameWithRole()} 击杀了萧暮触发随机复仇 => {target.GetNameWithRole()}", "Randomizer");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Randomizer), GetString("YouKillRandomizer4")));
            {
                var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId && target.RpcCheckAndMurder(x, true)).ToList();
                var pc = pcList[IRandom.Instance.Next(0, pcList.Count)];
                if (!pc.IsTransformedNeutralApocalypse())
                {
                    pc.SetDeathReason(PlayerState.DeathReason.Revenge);
                    pc.RpcMurderPlayer(pc);
                    pc.SetRealKiller(target);
                }
            }
        }
    }
}
