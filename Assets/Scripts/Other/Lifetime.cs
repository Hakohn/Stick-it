using UnityEngine;

public class Lifetime : MonoBehaviour
{
    [SerializeField] private float seconds = 1.5f;

    public float Seconds { get => seconds; set => seconds = value; }

    // Update is called once per frame
    void FixedUpdate()
    {
        seconds -= Time.fixedDeltaTime;
        if (seconds <= 0)
            Destroy(gameObject);
    }
}
