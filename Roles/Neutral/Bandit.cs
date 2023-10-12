using Hazel;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;
public static class Bandit
{
    private static readonly int Id = 184200;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem KillCooldown;
    public static OptionItem MaxSteals;
    public static OptionItem StealMode;
    public static OptionItem CanStealBetrayalAddon;
    public static OptionItem CanStealImpOnlyAddon;
    public static OptionItem CanUseSabotage;
    public static OptionItem CanVent;

    public static Dictionary<byte, int> TotalSteals = new();
    public static Dictionary<byte, Dictionary<byte, CustomRoles>> Targets = new();

    public static readonly string[] BanditStealModeOpt =
    {
        "BanditStealMode.OnMeeting",
        "BanditStealMode.Instantly"
    };

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Bandit);
        MaxSteals = IntegerOptionItem.Create(Id + 10, "BanditMaxSteals", new(1, 20, 1), 6, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        KillCooldown = FloatOptionItem.Create(Id + 11, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit])
            .SetValueFormat(OptionFormat.Seconds);
        StealMode = StringOptionItem.Create(Id + 12, "BanditStealMode", BanditStealModeOpt, 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        CanStealBetrayalAddon = BooleanOptionItem.Create(Id + 13, "BanditCanStealBetrayalAddon", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        CanStealImpOnlyAddon = BooleanOptionItem.Create(Id + 14, "BanditCanStealImpOnlyAddon", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        CanUseSabotage = BooleanOptionItem.Create(Id + 15, "CanUseSabotage", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
        CanVent = BooleanOptionItem.Create(Id + 16, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bandit]);
    }

    public static void Init()
    {
        playerIdList = new();
        Targets = new();
        TotalSteals = new();
        IsEnable = false;
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
        TotalSteals.Add(playerId, 0);
        Targets[playerId] = new();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC(byte playerId, bool isTargetList = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBanditStealLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(TotalSteals[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (TotalSteals.ContainsKey(PlayerId))
            TotalSteals[PlayerId] = Limit;
        else
            TotalSteals.Add(PlayerId, 0);
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);

    private static CustomRoles? SelectRandomAddon(PlayerControl Target)
    {
        if (!AmongUsClient.Instance.AmHost) return null;
        var AllSubRoles = Main.PlayerStates[Target.PlayerId].SubRoles;
        for (int i = AllSubRoles.Count - 1; i >= 0; i--)
        {
            var role = AllSubRoles[i];
            if (role == CustomRoles.Cleansed || // making Bandit unable to steal Cleansed for obvious reasons. Although it can still be cleansed by cleanser.
                role == CustomRoles.LastImpostor ||
                role == CustomRoles.Lovers || // Causes issues involving Lovers Suicide
                (role.IsImpOnlyAddon() && !CanStealImpOnlyAddon.GetBool()) ||
                (role == CustomRoles.Nimble && CanVent.GetBool()) ||
                (role.IsBetrayalAddon() && !CanStealBetrayalAddon.GetBool()))
            { 
                    Logger.Info($"Removed {role} from stealable addons", "Bandit");
                    AllSubRoles.Remove(role);
            }
        }

        if (!AllSubRoles.Any())
        {
            Logger.Info("No stealable addons found on the target.", "Bandit");
            return null;
        }
        var rand = IRandom.Instance;
        var addon = AllSubRoles[rand.Next(0, AllSubRoles.Count)];
        return addon;
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (!IsEnable) return true;
        if (!target.HasSubRole() || target.Is(CustomRoles.Stubborn)) return true;
        if (TotalSteals[killer.PlayerId] >= MaxSteals.GetInt())
        {
            Logger.Info("Max steals reached killing the player", "Bandit");
            TotalSteals[killer.PlayerId] = MaxSteals.GetInt();
            return true;
        }
        var SelectedAddOn = SelectRandomAddon(target);
        if (SelectedAddOn == null) return true; // no stealable addons found on the target.
        if (StealMode.GetValue() == 1)
        {    
            Main.PlayerStates[target.PlayerId].RemoveSubRole((CustomRoles)SelectedAddOn);
            Logger.Info($"Successfully removed {SelectedAddOn} addon from {target.GetNameWithRole()}", "Bandit");
            killer.RpcSetCustomRole((CustomRoles)SelectedAddOn);
            Logger.Info($"Successfully Added {SelectedAddOn} addon to {killer.GetNameWithRole()}", "Bandit");
        }
        else
        {
            Targets[killer.PlayerId][target.PlayerId] = (CustomRoles)SelectedAddOn;
            Logger.Info($"{killer.GetNameWithRole()} will steal {SelectedAddOn} addon from {target.GetNameWithRole()} after meeting starts", "Bandit");
        }
        TotalSteals[killer.PlayerId]++;
        SendRPC(killer.PlayerId);
        Utils.NotifyRoles();
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
        return false;
    }

    public static void OnReportDeadBody()
    {
        if (!IsEnable) return;
        if (StealMode.GetValue() == 1) return;
        foreach (var kvp1 in Targets)
        {
            byte banditId = kvp1.Key;
            var banditpc = Utils.GetPlayerById(banditId);
            if (banditpc == null || !banditpc.IsAlive()) continue;
            var innerDictionary = kvp1.Value;
            foreach (var kvp2 in innerDictionary)
            {
                byte targetId = kvp2.Key;
                var target = Utils.GetPlayerById(banditId);
                if (target == null) continue;
                CustomRoles role = kvp2.Value;
                Main.PlayerStates[targetId].RemoveSubRole(role);
                Logger.Info($"Successfully removed {role} addon from {target.GetNameWithRole()}", "Bandit");
                banditpc.RpcSetCustomRole(role);
                Logger.Info($"Successfully Added {role} addon to {banditpc.GetNameWithRole()}", "Bandit");
            }
            Targets[banditId].Clear();
        }
        Utils.NotifyRoles();
    }

    public static string GetStealLimit(byte playerId) => Utils.ColorString(TotalSteals[playerId] < MaxSteals.GetInt() ? Utils.GetRoleColor(CustomRoles.Bandit).ShadeColor(0.25f) : Color.gray, TotalSteals.TryGetValue(playerId, out var stealLimit) ? $"({MaxSteals.GetInt() - stealLimit})" : "Invalid");
}