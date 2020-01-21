using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ParticipantActionController : MonoBehaviour
{
    // Component references
    private Rigidbody2D rb2d = null;
    private BoxCollider2D boxCollider = null;
    private ParticipantStats participantStats = null;

    // Bomb placing and tilemaps
    private List<Transform> bombTransforms = null;
    private bool touchBombRequested = false;
    [SerializeField] private GameObject bombPrefab = null;
#pragma warning disable IDE0052 // Remove unread private members
    [SerializeField] private LayerMask blockingLayer = 512;
#pragma warning restore IDE0052 // Remove unread private members
    [HideInInspector] public Tilemap Tilemap = null;
    [HideInInspector] public TileBase DestructibleTile = null;

    // Buff related variables
    [HideInInspector] public bool canPlaceBombs = true;
    [HideInInspector] public bool UncontrollableBombPlacing = false;
    [Range(1, 5)] public int explosionRadius = 1;
    [Range(1, 8)] public int MaximumBombCount = 1;

    // Sound variables
    [SerializeField] private Sound bombPlacingSound = null;

    private void Awake()
    {
        // Using this method only for sounds that are going to be played multiple times while the gameObject is alive
        bombPlacingSound.SetAudioSource(gameObject);
    }

    private void Start()
    {
        // Grab the required components
        rb2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        participantStats = GetComponent<ParticipantStats>();

        // Check if this is the main player
        if (participantStats.IsMainPlayer)
        {
            // If it is, then add the required listeners to the appropriate buttons.
            InterfaceHolder.instance.BombButton.onClick.AddListener(() => touchBombRequested = true);
        }

        // Create the bomb transform list
        bombTransforms = new List<Transform>();
    }

    private void PlaceBomb()
    {
        // The middle of the tile where we want our bomb to spawn
        Vector2 worldSpawnPos = Tilemap.GetCellCenterWorld(Tilemap.WorldToCell(rb2d.position));

        // Check if that certain tile is open and available for bomb placement
        boxCollider.enabled = false;
        Vector2 direction = (worldSpawnPos - rb2d.position).normalized;
        float distance = Mathf.Sqrt(Mathf.Pow(worldSpawnPos.x - rb2d.position.x, 2) + Mathf.Pow(worldSpawnPos.y - rb2d.position.y, 2));
        RaycastHit2D hit = Physics2D.Raycast(rb2d.position, direction, distance);
        boxCollider.enabled = true;

        // If it is null, it means our path for bomb placing is open, and we're gonna instantiate the bomb and initialize it
        if (hit.collider == null)
        {
            bombPlacingSound.Play();

            GameObject spawnedBomb = Instantiate(bombPrefab, worldSpawnPos, Quaternion.identity) as GameObject;
            spawnedBomb.GetComponent<BombController>().Tilemap = Tilemap;
            spawnedBomb.GetComponent<BombController>().ExplosionRadius = explosionRadius;
            spawnedBomb.GetComponent<BombController>().DestructibleTile = DestructibleTile;
            spawnedBomb.GetComponent<BombController>().Owner = gameObject.name;

            // Allow the player to move through the currently placed bomb, so that we won't get stuck in it.
            gameObject.GetComponent<ParticipantMovementController>().transformsThatAllowCollision.Add(spawnedBomb.transform);

            // Add the spawned bomb to our list of placed bombs, so that we keep track of how many bombs we've already placed
            bombTransforms.Add(spawnedBomb.transform);
        }
    }

    private void Update()
    {
        // Attempt placing bombs only if it is possible to do so 
        // (based on our life status and maximum number of allowed bombs)
        if (participantStats.IsAlive && canPlaceBombs && bombTransforms.Count < MaximumBombCount)
        {
            // Allow input only if the participant is alive and can freely place bombs
            // Also, check if the correct button has been pressed (if applicable)
            if(UncontrollableBombPlacing)
            {
                PlaceBomb();
            }
            else
            {
                bool input = false;
                switch (participantStats.ControlType)
                {
                    case ParticipantStats.TypeOfControl.Player:
                        // If it's controlled by a player, let's check what type of
                        // control is he gonna use.
                        if(InterfaceHolder.instance.areTouchControlsEnabled)
                        {
                            input = touchBombRequested;
                        }
                        else
                        {
                            input = Input.GetButtonDown("Place_Bomb" + participantStats.participantNumber);
                        }
                        break;
                    case ParticipantStats.TypeOfControl.AI:
                        // Not implemented... yet!
                        break;
                }

                if(input)
                {
                    PlaceBomb();
                }
                // Bomb placement attempted, so set the un-request the bomb
                touchBombRequested = false;
            }
        }
        
        // Iterate through our list of bombs, and check if any of them has exploded. If so, remove them from the list and allow some more to be placed
        // Using casual for instead of foreach so that we will not get a null pointer error once we remove an element
        for (int i = 0; i < bombTransforms.Count; i++)
        {
            if (bombTransforms[i] == null)
                bombTransforms.RemoveAt(i);
        }
    }
}