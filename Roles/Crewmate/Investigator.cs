using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Investigator
{
    private static readonly int Id = 24900;
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, byte> InvestigatedList = new();
    public static bool IsEnable = false;
    public static readonly string[] ColorTypeText =
    {
        "Color.Red", "Color.Green","Color.Gray"
    };
    public static readonly string[] RevealMode =
    {
        "Investigator.Suspicion", "Investigator.Role"
    };

    public static OptionItem InvestigateCooldown;
    public static OptionItem InvestigateMax;
    public static OptionItem RoleRevealMode;
//    public static OptionItem CrewKillingShowAs;
//    public static OptionItem PassiveNeutralsShowAs;
//    public static OptionItem NeutralKillingShowAs;
    

    private static int InvestigateLimit = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Investigator, 1, zeroOne: false);
        InvestigateCooldown = FloatOptionItem.Create(Id + 10, "InvestigateCooldown", new(0f, 180f, 2.5f), 27.5f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator])
            .SetValueFormat(OptionFormat.Seconds);
        InvestigateMax = IntegerOptionItem.Create(Id + 11, "InvestigateMax", new(1, 15, 1), 5, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator])
            .SetValueFormat(OptionFormat.Times);
        RoleRevealMode = StringOptionItem.Create(Id + 12, "RoleRevealMode", RevealMode, 1, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator]);
    //    CrewKillingShowAs = StringOptionItem.Create(Id + 12, "CrewKillingShowAs", ColorTypeText, 1, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator]);
    //    PassiveNeutralsShowAs = StringOptionItem.Create(Id + 13, "PassiveNeutralsShowAs", ColorTypeText, 2, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator]);
    //    NeutralKillingShowAs = StringOptionItem.Create(Id + 14, "NeutralKillingShowAs", ColorTypeText, 0, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Investigator]);
    }
    public static void Init()
    {
        playerIdList = new();
        InvestigateLimit = new();
        InvestigatedList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        InvestigateLimit = InvestigateMax.GetInt();
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetInvestgatorLimit, SendOption.Reliable, -1);
        writer.Write(InvestigateLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    private static void SyncRPC(byte investigatorId, byte targetId, byte typeId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncInvestigator, SendOption.Reliable, -1);
        writer.Write(typeId);
        writer.Write(investigatorId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        InvestigateLimit = reader.ReadInt32();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = InvestigateCooldown.GetFloat();
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && InvestigateLimit >= 1;
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NSerialKiller)) return true;
        if (InvestigateLimit < 1) return false;
        if (CanBeInvestigated(target))
        {
            InvestigateLimit--;
            SendRPC();

            var role = $"<color={Utils.GetRoleColorCode(target.GetCustomRole())}>{Utils.GetRoleName(target.GetCustomRole())}</color>";
            if (RoleRevealMode.GetInt() == 1)
            {
                killer.Notify(string.Format(GetString("InvestigatorInvestigatedRole"), role, target.GetRealName()), 3f);
            }
            if (RoleRevealMode.GetInt() == 0)
            {
                if (target.GetCustomRole().IsCrewmate())
                killer.Notify(string.Format(GetString("InvestigatorInvestigatedCrew"), target.GetRealName()), 3f);
                if (!target.GetCustomRole().IsCrewmate())
                killer.Notify(string.Format(GetString("InvestigatorInvestigatedNotCrew"), target.GetRealName()), 3f);
            }
            Utils.NotifyRoles();

            killer.SetKillCooldownV3();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);

            if (InvestigateLimit < 0)
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{InvestigateLimit}次招募机会", "Investigator");
            return true;
        }
        
        if (InvestigateLimit < 0)
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Investigator), GetString("InvestigatorInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{InvestigateLimit}次招募机会", "Investigator");
        return false;
    }
    public static string GetInvestigateLimit() => Utils.ColorString(InvestigateLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Investigator).ShadeColor(0.25f) : Color.gray, $"({InvestigateLimit})");
    public static bool CanBeInvestigated(this PlayerControl pc)
    {
        return pc != null && !InvestigatedList.ContainsKey(pc.PlayerId)
        && !(
            false
            );
    }
}
