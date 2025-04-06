using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Main;
using TOHE.Modules;
using MS.Internal.Xml.XPath;
using TOHE.Roles.Double;
using AmongUs.GameOptions;

namespace TOHE.Roles.Impostor;
internal class Blinder : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Blinder;
    private const int Id = 35000;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorHindering;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem BlindTime;
    private static OptionItem BlindRadius;
    public static List<PlayerControl> BlindedPlayers = [];
    private static float DefaultSpeed = new();
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Blinder);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 60f, 1f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Blinder])
            .SetValueFormat(OptionFormat.Seconds);
        BlindTime = FloatOptionItem.Create(Id + 11, "BlindTime350", new(0f, 20f, 1f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Blinder])
            .SetValueFormat(OptionFormat.Seconds);
        BlindRadius = FloatOptionItem.Create(Id + 12, "BlindRadius350", new(0.5f, 1.5f, 0.1f), 1.3f, TabGroup.ImpostorRoles, false)
           .SetParent(CustomRoleSpawnChances[CustomRoles.Blinder])
           .SetValueFormat(OptionFormat.Multiplier);

    }
    public override void Add(byte playerId)
    {
        DefaultSpeed = Main.AllPlayerSpeed[playerId];
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {

        foreach (var player in Main.AllAlivePlayerControls)
        {
            if (player == killer) continue;

            if (player.IsTransformedNeutralApocalypse()) continue;
            else if ((player.Is(CustomRoles.NiceMini) || player.Is(CustomRoles.EvilMini)) && Mini.Age < 18) continue;

            if (Utils.GetDistance(killer.transform.position, player.transform.position) <= BlindRadius.GetFloat())
            {
                BlindedPlayers.Add(player);
                Main.PlayerStates[player.PlayerId].IsBlackOut = true;
                Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
                player.MarkDirtySettings();
            }
        }

        _ = new LateTask(() =>
        {
            if (BlindedPlayers == null) return;
            foreach (var player in BlindedPlayers)
            {
                BlindedPlayers.Remove(player);
                Main.AllPlayerSpeed[player.PlayerId] = DefaultSpeed;
                Main.PlayerStates[player.PlayerId].IsBlackOut = false;
                player.MarkDirtySettings();
            }
        }, BlindTime.GetFloat(), "Blind Finish");
        return true;
    }
    public override void SetKillCooldown(byte id)
    {
        AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    }
}

