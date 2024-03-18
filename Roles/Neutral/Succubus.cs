using Hazel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Cultist : RoleBase
{

    //===========================SETUP================================\\
    private static readonly int Id = 14800;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    //==================================================================\\

    public static OptionItem CharmCooldown;
    public static OptionItem CharmCooldownIncrese;
    public static OptionItem CharmMax;
    public static OptionItem KnowTargetRole;
    public static OptionItem TargetKnowOtherTarget;
    public static OptionItem CanCharmNeutral;
    public static OptionItem CharmedCountMode;

    public static readonly string[] charmedCountMode =
    [
        "CharmedCountMode.None",
        "CharmedCountMode.Cultist",
        "CharmedCountMode.Original",
    ];

    private static int CharmLimit = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Cultist, 1, zeroOne: false);
        CharmCooldown = FloatOptionItem.Create(Id + 10, "CultistCharmCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist])
            .SetValueFormat(OptionFormat.Seconds);
        CharmCooldownIncrese = FloatOptionItem.Create(Id + 11, "CultistCharmCooldownIncrese", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist])
            .SetValueFormat(OptionFormat.Seconds);
        CharmMax = IntegerOptionItem.Create(Id + 12, "CultistCharmMax", new(1, 15, 1), 15, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "CultistKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist]);
        TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "CultistTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist]);
        CharmedCountMode = StringOptionItem.Create(Id + 17, "CharmedCountMode", charmedCountMode, 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist]);
        CanCharmNeutral = BooleanOptionItem.Create(Id + 18, "CultistCanCharmNeutral", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cultist]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        CharmLimit = byte.MaxValue;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CharmLimit = CharmMax.GetInt();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Cultist); //SetCultistCharmLimit
        writer.Write(CharmLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        CharmLimit = reader.ReadInt32();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CharmLimit >= 1 ? CharmCooldown.GetFloat() + (CharmMax.GetInt() - CharmLimit) * CharmCooldownIncrese.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && CharmLimit >= 1;
    public override void OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (CharmLimit < 1) return;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return;
        }
        else if (CanBeCharmed(target) && Mini.Age == 18 || CanBeCharmed(target) && Mini.Age < 18 && !(target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            CharmLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Charmed);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CultistCharmedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CharmedByCultist")));
            
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Charmed.ToString(), "Assign " + CustomRoles.Charmed.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{CharmLimit}次魅惑机会", "Cultist");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CultistInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{CharmLimit}次魅惑机会", "Cultist");
        return;
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Cultist)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Cultist) && target.Is(CustomRoles.Charmed)) return true;
        if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed)) return true;
        return false;
    }
    public override string GetProgressText(byte playerid, bool cooms) => Utils.ColorString(CharmLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Cultist).ShadeColor(0.25f) : Color.gray, $"({CharmLimit})");
    private static bool CanBeCharmed(this PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || 
            (CanCharmNeutral.GetBool() && pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Charmed) 
            && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Infectious) 
            && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Cultist)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
    public static string NameRoleColor(PlayerControl seer, PlayerControl target)
    {
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Cultist)) return Main.roleColors[CustomRoles.Cultist];
        if (seer.Is(CustomRoles.Cultist) && target.Is(CustomRoles.Charmed)) return Main.roleColors[CustomRoles.Charmed];
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed) && Cultist.TargetKnowOtherTarget.GetBool()) return Main.roleColors[CustomRoles.Charmed];
        else return string.Empty;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("CultistKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Subbus");
}