
namespace TOHE.Roles.Impostor;

internal class Convict : RoleBase // Loonie ass role 💀💀💀
{
    public static bool On;
    public override bool IsEnable => On;

    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override bool HasTasks(GameData.PlayerInfo player, CustomRoles role, bool ForRecompute) => !ForRecompute;

    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
}
