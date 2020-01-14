using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffApplication : MonoBehaviour
{
    // Buff variable
    private enum BuffAction
    {
        INCREASE_BOMB_COUNT,
        INCREASE_BOMB_RADIUS,
        SPEED,
        SLOW,
        DENY_BOMB_PLACING,
        UNCONTROLLABLE_BOMB_PLACING
    }
    [SerializeField] private BuffAction buffAction = BuffAction.INCREASE_BOMB_COUNT;

    // Sound variables
    [SerializeField] private Sound pickupSound = null;
    [SerializeField] private Sound fadeSound = null;

    // Component references
    private Lifetime lifetime = null;

    private void Start()
    {
        // The EnumerableStart is doing exactly what Start was supposed to do, but with the ability to
        // use the IEnumerator and yield return thingy
        StartCoroutine(EnumerableStart());
    }

    private IEnumerator EnumerableStart()
    {
        AudioManager.instance.CreateSoundObject(pickupSound, GetComponentInParent<Transform>().position);

        // Initialize the references
        lifetime = gameObject.GetComponent<Lifetime>();

        // Determine what is this buff supposed to do
        switch (buffAction)
        {
            case BuffAction.INCREASE_BOMB_COUNT:
                ParticipantActionController bombPlacer = GetComponentInParent<ParticipantActionController>();
                if (bombPlacer.MaximumBombCount < 8)
                    bombPlacer.MaximumBombCount++;

                Destroy(gameObject);
                break;

            case BuffAction.INCREASE_BOMB_RADIUS:
                ParticipantActionController bombRadius = GetComponentInParent<ParticipantActionController>();
                if (bombRadius.explosionRadius < 5)
                    bombRadius.explosionRadius++;

                Destroy(gameObject);
                break;

            case BuffAction.SPEED:
                ParticipantMovementController movementController1 = GetComponentInParent<ParticipantMovementController>();
                movementController1.CurrentMovementSpeed *= 2f;
                break;

            case BuffAction.SLOW:
                ParticipantMovementController movementController2 = GetComponentInParent<ParticipantMovementController>();
                movementController2.CurrentMovementSpeed /= 2f;
                break;

            case BuffAction.DENY_BOMB_PLACING:
                ParticipantActionController bombPlacer1 = GetComponentInParent<ParticipantActionController>();
                bombPlacer1.canPlaceBombs = false;
                break;

            case BuffAction.UNCONTROLLABLE_BOMB_PLACING:
                ParticipantActionController bombPlacer2 = GetComponentInParent<ParticipantActionController>();
                // Wait a little before starting to place the uncontrollable bombs, so that the user won't get
                // instantly killed when grabbing the debuff. Also, adds the waiting time to the lifetime, 
                // so that it will not alter the total duration
                float waitTime = 2f;
                lifetime.Seconds += waitTime;
                yield return new WaitForSeconds(waitTime);
                bombPlacer2.UncontrollableBombPlacing = true;
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
                AudioManager.instance.CreateSoundObject(fadeSound, GetComponentInParent<Transform>().position);

            switch (buffAction)
            {
                case BuffAction.SPEED:
                    ParticipantMovementController movementController1 = GetComponentInParent<ParticipantMovementController>();
                    movementController1.CurrentMovementSpeed = movementController1.DefaultMovementSpeed;
                    break;

                case BuffAction.SLOW:
                    ParticipantMovementController movementController2 = GetComponentInParent<ParticipantMovementController>();
                    movementController2.CurrentMovementSpeed = movementController2.DefaultMovementSpeed;
                    break;

                case BuffAction.DENY_BOMB_PLACING:
                    ParticipantActionController bombPlacer1 = GetComponentInParent<ParticipantActionController>();
                    bombPlacer1.canPlaceBombs = true;
                    break;

                case BuffAction.UNCONTROLLABLE_BOMB_PLACING:
                    ParticipantActionController bombPlacer2 = GetComponentInParent<ParticipantActionController>();
                    bombPlacer2.UncontrollableBombPlacing = false;
                    break;
            }
        }
    }
}
