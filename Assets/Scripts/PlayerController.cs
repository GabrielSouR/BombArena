using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public LayerMask obstacleMask;

    // folga para não colar na parede
    const float SKIN = 0.01f;

    Rigidbody2D rb;
    Collider2D col;
    Animator animator;

    Vector2 moveInput;
    Vector2 lastMoveDir = Vector2.down;

    ContactFilter2D filter;
    RaycastHit2D[] hits = new RaycastHit2D[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponentInChildren<Animator>();

        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = obstacleMask,
            useTriggers = false
        };
    }

    // Input System (PlayerInput → Send Messages / Invoke Unity Events)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        Vector2 desiredDelta = moveInput.normalized * moveSpeed * Time.fixedDeltaTime;

        Vector2 newPos = rb.position;

        // X
        float movedX = 0f;
        if (Mathf.Abs(desiredDelta.x) > 0f)
        {
            Vector2 dir = new Vector2(Mathf.Sign(desiredDelta.x), 0f);
            float dist = Mathf.Abs(desiredDelta.x) + SKIN;
            int count = col.Cast(dir, filter, hits, dist);
            if (count > 0)
            {
                float min = hits[0].distance;
                for (int i = 1; i < count; i++)
                    if (hits[i].distance < min) min = hits[i].distance;

                float allowed = Mathf.Max(0f, min - SKIN);
                movedX = allowed * Mathf.Sign(desiredDelta.x);
            }
            else
            {
                movedX = desiredDelta.x;
            }
            newPos.x += movedX;
        }

        // Y
        float movedY = 0f;
        if (Mathf.Abs(desiredDelta.y) > 0f)
        {
            Vector2 dir = new Vector2(0f, Mathf.Sign(desiredDelta.y));
            float dist = Mathf.Abs(desiredDelta.y) + SKIN;
            int count = col.Cast(dir, filter, hits, dist);
            if (count > 0)
            {
                float min = hits[0].distance;
                for (int i = 1; i < count; i++)
                    if (hits[i].distance < min) min = hits[i].distance;

                float allowed = Mathf.Max(0f, min - SKIN);
                movedY = allowed * Mathf.Sign(desiredDelta.y);
            }
            else
            {
                movedY = desiredDelta.y;
            }
            newPos.y += movedY;
        }

        rb.MovePosition(newPos);

        // animação
        bool moved = (Mathf.Abs(movedX) > 0.00001f) || (Mathf.Abs(movedY) > 0.00001f);
        Vector2 animDir = Vector2.zero;
        if (Mathf.Abs(movedX) > Mathf.Abs(movedY)) animDir = new Vector2(Mathf.Sign(movedX), 0f);
        else if (Mathf.Abs(movedY) > 0f)          animDir = new Vector2(0f, Mathf.Sign(movedY));

        if (animator)
        {
            animator.SetFloat("MoveX", animDir.x);
            animator.SetFloat("MoveY", animDir.y);

            if (moved)
            {
                lastMoveDir = animDir == Vector2.zero ? lastMoveDir : animDir;
                animator.SetFloat("LastX", lastMoveDir.x);
                animator.SetFloat("LastY", lastMoveDir.y);
            }

            animator.SetBool("IsMoving", moved);
        }
    }
}
