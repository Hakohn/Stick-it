using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    // Component references
    private Rigidbody2D rb2d = null;
    private BoxCollider2D boxCollider = null;
    private PlayerStats playerStats = null;

    // Movement information variables
    private enum MovementMethod { Normal, Tile_Based }
#pragma warning disable IDE0044 // Add readonly modifier
    [SerializeField] private MovementMethod movementMethod = MovementMethod.Normal;
#pragma warning restore IDE0044 // Add readonly modifier
    public Vector2 DestinationTilePosition { get; private set; }
    public Vector2 Direction { get; private set; } = Vector2.down;
    public bool IsMoving { get; private set; } = false;
    public float DefaultMovementSpeed = 3f;
    public float CurrentMovementSpeed { get; set; } = 0f;

    // Collision variables
    [SerializeField] private LayerMask blockingLayer = 512;
    [HideInInspector] public List<Transform> transformsThatAllowCollision = null;

    private void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        playerStats = GetComponent<PlayerStats>();

        CurrentMovementSpeed = DefaultMovementSpeed;
        DestinationTilePosition = rb2d.position;
    }

    private bool AttemptMoving()
    {
        // The variables we're gonna use, no matter what kind of MovementMethod we're using.
        var moveDistance = 1f;
        var ableToMove = true;

        // Calculate the distance till our destination point, based on our MovementMethod.
        switch (movementMethod)
        {
            case MovementMethod.Normal:
                moveDistance = CurrentMovementSpeed * Time.fixedDeltaTime;
                break;

            case MovementMethod.Tile_Based:
                moveDistance = 1f;    
                break;
        }

        // Check collision with all the objects found on our way to our destination point.
        boxCollider.enabled = false;
        var hits = Physics2D.BoxCastAll(rb2d.position, boxCollider.size, transform.rotation.z, Direction, moveDistance, blockingLayer);
        boxCollider.enabled = true;

        // Clear the transforms if nothing is hit; This certainly means we're not colliding with a spawned bomb of ours, 
        // so we do this to avoid keeping in transforms of despawned bombs and bombs that no longer should allow us to pass through them.
        if (hits.Length == 0)
        {
            transformsThatAllowCollision.Clear();
        }

        // Calculate an auxiliary destinationTilePosition, for collision check purposes
        var auxDestinationPosition = new Vector2(DestinationTilePosition.x, DestinationTilePosition.y);
        if(rb2d.position == auxDestinationPosition)
        {
            auxDestinationPosition = rb2d.position + Direction.normalized * moveDistance;
        }

        // Iterate through the hit colliders. If any of the hit colliders does not belong to the transformsThatAllowCollision,
        // it means it is a collider that does NOT allow us to pass through it.
        foreach (RaycastHit2D hit in hits)
        {
            if (transformsThatAllowCollision.Contains(hit.transform) == false)
            {
                ableToMove = false;
                break;
            }
            
            var objectMovementController = hit.transform.GetComponent<PlayerMovementController>();
            if (objectMovementController != null && objectMovementController.DestinationTilePosition == auxDestinationPosition)
            {
                ableToMove = false;
                break;
            }
        }

        // If there isn't anything blocking our movement, then update our destination point.
        if (ableToMove)
        {
            DestinationTilePosition = auxDestinationPosition;
        }

        // Move towards our destiation point.
        rb2d.position = Vector2.MoveTowards(rb2d.position, DestinationTilePosition, CurrentMovementSpeed * Time.fixedDeltaTime);

        // Return the boolean that shows if we're able to move
        return ableToMove;
    }

    private void FixedUpdate()
    {
        // Allow input only if the player is alive
        if (playerStats.IsAlive)
        {
            Vector2 input;
            switch (movementMethod)
            {
                case MovementMethod.Normal:
                    // Input based on our player number
                    // Allow input and attempt moving, no matter if we've reached our destination point or not. We're able to move freely after all, right?
                    input = new Vector2(Input.GetAxisRaw("Horizontal" + playerStats.playerNumber), Input.GetAxisRaw("Vertical" + playerStats.playerNumber));

                    if (input != Vector2.zero)
                    {
                        Direction = input;
                        IsMoving = true;
                        AttemptMoving();
                    }
                    else IsMoving = false;
                    break;
                case MovementMethod.Tile_Based:
                    // Allow getting input ONLY if we've reached our destination point
                    if (rb2d.position == DestinationTilePosition)
                    {
                        // Input based on our player number
                        input = new Vector2(Input.GetAxisRaw("Horizontal" + playerStats.playerNumber), Input.GetAxisRaw("Vertical" + playerStats.playerNumber));

                        if (input != Vector2.zero)
                        {
                            Direction = input;
                            IsMoving = true;
                        }
                        else IsMoving = false;
                    }
                    
                    // If we detect input, try moving in that direction
                    if (IsMoving) AttemptMoving();
                    break;
            }
        }
    }
}