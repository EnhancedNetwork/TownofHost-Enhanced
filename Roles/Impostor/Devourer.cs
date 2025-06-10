using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Devourer : RoleBase
{
    private readonly static NetworkedPlayerInfo.PlayerOutfit ConsumedOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set("", 15, "", "", "visor_Crack", "", "");
    private static readonly Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> OriginalPlayerSkins = [];

    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Devourer;
    private const int Id = 5500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Devourer);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;
    private static OptionItem ShapeshiftCooldown;
    private static OptionItem HideNameOfConsumedPlayer;
    private static OptionItem ShowShapeshiftAnimationsOpt;

    private static readonly Dictionary<byte, float> NowCooldown = [];
    private static readonly Dictionary<byte, HashSet<byte>> PlayerSkinsCosumed = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Devourer);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.DefaultKillCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.ReduceKillCooldown, new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, GeneralOption.MinKillCooldown, new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 14, "DevourCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
            .SetValueFormat(OptionFormat.Seconds);
        HideNameOfConsumedPlayer = BooleanOptionItem.Create(Id + 16, "DevourerHideNameConsumed", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer]);
        ShowShapeshiftAnimationsOpt = BooleanOptionItem.Create(Id + 17, GeneralOption.ShowShapeshiftAnimations, true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer]);
    }
    public override void Init()
    {
        PlayerSkinsCosumed.Clear();
        OriginalPlayerSkins.Clear();
        NowCooldown.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerSkinsCosumed.TryAdd(playerId, []);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
    }
    public override void Remove(byte playerId)
    {
        OnDevourerDied(Utils.GetPlayerById(playerId));
        PlayerSkinsCosumed.Remove(playerId);
        NowCooldown.Remove(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (ShowShapeshiftAnimationsOpt.GetBool() || shapeshifter.PlayerId == target.PlayerId) return true;

        DoEatSkin(shapeshifter, target);
        return false;
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting) return;

        DoEatSkin(shapeshifter, target);
    }
    private static void DoEatSkin(PlayerControl shapeshifter, PlayerControl target)
    {
        if (!PlayerSkinsCosumed[shapeshifter.PlayerId].Contains(target.PlayerId))
        {
            if (!Camouflage.IsCamouflage)
            {
                target.SetNewOutfit(ConsumedOutfit, setName: false, setNamePlate: false);
            }

            PlayerSkinsCosumed[shapeshifter.PlayerId].Add(target.PlayerId);
            shapeshifter.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Devourer), GetString("DevourerEatenSkin")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Devourer), GetString("EatenByDevourer")));

            OriginalPlayerSkins.Add(target.PlayerId, Camouflage.PlayerSkins[target.PlayerId]);
            Camouflage.PlayerSkins[target.PlayerId] = ConsumedOutfit;

            float cdReduction = ReduceKillCooldown.GetFloat() * PlayerSkinsCosumed[shapeshifter.PlayerId].Count;
            float cd = DefaultKillCooldown.GetFloat() - cdReduction;

            NowCooldown[shapeshifter.PlayerId] = cd < MinKillCooldown.GetFloat() ? MinKillCooldown.GetFloat() : cd;
        }
    }

    public static bool HideNameOfTheDevoured(byte targetId) => HideNameOfConsumedPlayer.GetBool() && PlayerSkinsCosumed.Any(a => a.Value.Contains(targetId));
    private static void OnDevourerDied(PlayerControl devourer)
    {
        if (devourer == null) return;
        var devourerId = devourer.PlayerId;

        foreach (byte player in PlayerSkinsCosumed[devourerId])
        {
            Camouflage.PlayerSkins[player] = OriginalPlayerSkins[player];

            if (!Camouflage.IsCamouflage)
            {
                PlayerControl pc =
                    Main.AllAlivePlayerControls.FirstOrDefault(a => a.PlayerId == player);
                if (pc == null) continue;

                pc.SetNewOutfit(OriginalPlayerSkins[player], setName: false, setNamePlate: false);
            }
        }

        PlayerSkinsCosumed[devourerId].Clear();
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl devourer, bool inMeeting, bool isSuicide)
    {
        OnDevourerDied(devourer);
    }

    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        if (exiled != null && exiled.Object.Is(CustomRoles.Devourer))
            OnDevourerDied(exiled.Object);
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("DevourerButtonText"));
    }
}
