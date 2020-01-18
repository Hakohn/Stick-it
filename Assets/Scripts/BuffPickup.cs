using UnityEngine;

public class BuffPickup : MonoBehaviour
{
    // Pickup variables
    [SerializeField] private GameObject buffTimerPrefab = null;
    private bool pickedUpByPlayer = false;
    [HideInInspector] public bool destroyedByExplosion = false;

    [SerializeField] private Sound deathSound = null;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If it is not null, it means the one who hit the collider is a player, and so will pickup the buff
        if (collision.GetComponent<ParticipantStats>() != null)
        {
            Instantiate(buffTimerPrefab, collision.transform);
            pickedUpByPlayer = true;
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Play the death sound of the buff only if it happens because of a bomb or by despawn timer
        if(!pickedUpByPlayer && (destroyedByExplosion || gameObject.GetComponent<Lifetime>().Seconds <= 0))
            AudioManager.CreateSoundObject(deathSound, transform.position);
    }
}
