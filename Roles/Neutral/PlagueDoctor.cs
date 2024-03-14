using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

// https://github.com/tukasa0001/TownOfHost/blob/main/Roles/Neutral/PlagueDoctor.cs
internal class PlagueDoctor : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 27600;
    private static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    //==================================================================\\

    private static OptionItem OptionInfectLimit;
    private static OptionItem OptionInfectWhenKilled;
    private static OptionItem OptionInfectTime;
    private static OptionItem OptionInfectDistance;
    private static OptionItem OptionInfectInactiveTime;
    private static OptionItem OptionInfectCanInfectSelf;
    private static OptionItem OptionInfectCanInfectVent;

    private static int InfectCount;
    private static bool InfectActive;
    private static bool LateCheckWin;
    private static int InfectLimit;
    private static bool InfectWhenKilled;
    private static float InfectTime;
    private static float InfectDistance;
    private static float InfectInactiveTime;
    private static bool CanInfectSelf;
    private static bool CanInfectVent;

    private static Dictionary<byte, float> InfectInfos;


    public static void SetupCustomOption()
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
        playerIdList = [];
        InfectInfos = [];
    }
    public override void Add(byte playerId)
    {
        InfectLimit = OptionInfectLimit.GetInt();
        InfectWhenKilled = OptionInfectWhenKilled.GetBool();
        InfectTime = OptionInfectTime.GetFloat();
        InfectDistance = OptionInfectDistance.GetFloat();
        InfectInactiveTime = OptionInfectInactiveTime.GetFloat();
        CanInfectSelf = OptionInfectCanInfectSelf.GetBool();
        CanInfectVent = OptionInfectCanInfectVent.GetBool();

        CustomRoleManager.OnFixedUpdateOthers.Add(OnCheckPlayerPosition);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);

        InfectCount = InfectLimit;

        InfectActive = true;

        // Fixed airship respawn selection delay
        if (Main.NormalOptions.MapId == 4)
            InfectInactiveTime += 5f;

        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Options.DefaultKillCooldown;
    public override bool CanUseKillButton(PlayerControl pc) => InfectCount != 0;
    public override string GetProgressText(byte plr, bool coomns)
            => Utils.ColorString(Utils.GetRoleColor(CustomRoles.PlagueDoctor).ShadeColor(0.25f), $"({InfectCount})");
    
    public override void ApplyGameOptions(IGameOptions opt, byte id)
    {
        opt.SetVision(false);
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("InfectiousKillButtonText"));
    }
    public static bool CanInfect(PlayerControl player)
    {
        // Not a plague doctor, or capable of self-infection and infected person created
        return !playerIdList.Any(x => x == player.PlayerId) || (CanInfectSelf && InfectCount == 0);
    }
    public static void SendRPC(byte targetId, float rate, bool firstInfect)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncPlagueDoctor, SendOption.Reliable, -1);
        writer.Write(firstInfect);
        writer.Write(targetId);
        writer.Write(rate);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var firstInfect = reader.ReadBoolean();
        var targetId = reader.ReadByte();
        var rate = reader.ReadSingle();

        if (firstInfect)
        {
            InfectCount = 0;
        }
        InfectInfos[targetId] = rate;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (InfectCount > 0)
        {
            InfectCount = 0;
            killer.RpcGuardAndKill(target);
            DirectInfect(target, killer);
        }
        return false;
    }
    public override void OnTargetDead(PlayerControl killer, PlayerControl target)
    {
        if (InfectWhenKilled && InfectCount > 0)
        {
            InfectCount = 0;
            DirectInfect(killer, target);
        }
    }
    public static void OnAnyMurder()
    {
        // You may win if an uninfected person dies.
        LateCheckWin = true;
    }
    public override void OnReportDeadBody(PlayerControl W, PlayerControl L)
    {
        InfectActive = false;
    }
    private static void OnCheckPlayerPosition(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

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
                    SendRPC(target.PlayerId, newRate, false);
                }
            }
            if (changed)
            {
                //If someone is infected
                CheckWin();
                PlayerControl Plaguer = Utils.GetPlayerById(playerIdList.ToList().FirstOrDefault());
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

    private static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool Isformeetingguwno = false)
    {
        seen ??= seer;
        if (!CanInfect(seen)) return string.Empty;
        if (!seer.Is(CustomRoles.PlagueDoctor) && seer.IsAlive()) return string.Empty;
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.PlagueDoctor), GetInfectRateCharactor(seen));
    }
    private static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isformeetingguwno = false, bool znowupierdol = false)
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
    private static string GetInfectRateCharactor(PlayerControl player)
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
    private static void DirectInfect(PlayerControl target, PlayerControl plague)
    {
        if (playerIdList.Count == 0 || target == null) return;
        Logger.Info($"InfectRate [{target.GetNameWithRole()}]: 100%", "PlagueDoctor");
        InfectInfos[target.PlayerId] = 100;
        SendRPC(target.PlayerId, 100, true);
        Utils.NotifyRoles(SpecifySeer: plague, SpecifyTarget: target);
        CheckWin();
    }
    private static void CheckWin()
    {
        if (!HasEnabled) return;
        if (!AmongUsClient.Instance.AmHost) return;
        // Invalid if someone's victory is being processed
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;

        if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.PlagueDoctor) || IsInfected(p.PlayerId)))
        {
            InfectActive = false;

            foreach (PlayerControl player in Main.AllAlivePlayerControls)
            {
                if (player.Is(CustomRoles.PlagueDoctor)) continue;
                Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Infected;
                player.RpcMurderPlayerV3(player);
            }
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PlagueDoctor);
            foreach (var plagueDoctor in Main.AllPlayerControls.Where(p => p.Is(CustomRoles.PlagueDoctor)).ToArray())
            {
                CustomWinnerHolder.WinnerIds.Add(plagueDoctor.PlayerId);
            }
        }
    }
}
