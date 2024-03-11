using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine.Events;
using UnityEngine;
using static TOHE.Main;
using UnityEngine.UI;

namespace TOHE;

public static class ServerAddManager
{
    private static readonly ServerManager serverManager = DestroyableSingleton<ServerManager>.Instance;
    public static void Init()
    {
        //if (CultureInfo.CurrentCulture.Name.StartsWith("zh") && serverManager.AvailableRegions.Count == 10) return;
        //if (!CultureInfo.CurrentCulture.Name.StartsWith("zh") && serverManager.AvailableRegions.Count == 7) return;
        // No need to do above check

        serverManager.AvailableRegions = ServerManager.DefaultRegions;
        List<IRegionInfo> regionInfos = [];

        if (CultureInfo.CurrentCulture.Name.StartsWith("zh"))
        {
            regionInfos.Add((CreateHttp("45yun.cn", "小猫[北京]", 22000, false)));
            regionInfos.Add((CreateHttp("45yun.cn", "小猫[成都]", 2267, false)));
            regionInfos.Add((CreateHttp("mau.kaifuxia.top", "新梦初[上海]", 25000, false)));
        }
        regionInfos.Add(CreateHttp("au-as.duikbo.at", "Modded Asia (MAS)", 443, true));
        regionInfos.Add(CreateHttp("www.aumods.us", "Modded NA (MNA)", 443, true));
        regionInfos.Add(CreateHttp("au-eu.duikbo.at", "Modded EU (MEU)", 443, true));
        regionInfos.Add(CreateHttp("35.247.251.253", "Modded SA (MSA)", 22023, false));
        regionInfos.Add(new StaticHttpRegionInfo("Custom", StringNames.NoTranslation, CustomIp.Value, new Il2CppReferenceArray<ServerInfo>([new ServerInfo("Custom", CustomIp.Value, CustomPort.Value, false)])).CastFast<IRegionInfo>());

        regionInfos.Where(x => !serverManager.AvailableRegions.Contains(x)).Do(serverManager.AddOrUpdateRegion);
        UpdateRegions();
    }

    public static IRegionInfo CreateHttp(string ip, string name, ushort port, bool ishttps)
    {
        string serverIp = (ishttps ? "https://" : "http://") + ip;
        ServerInfo serverInfo = new(name, serverIp, port, false);
        ServerInfo[] ServerInfo = [serverInfo];
        return new StaticHttpRegionInfo(name, (StringNames)1003, ip, ServerInfo).CastFast<IRegionInfo>();
    }

    public static void UpdateRegions()
    {
        string realIp = (UseHttps.Value ? "https://" : "http://") + CustomIp;
        ServerInfo serverInfo = new("Custom", realIp, CustomPort.Value, false);
        ServerInfo[] ServerInfo = [serverInfo];
        var regions = new IRegionInfo[] {
                new StaticHttpRegionInfo("Custom", StringNames.NoTranslation, CustomIp.Value, ServerInfo).CastFast<IRegionInfo>()
            };

        IRegionInfo currentRegion = serverManager.CurrentRegion;

        foreach (IRegionInfo region in regions)
        {
            if (region == null)
                Logger.Error("Could not add region", "ServerAddManager");
            else
            {
                if (currentRegion != null && region.Name.Equals(currentRegion.Name, StringComparison.OrdinalIgnoreCase))
                    currentRegion = region;
                serverManager.AddOrUpdateRegion(region);
            }
        }

        // AU remembers the previous region that was set, so we need to restore it
        if (currentRegion != null)
        {
            Logger.Info("Resetting previous region", "ServerAddManager");
            serverManager.SetRegion(currentRegion);
        }
    }
    private static class CastHelper<T> where T : Il2CppObjectBase
    {
        public static Func<IntPtr, T> Cast;
        static CastHelper()
        {
            var constructor = typeof(T).GetConstructor(new[] { typeof(IntPtr) });
            var ptr = Expression.Parameter(typeof(IntPtr));
            var create = Expression.New(constructor!, ptr);
            var lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
            Cast = lambda.Compile();
        }
    }

    private static T CastFast<T>(this Il2CppObjectBase obj) where T : Il2CppObjectBase
    {
        if (obj is T casted) return casted;
        return CastHelper<T>.Cast(obj.Pointer);
    }

}

[HarmonyPatch(typeof(RegionMenu))]
internal class RegionMenuPatch
{
    private static TextBoxTMP ipField;
    private static TextBoxTMP portField;
    private static ToggleButtonBehaviour useHttpsButton;
    private static readonly ServerManager serverManager = DestroyableSingleton<ServerManager>.Instance;
    [HarmonyPatch(nameof(RegionMenu.Open))]
    [HarmonyPostfix]
    public static void RegionMenuOpen_Postfix(RegionMenu __instance)
    {
        if (!__instance.TryCast<RegionMenu>()) return;
        bool isCustomRegion = serverManager.CurrentRegion.Name == "Custom";
        if (!isCustomRegion)
        {
            if (ipField != null && ipField.gameObject != null)
            {
                ipField.gameObject.SetActive(false);

            }
            if (portField != null && portField.gameObject != null)
            {
                portField.gameObject.SetActive(false);
            }
            if (useHttpsButton != null && useHttpsButton.gameObject != null)
            {
                useHttpsButton.gameObject.SetActive(false);
            }
        }
        else
        {
            if (ipField != null && ipField.gameObject != null)
            {
                ipField.gameObject.SetActive(true);

            }
            if (portField != null && portField.gameObject != null)
            {
                portField.gameObject.SetActive(true);
            }
            if (useHttpsButton != null && useHttpsButton.gameObject != null)
            {
                useHttpsButton.gameObject.SetActive(true);
            }
        }
        var template = DestroyableSingleton<JoinGameButton>.Instance;
        var joinGameButtons = GameObject.FindObjectsOfType<JoinGameButton>();
        foreach (var t in joinGameButtons)
        {  // The correct button has a background, the other 2 dont
            if (t.GameIdText != null && t.GameIdText.Background != null)
            {
                template = t;
                break;
            }
        }
        if (template == null || template.GameIdText == null) return;
         
        if (useHttpsButton == null || useHttpsButton.gameObject == null)
        {
            useHttpsButton = UnityEngine.Object.Instantiate(new ToggleButtonBehaviour(), __instance.transform);
            useHttpsButton.gameObject.name = "UseHttpsButton";

            useHttpsButton.transform.localPosition = new Vector3(3.225f, -0.05f, -100f);
            useHttpsButton.Text.text = "Use HTTPS";

            useHttpsButton.onState = UseHttps.Value;
            useHttpsButton.UpdateText(UseHttps.Value);
            ipField.gameObject.SetActive(isCustomRegion);
        }

        UseHttps.Value = useHttpsButton.onState;

        if (ipField == null || ipField.gameObject == null)
        {
            ipField = UnityEngine.Object.Instantiate(template.GameIdText, __instance.transform);
            ipField.gameObject.name = "IpTextBox";
            var arrow = ipField.transform.FindChild("arrowEnter");
            if (arrow == null || arrow.gameObject == null) return;
            UnityEngine.Object.DestroyImmediate(arrow.gameObject);

            ipField.transform.localPosition = new Vector3(3.225f, -0.8f, -100f);
            ipField.characterLimit = 30;
            ipField.AllowSymbols = true;
            ipField.ForceUppercase = false;
            ipField.SetText(Main.CustomIp.Value);
            __instance.StartCoroutine(Effects.Lerp(0.1f, new Action<float>((p) =>
            {
                ipField.outputText.SetText(Main.CustomIp.Value);
                ipField.SetText(Main.CustomIp.Value);
            })));

            ipField.ClearOnFocus = false;
            ipField.OnEnter = ipField.OnChange = new Button.ButtonClickedEvent();
            ipField.OnFocusLost = new Button.ButtonClickedEvent();
            ipField.OnChange.AddListener((UnityAction)onEnterOrIpChange);
            ipField.OnFocusLost.AddListener((UnityAction)onFocusLost);
            ipField.gameObject.SetActive(isCustomRegion);

            void onEnterOrIpChange()
            {
                Main.CustomIp.Value = ipField.text;
            }

            void onFocusLost()
            {
                ServerAddManager.UpdateRegions();
            }
        }

        if (portField == null || portField.gameObject == null)
        {
            portField = UnityEngine.Object.Instantiate(template.GameIdText, __instance.transform);
            portField.gameObject.name = "PortTextBox";
            var arrow = portField.transform.FindChild("arrowEnter");
            if (arrow == null || arrow.gameObject == null) return;
            UnityEngine.Object.DestroyImmediate(arrow.gameObject);

            portField.transform.localPosition = new Vector3(3.225f, -1.55f, -100f);
            portField.characterLimit = 5;
            portField.SetText(CustomPort.Value.ToString());
            __instance.StartCoroutine(Effects.Lerp(0.1f, new Action<float>((p) =>
            {
                portField.outputText.SetText(CustomPort.Value.ToString());
                portField.SetText(CustomPort.Value.ToString());
            })));


            portField.ClearOnFocus = false;
            portField.OnEnter = portField.OnChange = new Button.ButtonClickedEvent();
            portField.OnFocusLost = new Button.ButtonClickedEvent();
            portField.OnChange.AddListener((UnityAction)onEnterOrPortFieldChange);
            portField.OnFocusLost.AddListener((UnityAction)onFocusLost);
            portField.gameObject.SetActive(isCustomRegion);

            void onEnterOrPortFieldChange()
            {
                ushort port = 0;
                if (ushort.TryParse(portField.text, out port))
                {
                    CustomPort.Value = port;
                    portField.outputText.color = Color.white;
                }
                else
                {
                    portField.outputText.color = Color.red;
                }
            }

            void onFocusLost()
            {
                ServerAddManager.UpdateRegions();
            }
        }
    }

    [HarmonyPatch(nameof(RegionMenu.ChooseOption))]
    [HarmonyPrefix]
    public static bool RegionMenuChooseOption_Prefix(RegionMenu __instance, IRegionInfo region)
    {
        if (region.Name != "Custom" || serverManager.CurrentRegion.Name == "Custom") return true;
        DestroyableSingleton<ServerManager>.Instance.SetRegion(region);
        __instance.RegionText.text = "Custom";
        foreach (var Button in __instance.ButtonPool.activeChildren)
        {
            ServerListButton serverListButton = Button.TryCast<ServerListButton>();
            serverListButton?.SetSelected(serverListButton.Text.text == "Custom");
        }
        __instance.Open();
        return false;
    }
}
