using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral
{
    internal class Innocent : RoleBase
    {

        //===========================SETUP================================\\
        private static HashSet<byte> PlayerIds = [];
        public static bool HasEnabled => PlayerIds.Count > 0;
        public override bool IsEnable => HasEnabled;
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
        //==================================================================\\


        public static OptionItem InnocentCanWinByImp;
        public static void SetupCustomOptions()
        {

            SetupRoleOptions(14300, TabGroup.NeutralRoles, CustomRoles.Innocent);
            InnocentCanWinByImp = BooleanOptionItem.Create(14302, "InnocentCanWinByImp", false, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Innocent]);
        }
        public override void Init()
        {
            PlayerIds = [];
        }
        public override void Add(byte playerId)
        {
            PlayerIds.Add(playerId);
        }
        public override void SetAbilityButtonText(HudManager hud, byte playerId)
        {
            hud.KillButton.OverrideText(GetString("InnocentButtonText"));

            hud.SabotageButton.ToggleVisible(false);
            hud.AbilityButton.ToggleVisible(false);
            hud.ReportButton.ToggleVisible(false);
        }
        public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Suidce");
        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            target.RpcMurderPlayerV3(killer);
            return false;
        }

    }
}
