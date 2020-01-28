using System.Collections;
using UnityEngine;

// Buff variable
public enum BuffAction
{
    Unknown,
    IncreaseBombCount, IncreaseBombRange, IncreaseMovementSpeed,
    SlowMovement, PauseBombPlacing, UncontrollableBombPlacing
}
public class BuffApplication : MonoBehaviour
{
    // Component references
    private Lifetime lifetime = null;
    private ParticipantActionController actionController = null;
    private ParticipantMovementController movementController = null;

    // Buff type
    [SerializeField] private BuffAction buffAction = BuffAction.IncreaseBombCount;

    // Sound variables
    [SerializeField] private Sound pickupSound = null;
    [SerializeField] private Sound fadeSound = null;


    private void Start()
    {
        // The EnumerableStart is doing exactly what Start was supposed to do, but with the ability to
        // use the IEnumerator and yield return thingy (for waiting purposes).
        StartCoroutine(EnumerableStart());
    }

    private IEnumerator EnumerableStart()
    {
        AudioManager.CreateSoundObject(pickupSound, GetComponentInParent<Transform>().position);

        // Initialize the references
        lifetime = gameObject.GetComponent<Lifetime>();
        actionController = GetComponentInParent<ParticipantActionController>();
        movementController = GetComponentInParent<ParticipantMovementController>();

        // Determine what is this buff supposed to do
        switch (buffAction)
        {
            case BuffAction.IncreaseBombCount:
                if (actionController.MaximumBombCount < 8) actionController.MaximumBombCount++;
                Destroy(gameObject);
                break;

            case BuffAction.IncreaseBombRange:
                if (actionController.explosionRadius < 5) actionController.explosionRadius++;
                Destroy(gameObject);
                break;

            case BuffAction.IncreaseMovementSpeed:
                movementController.CurrentMovementSpeed *= 2f;
                break;

            case BuffAction.SlowMovement:
                movementController.CurrentMovementSpeed /= 2f;
                break;

            case BuffAction.PauseBombPlacing:
                actionController.canPlaceBombs = false;
                break;

            case BuffAction.UncontrollableBombPlacing:
                // Wait a little before starting to place the uncontrollable bombs, so that the user won't get
                // instantly killed when grabbing the debuff. Also, adds the waiting time to the lifetime, 
                // so that it will not alter the total duration
                float waitTime = 2f;
                lifetime.Seconds += waitTime;
                yield return new WaitForSeconds(waitTime);
                actionController.UncontrollableBombPlacing = true;
                break;
        }
        yield return new WaitForEndOfFrame();
    }

    private void OnDestroy()
    {
        // We're making this check so that we won't get a null reference on the destroy from scene changing
        if(transform.parent != null)
        {
            // If this buff did have a duration and the parent it belongs to is still alive,
            // let him know that his buff has faded using the audiomanager (so that it won't be disturbed
            // by the fact that we're destroying the object)
            if (lifetime != null && GetComponentInParent<UnitStats>().IsAlive)
                AudioManager.CreateSoundObject(fadeSound, GetComponentInParent<Transform>().position);

            // Reverting to the owner's original stats after the buff fade.
            switch (buffAction)
            {
                case BuffAction.IncreaseMovementSpeed: case BuffAction.SlowMovement:
                    movementController.CurrentMovementSpeed = movementController.DefaultMovementSpeed;
                    break;

                case BuffAction.PauseBombPlacing:
                    actionController.canPlaceBombs = true;
                    break;

                case BuffAction.UncontrollableBombPlacing:
                    actionController.UncontrollableBombPlacing = false;
                    break;
            }
        }
    }
}
