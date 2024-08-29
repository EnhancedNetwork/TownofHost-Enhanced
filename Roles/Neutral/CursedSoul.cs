using Hazel;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class CursedSoul : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14000;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem CurseCooldown;
    private static OptionItem CurseCooldownIncrese;
    private static OptionItem CurseMax;
    private static OptionItem KnowTargetRole;
    private static OptionItem CanCurseNeutral;

    private int CurseLimit;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.CursedSoul, 1, zeroOne: false);
        CurseCooldown = FloatOptionItem.Create(Id + 10, "CursedSoulCurseCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul])
            .SetValueFormat(OptionFormat.Seconds);
        CurseCooldownIncrese = FloatOptionItem.Create(Id + 11, "CursedSoulCurseCooldownIncrese", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul])
            .SetValueFormat(OptionFormat.Seconds);
        CurseMax = IntegerOptionItem.Create(Id + 12, "CursedSoulCurseMax", new(1, 15, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "CursedSoulKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul]);
        CanCurseNeutral = BooleanOptionItem.Create(Id + 16, "CursedSoulCanCurseNeutral", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CursedSoul]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        CurseLimit = CurseMax.GetInt();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurseLimit = CurseMax.GetInt();
    }

    private void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCursedSoulCurseLimit, SendOption.Reliable, -1);
        writer.Write(_state.PlayerId);
        writer.Write(CurseLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var pID = reader.ReadByte();
        if (Main.PlayerStates[pID].RoleClass is CursedSoul cs) 
            cs.CurseLimit = reader.ReadInt32();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CurseLimit >= 1 ? CurseCooldown.GetFloat() + (CurseMax.GetInt() - CurseLimit) * CurseCooldownIncrese.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => CurseLimit >= 1;
    
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (CurseLimit < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }
        if (CanBeSoulless(target))
        {
            CurseLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Soulless);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CursedSoul), GetString("CursedSoulSoullessPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CursedSoul), GetString("SoullessByCursedSoul")));
            
            Utils.NotifyRoles(SpecifySeer: target , SpecifyTarget: killer, ForceLoop: true);
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();

            if (!DisableShieldAnimations.GetBool())
                killer.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Soulless.ToString(), "Assign " + CustomRoles.Soulless.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{CurseLimit}次魅惑机会", "CursedSoul");
            return false;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.CursedSoul), GetString("CursedSoulInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{CurseLimit}次魅惑机会", "CursedSoul");
        return false;
    }
    public override bool KnowRoleTarget(PlayerControl player, PlayerControl target)
        => player.Is(CustomRoles.CursedSoul) && target.Is(CustomRoles.Soulless);

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
        => KnowRoleTarget(seer, target) ? Main.roleColors[CustomRoles.Soulless] : string.Empty;
    
    public override string GetProgressText(byte id, bool cooms) => Utils.ColorString(CurseLimit >= 1 ? Utils.GetRoleColor(CustomRoles.CursedSoul) : Color.gray, $"({CurseLimit})");
    private static bool CanBeSoulless(PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || 
            (CanCurseNeutral.GetBool() && pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Loyal);
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("CursedSoulKillButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Soul");
}