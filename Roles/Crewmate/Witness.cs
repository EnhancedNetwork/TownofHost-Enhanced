using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Witness : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Witness;
    private const int Id = 10100;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    public static readonly Dictionary<byte, MurderTresspass> AllMurderTresspass = [];

    private static OptionItem WitnessCD;
    private static OptionItem WitnessTime;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Witness, 1);
        WitnessCD = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Witness])
            .SetValueFormat(OptionFormat.Seconds);
        WitnessTime = IntegerOptionItem.Create(Id + 11, "WitnessTime", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Witness])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateLowLoadOthers);
        }
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = WitnessCD.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("WitnessButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Examine");

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.SetKillCooldown();
        if (Illusionist.IsCovIllusioned(target.PlayerId) || !AllMurderTresspass.TryGetValue(target.PlayerId, out MurderTresspass murTres))
            killer.Notify(GetString("WitnessFoundInnocent"));
        else if (murTres.HasTresspass && !murTres.HasMurder)
            killer.Notify(GetString("WitnessFoundTresspass"));
        else if (!murTres.HasTresspass && murTres.HasMurder)
            killer.Notify(GetString("WitnessFoundKiller"));
        else if (murTres.HasTresspass && murTres.HasMurder)
            killer.Notify(GetString("WitnessFoundKillerTresspass"));

        return false;
    }
    public static void OnFixedUpdateLowLoadOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (!lowLoad && AllMurderTresspass.TryGetValue(player.PlayerId, out var muTr))
            muTr.Update(WitnessTime.GetInt(), nowTime);
    }
}

internal class MurderTresspass(long? murder = null, long? tresspass = null)
{
    private long? Murder = murder;
    private long? Tresspass = tresspass;

    public bool HasMurder => Murder != null;
    public bool HasTresspass => Tresspass != null;

    public void Update(int duration, long nowTime)
    {
        if (Murder != null && Murder + duration < nowTime) Murder = null;
        if (Tresspass != null && Tresspass + duration < nowTime) Tresspass = null;
    }

    public void SetMurder(long? murder)
    {
        Murder = murder;
    }
    public void SetTresspass(long? tresspass)
    {
        Tresspass = tresspass;
    }
}