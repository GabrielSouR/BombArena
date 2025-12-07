using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;

public class Bomb : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float fuseTime = 2f;

    [Header("Explosion Settings")]
    [SerializeField] private int explosionRange = 2; // tiles pra cada lado
    [SerializeField] private GameObject explosionCenterPrefab;
    [SerializeField] private GameObject explosionMiddlePrefab;
    [SerializeField] private GameObject explosionEndPrefab;

    [Header("PowerUps")]
    [SerializeField] private GameObject powerupRangePrefab;
    [SerializeField] private GameObject powerupSpeedPrefab;
    [SerializeField] private GameObject powerupBombPrefab;
    [SerializeField] private float powerupDropChance = 0.25f; // 25% de chance de dropar

    [Header("Visual Effects")]
    [SerializeField] private GameObject destructibleDestroyPrefab;

    [Header("Push Settings")]
    [SerializeField] private float pushTileTravelTime = 0.12f; 

    private bool isRolling = false;
    private Vector2Int rollingDir;
    private Coroutine rollingRoutine;

    private Collider2D bombCollider;
    private Collider2D ownerCollider;    
    private bool solidified = false;
    private PlayerController owner;
    private bool hasExploded = false;
    private Vector2Int? blockedDirection = null; 

    // referêcias de tilemap para interação com o cenário
    private Tilemap foreground;
    private Tilemap destructible;


    private void Awake()
    {
        bombCollider = GetComponent<Collider2D>();
        if (!bombCollider)
        {
            Debug.LogWarning("Bomb: precisa de um Collider2D no prefab!");
            return;
        }

        bombCollider.isTrigger = true;
    }

    private void Start()
    {
        var levelGen = FindObjectOfType<LevelGenerator>();
        if (levelGen != null)
        {
            foreground = levelGen.foreground;
            destructible = levelGen.destructible;
        }
        else
        {
            Debug.LogWarning("Bomb: LevelGenerator não encontrado na cena.");
        }

        Invoke(nameof(Explode), fuseTime);
    }

    public void InitOwner(PlayerController owner, Collider2D ownerCol, int range)
    {
        this.owner = owner;
        this.ownerCollider = ownerCol;
        explosionRange = Mathf.Max(1, range);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!solidified && ownerCollider != null && other == ownerCollider)
        {
            bombCollider.isTrigger = false;
            solidified = true;
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Debug.Log("💥 BOOM!");

        Vector3 origin = transform.position;

        if (explosionCenterPrefab != null)
            Instantiate(explosionCenterPrefab, origin, Quaternion.identity);

        SpawnInDirection(Vector2Int.up,    origin);
        SpawnInDirection(Vector2Int.down,  origin);
        SpawnInDirection(Vector2Int.left,  origin);
        SpawnInDirection(Vector2Int.right, origin);

        if (owner != null)
        {
            owner.OnBombExploded();
        }

        Destroy(gameObject);
    }


    public void ForceExplode()
    {
        Explode();
    }

    public void ForceExplode(Vector2Int incomingDir)
    {
        blockedDirection = -incomingDir;
        Explode();
    }

    private void SpawnInDirection(Vector2Int dir, Vector3 origin)
    {
        if (explosionRange <= 0) return;

        if (blockedDirection.HasValue && dir == blockedDirection.Value)
        return;

        for (int i = 1; i <= explosionRange; i++)
        {
            Vector3 pos = origin + new Vector3(dir.x, dir.y, 0f) * i;

            if (foreground != null || destructible != null)
            {
                Tilemap refMap = destructible != null ? destructible : foreground;
                Vector3Int cell = refMap.WorldToCell(pos);

                if (foreground != null && foreground.HasTile(cell))
                {
                    break;
                }

                if (destructible != null && destructible.HasTile(cell))
                {
                    destructible.SetTile(cell, null);

                    Vector3 cellWorldPos = destructible.GetCellCenterWorld(cell);

                    GameObject chosenPowerUp = ChoosePowerUpPrefab();

                    if (destructibleDestroyPrefab != null)
                    {
                        GameObject effectObj = Instantiate(destructibleDestroyPrefab, cellWorldPos, Quaternion.identity);
                        BrickDestroyEffect effect = effectObj.GetComponent<BrickDestroyEffect>();
                        if (effect != null)
                        {
                            effect.Init(chosenPowerUp);
                        }
                    }
                    else if (chosenPowerUp != null)
                    {
                        Instantiate(chosenPowerUp, cellWorldPos, Quaternion.identity);
                    }

                    if (i > 1)
                    {
                        Vector3 previousPos = origin + new Vector3(dir.x, dir.y, 0f) * (i - 1);
                        SpawnExplosionSegment(dir, previousPos, true);
                    }

                    break;
                }
            }

            // ---------- 2) Colisores no tile: PowerUp / Bombas ----------
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.25f);

            bool blockedByPowerUp = false;
            foreach (var h in hits)
            {
                if (h == null) continue;

                var powerUp = h.GetComponent<PowerUp>();
                if (powerUp != null)
                {
                    Destroy(powerUp.gameObject);
                    blockedByPowerUp = true;
                    break;
                }
            }

            if (blockedByPowerUp)
            {
                SpawnExplosionSegment(dir, pos, true);
                break;
            }

            Bomb otherBomb = null;
            foreach (var h in hits)
            {
                if (h == null) continue;

                var b = h.GetComponent<Bomb>();
                if (b != null && b != this)
                {
                    otherBomb = b;
                    break;
                }
            }

            if (otherBomb != null)
            {
                otherBomb.ForceExplode(dir);

                SpawnExplosionSegment(dir, pos, true);
                break;
            }

            // ---------- 3) Espaço vazio: explosão continua normalmente ----------
            bool isLast = (i == explosionRange);
            SpawnExplosionSegment(dir, pos, isLast);
        }
    }

    private void SpawnExplosionSegment(Vector2Int dir, Vector3 pos, bool isEnd)
    {
        GameObject prefab = isEnd ? explosionEndPrefab : explosionMiddlePrefab;
        if (prefab == null) return;

        Quaternion rot = Quaternion.identity;

        if (dir == Vector2Int.right)
        {
            rot = Quaternion.Euler(0f, 0f, 0f);
        }
        else if (dir == Vector2Int.left)
        {
            rot = Quaternion.Euler(0f, 0f, 180f);
        }
        else if (dir == Vector2Int.up)
        {
            rot = Quaternion.Euler(0f, 0f, 90f);
        }
        else if (dir == Vector2Int.down)
        {
            rot = Quaternion.Euler(0f, 0f, -90f);
        }

        Instantiate(prefab, pos, rot);
    }

    private GameObject ChoosePowerUpPrefab()
    {
        if (Random.value > powerupDropChance) return null;

        List<GameObject> options = new List<GameObject>();
        if (powerupRangePrefab != null) options.Add(powerupRangePrefab);
        if (powerupSpeedPrefab != null) options.Add(powerupSpeedPrefab);
        if (powerupBombPrefab != null) options.Add(powerupBombPrefab);

        if (options.Count == 0) return null;

        int index = Random.Range(0, options.Count);
        return options[index];
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!solidified || hasExploded) return;

        var player = collision.collider.GetComponent<PlayerController>();
        if (player == null) return;

        Vector2 moveDir = player.LastMoveDirection;  
        if (moveDir.sqrMagnitude < 0.01f)
            return;

        Vector2 dir;
        if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y))
            dir = new Vector2(Mathf.Sign(moveDir.x), 0f);
        else
            dir = new Vector2(0f, Mathf.Sign(moveDir.y));

        TryStartRolling(Vector2Int.RoundToInt(dir));
    }

    private void TryStartRolling(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return;
        if (foreground == null) return; 

        Vector3Int currentCell = foreground.WorldToCell(transform.position);
        Vector3Int nextCell = currentCell + new Vector3Int(dir.x, dir.y, 0);

        if (IsCellBlocked(nextCell))
            return;

        rollingDir = dir;

        if (rollingRoutine != null)
            StopCoroutine(rollingRoutine);

        rollingRoutine = StartCoroutine(RollInDirection());
    }

    private bool IsCellBlocked(Vector3Int cell)
    {
        // parede indestrutível
        if (foreground != null && foreground.HasTile(cell))
            return true;

        // bloco destrutível
        if (destructible != null && destructible.HasTile(cell))
            return true;

        // checa colisores (outra bomba, player)
        if (foreground != null)
        {
            Vector3 center = foreground.GetCellCenterWorld(cell);
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, 0.25f);

            foreach (var h in hits)
            {
                if (h == null) continue;

                if (h.GetComponent<Bomb>() != null && h.gameObject != this.gameObject)
                    return true;

                if (h.GetComponent<PlayerController>() != null)
                    return true;
            }
        }

        return false;
    }

    private void DestroyPowerUpsAtCell(Vector3Int cell)
    {
        if (foreground == null) return;

        Vector3 center = foreground.GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, 0.25f);

        foreach (var h in hits)
        {
            if (h == null) continue;

            var powerUp = h.GetComponent<PowerUp>();
            if (powerUp != null)
            {
                Destroy(powerUp.gameObject);
            }
        }
    }

    private IEnumerator RollInDirection()
    {
        if (foreground == null)
            yield break;

        isRolling = true;

        while (!hasExploded)
        {
            Vector3Int currentCell = foreground.WorldToCell(transform.position);
            Vector3Int nextCell = currentCell + new Vector3Int(rollingDir.x, rollingDir.y, 0);

            if (IsCellBlocked(nextCell))
                break;

            DestroyPowerUpsAtCell(nextCell);

            Vector3 startPos = transform.position;
            Vector3 targetPos = foreground.GetCellCenterWorld(nextCell);

            float t = 0f;
            while (t < 1f && !hasExploded)
            {
                t += Time.deltaTime / pushTileTravelTime;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            if (!hasExploded)
                transform.position = targetPos;
        }

        isRolling = false;
        rollingRoutine = null;
    }
}
