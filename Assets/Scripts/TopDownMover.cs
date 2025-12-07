using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMover : MonoBehaviour
{
    [Header("Input (troque para P2 etc.)")]
    public KeyCode upKey    = KeyCode.W;
    public KeyCode downKey  = KeyCode.S;
    public KeyCode leftKey  = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Header("Movimento")]
    public float moveSpeed = 5f;
    public bool fourDirectionsOnly = true;   
    public float deadZone = 0.1f;            

    [Header("Animator (filho Visual)")]
    public Animator animator;                
    private Rigidbody2D rb;
    private Vector2 input;                   
    private Vector2 lastDir = Vector2.down;  

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    void Update()
    {
        // — leitura de teclas —
        float x = 0f, y = 0f;
        if (Input.GetKey(leftKey))  x = -1f;
        if (Input.GetKey(rightKey)) x =  1f;
        if (Input.GetKey(downKey))  y = -1f;
        if (Input.GetKey(upKey))    y =  1f;

        input = new Vector2(x, y);

        // — trava em 4 direções (eixo dominante) —
        if (fourDirectionsOnly)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                input = new Vector2(Mathf.Sign(input.x), 0f);
            else
                input = new Vector2(0f, Mathf.Sign(input.y));
        }

        // normaliza e aplica deadzone
        input = (input.sqrMagnitude > deadZone * deadZone) ? input.normalized : Vector2.zero;

        bool isMoving = input.sqrMagnitude > 0.0001f;
        if (isMoving) lastDir = input; // guarda última direção “viva”

        // — alimenta o Animator —
        if (animator)
        {
            Vector2 animDir = isMoving ? input : lastDir; 
            animator.SetBool("IsMoving", isMoving);
            animator.SetFloat("MoveX", animDir.x);
            animator.SetFloat("MoveY", animDir.y);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }
}
