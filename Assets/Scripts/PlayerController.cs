using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public LayerMask obstacleMask;

    [Header("Bomb Settings")]
    public GameObject bombPrefab;
    public int explosionRange = 2;   
    public int maxBombs = 1;         
    private int activeBombs = 0;     
    private bool isDead = false;

    // folga para não colar na parede
    const float SKIN = 0.01f;

    PlayerInput playerInput;

    Rigidbody2D rb;
    Collider2D col;
    Animator animator;

    Vector2 moveInput;
    Vector2 lastMoveDir = Vector2.down;
    public Vector2 LastMoveDirection => lastMoveDir;

    ContactFilter2D filter;
    RaycastHit2D[] hits = new RaycastHit2D[8];

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
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

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnBomb(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;   
        PlaceBomb();
    }

    void PlaceBomb()
    {

        if (PauseManager.GameIsPaused)
            return;
            
        if (!bombPrefab)
        {
            Debug.LogWarning("PlayerController: bombPrefab não atribuído.");
            return;
        }

        if (activeBombs >= maxBombs)
        {
            return;
        }

        // Centraliza a bomba no tile mais próximo
        Vector3 pos = transform.position;

        float cellX = Mathf.Round(pos.x - 0.5f) + 0.5f;
        float cellY = Mathf.Round(pos.y - 0.5f) + 0.5f;

        Vector3 spawnPos = new Vector3(cellX, cellY, 0f);
        Vector2 checkPos = new Vector2(spawnPos.x, spawnPos.y);

        // não deixar colocar bomba em tile que já tem bomba
        Collider2D[] hits = Physics2D.OverlapPointAll(checkPos);
        foreach (var h in hits)
        {
            if (h != null && h.GetComponent<Bomb>() != null)
            {
                // já existe uma bomba nesse tile
                return;
            }
        }

        GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        AudioManager.Instance.PlayBombPlace();

        Bomb bomb = bombObj.GetComponent<Bomb>();
        if (bomb != null && col != null)
        {
            activeBombs++;
            bomb.InitOwner(this, col, explosionRange);
        }
    }

    void FixedUpdate()
    {
        Vector2 desiredDelta = moveInput.normalized * moveSpeed * Time.fixedDeltaTime;

        Vector2 newPos = rb.position;

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

    public void OnBombExploded()
    {
        activeBombs = Mathf.Max(0, activeBombs - 1);
        AudioManager.Instance.PlayExplosion();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // para movimento
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        var input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null)
            input.enabled = false;

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        // dispara animação de morte
        if (animator != null)
            animator.SetBool("IsDead", true);   
            animator.SetTrigger("die");
            AudioManager.Instance.PlayPlayerDeath();
    }

    public void OnDeathAnimationEnd()
    {
        Debug.Log($"[PlayerController] OnDeathAnimationEnd chamado em {gameObject.name}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied(this);
        }
        else
        {
            Debug.LogWarning("[PlayerController] GameManager.Instance é null!");
        }

        // garante que esse player não será mais mexido pela física
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        enabled = false;

    }

    public void Freeze()
    {
        Debug.Log($"[PlayerController] Freeze em {gameObject.name}");

        // trava física
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        var input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null)
            input.enabled = false;
    }

    public void SetControlsEnabled(bool enabled)
    {
        if (playerInput != null)
            playerInput.enabled = enabled;

        if (!enabled && rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}
