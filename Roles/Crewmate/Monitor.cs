using System;
using System.Collections.Generic;
using System.Text;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Utils;
using static TOHE.Translator;
using AmongUs.GameOptions;

namespace TOHE.Roles.Crewmate;

// 参考 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
// 贡献：https://github.com/Yumenopai/TownOfHost_Y/tree/Monitor
internal class Monitor : RoleBase
{
    private static readonly int Id = 12500;
    private static List<byte> playerIdList = [];
    public static bool On = false;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;

    private static OptionItem CanCheckCamera;
    private static OptionItem CanVent;
    private static bool IsAdminWatch;
    private static bool IsVitalWatch;
    private static bool IsDoorLogWatch;
    private static bool IsCameraWatch;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Monitor);
        CanCheckCamera = BooleanOptionItem.Create(Id + 10, "CanCheckCamera", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Monitor]);
        CanVent = BooleanOptionItem.Create(Id + 14, "CanVent", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Monitor]);
    }
    public override void Init()
    {
        playerIdList = [];
        IsAdminWatch = false;
        IsVitalWatch = false;
        IsDoorLogWatch = false;
        IsCameraWatch = false;
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    public static bool CanUseVent() => CanVent.GetBool();

    private static int Count = 0;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public static void FixedUpdate()
    {
        Count--; if (Count > 0) return; Count = 5;

        bool Admin = false, Camera = false, DoorLog = false, Vital = false;
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            if (Pelican.IsEaten(pc.PlayerId) || pc.inVent) continue;
            try
            {
                Vector2 PlayerPos = pc.transform.position;
                var mapId = GetActiveMapId();
                var mapName = (MapNames)mapId;

                switch (mapId)
                {
                    case 0:
                        if (!Options.DisableSkeldAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableSkeldCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldCamera"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 1:
                        if (!Options.DisableMiraHQAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableMiraHQDoorLog.GetBool())
                            DoorLog |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQDoorLog"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 2:
                        if (!Options.DisablePolusAdmin.GetBool())
                        {
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusLeftAdmin"]) <= DisableDevice.UsableDistance(mapName);
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusRightAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        }
                        if (!Options.DisablePolusCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusCamera"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisablePolusVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusVital"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 3:
                        if (!Options.DisableSkeldAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["DleksAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableSkeldCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["DleksCamera"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 4:
                        if (!Options.DisableAirshipCockpitAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCockpitAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableAirshipRecordsAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipRecordsAdmin"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableAirshipCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCamera"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableAirshipVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipVital"]) <= DisableDevice.UsableDistance(mapName);
                        break;
                    case 5:
                        if (!Options.DisableFungleBinoculars.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["FungleCamera"]) <= DisableDevice.UsableDistance(mapName);
                        if (!Options.DisableFungleVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["FungleVital"]) <= DisableDevice.UsableDistance(mapName);
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
            foreach (var pc in playerIdList.ToArray())
            {
                var antiAdminer = GetPlayerById(pc);
                NotifyRoles(SpecifySeer: antiAdminer, ForceLoop: false);
            }
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (isForMeeting) return "";

        StringBuilder sb = new();
        if (IsAdminWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.Monitor), "★")).Append(ColorString(GetRoleColor(CustomRoles.Monitor), GetString("AdminWarning")));
        if (IsVitalWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.Monitor), "★")).Append(ColorString(GetRoleColor(CustomRoles.Monitor), GetString("VitalsWarning")));
        if (IsDoorLogWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.Monitor), "★")).Append(ColorString(GetRoleColor(CustomRoles.Monitor), GetString("DoorlogWarning")));
        if (IsCameraWatch) sb.Append(ColorString(GetRoleColor(CustomRoles.Monitor), "★")).Append(ColorString(GetRoleColor(CustomRoles.Monitor), GetString("CameraWarning")));

        return sb.ToString();
    }
}