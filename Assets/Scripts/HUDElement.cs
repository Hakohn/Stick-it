using UnityEngine;

public enum HUDRole { Unknown, Timer, MovementStick, BombButton, Background }

public class HUDElement : MonoBehaviour
{
    public HUDRole HUDRole = HUDRole.Unknown;
}
