//using Hazel;
//using UnityEngine;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using static TOHE.Options;
//using static TOHE.Translator;
//using TOHE.Roles.Crewmate;
//using AmongUs.GameOptions;
//namespace TOHE.Roles.Neutral;

//public static class Occultist
//{
//    public enum SwitchTrigger
//    {
//        Kill,
//        Vent,
//        DoubleTrigger,
//    };
//    public static readonly string[] SwitchTriggerText =
//    {
//        "TriggerKill", "TriggerVent","TriggerDouble"
//    };

//    private static readonly int Id = 17200;
//    private static Color RoleColorCurse = Utils.GetRoleColor(CustomRoles.Occultist);
//    private static Color RoleColorSpell = Utils.GetRoleColor(CustomRoles.Impostor);

//    public static List<byte> playerIdList = new();
//    public static bool IsEnable = false;

//    public static Dictionary<byte, bool> HexMode = new();
//    public static Dictionary<byte, List<byte>> CursedPlayer = new();

//    public static OptionItem ModeSwitchAction;
//    public static OptionItem HasImpostorVision;
//    public static OptionItem CursesLookLikeSpells;
//    public static SwitchTrigger NowSwitchTrigger;
//    public static void SetupCustomOption()
//    {
//        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Occultist, 1, zeroOne: false);        
//        ModeSwitchAction = StringOptionItem.Create(Id + 10, "WitchModeSwitchAction", SwitchTriggerText, 2, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Occultist]);
//        CursesLookLikeSpells = BooleanOptionItem.Create(Id + 11, "CursesLookLikeSpells",  false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Occultist]);
//        HasImpostorVision = BooleanOptionItem.Create(Id + 12, "ImpostorVision",  true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Occultist]);
//    }
//    public static void Init()
//    {
//        playerIdList = new();
//        HexMode = new();
//        CursedPlayer = new();
//        IsEnable = false;
//    }
//    public static void Add(byte playerId)
//    {
//        playerIdList.Add(playerId);
//        IsEnable = true;
//        HexMode.Add(playerId, false);
//        CursedPlayer.Add(playerId, new());
//        NowSwitchTrigger = (SwitchTrigger)ModeSwitchAction.GetValue();
//        var pc = Utils.GetPlayerById(playerId);
//        pc.AddDoubleTrigger();

//        if (!AmongUsClient.Instance.AmHost) return;
//        if (!Main.ResetCamPlayerList.Contains(playerId))
//            Main.ResetCamPlayerList.Add(playerId);
//    }
//    private static void SendRPC(bool doCurse, byte hexId, byte target = 255)
//    {
//        if (doCurse)
//        {
//            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoCurse, SendOption.Reliable, -1);
//            writer.Write(hexId);
//            writer.Write(target);
//            AmongUsClient.Instance.FinishRpcImmediately(writer);
//        }
//        else
//        {
//            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillOrCurse, SendOption.Reliable, -1);
//            writer.Write(hexId);
//            writer.Write(HexMode[hexId]);
//            AmongUsClient.Instance.FinishRpcImmediately(writer);

//        }
//    }

//    public static void ReceiveRPC(MessageReader reader, bool doCurse)
//    {
//        if (doCurse)
//        {
//            var occultist = reader.ReadByte();
//            var CursedId = reader.ReadByte();
//            if (CursedId != 255)
//            {
//                CursedPlayer[occultist].Add(CursedId);
//            }
//            else
//            {
//                CursedPlayer[occultist].Clear();
//            }
//        }
//        else
//        {
//            byte playerId = reader.ReadByte();
//            HexMode[playerId] = reader.ReadBoolean();
//        }
//    }
//    public static bool IsHexMode(byte playerId)
//    {
//        return HexMode.ContainsKey(playerId) && HexMode[playerId];
//    }
//    public static void SwitchHexMode(byte playerId, bool kill)
//    {
//        bool needSwitch = false;
//        switch (NowSwitchTrigger)
//        {
//            case SwitchTrigger.Kill:
//                needSwitch = kill;
//                break;
//            case SwitchTrigger.Vent:
//                needSwitch = !kill;
//                break;
//        }
//        if (needSwitch)
//        {
//            HexMode[playerId] = !HexMode[playerId];
//            SendRPC(false, playerId);
//            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerId));
//        }
//    }
//    public static bool HaveCursedPlayer()
//    {
//        foreach (var occultist in playerIdList)
//        {
//            if (CursedPlayer[occultist].Count != 0) return true;
//        }
//        return false;

//    }
//    public static bool IsCursed(byte target)
//    {
//        foreach (var occultist in playerIdList)
//        {
//            if (CursedPlayer[occultist].Contains(target))
//            {
//                return true;
//            }
//        }
//        return false;
//    }
//    public static void SetCursed(PlayerControl killer, PlayerControl target)
//    {
//        if (!IsCursed(target.PlayerId))
//        {
//            CursedPlayer[killer.PlayerId].Add(target.PlayerId);
//            SendRPC(true, killer.PlayerId, target.PlayerId);
//            //キルクールの適正化
//            killer.SetKillCooldown();
//        }
//    }
//    public static void RemoveCursedPlayer()
//    {
//        foreach (var occultist in playerIdList)
//        {
//            CursedPlayer[occultist].Clear();
//            SendRPC(true, occultist);
//        }
//    }
//    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());

//    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
//    {
//        if (Medic.ProtectList.Contains(target.PlayerId)) return false;
//        if (target.Is(CustomRoles.Pestilence)) return true;
//        if (target.Is(CustomRoles.Occultist)) return true;

//        if (NowSwitchTrigger == SwitchTrigger.DoubleTrigger)
//        {
//            return killer.CheckDoubleTrigger(target, () => { SetCursed(killer, target); });
//        }
//        if (!IsHexMode(killer.PlayerId))
//        {
//            SwitchHexMode(killer.PlayerId, true);
//            //キルモードなら通常処理に戻る
//            return true;
//        }
//        SetCursed(killer, target);

//        //スペルに失敗してもスイッチ判定
//        SwitchHexMode(killer.PlayerId, true);
//        //キル処理終了させる
//        return false;
//    }
//    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
//    {
//        if (!IsEnable || deathReason != PlayerState.DeathReason.Vote) return;
//        foreach (var id in exileIds)
//        {
//            if (CursedPlayer.ContainsKey(id))
//                CursedPlayer[id].Clear();
//        }
//        var CursedIdList = new List<byte>();
//        foreach (var pc in Main.AllAlivePlayerControls)
//        {
//            var dic = CursedPlayer.Where(x => x.Value.Contains(pc.PlayerId));
//            if (!dic.Any()) continue;
//            var whichId = dic.FirstOrDefault().Key;
//            var occultist = Utils.GetPlayerById(whichId);
//            if (occultist != null && occultist.IsAlive())
//            {
//                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
//                {
//                    pc.SetRealKiller(occultist);
//                    CursedIdList.Add(pc.PlayerId);
//                }
//            }
//            else
//            {
//                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
//            }
//        }
//        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Curse, CursedIdList.ToArray());
//        RemoveCursedPlayer();
//    }
//    public static string GetCursedMark(byte target, bool isMeeting)
//    {

//        if (isMeeting && IsCursed(target))
//        {
//            if (!CursesLookLikeSpells.GetBool())
//            {
//                return Utils.ColorString(RoleColorCurse, "❖");
//            }
//            if (CursesLookLikeSpells.GetBool())
//            {
//                return Utils.ColorString(RoleColorSpell, "†");
//            }
//        }
//        return "";

//    }
//    public static string GetHexModeText(PlayerControl occultist, bool hud, bool isMeeting = false)
//    {
//        if (occultist == null || isMeeting) return "";

//        var str = new StringBuilder();
//        if (hud)
//        {
//            str.Append(GetString("WitchCurrentMode"));
//        }
//        else
//        {
//            str.Append($"{GetString("Mode")}:");
//        }
//        if (NowSwitchTrigger == SwitchTrigger.DoubleTrigger)
//        {
//            str.Append(GetString("OccultistModeDouble"));
//        }
//        else
//        {
//            str.Append(IsHexMode(occultist.PlayerId) ? GetString("OccultistModeCurse") : GetString("OccultistModeKill"));
//        }
//        return str.ToString();
//    }
//    public static void GetAbilityButtonText(HudManager hud)
//    {
//        if (IsHexMode(PlayerControl.LocalPlayer.PlayerId) && NowSwitchTrigger != SwitchTrigger.DoubleTrigger)
//        {
//            hud.KillButton.OverrideText($"{GetString("OccultistButtonText")}");
//        }
//        else
//        {
//            hud.KillButton.OverrideText($"{GetString("KillButtonText")}");
//        }
//    }

//    public static void OnEnterVent(PlayerControl pc)
//    {
//        if (!AmongUsClient.Instance.AmHost) return;
//        if (!IsEnable) return;
//        if (playerIdList.Contains(pc.PlayerId))
//        {
//            if (NowSwitchTrigger is SwitchTrigger.Vent)
//            {
//                SwitchHexMode(pc.PlayerId, false);
//            }
//        }
//    }
//}