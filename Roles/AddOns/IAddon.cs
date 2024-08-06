using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Thanks EHR for https://github.com/Gurge44/EndlessHostRoles/blob/main/Roles/AddOns/IAddon.cs and everything related ;)

namespace TOHE.Roles.AddOns
{
    public enum AddonTypes
    {
        Impostor,
        Helpful,
        Harmful,
        Misc,
        Guesser,
        Mixed,
        Experimental
    }
    internal interface IAddon
    {
        public AddonTypes Type { get; }
        public void SetupCustomOption();
    }
}
