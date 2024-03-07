using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using static TOHE.MeetingHudStartPatch;

namespace TOHE.Roles.Crewmate
{
    internal class Celebrity : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 6500;
        private static bool On = false;
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.Celebrity.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
        //==================================================================\\

        public static List<byte> CelebrityDead = [];

        public static OptionItem ImpKnowCelebrityDead;
        public static OptionItem NeutralKnowCelebrityDead;
        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Celebrity);
            ImpKnowCelebrityDead = BooleanOptionItem.Create(Id + 10, "ImpKnowCelebrityDead", false, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Celebrity]);
            NeutralKnowCelebrityDead = BooleanOptionItem.Create(Id + 11, "NeutralKnowCelebrityDead", false, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Celebrity]);

        }
        public override void Init()
        {
            CelebrityDead = [];
            On = false;
        }
        public override void Add(byte playerId)
        {
            On = true;
        }
        public override void AfterPlayerDeathTask(PlayerControl target)
        {
            if (GameStates.IsMeeting)
            {
                //Death Message
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (!Celebrity.ImpKnowCelebrityDead.GetBool() && pc.GetCustomRole().IsImpostor()) continue;
                    if (!Celebrity.NeutralKnowCelebrityDead.GetBool() && pc.GetCustomRole().IsNeutral()) continue;

                    SendMessage(string.Format(GetString("CelebrityDead"), target.GetRealName()), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Celebrity), GetString("CelebrityNewsTitle")));
                }
            }
            else
            {
                if (!Celebrity.CelebrityDead.Contains(target.PlayerId))
                    Celebrity.CelebrityDead.Add(target.PlayerId);
            }
        }
        public override void OnMeetingHudStart(PlayerControl targets)
        {
            foreach (var csId in Celebrity.CelebrityDead)
            {
                if (!Celebrity.ImpKnowCelebrityDead.GetBool() && targets.GetCustomRole().IsImpostor()) continue;
                if (!Celebrity.NeutralKnowCelebrityDead.GetBool() && targets.GetCustomRole().IsNeutral()) continue;
                AddMsg(string.Format(GetString("CelebrityDead"), Main.AllPlayerNames[csId]), targets.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Celebrity), GetString("CelebrityNewsTitle")));
            }
        }
        public override void MeetingHudClear()
        {
            CelebrityDead.Clear();
        }
    }
}
