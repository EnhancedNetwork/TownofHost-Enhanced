using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Translator;

// https://github.com/tukasa0001/TownOfHost/blob/main/Roles/Impostor/Penguin.cs
namespace TOHE.Roles.Impostor;

public static class Penguin
{
    private static readonly int Id = 27500;
    private static List<byte> playerIdList = [];

    private static OptionItem OptionAbductTimerLimit;
    private static OptionItem OptionMeetingKill;

    private static PlayerControl AbductVictim;
    private static float AbductTimer;
    private static float AbductTimerLimit;
    private static bool stopCount;
    private static bool MeetingKill;
    public static bool IsEnable;

    // Measures to prevent the opponent who is about to be killed during abduction from using their abilities
    public static bool IsKiller => AbductVictim == null;
    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Penguin, 1);
        OptionAbductTimerLimit = FloatOptionItem.Create(Id + 11, "PenguinAbductTimerLimit", new(1f, 20f, 1f), 10f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Penguin])
            .SetValueFormat(OptionFormat.Seconds);
        OptionMeetingKill = BooleanOptionItem.Create(Id + 12, "PenguinMeetingKill", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Penguin]);
    }
    public static void Init()
    {
        playerIdList = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        IsEnable = true;

        AbductTimerLimit = OptionAbductTimerLimit.GetFloat();
        MeetingKill = OptionMeetingKill.GetBool();

        playerIdList.Add(playerId);

        AbductTimer = 255f;
        stopCount = false;
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Options.DefaultKillCooldown;
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = AbductVictim != null ? AbductTimer : AbductTimerLimit;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PenguinSync, SendOption.Reliable, -1);
        writer.Write(AbductVictim?.PlayerId ?? 255);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var victim = reader.ReadByte();

        if (victim == 255)
        {
            AbductVictim = null;
            AbductTimer = 255f;
        }
        else
        {
            AbductVictim = Utils.GetPlayerById(victim);
            AbductTimer = AbductTimerLimit;
        }
    }
    private static void AddVictim(PlayerControl target)
    {
        //Prevent using of moving platform??
        AbductVictim = target;
        AbductTimer = AbductTimerLimit;
        Utils.GetPlayerById(playerIdList[0]).MarkDirtySettings();
        Utils.GetPlayerById(playerIdList[0]).RpcResetAbilityCooldown();
        SendRPC();
    }
    private static void RemoveVictim()
    {
        if (AbductVictim != null)
        {
            //PlayerState.GetByPlayerId(AbductVictim.PlayerId).CanUseMovingPlatform = true;
            AbductVictim = null;
        }
        //MyState.CanUseMovingPlatform = true;
        AbductTimer = 255f;
        Utils.GetPlayerById(playerIdList[0]).MarkDirtySettings();
        Utils.GetPlayerById(playerIdList[0]).RpcResetAbilityCooldown();
        SendRPC();
    }
    public static bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        bool doKill = true;
        if (AbductVictim != null)
        {
            if (target != AbductVictim)
            {
                // During an abduction, only the abductee can be killed.
                Utils.GetPlayerById(playerIdList[0]).RpcMurderPlayerV3(AbductVictim);
                Utils.GetPlayerById(playerIdList[0]).ResetKillCooldown();
                doKill = false;
            }
            RemoveVictim();
        }
        else
        {
            doKill = false;
            AddVictim(target);
        }
        return doKill;
    }
    public static string OverrideKillButtonText()
    {
        if (!IsEnable) return string.Empty;
        return AbductVictim != null ? GetString("KillButtonText") : GetString("PenguinKillButtonText");
    }
    public static string GetAbilityButtonText()
    {
        return GetString("PenguinTimerText");
    }
    public static bool CanUseAbilityButton()
    {
        return AbductVictim != null;
    }
    public static void OnReportDeadBody()
    {
        stopCount = true;
        // If you meet a meeting with time running out, kill it even if you're on a ladder.
        if (AbductVictim != null && AbductTimer <= 0f)
        {
            Utils.GetPlayerById(playerIdList[0]).RpcMurderPlayerV3(AbductVictim);
        }
        if (MeetingKill)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (AbductVictim == null) return;
            Utils.GetPlayerById(playerIdList[0]).RpcMurderPlayerV3(AbductVictim);
            RemoveVictim();
        }
    }
    public static void AfterMeetingTasks()
    {
        if (GameStates.AirshipIsActive) return;

        //Maps other than Airship
        RestartAbduct();
    }
    public static void OnSpawnAirship()
    {
        RestartAbduct();
    }
    public static void RestartAbduct()
    {
        if (!IsEnable) return;
        if (AbductVictim != null)
        {
            Utils.GetPlayerById(playerIdList[0]).MarkDirtySettings();
            Utils.GetPlayerById(playerIdList[0]).RpcResetAbilityCooldown();
            stopCount = false;
        }
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.IsMeeting) return;

        if (!stopCount)
            AbductTimer -= Time.fixedDeltaTime;

        if (AbductVictim != null)
        {
            if (!Utils.GetPlayerById(playerIdList[0]).IsAlive() || !AbductVictim.IsAlive())
            {
                RemoveVictim();
                return;
            }
            if (AbductTimer <= 0f && !Utils.GetPlayerById(playerIdList[0]).MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                // Set IsDead to true first (prevents ladder chase)
                AbductVictim.Data.IsDead = true;
                GameData.Instance.SetDirty();
                // If the penguin himself is on a ladder, kill him after getting off the ladder.
                if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    var abductVictim = AbductVictim;
                    _ = new LateTask(() =>
                    {
                        var sId = abductVictim.NetTransform.lastSequenceId + 5;
                        abductVictim.NetTransform.SnapTo(Utils.GetPlayerById(playerIdList[0]).transform.position, (ushort)sId);
                        Utils.GetPlayerById(playerIdList[0]).RpcMurderPlayerV3(abductVictim);

                        var sender = CustomRpcSender.Create("PenguinMurder");
                        {
                            sender.AutoStartRpc(abductVictim.NetTransform.NetId, (byte)RpcCalls.SnapTo);
                            {
                                NetHelpers.WriteVector2(Utils.GetPlayerById(playerIdList[0]).transform.position, sender.stream);
                                sender.Write(abductVictim.NetTransform.lastSequenceId);
                            }
                            sender.EndRpc();
                            sender.AutoStartRpc(Utils.GetPlayerById(playerIdList[0]).NetId, (byte)RpcCalls.MurderPlayer);
                            {
                                sender.WriteNetObject(abductVictim);
                            }
                            sender.EndRpc();
                        }
                        sender.SendMessage();
                    }, 0.3f, "PenguinMurder");
                    RemoveVictim();
                }
            }
            // SnapToRPC does not work for players on top of the ladder, and only the host behaves differently, so teleporting is not done uniformly.
            else if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                var position = Utils.GetPlayerById(playerIdList[0]).transform.position;
                if (Utils.GetPlayerById(playerIdList[0]).PlayerId != 0)
                {
                    Utils.RpcTeleport(AbductVictim, position);
                }
                else
                {
                    _ = new LateTask(() =>
                    {
                        if (AbductVictim != null)
                            Utils.RpcTeleport(AbductVictim, position);
                    }
                    , 0.25f, "");
                }
            }
        }
        else if (AbductTimer <= 100f)
        {
            AbductTimer = 255f;
            Utils.GetPlayerById(playerIdList[0]).RpcResetAbilityCooldown();
        }
    }
}
