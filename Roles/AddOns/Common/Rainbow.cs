using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common
{
    public static class Rainbow
    {
        private static readonly int Id = 27400;
        public static OptionItem CrewCanBeRainbow;
        public static OptionItem ImpCanBeRainbow;
        public static OptionItem NeutralCanBeRainbow;
        public static OptionItem RainbowColorChangeCoolDown;

        public static bool isEnabled = false;
        public static long LastColorChange;
        public static void SetupCustomOptions()
        {
            SetupAdtRoleOptions(Id, CustomRoles.Rainbow, canSetNum: true, tab: TabGroup.Addons);
            CrewCanBeRainbow = BooleanOptionItem.Create(Id + 10, "CrewCanBeRainbow", true, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow]);
            ImpCanBeRainbow = BooleanOptionItem.Create(Id + 11, "ImpCanBeRainbow", true, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow]);
            NeutralCanBeRainbow = BooleanOptionItem.Create(Id + 12, "NeutralCanBeRainbow", true, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow]);
            RainbowColorChangeCoolDown = IntegerOptionItem.Create(Id + 13, "RainbowColorChangeCoolDown", new(1, 100, 1), 3, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow]);
        }
        public static void Init()
        {
            LastColorChange = Utils.GetTimeStamp();
            isEnabled = false;
        }
        public static void Add()
        {
            isEnabled = true;
        }
        public static void OnFixedUpdate()
        {

            if (LastColorChange + RainbowColorChangeCoolDown.GetInt() <= Utils.GetTimeStamp())
            {
                LastColorChange = Utils.GetTimeStamp();
                ChangeAllColor();
            }

        }
        private static void ChangeAllColor()
        {
            var sender = CustomRpcSender.Create("Rainbow Sender");
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Rainbow)))
            {
                int color = PickRandomColor();
                pc.SetColor(color);
                sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetColor)
                    .Write(color)
                    .EndRpc();
            }
            sender.SendMessage();
        }
        private static int PickRandomColor()
        {
            return IRandom.Instance.Next(0, 17);
        }
    }
}