using AmongUs.GameOptions;
using MS.Internal.Xml.XPath;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.AddOns.Common
{
    public static class Lucky
    {
        private static readonly int Id = 19500;

        public static OptionItem LuckyProbability;
        public static OptionItem ImpCanBeLucky;
        public static OptionItem CrewCanBeLucky;
        public static OptionItem NeutralCanBeLucky;

        public static Dictionary<byte, bool> LuckyAvoid;
        public static void SetupCustomOptions()
        {
            SetupAdtRoleOptions(Id, CustomRoles.Lucky, canSetNum: true);
            LuckyProbability = IntegerOptionItem.Create(Id + 10, "LuckyProbability", new(0, 100, 5), 50, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lucky])
                .SetValueFormat(OptionFormat.Percent);
            ImpCanBeLucky = BooleanOptionItem.Create(Id + 11, "ImpCanBeLucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lucky]);
            CrewCanBeLucky = BooleanOptionItem.Create(Id + 12, "CrewCanBeLucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lucky]);
            NeutralCanBeLucky = BooleanOptionItem.Create(Id + 13, "NeutralCanBeLucky", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lucky]);
        }

        public static void Init()
        {
            LuckyAvoid = [];
        }
        public static void Add(byte PlayerId)
        {
            LuckyAvoid.Add(PlayerId, false);
        }
        

        public static void AvoidDeathChance(PlayerControl killer, PlayerControl target)
        {
            var rd = IRandom.Instance;
            if (rd.Next(0, 101) <= LuckyProbability.GetInt())
            {
                killer.RpcGuardAndKill(target);
                LuckyAvoid[target.PlayerId] = true;
            }
        }
    }
}