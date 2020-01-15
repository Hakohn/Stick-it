using UnityEngine;

public class UnitStats : MonoBehaviour
{
    // Reference variables
    private Lifetime lifetime = null;

    // General variables
    public bool IsInvulnerable = false;
    [HideInInspector] public bool IsAlive = true;

    // Sound variables
    [SerializeField] private Sound deathSound = null;

    protected virtual void Awake()
    {
        deathSound.SetAsAudioSourceToGameObject(gameObject);
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        // If the target died and it still has no despawn timer, create one and also do
        // the other things that need to be done ONLY ONCE after death.
        if(IsAlive == false && lifetime == null)
        {
            // Plays the death sound
            deathSound.source.Play();

            // Adds the time 'till corpse despawn to the unit
            lifetime = gameObject.AddComponent<Lifetime>();
            lifetime.Seconds = 7f;

            // Destroying the collider once dead, so that people won't get blocked out by the corpse
            Destroy(gameObject.GetComponent<Collider2D>());
        }
    }
}
