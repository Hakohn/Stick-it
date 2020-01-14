using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombController : MonoBehaviour
{
    // Component references
    private Rigidbody2D rb2d = null;
    private BoxCollider2D boxCollider = null;

    // Bomb related and tilemaps
    public string Owner = null;
    private Vector3Int originCell;
    [HideInInspector] public TileBase DestructibleTile = null;
    [HideInInspector] public Tilemap Tilemap = null;
    [HideInInspector] public int ExplosionRadius = 1;
    [SerializeField] private float timer = 2f;
    [SerializeField] private LayerMask blockingLayer = 512;
    [System.Serializable] public class Explosion
    {
        public GameObject center = null;
        public GameObject loop = null;
        public GameObject finish = null;
    } [SerializeField] private Explosion explosionPrefab = null;


    // Buff related
    [SerializeField] private GameObject[] buffPrefabs = null;

    // Sound variables
    [SerializeField] private Sound[] explosionSounds = null;

    private void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        originCell = Tilemap.WorldToCell(rb2d.position);
    }

    /// <summary>
    /// Checks if it hits another bomb with the same tag; returns true if it does, false otherwise.
    /// </summary>
    private bool HitsAnotherSameTag(Vector2 direction, float distance)
    {
        boxCollider.enabled = false;
        RaycastHit2D hit = Physics2D.Raycast(rb2d.position, direction, distance, blockingLayer);
        boxCollider.enabled = true;

        if (hit.collider != null && hit.collider.tag == gameObject.tag)
        {
            // Causes a chain reaction
            hit.collider.GetComponent<BombController>().SpeedUpDetonation();
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Destroys the targeted cell. Returns true if the explosion can continue in that direction; false otherwise
    /// </summary>
    private bool ExplodeTile(Vector3Int targetCell, float angle = 0)
    {
        TileBase tile = Tilemap.GetTile<TileBase>(targetCell);

        GameObject effectToInstantiate = null;
        Vector3Int distanceVector = targetCell - originCell;
        distanceVector.x = Mathf.Abs(distanceVector.x);
        distanceVector.y = Mathf.Abs(distanceVector.y);
        int distanceToTargetCell = Mathf.Max(distanceVector.x, distanceVector.y);

        // The effect, based on the distance from origin
        if (distanceToTargetCell == 0) effectToInstantiate = explosionPrefab.center;
        else if (distanceToTargetCell == ExplosionRadius) effectToInstantiate = explosionPrefab.finish;
        else effectToInstantiate = explosionPrefab.loop;


        if (tile == DestructibleTile)
        {
            Tilemap.SetTile(targetCell, null);

            // Updating the effect, if hitting a destroyable tile (and so the explosion must finish)
            effectToInstantiate = explosionPrefab.finish;
            GameObject auxEffObj = Instantiate(effectToInstantiate, Tilemap.GetCellCenterWorld(targetCell), Quaternion.Euler(new Vector3(0, 0, angle)));

            // Make it non-lethal in the area where it just destroyed a destructible tile
            Destroy(auxEffObj.GetComponent<TimedKillTrigger>());

            // Try spawning a random buff
            if(Random.Range(0, 100) <= 20)
            {
                // This means now we should spawn a buff. If the player is unlucky, there is a small chance that a debuff will be spawned instead of a buff
                int buffSpawnChance = 75;
                GameObject buffToInstantiate = null;
                // Do this until we get a buff
                if (Random.Range(0, 100) <= buffSpawnChance)
                    do
                    {
                        buffToInstantiate = buffPrefabs[Random.Range(0, buffPrefabs.Length)];
                    } while (!buffToInstantiate.tag.Contains("Buff"));
                else // do this until we get a debuff
                    do
                    {
                        buffToInstantiate = buffPrefabs[Random.Range(0, buffPrefabs.Length)];
                    } while (!buffToInstantiate.tag.Contains("Debuff"));


                Instantiate(buffToInstantiate, Tilemap.GetCellCenterWorld(targetCell), Quaternion.identity);
            }
            return false;
        }
        else if (tile == null)
        {
            GameObject auxEffObj = Instantiate(effectToInstantiate, Tilemap.GetCellCenterWorld(targetCell), Quaternion.Euler(new Vector3(0, 0, angle)));
            auxEffObj.GetComponent<TimedKillTrigger>().Owner = Owner;
            return true;
        }
        else // Indestructible
            return false;
    }


    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            // Plays one random the sounds using the audio manager, so that the sound won't be bugged out once this object gets destroyed
            AudioManager.instance.CreateSoundObject(explosionSounds[Random.Range(0, explosionSounds.Length)], transform.position);
            
            // Destroy the cells in each direction, unless it hits another bomb
            ExplodeTile(originCell, 0);    // ORIGIN CELL
            
            for (int i =  1; i <=  ExplosionRadius && !HitsAnotherSameTag(Vector2.up,    Mathf.Abs(i)) && ExplodeTile(originCell + new Vector3Int(0, i, 0),  90); i++);  // UP
            for (int i = -1; i >= -ExplosionRadius && !HitsAnotherSameTag(Vector2.left,  Mathf.Abs(i)) && ExplodeTile(originCell + new Vector3Int(i, 0, 0), 180); i--);  // LEFT
            for (int i = -1; i >= -ExplosionRadius && !HitsAnotherSameTag(Vector2.down,  Mathf.Abs(i)) && ExplodeTile(originCell + new Vector3Int(0, i, 0), 270); i--);  // DOWN
            for (int i =  1; i <=  ExplosionRadius && !HitsAnotherSameTag(Vector2.right, Mathf.Abs(i)) && ExplodeTile(originCell + new Vector3Int(i, 0, 0),   0); i++);  // RIGHT
            
            Destroy(gameObject);
        }
    }

    public void SpeedUpDetonation()
    {
        timer /= 2;
    }
}