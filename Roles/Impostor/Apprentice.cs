using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Double;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Apprentice : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Apprentice;
    private const int Id = 31400;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Madmate;
    //==================================================================\\

    private static OptionItem RevealCooldown;

    private static readonly Dictionary<byte, int> RevealCount = [];
    private static readonly Dictionary<byte, HashSet<byte>> RevealTarget = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Apprentice);
        RevealCooldown = FloatOptionItem.Create(Id + 10, "OverseerRevealCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Apprentice])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        RevealCount.Clear();
        RevealTarget.Clear();

    }
    public override void Add(byte playerId)
    {
        RevealCount.TryAdd(playerId, 2);
        RevealTarget.TryAdd(playerId, []);
    }

    private static void SendRPC(byte playerId, byte targetId)
    {
        MessageWriter writer = 
        AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetConsigliere, SendOption.Reliable, -1);
        //Consigliere's RPC for Apprentice XD
        writer.Write(playerId);
        writer.Write(RevealCount[playerId]);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        {
            if (RevealCount.ContainsKey(playerId))
                RevealCount[playerId] = reader.ReadInt32();
            else
                RevealCount.Add(playerId, 2);
        }
        {
            if (RevealCount.ContainsKey(playerId))
                RevealTarget[playerId].Add(reader.ReadByte());
            else
                RevealTarget.Add(playerId, []);
        }
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = 
    ((RevealCount[id] > 0) && Main.AliveImpostorCount > 1) ? RevealCooldown.GetFloat() : Options.DefaultKillCooldown;
    public override bool CanUseKillButton(PlayerControl pc) 
        => RevealCount[pc.PlayerId] > 0 || Main.AliveImpostorCount < 2;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => Main.AliveImpostorCount < 2;
    public override bool CanUseSabotage(PlayerControl pc) => Main.AliveImpostorCount < 2;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckImpTeamCanSeeTeammates() && target.CheckImpTeamCanSeeTeammates()) return false;
        else if (Main.AliveImpostorCount > 1)
        {
            if (RevealCount[killer.PlayerId] > 0) SetRevealed(killer, target);
                return false;
        }
        else 
        {
            if (RevealCount[killer.PlayerId] > 0)
            {
                if (target.Is(CustomRoles.Sheriff) && killer.Is(CustomRoles.Narc)) return true;
                if (target.CheckImpTeamCanSeeTeammates()) return true;
                if (target.Is(CustomRoles.Madmate) && !(killer.Is(CustomRoles.Narc) ? Narc.NarcCanKillMadmate.GetBool() : Madmate.ImpCanKillMadmate.GetBool()))
                    return true;
                if (target.GetCustomRole() is CustomRoles.NiceMini or CustomRoles.EvilMini && Mini.Age < 18) return true;
                if (target.Is(CustomRoles.Solsticer)) return true;
                
                RevealCount[killer.PlayerId]--;
                killer.RpcMurderPlayer(target);
                killer.ResetKillCooldown();
                return false;
            }
            else return true;            
        }
    }

    private static bool IsRevealed(byte seer, byte target)
    {
        if (RevealTarget[seer].Contains(target))
        {
            return true;
        }
        return false;
    }
    private static void SetRevealed(PlayerControl killer, PlayerControl target)
    {
        if (!IsRevealed(killer.PlayerId, target.PlayerId))
        {
            RevealCount[killer.PlayerId]--;
            RevealTarget[killer.PlayerId].Add(target.PlayerId);
            Logger.Info($"{killer.GetNameWithRole()}：Checked→{target.GetNameWithRole()} || Remaining Ability: {RevealCount[killer.PlayerId]}", "Apprentice");
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);

            SendRPC(killer.PlayerId, target.PlayerId);
            killer.SetKillCooldown(target: target, forceAnime: true);
        }
    }
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        var IsWatch = false;
        RevealTarget.Do(x =>
        {
            if (x.Value != null && seer.PlayerId == x.Key && x.Value.Contains(target.PlayerId) && Utils.GetPlayerById(x.Key).IsAlive())
                IsWatch = true;
        });
        return IsWatch;
    }
    public override string GetProgressText(byte playerId, bool comms)
        => Utils.ColorString(RevealCount[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.Apprentice).ShadeColor(0.25f) : Color.gray, RevealCount.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : string.Empty);
}
