using UnityEngine;

[ExecuteAlways]
public class SpriteYOffset : MonoBehaviour
{
    [Tooltip("Offset vertical em unidades do mundo (positivo = sobe, negativo = desce)")]
    public float yOffset = -0.5f;

    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Apply()
    {
        // Garante que só mova o filho 'Visual' no modo Play ou Editor ativo
        if (Application.isPlaying)
            transform.localPosition = new Vector3(0f, yOffset, 0f);
        else
            transform.localPosition = originalPosition; // mantém original no modo de edição
    }
}
