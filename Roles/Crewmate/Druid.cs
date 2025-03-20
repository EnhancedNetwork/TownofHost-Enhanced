using static TOHE.Utils;
using TOHE.Modules;
using MS.Internal.Xml.XPath;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate;
internal class Druid : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Druid;
    private const int Id = 34700;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem AbilityUses;
    private static OptionItem AbilityDuration;

    public static PlayerControl TargetPlayer;
    public static bool IsTargetMurdered = false;
    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Druid);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Druid])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityUses = IntegerOptionItem.Create(Id + 11, "AbilityUses347", new (1, 5, 1), 3, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Druid]).SetValueFormat(OptionFormat.Times);
        AbilityDuration = FloatOptionItem.Create(Id + 12, "AbilityDuration347", new(5f, 25f, 2f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Druid])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(AbilityUses.GetInt());
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;

    public override void SetKillCooldown(byte id) => KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target == TargetPlayer)
        {
            if (target.GetCustomRole().IsCrewmate())
            {
                killer.RpcChangeRoleBasis(CustomRoles.CrewmateTOHO);
                killer.RpcSetCustomRole(CustomRoles.CrewmateTOHO);
            }
            IsTargetMurdered = true;
            return true;
        }

        if (killer.GetAbilityUseLimit() < 1)
        {
            killer.RpcGuardAndKill(killer);
            return false;
        }
        TargetPlayer = target;
        killer.RpcRemoveAbilityUse();
        killer.RpcGuardAndKill(killer);
        return false;
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (TargetPlayer == null) return;

        string dname = pc.GetRealName(isMeeting: true);
        var druidSkin = new NetworkedPlayerInfo.PlayerOutfit()
        .Set(dname, pc.CurrentOutfit.ColorId, pc.CurrentOutfit.HatId, pc.CurrentOutfit.SkinId, pc.CurrentOutfit.VisorId, pc.CurrentOutfit.PetId, pc.CurrentOutfit.NamePlateId);
        var druidLvl = pc.Data.PlayerLevel;

        string tname = TargetPlayer.GetRealName(isMeeting: true);
        var targetSkin = new NetworkedPlayerInfo.PlayerOutfit()
        .Set(tname, TargetPlayer.CurrentOutfit.ColorId, TargetPlayer.CurrentOutfit.HatId, TargetPlayer.CurrentOutfit.SkinId, TargetPlayer.CurrentOutfit.VisorId, TargetPlayer.CurrentOutfit.PetId, TargetPlayer.CurrentOutfit.NamePlateId);
        var targetLvl = TargetPlayer.Data.PlayerLevel;

        pc.SetNewOutfit(targetSkin, newLevel: targetLvl);
        Main.OvverideOutfit[pc.PlayerId] = (targetSkin, Main.PlayerStates[pc.PlayerId].NormalOutfit.PlayerName);
        Logger.Info("Changed Druid skin", "Druid");
        pc?.MyPhysics?.RpcBootFromVent(vent.Id);

        new LateTask(() =>
        {
            if (IsTargetMurdered)
            {
                IsTargetMurdered = false;
                return;
            }
            pc.SetNewOutfit(druidSkin, newLevel: druidLvl);
            Main.OvverideOutfit[pc.PlayerId] = (druidSkin, Main.PlayerStates[pc.PlayerId].NormalOutfit.PlayerName);
            TargetPlayer = null;
        }, AbilityDuration.GetFloat(), "End Druid Transformation");
    }
}
