using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE.Modules
{
    /*
     * Niko cant understand the conditions of virus and infectious
     * So currently these 2 roles are not supported by convert Manager
     * 
     * Used by Gangster, Admirer, Jackal, Succubus
     */
    public static class ConvertManager
    {
        public static Dictionary<byte, List<byte>> AlreadyConverted = new();
        public static CustomRoles GetConvertSubRole(PlayerControl killer, CustomRoles original)
        {
            if (killer.Is(CustomRoles.Madmate)) return CustomRoles.Madmate;
            if (killer.Is(CustomRoles.Recruit)) return CustomRoles.Recruit;
            if (killer.Is(CustomRoles.Charmed)) return CustomRoles.Charmed;
            if (killer.Is(CustomRoles.Infected)) return CustomRoles.Infected;
            if (killer.Is(CustomRoles.Admired)) return CustomRoles.Admired;
            if (killer.Is(CustomRoles.Contagious)) return CustomRoles.Contagious;
            //Overall check if killer is converted

            return original;
            //Above check fails? Return original sub role here
            //Following code is not used
            /*var mainRole = killer.GetCustomRole();
            if (mainRole.IsImpostorTeam()) return CustomRoles.Madmate;
            switch (mainRole)
            {
                case CustomRoles.Gangster: 
                    return CustomRoles.Madmate;
                case CustomRoles.Jackal:
                    return CustomRoles.Recruit;
                case CustomRoles.Succubus:
                    return CustomRoles.Charmed;
                case CustomRoles.Virus:
                    return CustomRoles.Contagious;
                case CustomRoles.Infectious:
                    return CustomRoles.Infected;
                case CustomRoles.Admirer:
                    return CustomRoles.Admired;
            }*/
        }

        public static bool CanBeConvertSubRole(PlayerControl target, CustomRoles subRole, PlayerControl killer = null)
        {
            if (target == null && target.Data.IsDead) return false;
            if (killer != null)
            {
                if (AlreadyConverted.ContainsKey(killer.PlayerId))
                {
                    if (AlreadyConverted[killer.PlayerId].Contains(target.PlayerId))
                        return false;
                }
                else AlreadyConverted.Add(killer.PlayerId, new());
            }

            if (target.Is(subRole)) return false;

            if (target.Is(CustomRoles.Loyal) || target.Is(CustomRoles.Soulless)) return false;
            if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18) return false;
            if (target.Is(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool()) return false;

            switch (subRole)
            {
                case CustomRoles.Admired:
                    break; //重写
                case CustomRoles.Madmate:
                    if (Utils.CanBeMadmate(target, inGame: true)) 
                        return true;
                    break;
                case CustomRoles.Recruit:
                    if (!target.Is(CustomRoles.Sidekick) && !target.Is(CustomRoles.Recruit))
                        return true;
                    break; //Jackal is superb
                case CustomRoles.Charmed:
                    if (Succubus.CanBeCharmed(target)) return true;
                    break;
                //Following is Not used by main virus and infectious
                case CustomRoles.Infected:
                    if (Infectious.CanBeBitten(target)) return true;
                    break;
                case CustomRoles.Contagious:
                    if (Virus.CanBeInfected(target)) return true;
                    break;
            }

            return false;
        }

        public static void SetPlayerConverted(PlayerControl killer, PlayerControl target, CustomRoles targetSubRole = CustomRoles.NotAssigned)
        {
            if (!AlreadyConverted.ContainsKey(killer.PlayerId))
            {
                AlreadyConverted.Add(killer.PlayerId, new());
                AlreadyConverted[killer.PlayerId].Add(target.PlayerId);
            }

            foreach (var key in AlreadyConverted.Keys.ToList())
            {
                List<byte> byteList = AlreadyConverted[key];

                if (byteList.Contains(target.PlayerId))
                {
                    byteList.Remove(target.PlayerId);

                    if (byteList.Count == 0 && key != killer.PlayerId)
                    {
                        AlreadyConverted.Remove(key);
                    }
                }
            }

            AlreadyConverted[killer.PlayerId].Add(target.PlayerId);

            if (AlreadyConverted.ContainsKey(target.PlayerId)) 
            {
                ResetPlayerConverted(target, targetSubRole);
            }

            if (AmongUsClient.Instance.AmHost)
                SendRPC(killer.PlayerId, target.PlayerId, targetSubRole);
        }
        public static void ResetPlayerConverted(PlayerControl target, CustomRoles targetSubRole)
        {
            if (!Options.UseSuperbConvertSystem.GetBool())
                AlreadyConverted.Remove(target.PlayerId);
            else if (AmongUsClient.Instance.AmHost)
            {
                if (AlreadyConverted[target.PlayerId].Count > 0) 
                {
                    foreach (var playerId in AlreadyConverted[target.PlayerId])
                    {
                        var pc = Utils.GetPlayerById(playerId);
                        if (pc != null)
                        {
                            if (CanBeConvertSubRole(pc, targetSubRole))
                            { 
                                pc.RpcSetCustomRole(targetSubRole);
                                if (!pc.Data.IsDead)
                                {
                                    pc.ResetKillCooldown();
                                    pc.SetKillCooldown(forceAnime: true);
                                    pc.Notify(GetString("SuperbConvertChangedSub"));
                                    if (AlreadyConverted.ContainsKey(playerId))
                                    {
                                        ResetPlayerConverted(pc, targetSubRole);
                                    } //Loop over all the recruiter
                                }
                            }                            
                        }
                    }
                }
                Utils.NotifyRoles();
            }
            return;
        } //获取dic中每个target的List，之前包含就地清除，然后向killer添加targets

        public static void SendRPC(byte killer, byte target, CustomRoles customRoles)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
            writer.Write(killer);
            writer.Write(target);
            writer.WritePacked((int)customRoles);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader reader)
        {
            var killer = Utils.GetPlayerById(reader.ReadByte());
            var target = Utils.GetPlayerById(reader.ReadByte());
            CustomRoles role = (CustomRoles)reader.ReadPackedInt32();

            if (killer != null && target != null)
                SetPlayerConverted(killer, target, role);
        }
        public static bool KnowRole(PlayerControl seer, PlayerControl target)
        {
            return false;
        }
    }
}
