using System;
using UnityEngine;
using UnityEngine.UI;

public enum ButtonRole { Quit, Simple, Important }
public class ClickableButton : MonoBehaviour
{
    [SerializeField] private ButtonRole buttonRole = ButtonRole.Simple;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(() => AudioManager.instance.PlayGlobalSound(SoundCategory.UI, "click" + Enum.GetName(typeof(ButtonRole), buttonRole)));
    }
}
