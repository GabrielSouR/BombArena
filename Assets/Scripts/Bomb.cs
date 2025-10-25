using System;
using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [Header("Config")]
    public float fuseTime = 2f;
    public GameObject explosionPrefab;  // arraste seu prefab "Explosion" aqui

    public Action onFinished;           // callback pra liberar slot no player
    Animator anim;

    void OnEnable()
    {
        anim = GetComponent<Animator>();
        StartCoroutine(FuseRoutine());
    }

    IEnumerator FuseRoutine()
    {
        // espera o tempo do pavio
        yield return new WaitForSeconds(fuseTime);

        // dispara a animação de explosão
        if (anim) anim.SetTrigger("Explode");

        // instancia o prefab de explosão (que tem seu próprio Animator/vida)
        if (explosionPrefab)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // libera o slot pro player IMEDIATAMENTE ao explodir
        onFinished?.Invoke();
        onFinished = null;

        // dá um pequeno tempo pra animação de explosão da bomba (se tiver sprite próprio)
        yield return new WaitForSeconds(0.1f);

        Destroy(gameObject);
    }
}
