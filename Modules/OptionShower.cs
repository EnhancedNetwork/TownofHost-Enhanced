using System;
using System.Text;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[Obfuscation(Exclude = true, Feature = "renaming", ApplyToMembers = true)]
public class OptionShower : MonoBehaviour
{
    public static int currentPage = 0;
    public static List<string> pages = [];
    private static byte DelayInUpdate = 0;

    public OptionShower(IntPtr ptr) : base(ptr)
    {
    }

    internal HudManager hudManager = null!;

    public TMPro.TextMeshPro text;
    public Camera Camera => hudManager ? Camera.main : HudManager.Instance.PlayerCam.GetComponent<Camera>();
    public Vector3 TextOffset = new(0.38f, 0.38f, 0f);

    public void Start()
    {

    }
    public void Update()
    {
        if (text == null)
        {
            if (hudManager.AbilityButton != null && hudManager.AbilityButton.cooldownTimerText != null)
            {
                text = Instantiate(hudManager.AbilityButton.cooldownTimerText, hudManager.transform);
                text.name = "OptionShowerText";
                text.text = "";
                text.color = Color.white;
                text.outlineColor = Color.black;
                text.alignment = TMPro.TextAlignmentOptions.TopLeft;
                text.gameObject.SetActive(true);
                text.fontSize = 1.05f;
                text.fontSizeMin = 1.0f;
                text.enableWordWrapping = false;
            }
        }

        if (Camera == null || text == null) return;

        if (PlayerControl.LocalPlayer == null || !GameStates.IsLobby)
        {
            text.text = "";
            return;
        }

        text.transform.position = AspectPosition.ComputeWorldPosition(Camera, AspectPosition.EdgeAlignments.LeftTop, TextOffset);
        UpdateText();
    }
    public void UpdateText()
    {
        text.text = GetTextNoFresh();
    }

    public static string GetTextNoFresh()
    {
        try
        {
            if (currentPage == 0 && DelayInUpdate >= 100)
            {
                DelayInUpdate = 0;
                GetText();
            }
            DelayInUpdate++;
            return $"{pages[currentPage]}{GetString("PressTabToNextPage")}({currentPage + 1}/{pages.Count})";
        }
        catch
        {
            //Logger.Warn(error.ToString(), "GetTextNoFresh()");
            return GetText();
        }
    }
    public static string GetText()
    {
        //初期化
        StringBuilder sb = new();
        pages =
            [
                // Vanilla Settings
                GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10) + "\n\n"
            ];

        // Mod Settings
        sb.Append($"{Options.GameMode.GetName()}: {Options.GameMode.GetString()}\n\n");
        if (Options.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
        {
            sb.Append($"<color=#ff0000>{GetString("Message.HideGameSettings")}</color>");
        }
        else
        {
            //Standardの時のみ実行
            if (Options.CurrentGameMode == CustomGameMode.Standard)
            {
                //有効な役職一覧
                sb.Append($"<color={Utils.GetRoleColorCode(CustomRoles.GM)}>{Utils.GetRoleName(CustomRoles.GM)}:</color> {(Main.EnableGM.Value ? GetString("RoleRate") : GetString("RoleOff"))}\n\n");
                sb.Append(GetString("ActiveRolesList")).Append('\n');

                var count = 4;

                foreach (var kvp in Options.CustomRoleSpawnChances.ToArray())
                    if (kvp.Value.GameMode is CustomGameMode.Standard or CustomGameMode.All && kvp.Value.GetBool()) //スタンダードか全てのゲームモードで表示する役職
                    {
                        string mode = kvp.Value.GetString();
                        if (kvp.Key is CustomRoles.Lovers) mode = Utils.GetChance(Options.LoverSpawnChances.GetInt());
                        else if (kvp.Key.IsAdditionRole() && Options.CustomAdtRoleSpawnRate.ContainsKey(kvp.Key))
                        {
                            mode = Utils.GetChance(Options.CustomAdtRoleSpawnRate[kvp.Key].GetFloat());

                        }
                        sb.Append($"{Utils.ColorString(Utils.GetRoleColor(kvp.Key), Utils.GetRoleName(kvp.Key))}: {mode}×{kvp.Key.GetCount()}\n");
                        count++;

                        if (count > 35)
                        {
                            count = 0;
                            pages.Add(sb + "\n\n");
                            sb.Clear().Append(GetString("ActiveRolesList")).Append('\n');
                        }
                    }
                pages.Add(sb.ToString() + "\n\n");
                sb.Clear();
            }

            //有効な役職と詳細設定一覧
            pages.Add("");
            //nameAndValue(Options.EnableGM);
            foreach (var kvp in Options.CustomRoleSpawnChances.ToArray())
            {
                if (!kvp.Key.IsEnable() || kvp.Value.IsHiddenOn(Options.CurrentGameMode)) continue;
                sb.Append('\n');
                sb.Append($"{Utils.ColorString(Utils.GetRoleColor(kvp.Key), Utils.GetRoleName(kvp.Key))}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}\n");
                ShowChildren(kvp.Value, ref sb, Utils.GetRoleColor(kvp.Key).ShadeColor(-0.5f), 1);
                string rule = Utils.ColorString(Palette.ImpostorRed.ShadeColor(-0.5f), "┣ ");
                string ruleFooter = Utils.ColorString(Palette.ImpostorRed.ShadeColor(-0.5f), "┗ ");
            }

            foreach (var opt in OptionItem.AllOptions.Where(x => x.Id > 59999 && !x.IsHiddenOn(Options.CurrentGameMode) && x.Parent == null && !x.IsText).ToArray())
            {
                if (opt.IsHeader) sb.Append('\n');
                sb.Append($"{opt.GetName()}: {opt.GetString()}\n");
                if (opt.GetBool())
                    ShowChildren(opt, ref sb, Color.white, 1);
            }
            //Onの時に子要素まで表示するメソッド
            //void nameAndValue(OptionItem o) => sb.Append($"{o.GetName()}: {o.GetString()}\n");
        }
        //1ページにつき35行までにする処理
        List<string> tmpList = new(sb.ToString().Split("\n\n"));
        foreach (var tmp in tmpList.ToArray())
        {
            if (pages[^1].Count(c => c == '\n') + 1 + tmp.Count(c => c == '\n') + 1 > 35)
                pages.Add(tmp + "\n\n");
            else pages[^1] += tmp + "\n\n";
        }
        if (currentPage >= pages.Count) currentPage = pages.Count - 1; //現在のページが最大ページ数を超えていれば最後のページに修正
        return $"{pages[currentPage]}{GetString("PressTabToNextPage")}({currentPage + 1}/{pages.Count})";
    }
    public static void Next()
    {
        currentPage++;
        if (currentPage >= pages.Count) currentPage = 0; //現在のページが最大ページを超えていれば最初のページに
    }
    private static void ShowChildren(OptionItem option, ref StringBuilder sb, Color color, int deep = 0)
    {
        foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
        {
            if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
            sb.Append(string.Concat(Enumerable.Repeat(Utils.ColorString(color, "┃"), deep - 1)));
            sb.Append(Utils.ColorString(color, opt.Index == option.Children.Count ? "┗ " : "┣ "));
            sb.Append($"{opt.Value.GetName()}: {opt.Value.GetString()}\n");
            if (opt.Value.GetBool()) ShowChildren(opt.Value, ref sb, color, deep + 1);
        }
    }
}
