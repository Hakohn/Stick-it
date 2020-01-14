using UnityEngine;

public sealed class TimedKillTrigger : KillTrigger
{
    [SerializeField] private float Duration = 1.5f;

    private void Update()
    {
        Duration -= Time.deltaTime;
        if (Duration <= 0)
        {
            Destroy(this);
        }
    }
}