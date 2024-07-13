using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Doppelganger : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 25000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Doppelganger);
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\
    private static OptionItem KillCooldown;
    private static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem MaxSteals;
    public static OptionItem CurrentVictimCanSeeRolesAsDead;

    public static readonly Dictionary<byte, string> DoppelVictim = [];
    public static readonly Dictionary<PlayerControl, byte> PlayerControllerToIDRam = []; // Edit ids!
    public static readonly Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> DoppelPresentSkin = [];
    public static readonly Dictionary<byte, string> TrueNames = []; // Don't edit ids!
    public static PlayerControl DoppelgangerTarget = null;
    public static byte CurrentIdToSwap = byte.MaxValue;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Doppelganger, 1, zeroOne: false);
        MaxSteals = IntegerOptionItem.Create(Id + 10, "DoppelMaxSteals", new(1, 14, 1), 9, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger]);
        CurrentVictimCanSeeRolesAsDead = BooleanOptionItem.Create(Id + 11, "DoppelCurrentVictimCanSeeRolesAsDead", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger]);
        KillCooldown = FloatOptionItem.Create(Id + 12, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 13, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 14, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger]);
    }

    public override void Init()
    {
        DoppelVictim.Clear();
        PlayerControllerToIDRam.Clear();
        DoppelPresentSkin.Clear();
        TrueNames.Clear();
        DoppelgangerTarget = null;
        CurrentIdToSwap = byte.MaxValue;
    }

    public override void Add(byte playerId)
    {
        AbilityLimit = MaxSteals.GetInt();
        DoppelVictim[playerId] = Utils.GetPlayerById(playerId)?.GetRealName();

        DoppelgangerTarget = Utils.GetPlayerById(playerId);

        CurrentIdToSwap = playerId;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;

    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    // A quick check if a player has been killed by the doppelganger.
    public static bool CheckDoppelVictim(byte playerId) => DoppelVictim.ContainsKey(playerId);

    public static PlayerControl GetDoppelControl(PlayerControl player) => DoppelgangerTarget ?? player;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || Camouflage.IsCamouflage || Camouflager.AbilityActivated || Utils.IsActive(SystemTypes.MushroomMixupSabotage)) return true;
        if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out bool isShapeshifitng) && isShapeshifitng)
        {
            Logger.Info("Target was shapeshifting", "Doppelganger");
            return true; 
        } 
        if (AbilityLimit < 1)
        {
            return true;
        }

        AbilityLimit--;

        // Save names
        if (!TrueNames.ContainsKey(killer.PlayerId))
            TrueNames[killer.PlayerId] = killer.GetRealName();
        if (!TrueNames.ContainsKey(target.PlayerId))
            TrueNames[target.PlayerId] = target.GetRealName();

        var targetId = target.PlayerId;

        DoppelVictim[target.PlayerId] = target.GetRealName();

        // Make new outfit for taget
        var targetOutfit = new NetworkedPlayerInfo.PlayerOutfit()
            .Set(target.GetRealName(), target.CurrentOutfit.ColorId, target.CurrentOutfit.HatId, target.CurrentOutfit.SkinId, target.CurrentOutfit.VisorId, target.CurrentOutfit.PetId, target.CurrentOutfit.NamePlateId);
        var targetLvl = Utils.GetPlayerInfoById(target.PlayerId).PlayerLevel;

        // Make new outfit for killer
        var killerOutfit = new NetworkedPlayerInfo.PlayerOutfit()
            .Set(killer.GetRealName(), killer.CurrentOutfit.ColorId, killer.CurrentOutfit.HatId, killer.CurrentOutfit.SkinId, killer.CurrentOutfit.VisorId, killer.CurrentOutfit.PetId, killer.CurrentOutfit.NamePlateId);
        var killerLvl = Utils.GetPlayerInfoById(killer.PlayerId).PlayerLevel;

        // Set swap info
        if (!PlayerControllerToIDRam.ContainsKey(killer))
            PlayerControllerToIDRam[killer] = killer.PlayerId;
        if (!DoppelPresentSkin.ContainsKey(killer.PlayerId))
            DoppelPresentSkin[killer.PlayerId] = killerOutfit;

        if (!PlayerControllerToIDRam.ContainsKey(target))
            PlayerControllerToIDRam[target] = target.PlayerId;
        if (!DoppelPresentSkin.ContainsKey(target.PlayerId))
            DoppelPresentSkin[target.PlayerId] = targetOutfit;

        // Change player ID
        PlayerControllerToIDRam[target] = CurrentIdToSwap;
        PlayerControllerToIDRam[killer] = targetId;

        RpcChangeSkin(target, killerOutfit, killerLvl);
        Logger.Info("Changed target skin", "Doppelganger");
        RpcChangeSkin(killer, targetOutfit, targetLvl);
        Logger.Info("Changed target skin", "Doppelganger");

        CurrentIdToSwap = targetId;

        killer.Notify(Utils.ColorString(killer.GetRoleColor(), string.Format(GetString("Doppelganger_RoleInfo"), target.GetDisplayRoleAndSubName(target, true))));

        SendSkillRPC();
        Utils.NotifyRoles(ForceLoop: true, NoCache: true);
        RPC.SyncAllPlayerNames();
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return true;
    }

    // Function to swap players controllers for other functions
    public static PlayerControl SwapPlayerInfoFromRom(PlayerControl player) // Ram
    {
        if (!HasEnabled || player == null)
            return player; // No need for further processing if disabled or player is null

        if (PlayerControllerToIDRam.ContainsKey(player))
        {
            var RamId = PlayerControllerToIDRam[player];

            if (player.PlayerId != RamId)
            {
                var newPlayer = Utils.GetPlayerById(RamId);
                if (newPlayer != null)
                {
                    return newPlayer; // Swap player only if a valid player is found by ID
                }
            }
        }
        return player;
    }

    // Change cosmetic.
    private static void RpcChangeSkin(PlayerControl pc, NetworkedPlayerInfo.PlayerOutfit newOutfit, uint level)
    {
        var sender = CustomRpcSender.Create(name: $"Doppelganger.RpcChangeSkin({pc.Data.PlayerName})");
        pc.SetName(newOutfit.PlayerName);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetName)
            .Write(pc.Data.NetId)
            .Write(newOutfit.PlayerName)
        .EndRpc();

        Main.AllPlayerNames[pc.PlayerId] = newOutfit.PlayerName;

        pc.SetColor(newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetColor)
            .Write(pc.Data.NetId)
            .Write((byte)newOutfit.ColorId)
        .EndRpc();

        pc.SetHat(newOutfit.HatId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetHatStr)
            .Write(newOutfit.HatId)
            .Write(pc.GetNextRpcSequenceId(RpcCalls.SetHatStr))
        .EndRpc();

        pc.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(newOutfit.SkinId)
            .Write(pc.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
        .EndRpc();

        pc.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(newOutfit.VisorId)
            .Write(pc.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
        .EndRpc();

        pc.SetPet(newOutfit.PetId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetPetStr)
            .Write(newOutfit.PetId)
            .Write(pc.GetNextRpcSequenceId(RpcCalls.SetPetStr))
            .EndRpc();

        pc.SetNamePlate(newOutfit.NamePlateId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetNamePlateStr)
            .Write(newOutfit.NamePlateId)
            .Write(pc.GetNextRpcSequenceId(RpcCalls.SetNamePlateStr))
            .EndRpc();

        pc.SetLevel(level);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetLevel)
            .Write(level)
            .EndRpc();

        sender.SendMessage();
    }

    public override string GetProgressText(byte playerId, bool cooms) => Utils.ColorString(AbilityLimit > 0 ? Utils.GetRoleColor(CustomRoles.Doppelganger).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
}
