using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InterfaceHolder : MonoBehaviour
{
    // General variables
    public static InterfaceHolder instance;

    // Menu variables
    private List<Transform> menuCategories = new List<Transform>();
    private List<Transform> menuStages = new List<Transform>();
    private List<TMPro.TextMeshProUGUI> menuTextMeshes = new List<TMPro.TextMeshProUGUI>();
    private Transform currentActiveMenu = null;

    // In-game interface elements
    [HideInInspector] public Joystick IGMovementStick = null;
    [HideInInspector] public Button IGActionButton = null;
    [HideInInspector] public Button IGPauseButton = null;
    [HideInInspector] public bool areTouchControlsEnabled = false;
    private TMPro.TextMeshProUGUI IGTimer = null;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            // If it contains Menu, it means it's the child that contains all the menus
            if (transform.GetChild(i).name.Contains("Menu"))
            {
                Transform parentTransOfMenus = transform.GetChild(i);
                for (int j = 0; j < parentTransOfMenus.childCount; j++)
                    menuCategories.Add(parentTransOfMenus.GetChild(j));
            }
            // if it contains IG, it means it's the in-game interface
            else if (transform.GetChild(i).name.Contains("IG"))
            {
                Transform parentTransOfIGInterface = transform.GetChild(i);
                for (int j = 0; j < parentTransOfIGInterface.childCount; j++)
                {
                    switch (parentTransOfIGInterface.GetChild(j).name)
                    {
                        case "Timer":
                            IGTimer = parentTransOfIGInterface.GetChild(j).GetComponent<TMPro.TextMeshProUGUI>();
                            break;
                        case "MovementStick":
                            IGMovementStick = parentTransOfIGInterface.GetChild(j).GetComponent<Joystick>();
                            break;
                        case "ActionButton":
                            IGActionButton = parentTransOfIGInterface.GetChild(j).GetComponent<Button>();
                            break;
                        case "PauseButton":
                            IGPauseButton = parentTransOfIGInterface.GetChild(j).GetComponent<Button>();
                            break;
                    }
                }
            }
        }

        // For each menu type, gather all the menu stages it contains
        foreach (Transform objTrans in menuCategories)
            for (int i = 0; i < objTrans.childCount; i++)
                if (objTrans.GetChild(i).tag.Contains("Menu"))
                    menuStages.Add(objTrans.GetChild(i));

        // For each menu stage, gather all the texts of the buttons it contains
        foreach (Transform objTrans in menuStages)
            for (int i = 0; i < objTrans.childCount; i++)
                foreach (TMPro.TextMeshProUGUI textMesh in objTrans.GetChild(i).GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                    menuTextMeshes.Add(textMesh);
    }

    /// <summary> Updates the button text string </summary>
    /// <param name="buttonName"> A few of the characters found within the name of the button object </param>
    /// <param name="value"> The string value you want to update to </param>
    /// <param name="completelyChange"> If set to true, the string will completely change to the value. Else, it changes the string found after the ": " to value </param>
    /// <returns> Returns a reference to the text component you want to update </returns>
    public void UpdateMenuButtonTextValue(string buttonName, string value, bool completelyChange)
    {
        bool foundButton = false;
        foreach(TMPro.TextMeshProUGUI text in menuTextMeshes)
        {
            if(text.transform.parent.name.Contains(buttonName))
            {
                if (completelyChange == true)
                {
                    text.text = value;
                }
                else
                {
                    text.text = text.text.Replace(text.text.Substring(text.text.IndexOf(':')), ": " + value);
                }

                foundButton = true;
            }
        }

        if (foundButton)
            return;
        // If it reaches this point, it didn't find the text we were looking for
        Debug.LogError(buttonName + " button was not found within the active menu!");
    }
    /// <summary> Updates the button text string </summary>
    /// <param name="buttonName"> A few of the characters found within the name of the button object </param>
    /// <param name="value"> The string value you want to update to </param>
    /// <returns> Returns a reference to the text component you want to update </returns>
    public void UpdateMenuButtonTextValue(string buttonName, string value)
    {
        UpdateMenuButtonTextValue(buttonName, value, false);
    }

    public void SetButtonInteractible(string buttonName, bool interactible = true)
    {
        foreach (TMPro.TextMeshProUGUI text in menuTextMeshes)
        {
            if (text.transform.parent.name.Contains(buttonName))
            {
                text.transform.parent.GetComponent<Button>().interactable = interactible;
                return;
            }
        }

        // If it reaches this point, it didn't find the text we were looking for
        Debug.LogError(buttonName + " button was not found within the active menu!");
    }

    public void UpdateTimerValue(float value)
    {
        string minuteString = null;
        int minutes = (int)value / 60;
        if (minutes < 10)
            minuteString += "0";
        minuteString += minutes.ToString();

        string secondString = null;
        int seconds = (int)value % 60;
        if (seconds < 10)
            secondString += "0";
        secondString += seconds.ToString();


        IGTimer.SetText(minuteString + ":" + secondString);
    }

    public bool ToggleTouchControls()
    {
        areTouchControlsEnabled = !areTouchControlsEnabled;
        if(GameManager.instance.GameIsPaused)
        {
            SetInGameTouchInterfaceActive(areTouchControlsEnabled);
        }
        return areTouchControlsEnabled;
    }

    public void SetInGameTouchInterfaceActive(bool value)
    {
        IGMovementStick.gameObject.SetActive(value);
        IGActionButton.gameObject.SetActive(value);
    }

    public void SetInGameInterfaceActive(bool value)
    {
        IGPauseButton.gameObject.SetActive(value);
        IGTimer.gameObject.SetActive(value);
        if(areTouchControlsEnabled)
        {
            SetInGameTouchInterfaceActive(value);
        }
    }

    public void DisableAllActiveMenus()
    {
        foreach(Transform objTrans in menuCategories)
        {
            objTrans.gameObject.SetActive(false);
        }

        foreach (Transform objTrans in menuStages)
        {
            objTrans.gameObject.SetActive(false);
        }
    }

    public Transform SetActiveMenu(string menuCategory, string menuStage)
    {
        DisableAllActiveMenus();

        // Search first for the menu that we want to, then make sure that it is the one belonging to the required category
        foreach(Transform objTrans in menuStages)
        {
            if(objTrans.name.Contains(menuStage) && objTrans.parent.name.Contains(menuCategory))
            {
                // Enable both the menu we are in need of and its parent (which probably contains the background, title, and so on)
                currentActiveMenu = objTrans;
                objTrans.gameObject.SetActive(true);
                objTrans.parent.gameObject.SetActive(true);
                return objTrans;
            }
        }


        // If we reached this point, it means we didn't find the desired menu to switch to
        // So, we activate again the menu that we were at before and throw an error
        currentActiveMenu.gameObject.SetActive(true);
        currentActiveMenu.parent.gameObject.SetActive(true);
        Debug.LogError(menuCategory + "/" + menuStage + " was not found within the existing menus!");
        return null;
    }
}
