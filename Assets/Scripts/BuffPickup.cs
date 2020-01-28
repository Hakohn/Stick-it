using UnityEngine;

public class BuffPickup : MonoBehaviour
{
    // Pickup variables
    [SerializeField] private GameObject buffTimerPrefab = null;
    private bool pickedUpByParticipant = false;
    [HideInInspector] public bool destroyedByExplosion = false;

    [SerializeField] private Sound deathSound = null;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If it is not null, it means the one who hit the collider is a participant, and so will pickup the buff
        if (collision.GetComponent<ParticipantStats>() != null)
        {
            Instantiate(buffTimerPrefab, collision.transform);
            pickedUpByParticipant = true;
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Play the death sound of the buff only if it happens because of a bomb or by despawn timer
        if(!pickedUpByParticipant && (destroyedByExplosion || gameObject.GetComponent<Lifetime>().Seconds <= 0))
            AudioManager.CreateSoundObject(deathSound, transform.position);
    }
}
