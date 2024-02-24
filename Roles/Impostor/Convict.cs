
namespace TOHE.Roles.Impostor;

internal class Convict : RoleBase
{
    public static bool On;
    public override bool IsEnable => On;

    public override void Init()
    {
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }
}
