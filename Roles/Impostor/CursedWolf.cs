using Hazel;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class CursedWolf : RoleBase
{
    private const int Id = 1100;
    public static bool On;
    public override bool IsEnable => On;

    private static OptionItem GuardSpellTimes;
    private static OptionItem KillAttacker;

    private static Dictionary<byte, int> SpellCount = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.CursedWolf);
        GuardSpellTimes = IntegerOptionItem.Create(Id + 2, "CursedWolfGuardSpellTimes", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.CursedWolf])
            .SetValueFormat(OptionFormat.Times);
        KillAttacker = BooleanOptionItem.Create(Id + 3, "CursedWolfKillAttacker", true, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.CursedWolf]);
    }

    public override void Init()
    {
        SpellCount = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        SpellCount[playerId] = GuardSpellTimes.GetInt();
        On = true;
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCursedWolfSpellCount, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.WritePacked(SpellCount[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte CursedWolfId = reader.ReadByte();
        int GuardNum = reader.ReadInt32();
        if (SpellCount.ContainsKey(CursedWolfId))
            SpellCount[CursedWolfId] = GuardNum;
        else
            SpellCount.Add(CursedWolfId, GuardSpellTimes.GetInt());
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer == target || SpellCount[target.PlayerId] <= 0) return true;
        if (killer.Is(CustomRoles.Pestilence)) return true;

        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);

        SpellCount[target.PlayerId] -= 1;
        SendRPC(target.PlayerId);

        if (KillAttacker.GetBool())
        {
            killer.SetRealKiller(target);
            Logger.Info($"{target.GetNameWithRole()} Spell Count: {SpellCount[target.PlayerId]}", "Cursed Wolf");
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Curse;
            killer.RpcMurderPlayerV3(killer);
        }
        return false;
    }

    public static string GetProgressText(byte playerId) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.CursedWolf), $"({SpellCount[playerId]})");
}
