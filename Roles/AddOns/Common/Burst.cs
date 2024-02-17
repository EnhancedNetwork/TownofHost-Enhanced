using AmongUs.GameOptions;
using System.Linq;
using System.Collections.Generic;
using static TOHE.Options;

using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common
{
    public static class Burst
    {
        private static readonly int Id = 19000;


        public static OptionItem ImpCanBeBurst;
        public static OptionItem CrewCanBeBurst;
        public static OptionItem NeutralCanBeBurst;
        public static OptionItem BurstKillDelay;

        public static List<byte> BurstBodies = [];
        public static void SetupCustomOptions()
        {
            SetupAdtRoleOptions(Id, CustomRoles.Burst, canSetNum: true);
            ImpCanBeBurst = BooleanOptionItem.Create(Id + 10, "ImpCanBeBurst", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Burst]);
            CrewCanBeBurst = BooleanOptionItem.Create(Id + 11, "CrewCanBeBurst", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Burst]);
            NeutralCanBeBurst = BooleanOptionItem.Create(Id + 12, "NeutralCanBeBurst", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Burst]);
            BurstKillDelay = FloatOptionItem.Create(Id + 13, "BurstKillDelay", new(1f, 180f, 1f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Burst])
                .SetValueFormat(OptionFormat.Seconds);
        }

        public static void Init()
        {
            BurstBodies = [];
        }

        public static void AfterBurstDeadTasks(PlayerControl killer, PlayerControl target)
        {
            target.SetRealKiller(killer);
            BurstBodies.Add(target.PlayerId);
            if (killer.PlayerId != target.PlayerId && !killer.Is(CustomRoles.Pestilence))
            {
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Burst), GetString("BurstNotify")));
                _ = new LateTask(() =>
                {
                    if (!killer.inVent && killer.IsAlive() && !GameStates.IsMeeting && GameStates.IsInGame)
                    {
                        Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                        target.RpcMurderPlayerV3(killer);
                        killer.SetRealKiller(target);
                    }
                    else if (GameStates.IsInGame)
                    {
                        RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                        killer.SetKillCooldown(time: Main.AllPlayerKillCooldown[killer.PlayerId] - BurstKillDelay.GetFloat(), forceAnime: true);
                        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Burst), GetString("BurstFailed")));
                    }
                    BurstBodies.Remove(target.PlayerId);
                }, BurstKillDelay.GetFloat(), "Burst Suicide");
            }
        }
    }
}