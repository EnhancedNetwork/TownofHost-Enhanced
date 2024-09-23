using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Blackmailer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 24600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Blackmailer);
    
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem SkillCooldown;
    private static OptionItem ShowShapeshiftAnimationsOpt;

    private static readonly HashSet<byte> ForBlackmailer = [];

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Blackmailer);
        SkillCooldown = FloatOptionItem.Create(Id + 2, "BlackmailerSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blackmailer])
           .SetValueFormat(OptionFormat.Seconds);
        ShowShapeshiftAnimationsOpt = BooleanOptionItem.Create(Id + 3, GeneralOption.ShowShapeshiftAnimations, true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Blackmailer]);
    }
    public override void Init()
    {
        ForBlackmailer.Clear();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = SkillCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    private void SendRPC(byte target = byte.MaxValue)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(_Player);
        writer.Write(target);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        var targetId = reader.ReadByte();

        if (targetId == byte.MaxValue)
            ClearBlackmaile(true);
        else
            ForBlackmailer.Add(targetId);
    }
    public override bool OnCheckShapeshift(PlayerControl blackmailer, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (ShowShapeshiftAnimationsOpt.GetBool() || blackmailer.PlayerId == target.PlayerId) return true;

        DoBlackmaile(blackmailer, target);
        blackmailer.Notify(GetString("RejectShapeshift.AbilityWasUsed"), time: 2f);
        return false;
    }
    public override void OnShapeshift(PlayerControl blackmailer, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (shapeshifting && IsAnimate)
        {
            DoBlackmaile(blackmailer, target);
        }
    }
    private void DoBlackmaile(PlayerControl blackmailer, PlayerControl target)
    {
        if (!target.IsAlive())
        {
            blackmailer.Notify(Utils.ColorString(Utils.GetRoleColor(blackmailer.GetCustomRole()), GetString("TargetIsAlreadyDead")));
            return;
        }

        ClearBlackmaile(true);
        ForBlackmailer.Add(target.PlayerId);
        SendRPC(target.PlayerId);
    }

    public override void AfterMeetingTasks()
    {
        ClearBlackmaile(true);
    }
    public override void OnCoEndGame()
    {
        ClearBlackmaile(false);
    }
    private void ClearBlackmaile(bool sendRpc)
    {
        ForBlackmailer.Clear();
        if (sendRpc) SendRPC();
    }
    
    public static bool CheckBlackmaile(PlayerControl player) => HasEnabled && GameStates.IsInGame && ForBlackmailer.Contains(player.PlayerId);

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
       => isForMeeting && CheckBlackmaile(target) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), "╳") : string.Empty;

    public override void OnOthersMeetingHudStart(PlayerControl pc)
    {
        if (CheckBlackmaile(pc))
        {
            var playername = pc.GetRealName(isMeeting: true);
            if (Main.OvverideOutfit.TryGetValue(pc.PlayerId, out var realfit)) playername = realfit.name; 
            AddMsg(string.Format(string.Format(GetString("BlackmailerDead"), playername), byte.MaxValue, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Blackmailer), GetString("BlackmaileKillTitle"))));
        }
    }
}