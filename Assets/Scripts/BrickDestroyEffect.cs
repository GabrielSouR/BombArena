using System.Collections;
using UnityEngine;

public class BrickDestroyEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.4f; 

    private GameObject pendingPowerUp; // powerup escolhido pela Bomb (pode ser null)

    public void Init(GameObject powerUpPrefab)
    {
        pendingPowerUp = powerUpPrefab;
    }

    private void Start()
    {
        StartCoroutine(PlayAndFinish());
    }

    private IEnumerator PlayAndFinish()
    {
        // espera a animação terminar
        yield return new WaitForSeconds(duration);

        if (pendingPowerUp != null)
        {
            Instantiate(pendingPowerUp, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
