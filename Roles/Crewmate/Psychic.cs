using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using static TOHE.Options;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class Psychic : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Psychic;
    private const int Id = 9400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Psychic);

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateSupport;
    //==================================================================\\

    private static OptionItem CanSeeNum;
    private static OptionItem Fresh;
    private static OptionItem CkshowEvil;
    private static OptionItem NBshowEvil;
    private static OptionItem NEshowEvil;
    private static OptionItem NCshowEvil;
    private static OptionItem NAshowEvil;
    private static OptionItem NKshowEvil;
    private static OptionItem CovshowEvil;


    private readonly HashSet<byte> RedPlayer = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Psychic);
        CanSeeNum = IntegerOptionItem.Create(Id + 2, "PsychicCanSeeNum", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic])
            .SetValueFormat(OptionFormat.Pieces);
        Fresh = BooleanOptionItem.Create(Id + 6, "PsychicFresh", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        CkshowEvil = BooleanOptionItem.Create(Id + 3, "Psychic_CrewKillingRed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NBshowEvil = BooleanOptionItem.Create(Id + 4, "Psychic_NBareRed", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NEshowEvil = BooleanOptionItem.Create(Id + 5, "Psychic_NEareRed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NCshowEvil = BooleanOptionItem.Create(Id + 7, "Psychic_NCareRed", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NAshowEvil = BooleanOptionItem.Create(Id + 8, "Psychic_NAareRed", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        NKshowEvil = BooleanOptionItem.Create(Id + 9, "Psychic_NKareRed", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
        CovshowEvil = BooleanOptionItem.Create(Id + 10, "Psychic_CovareRed", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
    }
    public override void Init()
    {
        RedPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        if (!Fresh.GetBool())
        {
            _ = new LateTask(GetRedName, 10f, $"Get Red Name For {_state.PlayerId}");
        }
    }
    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(RedPlayer.Count);
        foreach (var pc in RedPlayer)
            writer.Write(pc);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int count = reader.ReadInt32();
        RedPlayer.Clear();

        for (int i = 0; i < count; i++)
            RedPlayer.Add(reader.ReadByte());
    }
    public bool IsRedForPsy(PlayerControl target, PlayerControl seer)
    {
        if (target == null || seer == null) return false;
        var targetRole = target.GetCustomRole();
        if (seer.Is(CustomRoles.Madmate)) return targetRole.IsNeutral() || targetRole.IsCrewKiller();
        else return RedPlayer != null && RedPlayer.Contains(target.PlayerId);
    }
    public override void OnReportDeadBody(PlayerControl reported, NetworkedPlayerInfo target)
    {
        if (Fresh.GetBool() || RedPlayer == null || RedPlayer.Count < 1)
        {
            if (Fresh.GetBool())
            {
                RedPlayer.Clear(); // Force generate new red names so its freshed?
            }

            GetRedName();
        }
    }
    private void GetRedName()
    {
        if (_Player == null || !_Player.IsAlive() || !AmongUsClient.Instance.AmHost) return;

        List<PlayerControl> BadListPc = [.. Main.AllAlivePlayerControls.Where(x => Illusionist.IsNonCovIllusioned(x.PlayerId) ||
        (x.Is(Custom_Team.Impostor) && !x.Is(CustomRoles.Trickster) && !x.Is(CustomRoles.Admired) && !x.Is(CustomRoles.Narc)) ||
        x.IsAnySubRole(x => x.IsConverted()) ||
        (x.GetCustomRole().IsCrewKiller() && CkshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNE() && NEshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNC() && NCshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNB() && NBshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNK() && NKshowEvil.GetBool()) ||
        (x.GetCustomRole().IsNA() && NAshowEvil.GetBool()) ||
        (x.GetCustomRole().IsCoven() && CovshowEvil.GetBool())
        )];

        var randomBadPlayer = BadListPc.RandomElement();
        List<byte> AllList = [];
        Main.AllAlivePlayerControls.Where(x => randomBadPlayer.PlayerId != x.PlayerId && x.PlayerId != _Player.PlayerId).Do(x => AllList.Add(x.PlayerId));

        int CountRed = CanSeeNum.GetInt() - 1;
        RedPlayer.Add(randomBadPlayer.PlayerId);

        for (int i = 0; i < CountRed; i++)
        {
            if (!AllList.Any()) break;

            var randomPlayer = AllList.RandomElement();
            RedPlayer.Add(randomPlayer);
            AllList.Remove(randomPlayer);
        }

        SendRPC();
    }

    public override string NotifyPlayerName(PlayerControl seer, PlayerControl target, string TargetPlayerName = "", bool IsForMeeting = false)
        => IsForMeeting && IsRedForPsy(target, seer) && seer.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Impostor), TargetPlayerName) : string.Empty;

    public override string PVANameText(PlayerVoteArea pva, PlayerControl seer, PlayerControl target)
        => IsRedForPsy(target, seer) && seer.IsAlive() ? ColorString(GetRoleColor(CustomRoles.Impostor), pva.NameText.text) : string.Empty;
}
