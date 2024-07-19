using static TOHE.Options;


namespace TOHE.Roles.AddOns.Common;

public static class Avanger
{
    private const int Id = 21500;
    
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
        var pcList = Main.AllAlivePlayerControls.Where(pc => pc.PlayerId != target.PlayerId && target.RpcCheckAndMurder(pc, true)).ToList();
        
        if (pcList.Any())
        {
            PlayerControl rp = pcList.RandomElement();
            rp.SetDeathReason(PlayerState.DeathReason.Revenge);
            rp.RpcMurderPlayer(rp);
            rp.SetRealKiller(target);
        }
    }
}

