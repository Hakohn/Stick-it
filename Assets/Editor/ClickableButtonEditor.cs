using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ClickableButton))]
public class ClickableButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ClickableButton clickableButton = target as ClickableButton;
        clickableButton.action = (ButtonAction)EditorGUILayout.EnumPopup("Button Action", clickableButton.action);
        if (clickableButton.action == ButtonAction.SwitchInterface)
            clickableButton.interfaceToSwitchTo = (InterfaceType)EditorGUILayout.EnumPopup("Switch to", clickableButton.interfaceToSwitchTo);
    }
}
