using UnityEngine;

public class PlayerStats : UnitStats
{
    [HideInInspector][Range(1, 4)] public int playerNumber;
    [SerializeField] private Sound suicideSound = null;
    [HideInInspector] public bool committedSuicide = false;
    private bool playedSuicideSound = false;

    protected override void Update()
    {
        base.Update();

        if (IsAlive == false && committedSuicide && !playedSuicideSound)
        {
            playedSuicideSound = true;
            AudioManager.instance.CreateSoundObject(suicideSound);
        }
    }
}
