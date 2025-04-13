using Hazel;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Puppeteer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Puppeteer;
    private const int Id = 4300;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem PuppeteerDoubleKills;

    private static readonly Dictionary<byte, byte> PuppeteerList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Puppeteer);
        PuppeteerDoubleKills = BooleanOptionItem.Create(Id + 12, "PuppeteerDoubleKills", false, TabGroup.ImpostorRoles, true)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Puppeteer]);
    }
    public override void Init()
    {
        PuppeteerList.Clear();
    }
    public override void Add(byte playerId)
    {
        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        }
    }
    public override void Remove(byte playerId)
    {
        DoubleTrigger.PlayerIdList.Remove(playerId);
        CustomRoleManager.OnFixedUpdateOthers.Remove(OnFixedUpdateOthers);
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

    public static bool PuppetIsActive(byte playerId) => PuppeteerList.ContainsKey(playerId);

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.LazyGuy)
            || target.Is(CustomRoles.Lazy)
            || target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
            return false;

        return killer.CheckDoubleTrigger(target, () =>
        {
            PuppeteerList[target.PlayerId] = killer.PlayerId;
            killer.SetKillCooldown();
            SendRPC(killer.PlayerId, target.PlayerId, 1);
            killer.RPCPlayCustomSound("Line");
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);
        });
    }

    private void OnFixedUpdateOthers(PlayerControl puppet, bool lowLoad, long nowTime)
    {
        if (lowLoad || !PuppetIsActive(puppet.PlayerId)) return;

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
                if (target.PlayerId != puppet.PlayerId && !(target.Is(Custom_Team.Impostor) || target.IsTransformedNeutralApocalypse()))
                {
                    dis = Utils.GetDistance(puppeteerPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }

            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = Utils.GetPlayerById(min.Key);
                var KillRange = ExtendedPlayerControl.GetKillDistances();

                if (min.Value <= KillRange && puppet.CanMove && target.CanMove)
                {
                    if (puppet.RpcCheckAndMurder(target, true))
                    {
                        var puppeteerId = PuppeteerList[puppet.PlayerId];
                        RPC.PlaySoundRPC(puppeteerId, Sounds.KillSound);
                        puppet.RpcMurderPlayer(target);
                        target.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                        Utils.MarkEveryoneDirtySettings();
                        PuppeteerList.Remove(puppet.PlayerId);
                        SendRPC(byte.MaxValue, puppet.PlayerId, 2);
                        //Utils.NotifyRoles(SpecifySeer: puppet);
                        Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(puppeteerId), SpecifyTarget: puppet, ForceLoop: true);

                        if (!puppet.IsTransformedNeutralApocalypse() && PuppeteerDoubleKills.GetBool())
                        {
                            puppet.SetDeathReason(PlayerState.DeathReason.Drained);
                            puppet.RpcMurderPlayer(puppet);
                            puppet.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                        }
                    }
                }
            }
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        PuppeteerList.Clear();
        SendRPC(byte.MaxValue, byte.MaxValue, 0);
    }

    public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (target == null || isForMeeting) return string.Empty;

        return (PuppeteerList.ContainsValue(seer.PlayerId) && PuppeteerList.ContainsKey(target.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Puppeteer), "◆") : "";
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
        => hud.KillButton?.OverrideText(GetString("PuppeteerOperateButtonText"));

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Puttpuer");
}
