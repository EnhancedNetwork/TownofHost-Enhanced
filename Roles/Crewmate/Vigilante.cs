using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TOHE.Translator;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate
{
    internal class Vigilante : RoleBase
    {
        private const int id = 11400;
        private static bool On = false;
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.Vigilante.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

        public static OptionItem VigilanteKillCooldown;
        public static void SetupCustomOptions()
        {
            SetupRoleOptions(11400, TabGroup.CrewmateRoles, CustomRoles.Vigilante);
            VigilanteKillCooldown = FloatOptionItem.Create(11402, "KillCooldown", new(5f, 180f, 2.5f), 30f, TabGroup.CrewmateRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Vigilante])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public override void Init()
        {
            On = false;
        }
        public override void Add(byte playerId)
        {
            On = true;
        }
        public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = VigilanteKillCooldown.GetFloat();
        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            if (killer.Is(CustomRoles.Madmate)) return true;
            if (target.GetCustomRole().IsCrewmate() && !target.Is(CustomRoles.Madmate) && !target.GetCustomRole().IsConverted())
            {
                killer.RpcSetCustomRole(CustomRoles.Madmate);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("VigilanteNotify")));
                //Utils.NotifyRoles(SpecifySeer: killer);
                Utils.MarkEveryoneDirtySettings();
            }
            return true;
        }
    }
}
