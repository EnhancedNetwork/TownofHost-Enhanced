using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate
{
    public static class Mole
    {
        private static readonly int Id = 26000;
        //private static List<byte> playerIdList = [];
        public static bool IsEnable = false;

        public static OptionItem VentCooldown;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mole);
            VentCooldown = FloatOptionItem.Create(Id + 11, "MoleVentCooldown", new(5f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mole])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            //playerIdList = [];
            IsEnable = false;
        }
        public static void Add(byte playerId)
        {
            //playerIdList.Add(playerId);
            IsEnable = true;
        }

        public static void OnExitVent(PlayerControl pc, int id)
        {
            if (!pc.Is(CustomRoles.Mole)) return;

            _ = new LateTask(() =>
            {
                var vents = Object.FindObjectsOfType<Vent>().Where(x => x.Id != id).ToArray();
                var rand = IRandom.Instance;
                var vent = vents[rand.Next(0, vents.Length)];

                Logger.Info($" {vent.transform.position}", "Mole vent teleport");
                pc.RpcTeleport(new Vector2(vent.transform.position.x, vent.transform.position.y + 0.3636f));
            }, 0.1f, "Mole On Exit Vent");
        }
    }
}
