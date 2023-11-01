using System.Collections.Generic;

namespace TOHE.Roles.Impostor
{
    public static class Camouflager
    {
        private static readonly int Id = 2500;
        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        private static OptionItem CamouflageCooldown;
        private static OptionItem CamouflageDuration;
        public static OptionItem CanUseCommsSabotage;
        public static OptionItem DisableReportWhenCamouflageIsActive;

        public static bool IsActive;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Camouflager);
            CamouflageCooldown = FloatOptionItem.Create(Id + 2, "CamouflageCooldown", new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
                .SetValueFormat(OptionFormat.Seconds);
            CamouflageDuration = FloatOptionItem.Create(Id + 4, "CamouflageDuration", new(1f, 180f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
                .SetValueFormat(OptionFormat.Seconds);
            CanUseCommsSabotage = BooleanOptionItem.Create(Id + 6, "CanUseCommsSabotage", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
            DisableReportWhenCamouflageIsActive = BooleanOptionItem.Create(Id + 8, "DisableReportWhenCamouflageIsActive", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);

        }
        public static void Init()
        {
            playerIdList = new();
            IsActive = false;
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            IsEnable = true;
        }

        public static void ApplyGameOptions()
        {
            AURoleOptions.ShapeshifterCooldown = CamouflageCooldown.GetFloat();
            AURoleOptions.ShapeshifterDuration = CamouflageDuration.GetFloat();
        }
        public static void OnShapeshift()
        {
            IsActive = true;
            Camouflage.CheckCamouflage();
        }
        public static void OnReportDeadBody()
        {
            IsActive = false;
            Camouflage.CheckCamouflage();
        }
        public static void isDead(PlayerControl target)
        {
            if (!target.Data.IsDead || GameStates.IsMeeting) return;

            if(target.Is(CustomRoles.Camouflager) && target.Data.IsDead)
            {
                IsActive = false;
                Camouflage.CheckCamouflage();
                Utils.NotifyRoles();
            }
        }
    }
}
