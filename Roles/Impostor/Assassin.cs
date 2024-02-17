using Hazel;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal static class Assassin
{
    private static readonly int Id = 2100;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    private static OptionItem MarkCooldown;
    private static OptionItem AssassinateCooldown;
    private static OptionItem CanKillAfterAssassinate;

    public static Dictionary<byte, byte> MarkedPlayer = [];
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Assassin);
        MarkCooldown = FloatOptionItem.Create(Id + 10, "AssassinMarkCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Assassin])
            .SetValueFormat(OptionFormat.Seconds);
        AssassinateCooldown = FloatOptionItem.Create(Id + 11, "AssassinAssassinateCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Assassin])
            .SetValueFormat(OptionFormat.Seconds);
        CanKillAfterAssassinate = BooleanOptionItem.Create(Id + 12, "AssassinCanKillAfterAssassinate", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Assassin]);
    }
    public static void Init()
    {
        playerIdList = [];
        MarkedPlayer = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
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
    private static bool Shapeshifting(this PlayerControl pc) => pc.PlayerId.Shapeshifting();
    private static bool Shapeshifting(this byte id) => Main.CheckShapeshift.TryGetValue(id, out bool shapeshifting) && shapeshifting;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = id.Shapeshifting() ? DefaultKillCooldown : MarkCooldown.GetFloat();
    public static void ApplyGameOptions() => AURoleOptions.ShapeshifterCooldown = AssassinateCooldown.GetFloat();
    public static bool CanUseKillButton(PlayerControl pc)
    {
        if (pc == null || !pc.IsAlive()) return false;
        if (!CanKillAfterAssassinate.GetBool() && pc.shapeshifting) return false;
        return true;
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer.Shapeshifting())
        {
            return CanUseKillButton(killer);
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
    public static void OnShapeshift(PlayerControl pc, bool shapeshifting, bool shapeshiftIsHidden = false)
    {
        if (shapeshiftIsHidden && (!MarkedPlayer.ContainsKey(pc.PlayerId) || !pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)))
        {
            pc.RejectShapeshiftAndReset(reset: false);
            return;
        }
        if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return;

        if (!shapeshifting || shapeshiftIsHidden)
        {
            pc.SetKillCooldown();
            if (!shapeshiftIsHidden) return;
        }

        if (MarkedPlayer.TryGetValue(pc.PlayerId, out var targetId))
        {
            var timer = shapeshiftIsHidden ? 0.1f : 1.5f;
            var marketTarget = Utils.GetPlayerById(targetId);
            
            MarkedPlayer.Remove(pc.PlayerId);
            SendRPC(pc.PlayerId);

            if (shapeshiftIsHidden)
                pc.RejectShapeshiftAndReset();

            _ = new LateTask(() =>
            {
                if (!(marketTarget == null || !marketTarget.IsAlive() || Pelican.IsEaten(marketTarget.PlayerId) || Medic.ProtectList.Contains(marketTarget.PlayerId) || marketTarget.inVent || !GameStates.IsInTask))
                {
                    pc.RpcTeleport(marketTarget.GetCustomPosition());
                    pc.ResetKillCooldown();
                    pc.RpcCheckAndMurder(marketTarget);
                }
            }, timer, "Assassin Assassinate");
        }
    }
    public static void SetKillButtonText(byte playerId)
    {
        if (!playerId.Shapeshifting())
            HudManager.Instance.KillButton.OverrideText(GetString("AssassinMarkButtonText"));
        else
            HudManager.Instance.KillButton.OverrideText(GetString("KillButtonText"));
    }
    public static void GetAbilityButtonText(HudManager __instance, byte playerId)
    {
        if (MarkedPlayer.ContainsKey(playerId) && !playerId.Shapeshifting())
            __instance.AbilityButton.OverrideText(GetString("AssassinShapeshiftText"));
    }
}