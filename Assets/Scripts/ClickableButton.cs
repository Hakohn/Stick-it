using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Importance level is used only to determine the button's looks / sounds. </summary>
public enum ButtonImportance { Unknown, Simple, Important, Quit }
/// <summary> Used to determine what the button it's attached to is supposed to do. </summary>
public enum ButtonAction
{
    Unknown, SwitchInterface, QuitGame, 
    StartGame, ModifyMap, ModifyMatchDuration, ModifyPlayerNumber, ModifyAINumber,
    ToggleSoundtrack, ToggleTouchControls,
    TogglePause, ReturnToMainMenu, Back
}

public class ClickableButton : MonoBehaviour
{
    private Button button = null;

    [SerializeField] private ButtonImportance importance = ButtonImportance.Simple;
    [HideInInspector] public ButtonAction action = ButtonAction.Unknown;
    [HideInInspector] public InterfaceType interfaceToSwitchTo = InterfaceType.None;
    [HideInInspector] private string initialText = null;
    public string Text
    {
        get => GetComponent<TextMeshProUGUI>() != null ? GetComponent<TextMeshProUGUI>().text : null;
        set
        {
            // If it contains tilda, it means we want only a value change.
            if(value.StartsWith("~"))
            {
                theValue = value.Substring(1);
                GetComponent<TextMeshProUGUI>().text = $"{initialText}: {theValue}";
            }
            else // We want a full text change.
            {
                theValue = null;
                GetComponent<TextMeshProUGUI>().text = value;
            }
        }
    }
    [HideInInspector] private string theValue = null;

    private void Awake()
    {
        // Grab the required variables.
        button = GetComponent<Button>();
        initialText = Text;

        // Add the necessary methods to the button.
        // The audio and visual methods.
        button.onClick.AddListener(() => AudioManager.instance.PlayGlobalSound(SoundCategory.UI, "click" + Enum.GetName(typeof(ButtonImportance), importance)));
        // The action methods.
        button.onClick.AddListener(() => GameManager.instance.ButtonPress(action, interfaceToSwitchTo));
    }

    
}
