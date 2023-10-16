using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.Neutral
{
    public static class FFF
    {
        private static readonly int Id = 11300;
        public static List<byte> playerIdList = new();
        public static bool IsEnable = false;

        public static OptionItem CanVent;
        public static OptionItem ChooseConverted;
        public static OptionItem MisFireKillTarget;

        public static OptionItem CanKillLovers;
        public static OptionItem CanKillMadmate;
        public static OptionItem CanKillCharmed;
        public static OptionItem CanKillAdmired;
        public static OptionItem CanKillSidekicks;
        public static OptionItem CanKillEgoists;
        public static OptionItem CanKillInfected;
        public static OptionItem CanKillContagious;

        public static List<byte> winnerFFFList = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.FFF, zeroOne: false);
            MisFireKillTarget = BooleanOptionItem.Create(Id + 11, "FFFMisFireKillTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FFF]);
            ChooseConverted = BooleanOptionItem.Create(Id + 12, "FFFChooseConverted", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FFF]);
            CanKillMadmate = BooleanOptionItem.Create(Id + 13, "FFFCanKillMadmate", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
            CanKillCharmed = BooleanOptionItem.Create(Id + 14, "FFFCanKillCharmed", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
            CanKillLovers = BooleanOptionItem.Create(Id + 15, "FFFCanKillLovers", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
            CanKillSidekicks = BooleanOptionItem.Create(Id + 16, "FFFCanKillSidekick", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
            CanKillEgoists = BooleanOptionItem.Create(Id + 17, "FFFCanKillEgoist", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
            CanKillInfected = BooleanOptionItem.Create(Id + 18, "FFFCanKillInfected", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
            CanKillContagious = BooleanOptionItem.Create(Id + 19, "FFFCanKillContagious", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
            CanKillAdmired = BooleanOptionItem.Create(Id + 20, "FFFCanKillAdmired", true, TabGroup.NeutralRoles, false).SetParent(ChooseConverted);
        }

        public static void Init()
        {
            playerIdList = new();
            IsEnable = false;
            winnerFFFList = new();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            IsEnable = true;

            if (!AmongUsClient.Instance.AmHost) return;
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }

        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (killer == null || target == null) return false;
            if (killer.PlayerId == target.PlayerId) return true;

            if (target.GetCustomSubRoles().Any(x => x.IsConverted() || x == CustomRoles.Madmate || x == CustomRoles.Admired)
                || IsConvertedMainRole(target.GetCustomRole()))
            {
                if (!ChooseConverted.GetBool())
                {
                    //killer.RpcMurderPlayerV3(target);
                    if (!winnerFFFList.Contains(killer.PlayerId))
                    {
                        winnerFFFList.Add(killer.PlayerId);
                    }
                    Logger.Info($"{killer.GetRealName()} killed right target case 1", "FFF");
                    return true;
                }
                else if (
                    ((target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Gangster)) && CanKillMadmate.GetBool())
                    || ((target.Is(CustomRoles.Charmed) || target.Is(CustomRoles.Succubus)) && CanKillCharmed.GetBool())
                    || ((target.Is(CustomRoles.Lovers) || target.Is(CustomRoles.Ntr)) && CanKillLovers.GetBool())
                    || ((target.Is(CustomRoles.Romantic) || target.Is(CustomRoles.RuthlessRomantic) || target.Is(CustomRoles.VengefulRomantic)
                        || Romantic.BetPlayer.ContainsValue(target.PlayerId)) && CanKillLovers.GetBool())
                    || ((target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Recruit)) && CanKillSidekicks.GetBool())
                    || ((target.Is(CustomRoles.Egoist) && CanKillEgoists.GetBool())
                    || ((target.Is(CustomRoles.Infected) || target.Is(CustomRoles.Infectious)) && CanKillInfected.GetBool())
                    || ((target.Is(CustomRoles.Contagious) || target.Is(CustomRoles.Virus)) && CanKillContagious.GetBool())
                    || ((target.Is(CustomRoles.Admired) || target.Is(CustomRoles.Admirer)) && CanKillAdmired.GetBool()))
                    )
                {
                    //killer.RpcMurderPlayerV3(target);
                    if (!winnerFFFList.Contains(killer.PlayerId))
                    {
                        winnerFFFList.Add(killer.PlayerId);
                    }
                    Logger.Info($"{killer.GetRealName()} killed right target case 2", "FFF");
                    return true;
                }
            }
            //Not return tigger following fail check
            if (MisFireKillTarget.GetBool() && killer.RpcCheckAndMurder(target, true))
            {
                killer.RpcMurderPlayerV3(target);
                target.SetRealKiller(killer);
                target.Data.IsDead = true;
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
            }
            killer.Data.IsDead = true;
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Sacrifice;
            killer.RpcMurderPlayerV3(killer);
            Main.PlayerStates[killer.PlayerId].SetDead();
            Logger.Info($"{killer.GetRealName()} 击杀了非目标玩家，壮烈牺牲了（bushi）", "FFF");
            return false;
        }

        private static bool IsConvertedMainRole(CustomRoles role)
        {
            return role switch
            {
                CustomRoles.Gangster or
                CustomRoles.Succubus or
                CustomRoles.Romantic or
                CustomRoles.RuthlessRomantic or
                CustomRoles.VengefulRomantic or
                CustomRoles.Sidekick or
                CustomRoles.Jackal or
                CustomRoles.Virus or
                CustomRoles.Infectious or
                CustomRoles.Admirer
                => true,

                _ => false,
            };
        }
    }
}
