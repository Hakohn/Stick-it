using UnityEngine;

public class ParticipantStats : UnitStats
{
    public enum TypeOfControl { Player, AI }
    public TypeOfControl ControlType = TypeOfControl.Player;
    public bool IsMainPlayer { get; set; }
    [Range(1, 4)] public int participantNumber;
    [SerializeField] private Sound suicideSound = null;
    [HideInInspector] public bool committedSuicide = false;
    private bool playedSuicideSound = false;

    protected override void Update()
    {
        base.Update();

        if (IsAlive == false && committedSuicide && !playedSuicideSound)
        {
            playedSuicideSound = true;
            AudioManager.CreateSoundObject(suicideSound);
        }
    }
}
