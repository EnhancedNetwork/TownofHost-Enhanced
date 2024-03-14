using AmongUs.GameOptions;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class SerialKiller : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 17900;
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    //==================================================================\\


    public static OptionItem KillCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    public static OptionItem HasSerialKillerBuddy;
    public static OptionItem ChanceToSpawn;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SerialKiller, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SerialKiller])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SerialKiller]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SerialKiller]);
        HasSerialKillerBuddy = BooleanOptionItem.Create(Id + 16, "HasSerialKillerBuddy", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SerialKiller]);
        ChanceToSpawn = IntegerOptionItem.Create(Id + 14, "ChanceToSpawn", new(0, 100, 5), 100, TabGroup.NeutralRoles, false)
            .SetParent(HasSerialKillerBuddy)
            .SetValueFormat(OptionFormat.Percent); 
    }
    public override void Init()
    {
        playerIdList = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override bool CanUseImpostorVentButton(PlayerControl pc)
    {
        bool NSerialKiller_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(NSerialKiller_canUse && !pc.Data.IsDead);
        return NSerialKiller_canUse;
    }
}
