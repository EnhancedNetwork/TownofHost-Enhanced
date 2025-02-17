using AmongUs.GameOptions;
using UnityEngine;

using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Pyromaniac : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Pyromaniac;
    private const int Id = 17800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Pyromaniac");

    private static OptionItem KillCooldown;
    private static OptionItem DouseCooldown;
    private static OptionItem BurnCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    private static readonly HashSet<byte> DousedList = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pyromaniac, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac])
            .SetValueFormat(OptionFormat.Seconds);
        DouseCooldown = FloatOptionItem.Create(Id + 11, "PyroDouseCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac])
            .SetValueFormat(OptionFormat.Seconds);
        BurnCooldown = FloatOptionItem.Create(Id + 12, "PyroBurnCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 13, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 14, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac]);
    }
    public override void Init()
    {
        DousedList.Clear();
    }
    public override void Add(byte playerId)
    {
        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
        => seer.Is(CustomRoles.Pyromaniac) && DousedList.Contains(target.PlayerId) ? "#BA4A00" : string.Empty;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null) return true;
        if (target == null) return true;

        if (DousedList.Contains(target.PlayerId))
        {
            _ = new LateTask(() => { killer.SetKillCooldown(BurnCooldown.GetFloat()); }, 0.1f, "Pyromaniac Set Kill Cooldown");
            return true;
        }
        else
        {
            return killer.CheckDoubleTrigger(target, () =>
            {
                DousedList.Add(target.PlayerId);
                killer.SetKillCooldown(DouseCooldown.GetFloat());
                Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);
            });
        }
    }
}
