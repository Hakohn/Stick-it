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
    [SerializeField] private GameObject bombPrefab = null;
    [SerializeField] private LayerMask blockingLayer = 512;
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
        if (bombPlacingSound.clip != null)
            bombPlacingSound.SetAsAudioSourceToGameObject(gameObject);
    }

    private void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        participantStats = GetComponent<ParticipantStats>();

        bombTransforms = new List<Transform>();
    }

    private void Update()
    {
        // Allow input only if the player is alive
        // Also, check if the correct button has been pressed (based on our player number)
        // And also, check if we can still keep placing bombs, based on our maximum number of allowed bombs
        if (participantStats.IsAlive && (UncontrollableBombPlacing || (canPlaceBombs && Input.GetButtonDown("Place_Bomb" + participantStats.participantNumber))) && bombTransforms.Count < MaximumBombCount)
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
                bombPlacingSound.source.Play();

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
        
        // Iterate through our list of bombs, and check if any of them has exploded. If so, remove them from the list and allow some more to be placed
        // Using casual for instead of foreach so that we will not get a null pointer error once we remove an element
        for (int i = 0; i < bombTransforms.Count; i++)
        {
            if (bombTransforms[i] == null)
                bombTransforms.RemoveAt(i);
        }
    }
}