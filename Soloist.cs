using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TOHE.Roles.Neutral
{
    public static class Soloist
    {
        private static readonly int Id = 93892;
        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;

      
        public static OptionItem KillCooldown;
        public static OptionItem CanVent;

      

        public static void SetupCustomOption()
        {
          
            Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Soloist, 1, zeroOne: false);

            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Soloist])
                 .SetValueFormat(OptionFormat.Seconds);
            CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Soloist]);

        }

        public static void Init()
        {
            playerIdList = new();
            IsEnable = false;

        }
        public static void Add(byte playerId)
        {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    {
      
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = canUse;
    }
}
