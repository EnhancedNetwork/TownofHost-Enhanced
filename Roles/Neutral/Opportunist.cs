using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Neutral
{
    internal class Opportunist : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 13300;
        private static HashSet<byte> PlayerIds = [];
        public static bool HasEnabled = PlayerIds.Count > 0;
        public override bool IsEnable => HasEnabled;
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

        //==================================================================\\
        public static void SetupCustomOptions()
        {
            SetupRoleOptions(13300, TabGroup.NeutralRoles, CustomRoles.Opportunist);
            OppoImmuneToAttacksWhenTasksDone = BooleanOptionItem.Create(13302, "ImmuneToAttacksWhenTasksDone", false, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Opportunist]);
            OverrideTasksData.Create(13303, TabGroup.NeutralRoles, CustomRoles.Opportunist);
        }
        public override void Init()
        {
            PlayerIds.Clear();
        }
        public override void Add(byte playerId)
        {
            PlayerIds.Add(playerId);
        }
        public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
            => !(OppoImmuneToAttacksWhenTasksDone.GetBool() && target.AllTasksCompleted());
        
    }
}
