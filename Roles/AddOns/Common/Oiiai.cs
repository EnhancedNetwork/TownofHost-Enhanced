using System.Collections.Generic;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common
{
    public static class Oiiai
    {
        private static readonly int Id = 7150;
        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        public static OptionItem CanBeOnCrew;
        public static OptionItem CanBeOnImp;
        public static OptionItem CanBeOnNeutral;
        public static OptionItem CanPassOn;
        public static OptionItem ChangeNeutralRole;

        public static readonly string[] NChangeRoles =
        {
            "Role.NoChange",
            "Role.Amnesiac",
            "Role.Imitator",
            //   CustomRoles.Crewmate.ToString(), CustomRoles.Jester.ToString(), CustomRoles.Opportunist.ToString(),
        };

        public static readonly CustomRoles[] NRoleChangeRoles =
        {
            CustomRoles.Amnesiac,
            CustomRoles.Imitator,
        }; //Just -1 to use this LOL

        public static void SetupCustomOptions()
        {
            Options.SetupAdtRoleOptions(Id, CustomRoles.Oiiai, canSetNum: true, tab: TabGroup.OtherRoles);
            CanBeOnImp = BooleanOptionItem.Create(Id + 11, "ImpCanBeOiiai", true, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
            CanBeOnCrew = BooleanOptionItem.Create(Id + 12, "CrewCanBeOiiai", true, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
            CanBeOnNeutral = BooleanOptionItem.Create(Id + 13, "NeutralCanBeOiiai", true, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
            CanPassOn = BooleanOptionItem.Create(Id + 14, "OiiaiCanPassOn", true, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
            ChangeNeutralRole = StringOptionItem.Create(Id + 15, "NeutralChangeRolesForOiiai", NChangeRoles, 1, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
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
        }

        public static void OnMurderPlayer(PlayerControl killer, PlayerControl target)
        {
            if (killer == null || target == null) return;
            if (killer.PlayerId == target.PlayerId) return;

            if (!target.Is(CustomRoles.Oiiai)) return;
            if (!CanGetOiiaied(killer)) return;

            if (CanPassOn.GetBool() && !playerIdList.Contains(killer.PlayerId))
            {
                Add(killer.PlayerId);
                killer.RpcSetCustomRole(CustomRoles.Oiiai);
                Logger.Info(killer.GetNameWithRole() + " gets Oiiai addon by " + target.GetNameWithRole(), "Oiiai");
            }

            if (!Main.ErasedRoleStorage.ContainsKey(killer.PlayerId))
            {
                Main.ErasedRoleStorage.Add(killer.PlayerId, killer.GetCustomRole());
                Logger.Info($"Added {killer.GetNameWithRole()} to ErasedRoleStorage", "Oiiai");
            }
            else
            {
                Logger.Info($"Canceled {killer.GetNameWithRole()} Oiiai bcz already erased.", "Oiiai");
                return;
            }

            if (!killer.GetCustomRole().IsNK())
            {
                //Use eraser here LOL
                killer.RpcSetCustomRole(CustomRolesHelper.GetErasedRole(killer.GetCustomRole().GetRoleTypes(), killer.GetCustomRole()));
            }
            else
            {
                //Typically only NK tiggers this
                killer.RpcSetCustomRole(NRoleChangeRoles[ChangeNeutralRole.GetValue() - 1]);
            }
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.Notify(GetString("LostRoleByOiiai"));
            Logger.Info($"{killer.GetRealName()} was OIIAIed", "Oiiai");
        }
        private static bool CanGetOiiaied(PlayerControl player)
        {
            if (player.GetCustomRole().IsNK() && ChangeNeutralRole.GetValue() == 0) return false;
            if (player.Is(CustomRoles.Loyal)) return false;

            return true;
        }
    }
}
