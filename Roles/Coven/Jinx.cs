using Hazel;
using InnerNet;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Jinx : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Jinx;
    private const int Id = 16800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    //private static OptionItem CanVent;
    //private static OptionItem HasImpostorVision;
    private static OptionItem JinxSpellTimes;
    //private static OptionItem killAttacker;
    private static OptionItem CovenCanDieToJinx;


    private static readonly Dictionary<byte, List<byte>> JinxedPlayers = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Jinx, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
            .SetValueFormat(OptionFormat.Seconds);
        //CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        //HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        JinxSpellTimes = IntegerOptionItem.Create(Id + 14, "JinxSpellTimes", new(1, 100, 1), 10, TabGroup.CovenRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Jinx])
        .SetValueFormat(OptionFormat.Times);
        //killAttacker = BooleanOptionItem.Create(Id + 15, GeneralOption.KillAttackerWhenAbilityRemaining, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);
        CovenCanDieToJinx = BooleanOptionItem.Create(Id + 16, "JinxCovenCanDieToJinx", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jinx]);

    }
    public override void Init()
    {
        JinxedPlayers.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(JinxSpellTimes.GetInt());
        JinxedPlayers[playerId] = [];
        playerId.GetPlayer()?.AddDoubleTrigger();
    }
    /*
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (AbilityLimit <= 0) return true;
        if (killer.IsTransformedNeutralApocalypse()) return true;
        if (killer == target) return true;
        
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
       
        AbilityLimit -= 1;
        SendSkillRPC();

        if (killAttacker.GetBool() && target.RpcCheckAndMurder(killer, true))
        {
            Logger.Info($"{target.GetNameWithRole()}: ability left {AbilityLimit}", "Jinx");
            killer.SetDeathReason(PlayerState.DeathReason.Jinx);
            killer.RpcMurderPlayer(killer);
            killer.SetRealKiller(target);
        }
        return false;
    }
    */
    //public override void ApplyGameOptions(IGameOptions opt, byte babushka) => opt.SetVision(HasImpostorVision.GetBool());
    private static bool IsJinxed(byte target)
    {
        if (JinxedPlayers.Count < 1) return false;
        foreach (var player in JinxedPlayers.Keys)
        {
            if (JinxedPlayers[player].Contains(target)) return true;
        }
        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (killer.CheckDoubleTrigger(target, () => { JinxPlayer(killer, target); }))
        {
            if (HasNecronomicon(killer))
            {
                if (target.GetCustomRole().IsCovenTeam())
                {
                    killer.Notify(GetString("CovenDontKillOtherCoven"));
                    return false;
                }
                else return true;
            }
        }
        return false;
    }
    private void JinxPlayer(PlayerControl jinx, PlayerControl target)
    {
        if (IsJinxed(target.PlayerId)) return;
        if (jinx.GetAbilityUseLimit() > 0)
        {
            JinxedPlayers[jinx.PlayerId].Add(target.PlayerId);
            jinx.ResetKillCooldown();
            jinx.SetKillCooldown();
            jinx.RpcRemoveAbilityUse();
            SendRPC(jinx, target);
        }
    }
    private void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        byte jinxID = reader.ReadByte();
        byte jinxedID = reader.ReadByte();

        JinxedPlayers[jinxID].Add(jinxedID);
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!IsJinxed(target.PlayerId)) return false;

        var jinx = _Player;
        if (!jinx.IsAlive() || jinx.PlayerId == target.PlayerId) return false;

        var killerRole = killer.GetCustomRole();
        // Not should kill
        if (killerRole is CustomRoles.Taskinator
            or CustomRoles.Bodyguard
            or CustomRoles.Veteran
            or CustomRoles.Deputy)
            return false;

        if (killer.GetCustomRole().IsCovenTeam() && !CovenCanDieToJinx.GetBool()) return false;

        if (jinx.CheckForInvalidMurdering(killer) && jinx.RpcCheckAndMurder(killer, true))
        {
            killer.RpcGuardAndKill(target);
            killer.SetDeathReason(PlayerState.DeathReason.Jinx);
            killer.SetRealKiller(jinx);
            killer.RpcMurderPlayer(killer);
            if (HasNecronomicon(jinx))
            {
                target.SetDeathReason(PlayerState.DeathReason.Jinx);
                target.RpcMurderPlayer(target);
                target.SetRealKiller(jinx);
            }
            JinxedPlayers[jinx.PlayerId].Remove(target.PlayerId);
            return true;
        }

        if (killer.Is(CustomRoles.Pestilence))
        {
            JinxedPlayers[jinx.PlayerId].Remove(target.PlayerId);
            return false;
        }


        return false;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        => IsJinxed(seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.Jinx), "⌘") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (_Player == null) return string.Empty;
        if (IsJinxed(target.PlayerId) && ((seer.GetCustomRole().IsCovenTeam() && seer.PlayerId != _Player.PlayerId) || !seer.IsAlive()))
        {
            return ColorString(GetRoleColor(CustomRoles.Jinx), "⌘");
        }
        return string.Empty;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("Jinx"));
    }
    //public override bool CanUseImpostorVentButton(PlayerControl player) => CanVent.GetBool();
}
