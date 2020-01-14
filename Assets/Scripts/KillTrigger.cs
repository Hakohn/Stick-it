using UnityEngine;

public class KillTrigger : MonoBehaviour
{
    [HideInInspector] public string Owner = null;

    protected void OnTriggerEnter2D(Collider2D collision)
    {
        // If it belongs to a Team, it means it is a player or a unit, and so it can be killed. We'll do it only if it is alive
        if (collision.tag.Contains("Team") && collision.GetComponent<UnitStats>().IsAlive && collision.GetComponent<UnitStats>().IsInvulnerable == false)
        {
            collision.GetComponent<UnitStats>().IsAlive = false;

            if (collision.name == Owner)
            {
                collision.GetComponent<PlayerStats>().committedSuicide = true;
            }
        }
        // If it is a buff, it will also be killed, but in a different method than the player.
        // Checking it with ToLower so that it will take in consideration both Buffs and Debuffs
        else if (collision.tag.ToLower().Contains("buff"))
        {
            collision.GetComponent<BuffPickup>().destroyedByExplosion = true;
            Destroy(collision.gameObject);
        }
    }
}
