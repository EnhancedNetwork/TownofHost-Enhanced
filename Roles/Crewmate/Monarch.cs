using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Monarch : RoleBase
{
    private static readonly int Id = 12100;
    private static List<byte> playerIdList = [];
    public static bool On = false;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    public static OptionItem KnightCooldown;
    public static OptionItem KnightMax;
    
    private static int KnightLimit = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Monarch, 1);
        KnightCooldown = FloatOptionItem.Create(Id + 10, "MonarchKnightCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Monarch])
            .SetValueFormat(OptionFormat.Seconds);
        KnightMax = IntegerOptionItem.Create(Id + 12, "MonarchKnightMax", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Monarch])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList = [];
        KnightLimit = new();
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        KnightLimit = KnightMax.GetInt();
        On = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }
    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Monarch);
        writer.Write(KnightLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        KnightLimit = reader.ReadInt32();
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KnightCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && KnightLimit >= 1;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (KnightLimit < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CantRecruit")));
            return false;
        }
        if (CanBeKnighted(target))
        {
            KnightLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Knighted);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("MonarchKnightedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("KnightedByMonarch")));

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
      //      killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.SetKillCooldown(forceAnime: true);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Knighted.ToString(), "Assign " + CustomRoles.Knighted.ToString());
            if (KnightLimit < 0)
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{KnightLimit}次招募机会", "Monarch");
            return false;
        }
        
        if (KnightLimit < 0)
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("MonarchInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{KnightLimit}次招募机会", "Monarch");
        return false;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser)
    {
        if (target.Is(CustomRoles.Monarch) && CustomRoles.Knighted.RoleExist())
        {
            if (!isUI) Utils.SendMessage(GetString("GuessMonarch"), guesser.PlayerId);
            else guesser.ShowPopUp(GetString("GuessMonarch"));
            return true;
        }
        return false;
    }
    public override string GetProgressText(byte PlayerId, bool comms) => Utils.ColorString(KnightLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Monarch).ShadeColor(0.25f) : Color.gray, $"({KnightLimit})");
    public static bool CanBeKnighted(PlayerControl pc)
    {
        return pc != null && !pc.GetCustomRole().IsNotKnightable() && 
            !pc.IsAnySubRole(x => x is CustomRoles.Knighted or CustomRoles.Stubborn or CustomRoles.TicketsStealer);
    }
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => seer.Is(CustomRoles.Monarch) && target.Is(CustomRoles.Knighted) ? Main.roleColors[CustomRoles.Knighted] : "";
}
