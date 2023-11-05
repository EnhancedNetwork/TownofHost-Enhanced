using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor
{
    public static class Devourer
    {
        static GameData.PlayerOutfit ConsumedOutfit = new GameData.PlayerOutfit().Set("", 15, "", "", "visor_Crack", "", "");
        private static Dictionary<byte, GameData.PlayerOutfit> OriginalPlayerSkins = new();

        private static readonly int Id = 5500;
        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        private static OptionItem DefaultKillCooldown;
        private static OptionItem ReduceKillCooldown;
        private static OptionItem MinKillCooldown;
        private static OptionItem ShapeshiftCooldown;
        public static OptionItem HideNameOfConsumedPlayer;
        public static OptionItem ShowShapeshiftAnimation;

        public static Dictionary<byte, List<byte>> PlayerSkinsCosumed = new();

        private static Dictionary<byte, float> NowCooldown;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Devourer);
            DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "SansDefaultKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
                .SetValueFormat(OptionFormat.Seconds);
            ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "SansReduceKillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
                .SetValueFormat(OptionFormat.Seconds);
            MinKillCooldown = FloatOptionItem.Create(Id + 12, "SansMinKillCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
                .SetValueFormat(OptionFormat.Seconds);
            ShapeshiftCooldown = FloatOptionItem.Create(Id + 14, "DevourCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer])
                .SetValueFormat(OptionFormat.Seconds);
            HideNameOfConsumedPlayer = BooleanOptionItem.Create(Id + 16, "DevourerHideNameConsumed", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer]);
            ShowShapeshiftAnimation = BooleanOptionItem.Create(Id + 17, "ShowShapeshiftAnimation", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Devourer]);
        }
        public static void Init()
        {
            playerIdList = new();
            PlayerSkinsCosumed = new();
            OriginalPlayerSkins = new();
            NowCooldown = new();
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            PlayerSkinsCosumed.TryAdd(playerId, new List<byte>());
            NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
            IsEnable = true;
        }

        public static void ApplyGameOptions()
        {
            AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = 1f;
        }

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];

        public static void OnShapeshift(PlayerControl pc, PlayerControl target)
        {
            if (!pc.IsAlive() || Pelican.IsEaten(pc.PlayerId)) return;

            if (!PlayerSkinsCosumed[pc.PlayerId].Contains(target.PlayerId))
            {
                if (!Camouflage.IsCamouflage)
                {
                    SetSkin(target, ConsumedOutfit);
                }

                PlayerSkinsCosumed[pc.PlayerId].Add(target.PlayerId);
                pc.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Devourer), GetString("DevourerEatenSkin")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Devourer), GetString("EatenByDevourer")));
                Utils.NotifyRoles();

                OriginalPlayerSkins.Add(target.PlayerId, Camouflage.PlayerSkins[target.PlayerId]);
                Camouflage.PlayerSkins[target.PlayerId] = ConsumedOutfit;

                float cdReduction = ReduceKillCooldown.GetFloat() * PlayerSkinsCosumed[pc.PlayerId].Count;
                float cd = DefaultKillCooldown.GetFloat() - cdReduction;

                NowCooldown[pc.PlayerId] = cd < MinKillCooldown.GetFloat() ? MinKillCooldown.GetFloat() : cd;
            }
        }

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
}