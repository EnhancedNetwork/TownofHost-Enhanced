using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Medic : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8600;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem WhoCanSeeProtectOpt;
    private static OptionItem KnowShieldBrokenOpt;
    private static OptionItem ShieldDeactivatesWhenMedicDies;
    private static OptionItem ShieldDeactivationIsVisibleOpt;
    private static OptionItem ResetCooldown;
    public static OptionItem GuesserIgnoreShield;

    public static readonly List<byte> ProtectList = [];
    private static readonly Dictionary<byte, int> ProtectLimit = [];

    private static byte TempMarkProtected;
    private static int SkillLimit;

    private enum SelectOptions
    {
        Medic_SeeMedicAndTarget,
        Medic_SeeMedic,
        Medic_SeeTarget,
        Medic_SeeNoOne
    }

    private enum ShieldDeactivationIsVisible
    {
        MedicShieldDeactivationIsVisible_DeactivationImmediately,
        MedicShieldDeactivationIsVisible_DeactivationAfterMeeting,
        MedicShieldDeactivationIsVisible_DeactivationIsVisibleOFF
    }

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medic);
        WhoCanSeeProtectOpt = StringOptionItem.Create(Id + 10, "MedicWhoCanSeeProtect", EnumHelper.GetAllNames<SelectOptions>(), 0, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic]);
        KnowShieldBrokenOpt = StringOptionItem.Create(Id + 16, "MedicKnowShieldBroken", EnumHelper.GetAllNames<SelectOptions>(), 1, TabGroup.CrewmateRoles, false)
           .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic]);
        ShieldDeactivatesWhenMedicDies = BooleanOptionItem.Create(Id + 24, "MedicShieldDeactivatesWhenMedicDies", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic]);
        ShieldDeactivationIsVisibleOpt = StringOptionItem.Create(Id + 25, "MedicShielDeactivationIsVisible", EnumHelper.GetAllNames<ShieldDeactivationIsVisible>(), 0, TabGroup.CrewmateRoles, false)
            .SetParent(ShieldDeactivatesWhenMedicDies);
        ResetCooldown = FloatOptionItem.Create(Id + 30, "MedicResetCooldown", new(0f, 120f, 1f), 10f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic])
            .SetValueFormat(OptionFormat.Seconds);
        GuesserIgnoreShield = BooleanOptionItem.Create(Id + 32, "MedicShieldedCanBeGuessed", false, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        ProtectList.Clear();
        ProtectLimit.Clear();
        TempMarkProtected = byte.MaxValue;
        SkillLimit = 1;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ProtectLimit.TryAdd(playerId, SkillLimit);

        CustomRoleManager.MarkOthers.Add(GetMarkForOthers);

        if (AmongUsClient.Instance.AmHost)
        {
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        ProtectLimit.Remove(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Medic);
        writer.Write(playerId);
        writer.Write(ProtectLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (ProtectLimit.ContainsKey(PlayerId))
            ProtectLimit[PlayerId] = Limit;
        else
            ProtectLimit.Add(PlayerId, SkillLimit);
    }
    private static void SendRPCForProtectList()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMedicalerProtectList, SendOption.Reliable, -1);
        writer.Write(ProtectList.Count);
        for (int i = 0; i < ProtectList.Count; i++)
            writer.Write(ProtectList[i]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPCForProtectList(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ProtectList.Clear();
        for (int i = 0; i < count; i++)
            ProtectList.Add(reader.ReadByte());
    }

    public static bool InProtect(byte id)
        => ProtectList.Contains(id) && Main.PlayerStates.TryGetValue(id, out var ps) && !ps.IsDead;

    public static bool CheckKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && ProtectLimit.TryGetValue(playerId, out var x) && x >= 1;

    public override bool CanUseKillButton(PlayerControl pc) => CheckKillButton(pc.PlayerId);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CheckKillButton(id) ? 5f : 300f;
    public override string GetProgressText(byte playerId, bool comms) => ColorString(CheckKillButton(playerId) ? GetRoleColor(CustomRoles.Medic).ShadeColor(0.25f) : Color.gray, ProtectLimit.TryGetValue(playerId, out var protectLimit) ? $"({protectLimit})" : "Invalid");

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (!CheckKillButton(killer.PlayerId)) return false;
        if (ProtectList.Contains(target.PlayerId)) return false;

        ProtectLimit[killer.PlayerId]--;
        SkillLimit--;

        SendRPC(killer.PlayerId);
        ProtectList.Add(target.PlayerId);
        TempMarkProtected = target.PlayerId;
        SendRPCForProtectList();

        if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill();

        switch (WhoCanSeeProtectOpt.GetValue())
        {
            case 0:
                if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                killer.RPCPlayCustomSound("Shield");
                target.RPCPlayCustomSound("Shield");
                break;
            case 1:
                if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                killer.RPCPlayCustomSound("Shield");
                break;
            case 2:
                target.RPCPlayCustomSound("Shield");
                break;
        }

        NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);
        NotifyRoles(SpecifySeer: target, SpecifyTarget: killer);

        Logger.Info($"{killer.GetNameWithRole()} : {ProtectLimit[killer.PlayerId]} shields left", "Medic");
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return true;
        if (!ProtectList.Contains(target.PlayerId)) return false;

        SendRPCForProtectList();

        killer.RpcGuardAndKill(target);
        killer.SetKillCooldown(ResetCooldown.GetFloat());

        NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

        switch (KnowShieldBrokenOpt.GetValue())
        {
            case 0:
                foreach (var medicId in playerIdList.ToArray())
                {
                    var medic = GetPlayerById(medicId);
                    if (medic == null || !medic.IsAlive()) continue;

                    medic.Notify(GetString("MedicKillerTryBrokenShieldTargetForMedic"));
                }
                target.RpcGuardAndKill(target);
                target.Notify(GetString("MedicKillerTryBrokenShieldTargetForTarget"));
                break;
            case 1:
                foreach (var medicId in playerIdList.ToArray())
                {
                    var medic = GetPlayerById(medicId);
                    if (medic == null || !medic.IsAlive()) continue;

                    medic.Notify(GetString("MedicKillerTryBrokenShieldTargetForMedic"));
                }
                break;
            case 2:
                target.RpcGuardAndKill(target);
                target.Notify(GetString("MedicKillerTryBrokenShieldTargetForTarget"));
                break;
        }

        Logger.Info($"{target.GetNameWithRole()} : Shield Shatter from the Medic", "Medic");
        return true;
    }
    public static void OnCheckMark()
    {
        if (!ShieldDeactivatesWhenMedicDies.GetBool()) return;

        if (ShieldDeactivationIsVisibleOpt.GetInt() == 1)
        {
            TempMarkProtected = byte.MaxValue;
            NotifyRoles();
        }
    }
    private static void IsDead(PlayerControl target)
    {
        if (!target.Is(CustomRoles.Medic)) return;
        if (!ShieldDeactivatesWhenMedicDies.GetBool()) return;

        ProtectList.Clear();
        Logger.Info($"{target.GetNameWithRole()} : Medic is dead", "Medic");

        if (ShieldDeactivationIsVisibleOpt.GetInt() == 0)
        {
            TempMarkProtected = byte.MaxValue;
        }
        NotifyRoles(ForceLoop: true);
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        IsDead(target);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);

    public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (target == null) return string.Empty;

        if ((WhoCanSeeProtectOpt.GetInt() is 0 or 1) && (InProtect(target.PlayerId) || TempMarkProtected == target.PlayerId))
        {
            return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
        }

        return string.Empty;
    }
    private static string GetMarkForOthers(PlayerControl seer, PlayerControl target, bool isformeeting)
    {
        if (!(InProtect(target.PlayerId) || TempMarkProtected == target.PlayerId)) return string.Empty;

        if ((WhoCanSeeProtectOpt.GetInt() is 0 or 1) || (!seer.IsAlive() && !seer.Is(CustomRoles.Medic)))
        {
            return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
        }

        return string.Empty;
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton?.OverrideText(GetString("ReportButtonText"));
        hud.KillButton?.OverrideText(GetString("MedicalerButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Shield");
}