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
    public static GridArrangeV grid;

    public static SpriteRenderer CustomBackground;
    public static GameObject ButtonsGrid;
    private static int numOptions = 0;

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
                numOptions = 0;
                CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
                CustomBackground.name = "CustomBackground";
                CustomBackground.transform.localScale = new(0.9f, 0.9f, 1f);
                CustomBackground.transform.localPosition += Vector3.back * 8;
                CustomBackground.gameObject.SetActive(false);

                //Buttons Grid
                ButtonsGrid = new GameObject("ButtonsGrid");
                ButtonsGrid.layer = 5;
                ButtonsGrid.transform.SetParent(CustomBackground.transform, true);
                ButtonsGrid.name = "ButtonsGrid";
                ButtonsGrid.transform.localScale = new(0.8f, 0.8f, 1f);
                ButtonsGrid.transform.localPosition = new Vector3(-1.75f, -0.99f, -1f);
                grid = ButtonsGrid.gameObject.AddComponent<GridArrangeV>();
                grid.Alignment = GridArrangeV.StartAlign.Right;
                grid.MaxColumns = 3;
                grid.CellSize = new Vector2(2.2f, 0.6f);
                grid.DeclareCells();

                var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                closeButton.transform.localPosition = new(1.3f, -2.3f, -6f);
                closeButton.name = "Back";
                closeButton.Text.text = Translator.GetString("Back");
                closeButton.Background.color = Palette.DisabledGrey;
                var closePassiveButton = closeButton.GetComponent<PassiveButton>();
                closePassiveButton.OnClick = new();
                closePassiveButton.OnClick.AddListener(new Action(() =>
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
                modOptionsPassiveButton.OnClick.AddListener(new Action(() =>
                {
                    CustomBackground.gameObject.SetActive(true);
                    grid.CheckCurrentChildren();
                    grid.ArrangeChilds();
                }));

                if (leaveButton != null)
                    leaveButton.transform.localPosition = new(-1.35f, -2.411f, -1f);
                if (returnButton != null)
                    returnButton.transform.localPosition = new(1.35f, -2.411f, -1f);
            }

            // ボタン生成
            ToggleButton = Object.Instantiate(mouseMoveToggle, ButtonsGrid.transform);
            ToggleButton.name = name;
            ToggleButton.Text.text = Translator.GetString(name);
            var passiveButton = ToggleButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener(new Action(() =>
            {
                config.Value = !config.Value;
                UpdateToggle();
                additionalOnClickAction?.Invoke();
            }));
            UpdateToggle();
        }
        finally { numOptions++; }
    }

    public static ClientOptionItem Create(
        string name,
        ConfigEntry<bool> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction = null)
    {
        return new(name, config, optionsMenuBehaviour, additionalOnClickAction);
    }

    public void UpdateToggle()
    {
        if (ToggleButton == null) return;

        var color = Config.Value ? new Color32(255, 192, 203, byte.MaxValue) : new Color32(77, 77, 77, byte.MaxValue);
        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
    }
}