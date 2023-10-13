using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOHE.Roles.Neutral;
using TOHE;
using static TOHE.Options;
using MS.Internal.Xml.XPath;
using UnityEngine;
using static Il2CppSystem.Net.Http.Headers.Parser;
using static UnityEngine.GraphicsBuffer;
using Sentry.Internal;
using static Rewired.Demos.GamepadTemplateUI.GamepadTemplateUI;

namespace TOHE.Roles.Neutral;

public static class Yandere
{
    private static readonly int Id = 156465;
    public static List<byte> playerIdList = new();
    public static byte WinnerID;

    public static OptionItem KnowTargetRole;
    public static OptionItem TargetKnowsYandere;
    public static OptionItem SkillCooldown;
    private static Dictionary<byte, string> lastPlayerName = new();
    public static Dictionary<byte, string> msgToSend = new();
    public static List<byte> NeedKillYandere = new();
    public static List<byte> ForYandere = new();
    public static Dictionary<byte, byte> Target = new();
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Yandere);
        KnowTargetRole = BooleanOptionItem.Create(Id + 14, "KnowTargetRole", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere]);
        TargetKnowsYandere = BooleanOptionItem.Create(Id + 15, "TargetKnowsYandere", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere]);
        SkillCooldown = FloatOptionItem.Create(Id + 15, "KillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Yandere])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        Target = new();
        lastPlayerName = new();
        msgToSend = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (AmongUsClient.Instance.AmHost)
        {
            List<PlayerControl> targetList = new();
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                if (target.GetCustomRole() is CustomRoles.GM  ) continue;
                if (Utils.GetPlayerById(playerId).Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers)) continue;
                targetList.Add(target);
            }
            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            Target.Add(playerId, SelectedTarget.PlayerId);
            Main.ForYandere.Add(SelectedTarget.PlayerId);
            SendRPC(playerId, SelectedTarget.PlayerId, "SetTarget");
        }
    }
    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Remove(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] =  SkillCooldown.GetFloat();
    public static void SendRPC(byte lawyerId, byte targetId = 0x73, string Progress = "", bool add = true, Vector3 loc = new())
    {      
        MessageWriter writers = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetYandereArrow, SendOption.Reliable, -1);
        writers.Write(lawyerId);
        writers.Write(add);
        if (add)
        {
            writers.Write(loc.x);
            writers.Write(loc.y);
            writers.Write(loc.z);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writers);
        MessageWriter writer;
        switch (Progress)
        {
            case "SetTarget":
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetYandereTarget, SendOption.Reliable);
                writer.Write(lawyerId);
                writer.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                break;
            case "":
                if (!AmongUsClient.Instance.AmHost) return;
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveYandereTarget, SendOption.Reliable);
                writer.Write(lawyerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                break;
        }
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        // 方法实现
        byte playerId = reader.ReadByte();
        bool add = reader.ReadBoolean();
        if (add)
            LocateArrow.Add(playerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else
            LocateArrow.RemoveAllTarget(playerId);
        byte bountyId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        Target[bountyId] = targetId;
        TargetArrow.Add(bountyId, targetId);
    }
    public static void ChangeRoleByTarget(PlayerControl target)
    {
        target.RpcMurderPlayerV3(target);
        Utils.NotifyRoles();
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        return player.Is(CustomRoles.Yandere) && Target.TryGetValue(player.PlayerId, out var tar) && tar == target.PlayerId;
    }

    public static void OnFixedUpdate(PlayerControl yandere)
    {
        List<byte> deList = new();
        if (!IsEnable || !GameStates.IsInTask) return;
        foreach (var lv in Main.ForYandere)
        {
            var si = Utils.GetPlayerById(lv);
            //foreach (var pcs in Main.AllAlivePlayerControls)
            {
                var posi = si.transform.position;
                var diss = Vector2.Distance(posi, pcs.transform.position);
                if (diss > 0.3f) continue;
                if (Main.ForYandere.Contains(pcs.PlayerId)) continue;
                if (!Main.NeedKillYandere.Contains(pcs.PlayerId) && !pcs.Is(CustomRoles.Yandere))
                {
                    Main.NeedKillYandere.Add(pcs.PlayerId);
                    //foreach (var yanderes in Main.AllAlivePlayerControls)
                    {
                        if (yanderes.Is(CustomRoles.Yandere))
                        {
                            var playerId = yanderes.PlayerId;
                            NameColorManager.Add(yanderes.PlayerId,pcs.PlayerId, "#FF0000");
                            var targetId = pcs.PlayerId;
                            Target[playerId] = targetId;
                            TargetArrow.Add(playerId, targetId);
                        }
                    }                                      
                    break;
                }
            }
        }
    }
    public static void ChangeRole(PlayerControl lawyer)
    {
        lawyer.RpcMurderPlayerV3(lawyer);
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.Yandere))
        {
            if (!TargetKnowsYandere.GetBool()) return "";
            return (Target.TryGetValue(target.PlayerId, out var x) && seer.PlayerId == x) ?
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Totocalcio), "❥") : "";
        }
        var GetValue = Target.TryGetValue(seer.PlayerId, out var targetId);
        return GetValue && targetId == target.PlayerId ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Yandere), "❥") : "";
    }
    public static bool CheckExileTarget(GameData.PlayerInfo exiled, bool DecidedWinner, bool Check = false)
    {
        foreach (var kvp in Target.Where(x => x.Value == exiled.PlayerId))
        {
            var lawyer = Utils.GetPlayerById(kvp.Key);
            if (lawyer == null || !lawyer.IsAlive() || lawyer.Data.Disconnected) continue;
            return true;
        }
        return false;
    }
    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!seer.Is(CustomRoles.Yandere)) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        if (GameStates.IsMeeting) return "";
        //foreach (var pcs in Main.AllAlivePlayerControls)
        {
            if (Main.NeedKillYandere.Contains(pcs.PlayerId))
            {
                //foreach (var yanderes in Main.AllAlivePlayerControls)
                {
                    if (yanderes.Is(CustomRoles.Yandere))
                    {
                        var playerId = yanderes.PlayerId;
                        NameColorManager.Add(yanderes.PlayerId, pcs.PlayerId, "#FF0000");
                        var targetId = pcs.PlayerId;
                        Target[playerId] = targetId;
                        TargetArrow.Add(playerId, targetId);
                        return TargetArrow.GetArrows(seer, targetId);
                    }
                }
            }
        }
        return TargetArrow.GetArrows(seer);
    }
}