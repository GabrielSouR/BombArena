using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] private float duration = 0.3f;

    private void Start()
    {
        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            Debug.Log("Player atingido pela explosão!");
            if (other.CompareTag("Player"))
            {
                PlayerController controller = other.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.Die();
                }
            }
        }

        // PowerUp
        var powerUp = other.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            Destroy(powerUp.gameObject);
        }
    }
}
