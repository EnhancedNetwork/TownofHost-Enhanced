using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Medic : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Medic;
    private const int Id = 8600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Medic);
    public override bool IsDesyncRole => !GiveTasks;
    public override CustomRoles ThisRoleBase => GiveTasks ? (MedicPortableVitals.GetBool() ? CustomRoles.Scientist : CustomRoles.Crewmate) : CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem WhoCanSeeProtectOpt;
    private static OptionItem KnowShieldBrokenOpt;
    private static OptionItem ShieldDeactivatesWhenMedicDies;
    private static OptionItem ShieldDeactivationIsVisibleOpt;
    private static OptionItem ResetCooldown;
    public static OptionItem GuesserIgnoreShield;
    private static OptionItem MedicPortableVitals;
    private static OptionItem VitalsCooldown;
    private static OptionItem VitalsDuration;

    private static readonly HashSet<byte> GlobalProtectedList = [];
    private static readonly Dictionary<byte, HashSet<byte>> ProtectedPlayers = [];

    private readonly HashSet<byte> ProtectedList = [];
    private readonly HashSet<byte> TempMarkProtected = [];
    private bool GiveTasks = false;

    [Obfuscation(Exclude = true)]
    private enum SelectOptionsList
    {
        Medic_SeeMedicAndTarget,
        Medic_SeeMedic,
        Medic_SeeTarget,
        Medic_SeeNoOne
    }
    [Obfuscation(Exclude = true)]
    private enum ShieldDeactivationIsVisibleList
    {
        MedicShieldDeactivationIsVisible_Immediately,
        MedicShieldDeactivationIsVisible_AfterMeeting,
        MedicShieldDeactivationIsVisible_OFF
    }

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medic);
        WhoCanSeeProtectOpt = StringOptionItem.Create(Id + 10, "MedicWhoCanSeeProtect", EnumHelper.GetAllNames<SelectOptionsList>(), 0, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic]);
        KnowShieldBrokenOpt = StringOptionItem.Create(Id + 16, "MedicKnowShieldBroken", EnumHelper.GetAllNames<SelectOptionsList>(), 1, TabGroup.CrewmateRoles, false)
           .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic]);
        ShieldDeactivatesWhenMedicDies = BooleanOptionItem.Create(Id + 24, "MedicShieldDeactivatesWhenMedicDies", true, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic]);
        ShieldDeactivationIsVisibleOpt = StringOptionItem.Create(Id + 25, "MedicShielDeactivationIsVisible", EnumHelper.GetAllNames<ShieldDeactivationIsVisibleList>(), 0, TabGroup.CrewmateRoles, false)
            .SetParent(ShieldDeactivatesWhenMedicDies);
        ResetCooldown = FloatOptionItem.Create(Id + 30, "MedicResetCooldown", new(0f, 120f, 1f), 10f, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic])
            .SetValueFormat(OptionFormat.Seconds);
        GuesserIgnoreShield = BooleanOptionItem.Create(Id + 32, "MedicShieldedCanBeGuessed", false, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic]);
        MedicPortableVitals = BooleanOptionItem.Create(Id + 35, "MedicPortableVitals", false, TabGroup.CrewmateRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Medic]);
        VitalsCooldown = IntegerOptionItem.Create(Id + 36, GeneralOption.ScientistBase_BatteryCooldown, new(1, 250, 1), 15, TabGroup.CrewmateRoles, false)
            .SetParent(MedicPortableVitals)
            .SetValueFormat(OptionFormat.Seconds);
        VitalsDuration = IntegerOptionItem.Create(Id + 37, GeneralOption.ScientistBase_BatteryDuration, new(1, 250, 1), 5, TabGroup.CrewmateRoles, false)
            .SetParent(MedicPortableVitals)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        GlobalProtectedList.Clear();
        ProtectedPlayers.Clear();
        ProtectedList.Clear();
        TempMarkProtected.Clear();
        GiveTasks = false;
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(1);
        ProtectedPlayers[playerId] = [];
    }
    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(TempMarkProtected.Count);
        foreach (var markProtected in TempMarkProtected)
        {
            writer.Write(markProtected);
        }
        writer.Write(ProtectedList.Count);
        foreach (var protect in ProtectedList)
        {
            writer.Write(protect);
        }
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int countMarkProtected = reader.ReadInt32();
        TempMarkProtected.Clear();
        for (int i = 0; i < countMarkProtected; i++)
            TempMarkProtected.Add(reader.ReadByte());

        int countProtected = reader.ReadInt32();
        ProtectedList.Clear();
        for (int i = 0; i < countProtected; i++)
            ProtectedList.Add(reader.ReadByte());
    }

    public static bool IsProtected(byte id)
        => GlobalProtectedList.Contains(id) && Main.PlayerStates.TryGetValue(id, out var ps) && !ps.IsDead;

    private bool IsProtect(byte id)
        => ProtectedList.Contains(id) && Main.PlayerStates.TryGetValue(id, out var ps) && !ps.IsDead;

    private static bool CheckKillButton(byte playerId) => playerId.GetAbilityUseLimit() > 0;

    public override bool CanUseKillButton(PlayerControl pc) => CheckKillButton(pc.PlayerId);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CheckKillButton(id) ? 5f : 300f;

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (ProtectedList.Contains(target.PlayerId)) return false;

        killer.RpcRemoveAbilityUse();
        ProtectedPlayers[killer.PlayerId].Add(target.PlayerId);
        GlobalProtectedList.Add(target.PlayerId);
        ProtectedList.Add(target.PlayerId);
        TempMarkProtected.Add(target.PlayerId);
        SendRPC();

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

        ChangeToCrewmate();

        NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);
        NotifyRoles(SpecifySeer: target, SpecifyTarget: killer);
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (_Player == null || !IsProtect(target.PlayerId)) return false;

        var medic = _Player;
        SendRPC();

        killer.RpcGuardAndKill(target);
        killer.SetKillCooldown(ResetCooldown.GetFloat());

        NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

        switch (KnowShieldBrokenOpt.GetValue())
        {
            case 0:
                medic?.Notify(GetString("MedicKillerTryBrokenShieldTargetForMedic"));
                target.RpcGuardAndKill(target);
                target.Notify(GetString("MedicKillerTryBrokenShieldTargetForTarget"));
                break;
            case 1:
                medic?.Notify(GetString("MedicKillerTryBrokenShieldTargetForMedic"));
                break;
            case 2:
                target.RpcGuardAndKill(target);
                target.Notify(GetString("MedicKillerTryBrokenShieldTargetForTarget"));
                break;
        }

        Logger.Info($"{target.GetNameWithRole()} : Shield Shatter from the Medic", "Medic");
        return true;
    }
    private void ChangeToCrewmate()
    {
        if (_Player == null) return;

        var medic = _Player;
        var medicClientId = medic.GetClientId();
        GiveTasks = true;

        if (!MedicPortableVitals.GetBool())
        {
            medic.RpcSetRoleDesync(RoleTypes.Crewmate, medicClientId);
        }
        else
        {
            medic.RpcSetRoleDesync(RoleTypes.Scientist, medicClientId);
        }
        medic.RpcResetTasks();
    }
    public override void AfterMeetingTasks()
    {
        if (!ShieldDeactivatesWhenMedicDies.GetBool()) return;

        if (ShieldDeactivationIsVisibleOpt.GetValue() is 1)
        {
            TempMarkProtected.Clear();
            SendRPC();
        }
    }
    private void AfterMedicDeadTask(PlayerControl target)
    {
        if (!ShieldDeactivatesWhenMedicDies.GetBool()) return;

        if (ProtectedPlayers.TryGetValue(target.PlayerId, out var protectedList))
        {
            foreach (var protectedId in protectedList)
            {
                ProtectedPlayers[target.PlayerId].Remove(protectedId);
                GlobalProtectedList.Remove(protectedId);
            }
        }

        ProtectedList.Clear();
        Logger.Info($"{target.GetNameWithRole()} : Medic is dead", "Medic");

        if (ShieldDeactivationIsVisibleOpt.GetValue() is 0)
        {
            TempMarkProtected.Clear();
        }
        if (!target.IsDisconnected())
        {
            SendRPC();
        }
        NotifyRoles(ForceLoop: true);
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        AfterMedicDeadTask(target);
    }

    public override bool HasTasks(NetworkedPlayerInfo player, CustomRoles role, bool ForRecompute) => GiveTasks;
    public override void ApplyGameOptions(IGameOptions opt, byte playerid)
    {
        opt.SetVision(false);
        AURoleOptions.ScientistCooldown = VitalsCooldown.GetFloat();
        AURoleOptions.ScientistBatteryCharge = VitalsDuration.GetFloat();
    }

    public override string GetMark(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (WhoCanSeeProtectOpt.GetInt() is 0 or 1)
        {
            if (seer.PlayerId == target.PlayerId && (IsProtect(seer.PlayerId) || TempMarkProtected.Contains(seer.PlayerId)))
            {
                return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
            }
            else if (seer.PlayerId != target.PlayerId && (IsProtect(target.PlayerId) || TempMarkProtected.Contains(target.PlayerId)))
            {
                return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
            }
        }
        return string.Empty;
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (!seer.Is(CustomRoles.Medic))
        {
            // The seer sees protect on himself
            if (seer.PlayerId == target.PlayerId && (IsProtect(seer.PlayerId) || TempMarkProtected.Contains(seer.PlayerId)) && (WhoCanSeeProtectOpt.GetInt() is 0 or 2))
            {
                return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
            }
            else if (seer.PlayerId != target.PlayerId && !seer.IsAlive() && (IsProtect(target.PlayerId) || TempMarkProtected.Contains(target.PlayerId)))
            {
                // Dead players see protect
                return ColorString(GetRoleColor(CustomRoles.Medic), "✚");
            }
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
