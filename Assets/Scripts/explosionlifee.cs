using UnityEngine;

public class ExplosionLifee : MonoBehaviour
{
    public float extraLifetime = 0f; // 0–0.1 só se quiser garantir fim do fade

    void OnEnable()
    {
        float len = 0.2f;
        var anim = GetComponent<Animator>();
        if (anim && anim.runtimeAnimatorController && anim.runtimeAnimatorController.animationClips.Length > 0)
        {
            len = anim.runtimeAnimatorController.animationClips[0].length;
        }
        Destroy(gameObject, len + extraLifetime);
    }
}
