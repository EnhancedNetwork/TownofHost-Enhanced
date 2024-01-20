using AmongUs.GameOptions;
using System.Collections.Generic;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common
{
    public static class Oiiai
    {
        private static readonly int Id = 25700;
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
            Options.SetupAdtRoleOptions(Id, CustomRoles.Oiiai, canSetNum: true, tab: TabGroup.Addons);
            CanBeOnImp = BooleanOptionItem.Create(Id + 11, "ImpCanBeOiiai", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
            CanBeOnCrew = BooleanOptionItem.Create(Id + 12, "CrewCanBeOiiai", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
            CanBeOnNeutral = BooleanOptionItem.Create(Id + 13, "NeutralCanBeOiiai", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
            CanPassOn = BooleanOptionItem.Create(Id + 14, "OiiaiCanPassOn", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
            ChangeNeutralRole = StringOptionItem.Create(Id + 15, "NeutralChangeRolesForOiiai", NChangeRoles, 1, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Oiiai]);
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
            if (killer.Is(CustomRoles.Minimalism)) return;
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

            if (!killer.GetCustomRole().IsNeutral())
            {
                //Use eraser here LOL
                killer.RpcSetCustomRole(CustomRolesHelper.GetErasedRole(killer.GetCustomRole().GetRoleTypes(), killer.GetCustomRole()));
                Logger.Info($"Oiiai {killer.GetNameWithRole()} with eraser assign.", "Oiiai");
            }
            else
            {
                if (killer.HasImpKillButton())
                {
                    int changeValue = ChangeNeutralRole.GetValue();

                    if (changeValue != 0)
                    {
                        killer.RpcSetCustomRole(NRoleChangeRoles[changeValue - 1]);
                        if (changeValue == 1) Amnesiac.Add(killer.PlayerId);
                        else if (changeValue == 2) Imitator.Add(killer.PlayerId);

                        Logger.Info($"Oiiai {killer.GetNameWithRole()} with Neutrals with kill button assign.", "Oiiai");
                    }
                }
                else
                {
                    killer.RpcSetCustomRole(CustomRoles.Opportunist);
                    Logger.Info($"Oiiai {killer.GetNameWithRole()} with Neutrals without kill button assign.", "Oiiai");
                }
            }
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.Notify(GetString("LostRoleByOiiai"));
            Logger.Info($"{killer.GetRealName()} was OIIAIed", "Oiiai");
        }

        private static bool CanGetOiiaied(PlayerControl player)
        {
            if (player.GetCustomRole().IsNeutral() && ChangeNeutralRole.GetValue() == 0) return false;
            if (player.Is(CustomRoles.Loyal)) return false;

            return true;
        }
    }
}
