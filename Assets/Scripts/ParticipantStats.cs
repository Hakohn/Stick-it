using UnityEngine;

public enum InputSource { Player, AI }

public class ParticipantStats : UnitStats
{
    public InputSource InputSource = InputSource.Player;
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
