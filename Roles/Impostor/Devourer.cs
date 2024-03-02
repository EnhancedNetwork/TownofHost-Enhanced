using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Impostor;

internal class Devourer : RoleBase
{
    static GameData.PlayerOutfit ConsumedOutfit = new GameData.PlayerOutfit().Set("", 15, "", "", "visor_Crack", "", "");
    private static Dictionary<byte, GameData.PlayerOutfit> OriginalPlayerSkins = [];

    private const int Id = 5500;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;
    private static OptionItem ShapeshiftCooldown;
    private static OptionItem HideNameOfConsumedPlayer;

    private static Dictionary<byte, float> NowCooldown = [];
    private static Dictionary<byte, List<byte>> PlayerSkinsCosumed = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Devourer);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "ArroganceDefaultKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "ArroganceReduceKillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, "ArroganceMinKillCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 14, "DevourCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
            .SetValueFormat(OptionFormat.Seconds);
        HideNameOfConsumedPlayer = BooleanOptionItem.Create(Id + 16, "DevourerHideNameConsumed", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer]);
    }
    public override void Init()
    {
        PlayerSkinsCosumed = [];
        OriginalPlayerSkins = [];
        NowCooldown = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        PlayerSkinsCosumed.TryAdd(playerId, []);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
        On = true;
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];

    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool shapeshifting, bool shapeshiftIsHidden)
    {
        if (!shapeshifting && !shapeshiftIsHidden) return;
        if (!shapeshifter.IsAlive() || Pelican.IsEaten(shapeshifter.PlayerId)) return;

        if (!PlayerSkinsCosumed[shapeshifter.PlayerId].Contains(target.PlayerId))
        {
            if (!Camouflage.IsCamouflage)
            {
                SetSkin(target, ConsumedOutfit);
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

    public static void OnDevourerDied(byte Devourer)
    {
        foreach (byte player in PlayerSkinsCosumed[Devourer])
        {
            Camouflage.PlayerSkins[player] = OriginalPlayerSkins[player];

            if (!Camouflage.IsCamouflage)
            {
                PlayerControl pc =
                    Main.AllAlivePlayerControls.FirstOrDefault(a => a.PlayerId == player);
                if (pc == null) continue;

                SetSkin(pc, OriginalPlayerSkins[player]);
            }
        }

        PlayerSkinsCosumed[Devourer].Clear();
    }

    private static void SetSkin(PlayerControl target, GameData.PlayerOutfit outfit)
    {
        var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.PlayerName})");

        target.SetColor(outfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
            .Write(outfit.ColorId)
            .EndRpc();

        target.SetHat(outfit.HatId, outfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
            .Write(outfit.HatId)
            .EndRpc();

        target.SetSkin(outfit.SkinId, outfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(outfit.SkinId)
            .EndRpc();

        target.SetVisor(outfit.VisorId, outfit.ColorId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(outfit.VisorId)
            .EndRpc();

        target.SetPet(outfit.PetId);
        sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
            .Write(outfit.PetId)
            .EndRpc();

        sender.SendMessage();
    }
}