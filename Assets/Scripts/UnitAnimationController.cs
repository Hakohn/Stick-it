using UnityEngine;

public class UnitAnimationController : MonoBehaviour
{
    private Animator animator;
    private UnitStats unitStats;
    private PlayerMovementController movementController;

    private void Start()
    {
        animator = GetComponent<Animator>();
        movementController = GetComponent<PlayerMovementController>();
        unitStats = GetComponent<UnitStats>();
    }

    private void Update()
    {
        animator.SetFloat("Horizontal", movementController.Direction.x);
        animator.SetFloat("Vertical", movementController.Direction.y);
        animator.SetBool("IsAlive", unitStats.IsAlive);

        for (int i = 0; i < animator.layerCount; i++)
            animator.SetLayerWeight(i, 0);

        if (unitStats.IsAlive == false) animator.SetLayerWeight(animator.GetLayerIndex("DeathLayer"), 1);
        else if (movementController.IsMoving) animator.SetLayerWeight(animator.GetLayerIndex("WalkLayer"), 1);
        else animator.SetLayerWeight(animator.GetLayerIndex("IdleLayer"), 1);
    }
}
