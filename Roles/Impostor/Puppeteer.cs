using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;


namespace TOHE.Roles.Impostor;

public static class Puppeteer
{
    private static readonly int Id = 4300;
    public static bool IsEnable = false;

    public static Dictionary<byte, byte> PuppeteerList = [];
    public static OptionItem PuppeteerDoubleKills;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Puppeteer);
        PuppeteerDoubleKills = BooleanOptionItem.Create(Id + 12, "PuppeteerDoubleKills", false, TabGroup.ImpostorRoles, true)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Puppeteer]);
    }
    public static void Init()
    {
        PuppeteerList = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        IsEnable = true;

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }
    private static void SendRPC(byte puppetId, byte targetId, byte typeId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncPuppet, SendOption.Reliable, -1);
        writer.Write(typeId);
        writer.Write(puppetId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var typeId = reader.ReadByte();
        var puppetId = reader.ReadByte();
        var targetId = reader.ReadByte();

        switch (typeId)
        {
            case 0:
                PuppeteerList.Clear();
                break;
            case 1:
                PuppeteerList[targetId] = puppetId;
                break;
            case 2:
                PuppeteerList.Remove(targetId);
                break;
        }
    }
    public static bool OnCheckPuppet(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.Needy) || target.Is(CustomRoles.Lazy) || Medic.ProtectList.Contains(target.PlayerId)) return false;
            return killer.CheckDoubleTrigger(target, () => 
            {         
                PuppeteerList[target.PlayerId] = killer.PlayerId;
                killer.SetKillCooldown();
                SendRPC(killer.PlayerId, target.PlayerId, 1);
                killer.RPCPlayCustomSound("Line");
                Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            }

        );
    }


    public static void OnFixedUpdate(PlayerControl puppet)
    {
        if (!PuppeteerList.ContainsKey(puppet.PlayerId)) return;

        if (!puppet.IsAlive() || Pelican.IsEaten(puppet.PlayerId))
        {
            PuppeteerList.Remove(puppet.PlayerId);
        }
        else
        {
            var puppeteerPos = puppet.transform.position;
            Dictionary<byte, float> targetDistance = [];
            float dis;

            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.PlayerId != puppet.PlayerId && !(target.Is(CustomRoleTypes.Impostor) || target.Is(CustomRoles.Pestilence)))
                {
                    dis = Vector2.Distance(puppeteerPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }

            if (targetDistance.Count > 0)
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = Utils.GetPlayerById(min.Key);
                var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];

                if (min.Value <= KillRange && puppet.CanMove && target.CanMove)
                {
                    if (puppet.RpcCheckAndMurder(target, true))
                    {
                        var puppeteerId = PuppeteerList[puppet.PlayerId];
                        RPC.PlaySoundRPC(puppeteerId, Sounds.KillSound);
                        target.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                        puppet.RpcMurderPlayerV3(target);
                        Utils.MarkEveryoneDirtySettings();
                        PuppeteerList.Remove(puppet.PlayerId);
                        SendRPC(byte.MaxValue, puppet.PlayerId, 2);
                        //Utils.NotifyRoles(SpecifySeer: puppet);
                        Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(puppeteerId), SpecifyTarget: puppet, ForceLoop: true);

                        if (!puppet.Is(CustomRoles.Pestilence) && PuppeteerDoubleKills.GetBool())
                        {
                            
                            Main.PlayerStates[puppet.PlayerId].deathReason = PlayerState.DeathReason.Drained;
                            puppet.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                            puppet.RpcMurderPlayerV3(puppet);
                        }
                    }
                }
            }
        }
    }

    public static void OnReportDeadBody()
    {
        PuppeteerList.Clear();
        SendRPC(byte.MaxValue, byte.MaxValue, 0);
    }

    public static string TargetMark(PlayerControl seer, PlayerControl target)
        => (PuppeteerList.ContainsValue(seer.PlayerId) && PuppeteerList.ContainsKey(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Puppeteer), "◆") : "";

    public static void SetKillButtonText(HudManager __instance)
        => __instance.KillButton.OverrideText(GetString("PuppeteerOperateButtonText"));
}
