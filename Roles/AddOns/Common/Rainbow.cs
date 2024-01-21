namespace TOHE.Roles.AddOns.Common
{
    public static class Rainbow
    {
        private static readonly int Id = 27400;
        public static bool IsEnable = false;
        public static OptionItem CrewCanBeRainbow;
        public static OptionItem ImpCanBeRainbow;
        public static OptionItem NeutralCanBeRainbow;
        //public static OptionItem RainbowColorChangeTime;
        public static void SetupCustomOptions()
        {
            //RainbowColorChangeTime = FloatOptionItem.Create(27303, "RainbowColorChangeTime", new(0.1f, 5f, 2.5f), 0.5f, TabGroup.Addons, false)
            //    .SetParent(CustomRoleSpawnChances[CustomRoles.Rainbow])
            //    .SetValueFormat(OptionFormat.Seconds);
            Options.SetupAdtRoleOptions(Id, CustomRoles.Rainbow, canSetNum: true, tab: TabGroup.Addons);
            CrewCanBeRainbow = BooleanOptionItem.Create(Id + 10, "CrewCanBeRainbow", true, TabGroup.Addons, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Rainbow]);
            ImpCanBeRainbow = BooleanOptionItem.Create(Id + 11, "ImpCanBeRainbow", true, TabGroup.Addons, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Rainbow]);
            NeutralCanBeRainbow = BooleanOptionItem.Create(Id + 12, "NeutralCanBeRainbow", true, TabGroup.Addons, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Rainbow]);
        }
        public static void OnFixedUpdate(PlayerControl player)
        {
            if (!IsEnable) return;
            //来源：TOHY https://github.com/Yumenopai/TownOfHost_Y
            if (GameStates.IsInTask && player.Is(CustomRoles.Rainbow))
            {
                var rain = IRandom.Instance;
                int rndNum = rain.Next(0, 18);
                if (rndNum is >= 1 and < 2) player.RpcSetColor(1);
                else if (rndNum is >= 2 and < 3) player.RpcSetColor(10);
                else if (rndNum is >= 3 and < 4) player.RpcSetColor(2);
                else if (rndNum is >= 4 and < 5) player.RpcSetColor(11);
                else if (rndNum is >= 5 and < 6) player.RpcSetColor(14);
                else if (rndNum is >= 6 and < 7) player.RpcSetColor(5);
                else if (rndNum is >= 7 and < 8) player.RpcSetColor(4);
                else if (rndNum is >= 8 and < 9) player.RpcSetColor(17);
                else if (rndNum is >= 9 and < 10) player.RpcSetColor(0);
                else if (rndNum is >= 10 and < 11) player.RpcSetColor(3);
                else if (rndNum is >= 11 and < 12) player.RpcSetColor(13);
                else if (rndNum is >= 12 and < 13) player.RpcSetColor(7);
                else if (rndNum is >= 13 and < 14) player.RpcSetColor(15);
                else if (rndNum is >= 14 and < 15) player.RpcSetColor(6);
                else if (rndNum is >= 15 and < 16) player.RpcSetColor(12);
                else if (rndNum is >= 16 and < 17) player.RpcSetColor(9);
                else if (rndNum is >= 17 and < 18) player.RpcSetColor(16);
            }
        }
    }
}