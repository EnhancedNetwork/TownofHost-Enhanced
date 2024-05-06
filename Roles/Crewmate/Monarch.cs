using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Monarch : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 12100;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem KnightCooldown;
    private static OptionItem KnightMax;

    private static Dictionary<byte, int> KnightLimit = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Monarch, 1);
        KnightCooldown = FloatOptionItem.Create(Id + 10, "MonarchKnightCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Monarch])
            .SetValueFormat(OptionFormat.Seconds);
        KnightMax = IntegerOptionItem.Create(Id + 12, "MonarchKnightMax", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Monarch])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList.Clear();
        KnightLimit = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        KnightLimit[playerId] = KnightMax.GetInt();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        KnightLimit.Remove(playerId);
        playerIdList.Remove(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Monarch);
        writer.Write(playerId);
        writer.Write(KnightLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();
        KnightLimit[playerId] = reader.ReadInt32();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KnightCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl player) => player.IsAlive() 
                                                                    && KnightLimit.ContainsKey(player.PlayerId) 
                                                                    && KnightLimit[player.PlayerId] >= 1;

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        return !CustomRoles.Knighted.RoleExist();
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!KnightLimit.ContainsKey(killer.PlayerId)) KnightLimit[killer.PlayerId] = KnightMax.GetInt();
        if (KnightLimit[killer.PlayerId] < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }
        if (CanBeKnighted(target))
        {
            KnightLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            target.RpcSetCustomRole(CustomRoles.Knighted);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("MonarchKnightedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("KnightedByMonarch")));

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            //      killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.SetKillCooldown(forceAnime: true);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Knighted.ToString(), "Assign " + CustomRoles.Knighted.ToString());
            if (KnightLimit[killer.PlayerId] < 0)
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{KnightLimit}次招募机会", "Monarch");
            return false;
        }

        if (KnightLimit[killer.PlayerId] < 0)
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("MonarchInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{KnightLimit}次招募机会", "Monarch");
        return false;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (target.Is(CustomRoles.Monarch) && CustomRoles.Knighted.RoleExist())
        {
            if (!isUI) Utils.SendMessage(GetString("GuessMonarch"), guesser.PlayerId);
            else guesser.ShowPopUp(GetString("GuessMonarch"));
            return true;
        }
        return false;
    }
    public override string GetProgressText(byte PlayerId, bool comms)
    {
        if (!KnightLimit.ContainsKey(PlayerId)) return "Invalid";
        Color color;
        if (KnightLimit[PlayerId] >= 1)
            color = Utils.GetRoleColor(CustomRoles.Monarch);
        else color = Color.gray;
        return (Utils.ColorString(color, $"({KnightLimit[PlayerId]})"));
    }
    private static bool CanBeKnighted(PlayerControl pc)
    {
        return pc != null && !pc.GetCustomRole().IsNotKnightable() &&
            !pc.IsAnySubRole(x => x is CustomRoles.Knighted or CustomRoles.Stubborn or CustomRoles.TicketsStealer);
    }
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => seer.Is(CustomRoles.Monarch) && target.Is(CustomRoles.Knighted) ? Main.roleColors[CustomRoles.Knighted] : "";

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("MonarchKillButtonText"));
    }
}