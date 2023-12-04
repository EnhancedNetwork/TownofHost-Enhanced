using UnityEngine;
using System.Collections.Generic;

namespace TOHE;
public class MenuUI : MonoBehaviour
{

    public List<GroupInfo> groups = new List<GroupInfo>();

    private bool isDragging = false;
    private Rect windowRect = new Rect(10, 10, 300, 500);
    private bool isGUIActive = false;

    //Create all groups (buttons) and their toggles on start
    public void Start()
    {
        groups.Add(new GroupInfo("MODS", false, new List<ToggleInfo>() {
            new ToggleInfo(" Ver Fantasmas", () => CheatSettings.seeGhosts, x => CheatSettings.seeGhosts = x),
            new ToggleInfo(" Rastrear Fantasmas", () => CheatSettings.tracersGhosts, x => CheatSettings.tracersGhosts = x),
            new ToggleInfo(" Rastrear Corpos", () => CheatSettings.tracersBodies, x => CheatSettings.tracersBodies = x),
            new ToggleInfo(" Rastreio Baseado em Cor", () => CheatSettings.colorBasedTracers, x => CheatSettings.colorBasedTracers = x),
            new ToggleInfo(" Mostrar Fantasmas no Minimapa", () => CheatSettings.mapGhosts, x => CheatSettings.mapGhosts = x),
            new ToggleInfo(" Minimapa Baseado em Cor", () => CheatSettings.colorBasedMap, x => CheatSettings.colorBasedMap = x),
            new ToggleInfo(" Blackout", () => CheatSettings.blackOut, x => CheatSettings.blackOut = x),
            new ToggleInfo(" Fechar Portas", () => CheatSettings.fullLockdown, x => CheatSettings.fullLockdown = x),
            new ToggleInfo(" Sabotar Reator", () => CheatSettings.reactorSab, x => CheatSettings.reactorSab = x),
            new ToggleInfo(" Sabotar Oxigênio", () => CheatSettings.oxygenSab, x => CheatSettings.oxygenSab = x),
            new ToggleInfo(" Apagar Luzes", () => CheatSettings.elecSab, x => CheatSettings.elecSab = x),
            new ToggleInfo(" Sabotar Comms", () => CheatSettings.commsSab, x => CheatSettings.commsSab = x),
            new ToggleInfo(" Ativar Confusão de Cogumelos", () => CheatSettings.MushRoomMixUp, x => CheatSettings.MushRoomMixUp = x)
        }));
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            //Enable-disable GUI with F8 key
            isGUIActive = !isGUIActive;

            //Also teleport the window to the mouse for immediate use
            Vector2 mousePosition = Input.mousePosition;
            windowRect.position = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        }

        //Some cheats only work if the ship is present, so they are turned off if it is not
        if (!isShipCheck.isShip)
        {
            CheatSettings.blackOut = CheatSettings.reactorSab = CheatSettings.oxygenSab = CheatSettings.commsSab = false;
        }
    }

    public void OnGUI()
    {
        //If GUI is enabled render the window
        if (isGUIActive)
        {
            GUI.skin.toggle.fontSize = 20;
            GUI.skin.button.fontSize = 20;

            //Only change the window height while the user is not dragging it
            //Or else dragging breaks
            if (!isDragging)
            {
                int windowHeight = CalculateWindowHeight();
                windowRect.height = windowHeight;
            }

            windowRect = GUI.Window(0, windowRect, (UnityEngine.GUI.WindowFunction)WindowFunction, "???");
        }
    }

    public void WindowFunction(int windowID)
    {
        int groupSpacing = 50;
        int toggleSpacing = 50;
        int currentYPosition = 20;

        //Add a button for each group
        for (int i = 0; i < groups.Count; i++)
        {
            GroupInfo group = groups[i];

            //Expand group when its button is pressed
            if (GUI.Button(new Rect(10, currentYPosition, 280, 40), group.name))
            {
                group.isExpanded = !group.isExpanded;
                groups[i] = group;
            }

            if (group.isExpanded) //Group expansion
            {
                CloseAllGroupsExcept(i); //Close all other groups to avoid problems

                //Show all the toggles in the group
                for (int j = 0; j < group.toggles.Count; j++)
                {
                    bool currentState = group.toggles[j].getState();
                    bool newState = GUI.Toggle(new Rect(10, currentYPosition + ((j + 1) * toggleSpacing), 280, 40), currentState, group.toggles[j].label);
                    if (newState != currentState)
                    {
                        group.toggles[j].setState(newState);
                    }
                }
                currentYPosition += group.toggles.Count * toggleSpacing; //Update currentYPosition for each toggle
            }

            currentYPosition += groupSpacing; //Update currentYPosition for each group
        }

        if (Event.current.type == EventType.MouseDrag)
        {
            isDragging = true;
        }

        if (Event.current.type == EventType.MouseUp)
        {
            isDragging = false;
        }

        GUI.DragWindow(); //Allows dragging the GUI window with mouse
    }


    //Dynamically calculate the window's height depending on
    //The number of toggles & group expansion
    private int CalculateWindowHeight()
    {
        int baseHeight = 70;
        int groupHeight = 50;
        int expandedGroupHeight = 0;
        int totalHeight = baseHeight;

        foreach (GroupInfo group in groups)
        {
            expandedGroupHeight = group.toggles.Count * 50;
            totalHeight += group.isExpanded ? expandedGroupHeight : groupHeight;
        }

        return totalHeight;
    }

    //Closes all expanded groups other than indexToKeepOpen
    private void CloseAllGroupsExcept(int indexToKeepOpen)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            if (i != indexToKeepOpen)
            {
                GroupInfo group = groups[i];
                group.isExpanded = false;
                groups[i] = group;
            }
        }
    }

}
