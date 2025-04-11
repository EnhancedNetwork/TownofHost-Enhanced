using BepInEx.Configuration;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TOHE;

//来源：https://github.com/tukasa0001/TownOfHost/pull/1265
public class ClientOptionItem
{
    public ConfigEntry<bool> Config;
    public ToggleButtonBehaviour ToggleButton;

    public static SpriteRenderer CustomBackground;
    private static List<ToggleButtonBehaviour> OptionButtons;

    private ClientOptionItem(
        string name,
        ConfigEntry<bool> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction = null)
    {
        try
        {
            Config = config;

            var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

            // 1つ目のボタンの生成時に背景も生成
            if (CustomBackground == null)
            {
                CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
                CustomBackground.name = "CustomBackground";
                CustomBackground.transform.localScale = new(0.9f, 0.9f, 1f);
                CustomBackground.transform.localPosition += Vector3.back * 8;
                CustomBackground.size += new Vector2(3f, 0f);
                CustomBackground.gameObject.SetActive(false);

                var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                closeButton.transform.localPosition = new(2.6f, -2.3f, -6f);
                closeButton.name = "Back";
                closeButton.Text.text = Translator.GetString("Back");
                closeButton.Background.color = Palette.DisabledGrey;
                var closePassiveButton = closeButton.GetComponent<PassiveButton>();
                closePassiveButton.OnClick = new();
                closePassiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    CustomBackground.gameObject.SetActive(false);
                }));

                UiElement[] selectableButtons = optionsMenuBehaviour.ControllerSelectable.ToArray();
                PassiveButton leaveButton = null;
                PassiveButton returnButton = null;
                foreach (var button in selectableButtons)
                {
                    if (button == null) continue;

                    if (button.name == "LeaveGameButton")
                        leaveButton = button.GetComponent<PassiveButton>();
                    else if (button.name == "ReturnToGameButton")
                        returnButton = button.GetComponent<PassiveButton>();
                }
                var generalTab = mouseMoveToggle.transform.parent.parent.parent;

                var modOptionsButton = Object.Instantiate(mouseMoveToggle, generalTab);
                modOptionsButton.transform.localPosition = leaveButton?.transform?.localPosition ?? new(0f, -2.4f, 1f);
                modOptionsButton.name = "TOHEOptions";
                modOptionsButton.Text.text = Translator.GetString("TOHEOptions");
                modOptionsButton.Background.color = new Color32(255, 192, 203, byte.MaxValue);
                var modOptionsPassiveButton = modOptionsButton.GetComponent<PassiveButton>();
                modOptionsPassiveButton.OnClick = new();
                modOptionsPassiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    AdjustButtonPositions();
                    CustomBackground.gameObject.SetActive(true);
                }));

                if (leaveButton != null)
                    leaveButton.transform.localPosition = new(-1.35f, -2.411f, -1f);
                if (returnButton != null)
                    returnButton.transform.localPosition = new(1.35f, -2.411f, -1f);

                OptionButtons = [];
            }


            // ボタン生成
            ToggleButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            OptionButtons.Add(ToggleButton);

            ToggleButton.transform.localPosition = new Vector3(
                           (OptionButtons.Count - 1) % 3 == 0 ? -2.6f : ((OptionButtons.Count - 1) % 3 == 1 ? 0f : 2.6f),
                           2.2f - (0.5f * ((OptionButtons.Count - 1) / 3)),
                           -6f);
            ToggleButton.name = name;
            ToggleButton.Text.text = Translator.GetString(name);
            var passiveButton = ToggleButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                if (config != null) config.Value = !config.Value;
                UpdateToggle();
                additionalOnClickAction?.Invoke();
            }));
            UpdateToggle();
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString(), "ClientOptionItem.Create");
        }
    }

    public static ClientOptionItem Create(
        string name,
        ConfigEntry<bool> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction = null)
    {
        return new(name, config, optionsMenuBehaviour, additionalOnClickAction);
    }

    public static void AdjustButtonPositions()
    {
        if (OptionButtons == null || OptionButtons.Count == 0) return;

        int totalRows = (OptionButtons.Count + 2) / 3;

        float topPosition = 2.2f;
        float bottomLimit = -1.6f;
        float availableHeight = topPosition - bottomLimit;
        float rowSpacing = totalRows > 1 ? availableHeight / (totalRows - 1) : 0f;

        for (int i = 0; i < OptionButtons.Count; i++)
        {
            var button = OptionButtons[i];
            if (button == null) continue;

            int row = i / 3;
            int col = i % 3;

            float xPos = col == 0 ? -2.6f : (col == 1 ? 0f : 2.6f);

            float yPos = topPosition - (row * rowSpacing);

            button.transform.localPosition = new Vector3(xPos, yPos, -6f);
        }
    }

    public void UpdateToggle()
    {
        if (ToggleButton == null) return;

        var color = (Config != null && Config.Value) ? new Color32(255, 192, 203, byte.MaxValue) : new Color32(77, 77, 77, byte.MaxValue);
        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
    }
}
