using System.Collections.Generic;

namespace TOHE.Roles.AddOns.Common
{
    public static class DoubleShot
    {
        public static List<byte> IsActive = new();
        public static void Init()
        {
            IsActive = new();
        }
    }
}