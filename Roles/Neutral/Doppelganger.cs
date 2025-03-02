using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Doppelganger : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Doppelganger;
    private const int Id = 25000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Doppelganger);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem MaxSteals;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Doppelganger, 1, zeroOne: false);
        MaxSteals = IntegerOptionItem.Create(Id + 10, "DoppelMaxSteals", new(1, 14, 1), 9, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger]);
        KillCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger]);
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(MaxSteals.GetInt());
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || Camouflage.IsCamouflage || Camouflager.AbilityActivated || Utils.IsActive(SystemTypes.MushroomMixupSabotage)) return true;
        if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out bool isShapeshifitng) && isShapeshifitng)
        {
            Logger.Info("Target was shapeshifting", "Doppelganger");
            return true;
        }
        if (killer.GetAbilityUseLimit() < 1)
        {
            return true;
        }

        killer.RpcRemoveAbilityUse();

        string kname = killer.GetRealName(isMeeting: true);
        string tname = target.GetRealName(isMeeting: true);

        var killerSkin = new NetworkedPlayerInfo.PlayerOutfit()
            .Set(kname, killer.CurrentOutfit.ColorId, killer.CurrentOutfit.HatId, killer.CurrentOutfit.SkinId, killer.CurrentOutfit.VisorId, killer.CurrentOutfit.PetId, killer.CurrentOutfit.NamePlateId);
        var killerLvl = killer.Data.PlayerLevel;

        var targetSkin = new NetworkedPlayerInfo.PlayerOutfit()
            .Set(tname, target.CurrentOutfit.ColorId, target.CurrentOutfit.HatId, target.CurrentOutfit.SkinId, target.CurrentOutfit.VisorId, target.CurrentOutfit.PetId, target.CurrentOutfit.NamePlateId);
        var targetLvl = target.Data.PlayerLevel;


        target.SetNewOutfit(killerSkin, newLevel: killerLvl);
        Main.OvverideOutfit[target.PlayerId] = (killerSkin, Main.PlayerStates[killer.PlayerId].NormalOutfit.PlayerName);
        Logger.Info("Changed target skin", "Doppelganger");

        killer.SetNewOutfit(targetSkin, newLevel: targetLvl);
        Main.OvverideOutfit[killer.PlayerId] = (targetSkin, Main.PlayerStates[target.PlayerId].NormalOutfit.PlayerName);
        Logger.Info("Changed killer skin", "Doppelganger");

        RPC.SyncAllPlayerNames();
        Utils.NotifyRoles(SpecifyTarget: killer, NoCache: true);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return true;
    }
}
