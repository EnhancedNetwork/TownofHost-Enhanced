using System;
using System.Text;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

// 参考 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
// 贡献：https://github.com/Yumenopai/TownOfHost_Y/tree/AntiAdminer
internal class AntiAdminer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.AntiAdminer;
    private const int Id = 2800;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem CanCheckCamera;

    private static bool IsAdminWatch;
    private static bool IsVitalWatch;
    private static bool IsDoorLogWatch;
    private static bool IsCameraWatch;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.AntiAdminer);
        CanCheckCamera = BooleanOptionItem.Create(Id + 10, "CanCheckCamera", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.AntiAdminer]);
    }
    public override void Init()
    {
        IsAdminWatch = false;
        IsVitalWatch = false;
        IsDoorLogWatch = false;
        IsCameraWatch = false;
    }

    private static int Count = 0;
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;
        Count--; if (Count > 0) return; Count = 3;

        bool Admin = false, Camera = false, DoorLog = false, Vital = false;
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            if (pc.inVent || pc.GetCustomRole().IsImpostor()) continue;

            try
            {
                Vector2 PlayerPos = pc.transform.position;
                var mapId = GetActiveMapId();
                var mapName = (MapNames)mapId;

                switch (mapId)
                {
                    case 0:
                        if (!Options.DisableSkeldAdmin.GetBool())
                            Admin |= GetDistance(PlayerPos, DisableDevice.DevicePos["SkeldAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableSkeldCamera.GetBool())
                            Camera |= GetDistance(PlayerPos, DisableDevice.DevicePos["SkeldCamera"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 1:
                        if (!Options.DisableMiraHQAdmin.GetBool())
                            Admin |= GetDistance(PlayerPos, DisableDevice.DevicePos["MiraHQAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableMiraHQDoorLog.GetBool())
                            DoorLog |= GetDistance(PlayerPos, DisableDevice.DevicePos["MiraHQDoorLog"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 2:
                        if (!Options.DisablePolusAdmin.GetBool())
                        {
                            Admin |= GetDistance(PlayerPos, DisableDevice.DevicePos["PolusLeftAdmin"]) <= DisableDevice.UsableDistance(mapName);
                            Admin |= GetDistance(PlayerPos, DisableDevice.DevicePos["PolusRightAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        }
                        if (!Options.DisablePolusCamera.GetBool())
                            Camera |= GetDistance(PlayerPos, DisableDevice.DevicePos["PolusCamera"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisablePolusVital.GetBool())
                            Vital |= GetDistance(PlayerPos, DisableDevice.DevicePos["PolusVital"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 3:
                        if (!Options.DisableSkeldAdmin.GetBool())
                            Admin |= GetDistance(PlayerPos, DisableDevice.DevicePos["DleksAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableSkeldCamera.GetBool())
                            Camera |= GetDistance(PlayerPos, DisableDevice.DevicePos["DleksCamera"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 4:
                        if (!Options.DisableAirshipCockpitAdmin.GetBool())
                            Admin |= GetDistance(PlayerPos, DisableDevice.DevicePos["AirshipCockpitAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableAirshipRecordsAdmin.GetBool())
                            Admin |= GetDistance(PlayerPos, DisableDevice.DevicePos["AirshipRecordsAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableAirshipCamera.GetBool())
                            Camera |= GetDistance(PlayerPos, DisableDevice.DevicePos["AirshipCamera"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableAirshipVital.GetBool())
                            Vital |= GetDistance(PlayerPos, DisableDevice.DevicePos["AirshipVital"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 5:
                        if (!Options.DisableFungleBinoculars.GetBool())
                            Camera |= GetDistance(PlayerPos, DisableDevice.DevicePos["FungleCamera"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableFungleVital.GetBool())
                            Vital |= GetDistance(PlayerPos, DisableDevice.DevicePos["FungleVital"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), "AntiAdmin");
            }
        }

        var isChange = false;

        isChange |= IsAdminWatch != Admin;
        IsAdminWatch = Admin;
        isChange |= IsVitalWatch != Vital;
        IsVitalWatch = Vital;
        isChange |= IsDoorLogWatch != DoorLog;
        IsDoorLogWatch = DoorLog;
        if (CanCheckCamera.GetBool())
        {
            isChange |= IsCameraWatch != Camera;
            IsCameraWatch = Camera;
        }

        if (isChange)
        {
            if (_Player)
                NotifyRoles(SpecifySeer: _Player, ForceLoop: false);
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.PlayerId != seen.PlayerId || isForMeeting) return string.Empty;

        StringBuilder sb = new();
        if (IsAdminWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.AntiAdminer), "⚠")).Append(ColorString(GetRoleColor(CustomRoles.AntiAdminer), GetString("AdminWarning")));
        if (IsVitalWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.AntiAdminer), "⚠")).Append(ColorString(GetRoleColor(CustomRoles.AntiAdminer), GetString("VitalsWarning")));
        if (IsDoorLogWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.AntiAdminer), "⚠")).Append(ColorString(GetRoleColor(CustomRoles.AntiAdminer), GetString("DoorlogWarning")));
        if (IsCameraWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.AntiAdminer), "⚠")).Append(ColorString(GetRoleColor(CustomRoles.AntiAdminer), GetString("CameraWarning")));

        return sb.ToString();
    }
}
