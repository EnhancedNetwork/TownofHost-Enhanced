using Hazel;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Imitator : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13000;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\

    private static OptionItem RememberCooldown;
    private static OptionItem IncompatibleNeutralMode;

    private static readonly Dictionary<byte, int> RememberLimit = [];

    private enum ImitatorIncompatibleNeutralModeSelect
    {
        Role_Imitator,
        Role_Pursuer,
        Role_Follower,
        Role_Maverick,
        Role_Amnesiac
    }

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Imitator);
        RememberCooldown = FloatOptionItem.Create(Id + 10, "RememberCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Imitator])
                .SetValueFormat(OptionFormat.Seconds);
        IncompatibleNeutralMode = StringOptionItem.Create(Id + 12, "IncompatibleNeutralMode", EnumHelper.GetAllNames<ImitatorIncompatibleNeutralModeSelect>(), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Imitator]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        RememberLimit.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        RememberLimit.Add(playerId, 1);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Imitator);
        writer.Write(playerId);
        writer.Write(RememberLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();
        int Limit = reader.ReadInt32();

        if (!RememberLimit.ContainsKey(playerId))
        {
            RememberLimit.Add(playerId, Limit);
        }
        else
        {
            RememberLimit[playerId] = Limit;
        }
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = RememberLimit[id] >= 1 ? RememberCooldown.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && (!RememberLimit.TryGetValue(player.PlayerId, out var x) || x > 0);
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (RememberLimit[killer.PlayerId] < 1) return true;

        var role = target.GetCustomRole();

        if (role is CustomRoles.Jackal
            or CustomRoles.HexMaster
            or CustomRoles.Poisoner
            or CustomRoles.Juggernaut 
            or CustomRoles.BloodKnight
            or CustomRoles.Sheriff)
        {
            RememberLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            killer.RpcSetCustomRole(role);
            killer.GetRoleClass().Add(killer.PlayerId);

            if (role.IsCrewmate())
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedCrewmate")));
            else
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedNeutralKiller")));

            // Notify target
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        }
        else if (role.IsAmneMaverick())
        {
            RememberLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            switch (IncompatibleNeutralMode.GetInt())
            {
                case 0:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedImitator")));
                    break;
                case 1:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedPursuer")));
                    killer.RpcSetCustomRole(CustomRoles.Pursuer);
                    killer.GetRoleClass().Add(killer.PlayerId);
                    break;
                case 2:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedFollower")));
                    killer.RpcSetCustomRole(CustomRoles.Follower);
                    killer.GetRoleClass().Add(killer.PlayerId);
                    break;
                case 3:
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedMaverick")));
                    killer.RpcSetCustomRole(CustomRoles.Maverick);
                    killer.GetRoleClass().Add(killer.PlayerId);
                    break;
                case 4: //....................................................................................x100
                    killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedAmnesiac")));
                    killer.RpcSetCustomRole(CustomRoles.Amnesiac);
                    killer.GetRoleClass().Add(killer.PlayerId);
                    break;
            }

        }
        else if (role.IsCrewmate())
        {
            RememberLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            killer.RpcSetCustomRole(CustomRoles.Sheriff);
            killer.GetRoleClass().Add(killer.PlayerId);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedCrewmate")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        }
        else if (role.IsImpostor())
        {
            RememberLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            killer.RpcSetCustomRole(CustomRoles.Refugee);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("RememberedImpostor")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorImitated")));
        }

        var killerRole = killer.GetCustomRole();

        if (killerRole != CustomRoles.Imitator)
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldown(forceAnime: true);

            Logger.Info("Imitator remembered: " + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString(), "Imitator Assign");
            Logger.Info($"{killer.GetNameWithRole()} : {RememberLimit} remember limits left", "Imitator");

            Utils.NotifyRoles(SpecifySeer: killer);
        }
        else if (killerRole == CustomRoles.Imitator)
        {
            killer.SetKillCooldown(forceAnime: true);
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Imitator), GetString("ImitatorInvalidTarget")));
        }

        return true;
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("ImitatorKillButtonText"));
    }

}
