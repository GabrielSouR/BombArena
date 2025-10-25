using UnityEngine;

public class ExplosionLife : MonoBehaviour
{
    public float totalLifetime = 2f; // 2s no total
    private Animator anim;

    void Awake() => anim = GetComponent<Animator>();

    void OnEnable() => StartCoroutine(Run());

    System.Collections.IEnumerator Run()
    {
        // Toca Start -> entra no loop Mid
        yield return new WaitForSeconds(totalLifetime);
        anim.SetTrigger("End");
        // O Destroy pode vir por Animation Event no último frame do End,
        // ou aqui aguardando o End terminar:
        yield return null; // opcional, se usar Animation Event para destruir
    }

    // Chame isso no último frame do clip ExplosionEnd (Animation Event)
    public void Kill() => Destroy(gameObject);
}
