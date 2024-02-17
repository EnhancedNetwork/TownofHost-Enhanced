using System.Linq;
using static TOHE.Options;
using static UnityEngine.GraphicsBuffer;
using TOHE.Roles.Neutral;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;


namespace TOHE.Roles.AddOns.Common;

public static class Avanger
{
    private static readonly int Id = 21500;
    
    public static OptionItem ImpCanBeAvanger;
    public static OptionItem CrewCanBeAvanger;
    public static OptionItem NeutralCanBeAvanger;

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Avanger, canSetNum: true);
        ImpCanBeAvanger = BooleanOptionItem.Create(Id + 10, "ImpCanBeAvanger", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
        CrewCanBeAvanger = BooleanOptionItem.Create(Id +  12, "CrewCanBeAvanger", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
        NeutralCanBeAvanger = BooleanOptionItem.Create(Id + 13, "NeutralCanBeAvanger", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Avanger]);
    }

    public static void OnMurderPlayer(PlayerControl target)
    {
        var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId && !Pelican.IsEaten(x.PlayerId) && !Medic.ProtectList.Contains(x.PlayerId)
            && !x.Is(CustomRoles.Pestilence) && !x.Is(CustomRoles.Masochist) && !x.Is(CustomRoles.Solsticer) && !((x.Is(CustomRoles.NiceMini) || x.Is(CustomRoles.EvilMini)) && Mini.Age < 18)).ToList();
        if (pcList.Count > 0)
        {
            PlayerControl rp = pcList[IRandom.Instance.Next(0, pcList.Count)];
            Main.PlayerStates[rp.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
            rp.SetRealKiller(target);
            rp.RpcMurderPlayerV3(rp);
        }
    }
}

