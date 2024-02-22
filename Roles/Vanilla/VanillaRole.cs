using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOHE;

internal class VanillaRole : RoleBase
{
    public static bool On;
    public override bool IsEnable => On;

    public override void Add(byte playerId)
    {
        On = true;
    }

    public override void Init()
    {
        On = false;
    }
}
