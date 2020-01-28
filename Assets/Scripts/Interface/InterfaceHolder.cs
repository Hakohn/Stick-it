using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InterfaceType { None, MainMenu, MatchLobby, Options, Pause, HUD, TouchScreenHUD }

public class InterfaceHolder : MonoBehaviour
{
    // General variables
    public static InterfaceHolder instance;

    // Interfaces variables
    private Dictionary<InterfaceType, Transform> interfaceDictionary = new Dictionary<InterfaceType, Transform>();
    public InterfaceType PreviouslyActiveInterface { get; private set; } = InterfaceType.None;

    // HUD elements
    [HideInInspector] public Joystick MovementStick = null;
    [HideInInspector] public Button BombButton = null;
    [HideInInspector] public bool areTouchControlsEnabled = false;
    [HideInInspector] public Transform backgroundPause = null;
    private TextMeshProUGUI timerGUI = null;

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
        foreach (Transform transform in GetComponentsInChildren<Transform>(true).Where(transf => transf.GetComponent<InterfaceElementsHolder>() != null))
            interfaceDictionary.Add(transform.GetComponent<InterfaceElementsHolder>().InterfaceType, transform);

        MovementStick = interfaceDictionary[InterfaceType.TouchScreenHUD].GetComponentsInChildren<HUDElement>(true).First(elem => elem.HUDRole == HUDRole.MovementStick).GetComponent<Joystick>();
        BombButton = interfaceDictionary[InterfaceType.TouchScreenHUD].GetComponentsInChildren<HUDElement>(true).First(elem => elem.HUDRole == HUDRole.BombButton).GetComponent<Button>();
        timerGUI = interfaceDictionary[InterfaceType.HUD].GetComponentsInChildren<HUDElement>(true).First(elem => elem.HUDRole == HUDRole.Timer).GetComponent<TextMeshProUGUI>();
        backgroundPause = interfaceDictionary[InterfaceType.HUD].GetComponentsInChildren<HUDElement>(true).First(elem => elem.HUDRole == HUDRole.Background).transform;
    }

    /// <summary> Modify the text value of a button. </summary>
    /// <param name="interfaceType"> The interface / menu it belongs to. </param>
    /// <param name="buttonAction"> The action that the button is supposed to do. </param>
    /// <param name="text"> 
    /// The value to modify it to. If the string starts with a tilda (~), then
    /// only the "value" (e.g: ON / OFF) of the button will be updated to it. 
    /// Else, the whole text on the button will be updated. 
    /// </param>
    public void ModifyButtonText(InterfaceType interfaceType, ButtonAction buttonAction, string text)
    {
        GetClickableButton(interfaceType, buttonAction).Text = text;
    }

    public void ModifyButtonInteraction(InterfaceType interfaceType, ButtonAction buttonAction, bool interactible = true)
    {
        GetClickableButton(interfaceType, buttonAction).GetComponent<Button>().interactable = interactible;
    }

    private ClickableButton GetClickableButton(InterfaceType interfaceType, ButtonAction buttonAction)
    {
        return interfaceDictionary[interfaceType].GetComponentsInChildren<ClickableButton>(true).First(button => button.action == buttonAction);
    }

    public void UpdateTimerValue(float value)
    {
        string minuteString = null;
        int minutes = (int)value / 60;
        if (minutes < 10) minuteString += "0";
        minuteString += minutes.ToString();

        string secondString = null;
        int seconds = (int)value % 60;
        if (seconds < 10) secondString += "0";
        secondString += seconds.ToString();


        timerGUI.text = $"{minuteString}:{secondString}";
    }

    public bool ToggleTouchControls()
    {
        areTouchControlsEnabled = !areTouchControlsEnabled;
        switch (areTouchControlsEnabled)
        {
            case false:
                interfaceDictionary[InterfaceType.TouchScreenHUD].gameObject.SetActive(false);
                break;


            case true:
                interfaceDictionary[InterfaceType.TouchScreenHUD].gameObject.SetActive(true);
                break;
        }

        return areTouchControlsEnabled;
    }

    public void SetActiveInterface(InterfaceType interfaceType, bool deactivateCurrentOnes = true)
    {
        PreviouslyActiveInterface = new List<InterfaceType>() { InterfaceType.MainMenu, InterfaceType.Pause, InterfaceType.Options, InterfaceType.MatchLobby, InterfaceType.HUD }
                                    .First(interf => interfaceDictionary[interf].gameObject.activeInHierarchy);


        if (deactivateCurrentOnes)
            foreach (var elem in interfaceDictionary)
                    if(elem.Key != InterfaceType.TouchScreenHUD) elem.Value.gameObject.SetActive(false);

        if(GameManager.instance.GameIsPaused)
            interfaceDictionary[InterfaceType.HUD].gameObject.SetActive(true);
        backgroundPause.gameObject.SetActive(GameManager.instance.GameIsPaused);

        if (interfaceType != InterfaceType.None)
            interfaceDictionary[interfaceType].gameObject.SetActive(true);


    }
}
