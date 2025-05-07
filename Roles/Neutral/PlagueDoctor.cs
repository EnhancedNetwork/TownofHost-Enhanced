using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

// https://github.com/tukasa0001/TownOfHost/blob/main/Roles/Neutral/PlagueDoctor.cs
internal class PlagueDoctor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.PlagueDoctor;
    private const int Id = 27600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.PlagueDoctor);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem OptionInfectLimit;
    private static OptionItem OptionInfectWhenKilled;
    private static OptionItem OptionInfectTime;
    private static OptionItem OptionInfectDistance;
    private static OptionItem OptionInfectInactiveTime;
    private static OptionItem OptionInfectCanInfectSelf;
    private static OptionItem OptionInfectCanInfectVent;

    private bool InfectActive;
    private bool LateCheckWin;
    private static bool InfectWhenKilled;
    private float InfectTime;
    private float InfectDistance;
    private static float InfectInactiveTime;
    private static bool CanInfectSelf;
    private static bool CanInfectVent;

    private static readonly Dictionary<byte, float> InfectInfos = [];

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.PlagueDoctor, 1);
        OptionInfectLimit = IntegerOptionItem.Create(Id + 10, "PlagueDoctorInfectLimit", new(1, 3, 1), 1, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueDoctor])
            .SetValueFormat(OptionFormat.Times);
        OptionInfectWhenKilled = BooleanOptionItem.Create(Id + 11, "PlagueDoctorInfectWhenKilled", false, TabGroup.NeutralRoles, false)
           .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueDoctor]);
        OptionInfectTime = FloatOptionItem.Create(Id + 12, "PlagueDoctorInfectTime", new(3f, 20f, 1f), 8f, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueDoctor])
            .SetValueFormat(OptionFormat.Seconds);
        OptionInfectDistance = FloatOptionItem.Create(Id + 13, "PlagueDoctorInfectDistance", new(0.5f, 2f, 0.25f), 1.5f, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueDoctor])
            .SetValueFormat(OptionFormat.Multiplier);
        OptionInfectInactiveTime = FloatOptionItem.Create(Id + 14, "PlagueDoctorInfectInactiveTime", new(0.5f, 10f, 0.5f), 3.5f, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueDoctor])
            .SetValueFormat(OptionFormat.Seconds);
        OptionInfectCanInfectSelf = BooleanOptionItem.Create(Id + 15, "PlagueDoctorCanInfectSelf", false, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueDoctor]);
        OptionInfectCanInfectVent = BooleanOptionItem.Create(Id + 16, "PlagueDoctorCanInfectVent", false, TabGroup.NeutralRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.PlagueDoctor]);
    }

    public override void Init()
    {
        InfectInfos.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(OptionInfectLimit.GetInt());

        InfectWhenKilled = OptionInfectWhenKilled.GetBool();
        InfectTime = OptionInfectTime.GetFloat();
        InfectDistance = OptionInfectDistance.GetFloat();
        InfectInactiveTime = OptionInfectInactiveTime.GetFloat();
        CanInfectSelf = OptionInfectCanInfectSelf.GetBool();
        CanInfectVent = OptionInfectCanInfectVent.GetBool();

        InfectActive = true;

        // Fixed airship respawn selection delay
        if (Main.NormalOptions.MapId == 4)
            InfectInactiveTime += 5f;

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateOthers.Add(OnCheckPlayerPosition);
            CustomRoleManager.CheckDeadBodyOthers.Add(OnAnyMurder);
        }
    }

    public override bool CanUseKillButton(PlayerControl pc) => pc.GetAbilityUseLimit() > 0;

    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(false);

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("InfectiousKillButtonText"));
    }
    public bool CanInfect(PlayerControl player)
    {
        // Not a plague doctor, or capable of self-infection and infected person created
        return player != _Player || (CanInfectSelf && player.GetAbilityUseLimit() == 0);
    }
    public void SendRPC(byte targetId, float rate)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(targetId);
        writer.Write(rate);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        var targetId = reader.ReadByte();
        var rate = reader.ReadSingle();

        InfectInfos[targetId] = rate;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() > 0)
        {
            killer.RpcRemoveAbilityUse();
            killer.RpcGuardAndKill(target);
            DirectInfect(target, killer);
        }
        return false;
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (InfectWhenKilled && target.GetAbilityUseLimit() > 0)
        {
            target.SetAbilityUseLimit(0);
            DirectInfect(killer, target);
        }
    }
    private void OnAnyMurder(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        LateCheckWin = true;
    }
    public override void OnReportDeadBody(PlayerControl W, NetworkedPlayerInfo L)
    {
        InfectActive = false;
    }
    private void OnCheckPlayerPosition(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (LateCheckWin)
        {
            // After hanging/killing, check the victory conditions just to be sure.
            LateCheckWin = false;
            CheckWin();
        }
        if (!player.IsAlive() || player == null || !InfectActive) return;

        if (InfectInfos.TryGetValue(player.PlayerId, out var rate) && rate >= 100)
        {
            // In case of an infected person
            var changed = false;
            var inVent = player.inVent;
            List<PlayerControl> updates = [];
            foreach (PlayerControl target in Main.AllAlivePlayerControls)
            {
                // Plague doctors are excluded if they cannot infect themselves.
                if (!CanInfect(target)) continue;
                // Excluded if inside or outside the vent
                if (!CanInfectVent && target.inVent != inVent) continue;

                InfectInfos.TryGetValue(target.PlayerId, out var oldRate);
                // Exclude infected people
                if (oldRate >= 100) continue;

                // Exclude players outside the range
                var distance = Vector3.Distance(player.transform.position, target.transform.position);
                if (distance > InfectDistance) continue;

                var newRate = oldRate + Time.fixedDeltaTime / InfectTime * 100;
                newRate = Math.Clamp(newRate, 0, 100);
                InfectInfos[target.PlayerId] = newRate;
                if ((oldRate < 50 && newRate >= 50) || newRate >= 100)
                {
                    changed = true;
                    updates.Add(target);
                    Logger.Info($"InfectRate [{target.GetNameWithRole()}]: {newRate}%", "PlagueDoctor");
                    SendRPC(target.PlayerId, newRate);
                }
            }
            if (changed)
            {
                //If someone is infected
                CheckWin();
                PlayerControl Plaguer = _Player;
                foreach (PlayerControl x in updates.ToArray())
                {
                    Utils.NotifyRoles(SpecifySeer: Plaguer, SpecifyTarget: x);
                }
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        // You may win if a non-infected person is hanged.
        LateCheckWin = true;

        _ = new LateTask(() =>
        {
            Logger.Info("Infect Active", "PlagueDoctor");
            InfectActive = true;
        },
        InfectInactiveTime, "ResetInfectInactiveTime");
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool IsForMeeting = false)
    {
        if (!CanInfect(seen)) return string.Empty;
        if (!seer.Is(CustomRoles.PlagueDoctor) && seer.IsAlive()) return string.Empty;
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.PlagueDoctor), GetInfectRateCharactor(seen));
    }
    public override string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool IsForMeeting = false, bool znowupierdol = false)
    {
        seen ??= seer;
        if (!seen.Is(CustomRoles.PlagueDoctor)) return string.Empty;
        if (!seer.Is(CustomRoles.PlagueDoctor) && seer.IsAlive()) return string.Empty;

        var str = new StringBuilder(40);
        foreach (PlayerControl player in Main.AllAlivePlayerControls)
        {
            if (!player.Is(CustomRoles.PlagueDoctor))
                str.Append(GetInfectRateCharactor(player));
        }
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.PlagueDoctor), str.ToString());
    }
    private static bool IsInfected(byte playerId)
    {
        InfectInfos.TryGetValue(playerId, out var rate);
        return rate >= 100;
    }
    private string GetInfectRateCharactor(PlayerControl player)
    {
        if (!HasEnabled) return string.Empty;
        if (!CanInfect(player) || !player.IsAlive()) return string.Empty;
        InfectInfos.TryGetValue(player.PlayerId, out var rate);
        return rate switch
        {
            < 50 => "\u2581",
            >= 50 and < 100 => "\u2584",
            >= 100 => "\u2588",
            _ => string.Empty,
        };
    }
    private void DirectInfect(PlayerControl target, PlayerControl plague)
    {
        if (target == null) return;
        Logger.Info($"InfectRate [{target.GetNameWithRole()}]: 100%", "PlagueDoctor");
        InfectInfos[target.PlayerId] = 100;
        SendRPC(target.PlayerId, 100);
        Utils.NotifyRoles(SpecifySeer: plague, SpecifyTarget: target);
        CheckWin();
    }
    private void CheckWin()
    {
        if (_Player == null) return;
        if (!AmongUsClient.Instance.AmHost) return;
        // Invalid if someone's victory is being processed
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;

        if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.PlagueDoctor) || IsInfected(p.PlayerId)))
        {
            InfectActive = false;

            if (!CustomWinnerHolder.CheckForConvertedWinner(_Player.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PlagueDoctor);
                foreach (var plagueDoctor in Main.AllPlayerControls.Where(p => p.Is(CustomRoles.PlagueDoctor)).ToArray())
                {
                    CustomWinnerHolder.WinnerIds.Add(plagueDoctor.PlayerId);
                }
            }

            foreach (PlayerControl player in Main.AllAlivePlayerControls)
            {
                if (player.Is(CustomRoles.PlagueDoctor)) continue;
                player.SetDeathReason(PlayerState.DeathReason.Infected);
                player.RpcMurderPlayer(player);
                player.SetRealKiller(_Player);
            }
        }
    }
}
