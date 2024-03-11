using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Ninja : RoleBase
{
    private const int Id = 2100;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;

    private static OptionItem MarkCooldown;
    private static OptionItem AssassinateCooldown;
    private static OptionItem CanKillAfterAssassinate;

    private static List<byte> playerIdList = [];
    private static Dictionary<byte, byte> MarkedPlayer = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Ninja);
        MarkCooldown = FloatOptionItem.Create(Id + 10, "NinjaMarkCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ninja])
            .SetValueFormat(OptionFormat.Seconds);
        AssassinateCooldown = FloatOptionItem.Create(Id + 11, "NinjaAssassinateCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ninja])
            .SetValueFormat(OptionFormat.Seconds);
        CanKillAfterAssassinate = BooleanOptionItem.Create(Id + 12, "NinjaCanKillAfterAssassinate", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ninja]);
    }
    public override void Init()
    {
        On = false;
        playerIdList = [];
        MarkedPlayer = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMarkedPlayer, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(MarkedPlayer.ContainsKey(playerId) ? MarkedPlayer[playerId] : byte.MaxValue);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        MarkedPlayer.Remove(playerId);
        if (targetId != byte.MaxValue)
            MarkedPlayer.Add(playerId, targetId);
    }

    private static bool Shapeshifting(byte id) => Main.CheckShapeshift.TryGetValue(id, out bool shapeshifting) && shapeshifting;
    
    public override void SetKillCooldown(byte id)
        => Main.AllPlayerKillCooldown[id] = Shapeshifting(id) ? DefaultKillCooldown : MarkCooldown.GetFloat();

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        => AURoleOptions.ShapeshifterCooldown = AssassinateCooldown.GetFloat();

    private static bool CheckCanUseKillButton(PlayerControl pc)
    {
        if (pc == null || !pc.IsAlive()) return false;
        if (!CanKillAfterAssassinate.GetBool() && pc.shapeshifting) return false;
        return true;
    }
    public override bool CanUseKillButton(PlayerControl pc) => CheckCanUseKillButton(pc);

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (Shapeshifting(killer.PlayerId))
        {
            return CheckCanUseKillButton(killer);
        }
        else
        {
            MarkedPlayer.Remove(killer.PlayerId);
            MarkedPlayer.Add(killer.PlayerId, target.PlayerId);
            SendRPC(killer.PlayerId);
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.SyncSettings();
            killer.RPCPlayCustomSound("Clothe");
            return false;
        }
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (shapeshiftIsHidden && (!MarkedPlayer.ContainsKey(shapeshifter.PlayerId) || !shapeshifter.IsAlive() || Pelican.IsEaten(shapeshifter.PlayerId)))
        {
            shapeshifter.RejectShapeshiftAndReset(reset: false);
            return;
        }
        if (!shapeshifter.IsAlive() || Pelican.IsEaten(shapeshifter.PlayerId)) return;

        if (!shapeshifting || shapeshiftIsHidden)
        {
            shapeshifter.SetKillCooldown();
            if (!shapeshiftIsHidden) return;
        }

        if (MarkedPlayer.TryGetValue(shapeshifter.PlayerId, out var targetId))
        {
            var timer = shapeshiftIsHidden ? 0.1f : 1.5f;
            var marketTarget = Utils.GetPlayerById(targetId);
            
            MarkedPlayer.Remove(shapeshifter.PlayerId);
            SendRPC(shapeshifter.PlayerId);

            if (shapeshiftIsHidden)
                shapeshifter.RejectShapeshiftAndReset();

            _ = new LateTask(() =>
            {
                if (!(marketTarget == null || !marketTarget.IsAlive() || Pelican.IsEaten(marketTarget.PlayerId) || Medic.ProtectList.Contains(marketTarget.PlayerId) || marketTarget.inVent || !GameStates.IsInTask))
                {
                    shapeshifter.RpcTeleport(marketTarget.GetCustomPosition());
                    shapeshifter.ResetKillCooldown();
                    shapeshifter.RpcCheckAndMurder(marketTarget);
                }
            }, timer, "Ninja Assassinate");
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerid)
    {
        if (!Shapeshifting(playerid))
            hud.KillButton.OverrideText(GetString("NinjaMarkButtonText"));
        else
            hud.KillButton.OverrideText(GetString("KillButtonText"));

        if (MarkedPlayer.ContainsKey(playerid) && !Shapeshifting(playerid))
            hud.AbilityButton.OverrideText(GetString("NinjaShapeshiftText"));
    }

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => !shapeshifting ? CustomButton.Get("Mark") : null;
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => !shapeshifting && MarkedPlayer.ContainsKey(player.PlayerId) ? CustomButton.Get("Assassinate") : null;
}