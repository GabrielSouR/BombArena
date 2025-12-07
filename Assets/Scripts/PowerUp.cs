using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { Range, Speed, Bomb }

    public PowerUpType type;
    public float speedIncrease = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        switch (type)
        {
            case PowerUpType.Range:
                player.explosionRange++;
                AudioManager.Instance.PlayItemPickup();
                break;

            case PowerUpType.Speed:
                player.moveSpeed += speedIncrease;
                AudioManager.Instance.PlayItemPickup();
                break;

            case PowerUpType.Bomb:
                player.maxBombs++;
                AudioManager.Instance.PlayItemPickup();
                break;
        }

        Destroy(gameObject);
    }
}
