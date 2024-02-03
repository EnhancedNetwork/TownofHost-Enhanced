using System.Collections.Generic;
using UnityEngine;
using System;
using Rewired.Utils.Platforms.Windows;

namespace TOHE.Roles.AddOns.Common
{
    public static class Statue
    {
        private static readonly int Id = 13800;

        public static OptionItem CanBeOnCrew;
        public static OptionItem CanBeOnImp;
        public static OptionItem CanBeOnNeutral;
        public static OptionItem SlowDown;
        public static OptionItem PeopleAmount;
        public static List<byte> CountNearplr;
        public static bool Active;
        public static Dictionary<byte, float> tempSpeed;
        public static void SetupCustomOptions()
        {
            Options.SetupAdtRoleOptions(Id, CustomRoles.Statue, canSetNum: true, tab: TabGroup.Addons);
            SlowDown = FloatOptionItem.Create(Id + 10, "StatueSlow", new(0f, 1.25f, 0.25f), 0f, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Statue])
                 .SetValueFormat(OptionFormat.Multiplier);
            PeopleAmount = IntegerOptionItem.Create(Id + 11, "StatuePeopleToSlow", new(1, 5, 1), 3, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Statue])
                 .SetValueFormat(OptionFormat.Times);
            CanBeOnImp = BooleanOptionItem.Create(Id + 12, "ImpCanBeStatue", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Statue]);
            CanBeOnCrew = BooleanOptionItem.Create(Id + 13, "CrewCanBeStatue", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Statue]);
            CanBeOnNeutral = BooleanOptionItem.Create(Id + 14, "NeutralCanBeStatue", true, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Statue]);
        }

        public static void Init()
        {
            CountNearplr = [];
            tempSpeed = [];
            Active = true;
        }

        public static void Add(byte player)
        {
            tempSpeed.Add(player, Main.AllPlayerSpeed[player]);
        }

        public static void AfterMeetingTasks()
        {
            Active = false;
            CountNearplr = [];
            _ = new LateTask(() => 
            {
                Active = true;
            }, 6f);
        }

        public static void OnFixedUpdate(PlayerControl victim) 
        {
            foreach (var PVC in Main.AllAlivePlayerControls)
            {
                if (CountNearplr.Contains(PVC.PlayerId) && PVC.PlayerId != victim.PlayerId && Vector2.Distance(PVC.transform.position, victim.transform.position) > 2f)
                {
                    CountNearplr.Remove(PVC.PlayerId);
                }
            }

            if (Active)
            {
                foreach (var plr in Main.AllAlivePlayerControls)
                {
                    if (Vector2.Distance(plr.transform.position, victim.transform.position) < 2f && plr != victim)
                    {
                        if (!CountNearplr.Contains(plr.PlayerId)) CountNearplr.Add(plr.PlayerId);
                    }
                }

                if (CountNearplr.Count >= PeopleAmount.GetInt())
                {
                    if (Main.AllPlayerSpeed[victim.PlayerId] != SlowDown.GetFloat()) 
                    { 
                        Main.AllPlayerSpeed[victim.PlayerId] = SlowDown.GetFloat();
                        victim.MarkDirtySettings();
                    }
                    return;
                }
                else if (Main.AllPlayerSpeed[victim.PlayerId] == SlowDown.GetFloat())
                {
                    float tmpFloat = tempSpeed[victim.PlayerId];
                    Main.AllPlayerSpeed[victim.PlayerId] = Main.AllPlayerSpeed[victim.PlayerId] - SlowDown.GetFloat() + tmpFloat;
                    victim.MarkDirtySettings();
                }

            }

        
        }
    }
}
