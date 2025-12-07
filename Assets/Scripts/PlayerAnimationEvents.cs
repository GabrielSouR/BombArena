using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerController owner;

    private void Awake()
    {
        owner = GetComponentInParent<PlayerController>();
    }

    public void OnDeathAnimationEnd()
    {
        if (owner != null)
        {
            owner.OnDeathAnimationEnd();
        }
    }
}
