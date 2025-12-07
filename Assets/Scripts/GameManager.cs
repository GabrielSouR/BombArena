using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public LevelGenerator level;      
    public Tilemap foreground;        
    public List<GameObject> playerPrefabs = new List<GameObject>(); 

    [Header("Players")]
    [Range(1,2)] public int playerCount = 2;
    public bool regenerateLevelOnStart = false; // gera o tabuleiro no Start

    [Header("Tempo de partida")]
    [SerializeField] private float matchDurationSeconds = 180f; 
    [SerializeField] private bool startSuddenDeathOnTimer = true;

    [Header("Morte súbita")]
    [SerializeField] private Tilemap solidTilemap;          
    [SerializeField] private TileBase suddenDeathTile;      

    [Header("Pre-game")]
    [SerializeField] private float preGameDelaySeconds = 3f;
    [SerializeField] private GameObject preGamePanel;

    private bool gameStarted = false;

    private float timer;
    private bool matchOver = false;
    private bool suddenDeathStarted = false;
    private Coroutine suddenDeathRoutine;
    private float lastBlockTime = -1f;

    [SerializeField] private float suddenDeathWarningSeconds = 5f;
    private bool suddenDeathWarningPlayed = false;

    private List<Vector3Int> suddenDeathCells;

    private readonly Vector3Int[] cornerCellsOrder = new Vector3Int[2];
    private readonly List<Transform> spawnedPlayers = new List<Transform>();

    private bool endMatchRoutineStarted = false;
    private readonly List<PlayerController> deadPlayers = new List<PlayerController>();

    public float CurrentTimer => Mathf.Max(0f, timer);
    public bool SuddenDeathStarted => suddenDeathStarted;
    public bool MatchOver => matchOver;


    [SerializeField] private float gameOverDelay = 3f;       
    [SerializeField] private float doubleKillWindow = 0.25f; 

    public static GameResult LastResult = GameResult.Draw;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {

        matchOver = false;
        endMatchRoutineStarted = false;
        deadPlayers.Clear();
        LastResult = GameResult.Draw;

        if (!level || !foreground)
        {
            Debug.LogError("Assign LevelGenerator and Foreground tilemap.");
            return;
        }

        if (regenerateLevelOnStart)
            level.GenerateLevel();

        int innerW = Mathf.Max(level.outerWidth  - 2, 1);
        int innerH = Mathf.Max(level.outerHeight - 2, 1);

        cornerCellsOrder[0] = new Vector3Int(0,         0,         0);
        cornerCellsOrder[1] = new Vector3Int(innerW-1,  innerH-1,  0);

        SpawnPlayers();

        timer = matchDurationSeconds;
        suddenDeathWarningPlayed = false;
        gameStarted = false;

        StartCoroutine(PreGameRoutine());

        if (solidTilemap != null)
        {
            BuildSuddenDeathOrder();
        }
        else
        {
            Debug.LogWarning("GameManager: solidTilemap não atribuído para a morte súbita.");
        }
    }

    private void Update()
    {
        if (!gameStarted) return;

        if (!startSuddenDeathOnTimer || matchOver) return;

        if (!suddenDeathStarted)
        {
            timer -= Time.deltaTime;

            if (!suddenDeathWarningPlayed && timer <= suddenDeathWarningSeconds)
            {
                suddenDeathWarningPlayed = true;
                AudioManager.Instance.PlaySiren();
            }

            if (timer <= 0f)
            {
                StartSuddenDeath();
            }
        }
    }

    [ContextMenu("Forçar morte súbita agora")]
    public void StartSuddenDeath()
    {           
        if (suddenDeathStarted || matchOver) return;

        suddenDeathStarted = true;

        if (!suddenDeathWarningPlayed)
        {
            suddenDeathWarningPlayed = true;
            AudioManager.Instance.PlaySiren();
        }

        AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmSuddenDeath);

        if (suddenDeathCells == null || suddenDeathCells.Count == 0)
        {
            BuildSuddenDeathOrder();
        }

        suddenDeathRoutine = StartCoroutine(SuddenDeathCoroutine());
    }

    private void BuildSuddenDeathOrder()
    {
        suddenDeathCells = new List<Vector3Int>();

        if (solidTilemap == null) return;

        BoundsInt bounds = solidTilemap.cellBounds;

        int left   = bounds.xMin;
        int right  = bounds.xMax - 1;
        int bottom = bounds.yMin;
        int top    = bounds.yMax - 1;

        void AddIfEmpty(int x, int y)
        {
            var cell = new Vector3Int(x, y, 0);
            if (!solidTilemap.HasTile(cell))
            {
                suddenDeathCells.Add(cell);
            }
        }

        while (left <= right && bottom <= top)
        {
            // topo: esquerda -> direita
            for (int x = left; x <= right; x++)
                AddIfEmpty(x, top);

            // lado direito: topo-1 -> baixo
            for (int y = top - 1; y >= bottom; y--)
                AddIfEmpty(right, y);

            // base: direita-1 -> esquerda (se ainda tiver linha diferente)
            if (top > bottom)
            {
                for (int x = right - 1; x >= left; x--)
                    AddIfEmpty(x, bottom);
            }

            // lado esquerdo: baixo+1 -> topo-1 (se ainda tiver coluna diferente)
            if (right > left)
            {
                for (int y = bottom + 1; y < top; y++)
                    AddIfEmpty(left, y);
            }

            left++;
            right--;
            top--;
            bottom++;
        }

        Debug.Log($"Morte súbita: ordem de {suddenDeathCells.Count} células gerada em espiral.");
    }

    private IEnumerator SuddenDeathCoroutine()
    {
        if (solidTilemap == null || suddenDeathCells == null) yield break;

        foreach (var cell in suddenDeathCells)
        {
            if (matchOver) yield break;

            if (lastBlockTime < 0)
            {
                lastBlockTime = Time.time;
            }
            else
            {
                float delta = Time.time - lastBlockTime;
                lastBlockTime = Time.time;
                Debug.Log($"[SuddenDeath] Tempo entre blocos: {delta:F2}s");
            }

            // posição do centro do tile
            Vector3 worldPos = solidTilemap.GetCellCenterWorld(cell);

            // mata / limpa o que estiver nesse tile
            Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.25f);
            foreach (var h in hits)
            {
                if (h == null) continue;

                // player
                var player = h.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.Die();
                    continue;
                }

                // bomba -> explode
                var bomb = h.GetComponent<Bomb>();
                if (bomb != null)
                {
                    bomb.ForceExplode();
                    continue;
                }

                // powerup -> some
                var powerUp = h.GetComponent<PowerUp>();
                if (powerUp != null)
                {
                    Destroy(powerUp.gameObject);
                    continue;
                }
            }

            // coloca o bloco indestrutível
            AudioManager.Instance.PlayBlockFall();
            solidTilemap.SetTile(cell, suddenDeathTile);

            yield return new WaitForSeconds(0.35f);
        }
    }

    private void SpawnPlayers()
    {
        spawnedPlayers.Clear();

        for (int i = 0; i < playerCount && i < cornerCellsOrder.Length; i++)
        {
            Vector3Int cell = cornerCellsOrder[i];
            Vector3 posWorld = foreground.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

            GameObject prefab = GetPrefabForIndex(i);
            if (!prefab)
            {
                Debug.LogError($"Player prefab for index {i} not set.");
                continue;
            }

            var go = Instantiate(prefab, posWorld, Quaternion.identity, transform);
            go.name = $"Player_{i+1}";

            // garante centro perfeito da célula e zera velocidade
            var rb = go.GetComponent<Rigidbody2D>();
            Vector3 center = foreground.GetCellCenterWorld(cornerCellsOrder[i]); // centro da célula
            if (rb) {
                rb.position = center;
                rb.linearVelocity = Vector2.zero;
            } else {
                go.transform.position = center; 
            }

            spawnedPlayers.Add(go.transform);
        }

        Debug.Log($"Spawned {spawnedPlayers.Count} player(s).");
    }

    private GameObject GetPrefabForIndex(int i)
    {
        if (playerPrefabs == null || playerPrefabs.Count == 0) return null;
        if (i < playerPrefabs.Count && playerPrefabs[i] != null) return playerPrefabs[i];

        return playerPrefabs[0];
    }

    public void OnPlayerDied(PlayerController deadPlayer)
    {
        if (deadPlayer == null) return;

        // registra o morto
        if (!deadPlayers.Contains(deadPlayer))
            deadPlayers.Add(deadPlayer);

        Debug.Log($"[GameManager] OnPlayerDied chamado para {deadPlayer.gameObject.name} no tempo {Time.time}");

        // na primeira morte: marca fim de partida, para morte súbita e congela todos
        if (!matchOver)
        {
            matchOver = true;

            if (suddenDeathRoutine != null)
                StopCoroutine(suddenDeathRoutine);

            var players = FindObjectsOfType<PlayerController>();
            foreach (var p in players)
            {
                if (p != null)
                    p.Freeze();
            }
        }

        // garante que só UMA coroutine vai decidir o resultado
        if (!endMatchRoutineStarted)
        {
            endMatchRoutineStarted = true;
            StartCoroutine(HandleEndMatchAfterDelay());
        }
    }

    private IEnumerator HandleEndMatchAfterDelay()
    {
        // 1) Espera a janela de morte dupla
        yield return new WaitForSeconds(doubleKillWindow);

        var players = FindObjectsOfType<PlayerController>();

        int aliveCount = 0;
        PlayerController lastAlive = null;

        foreach (var p in players)
        {
            if (p == null) continue;

            bool isDead = deadPlayers.Contains(p);

            if (!isDead)
            {
                aliveCount++;
                lastAlive = p;
            }
        }

        string winnerName;

        if (aliveCount == 0)
        {
            // empate
            winnerName = "";
            LastResult = GameResult.Draw;
            Debug.Log("[GameManager] Empate detectado (morte dupla).");
        }
        else if (aliveCount == 1)
        {
            // vitória de quem sobrou vivo
            winnerName = lastAlive.gameObject.name.Replace("_", " ");
            Debug.Log($"[GameManager] Vencedor: {winnerName}");

            if (lastAlive.gameObject.name.Contains("1"))
                LastResult = GameResult.Player1Win;
            else
                LastResult = GameResult.Player2Win;
        }
        else
        {
            // cenário estranho (ninguém morreu / mais de um vivo) -> trata como empate
            winnerName = "";
            LastResult = GameResult.Draw;
            Debug.LogWarning("[GameManager] Estado inesperado, mais de um vivo no fim. Considerando empate.");
        }

        // 2) Espera o resto do delay até trocar pra tela de GameOver
        float remaining = Mathf.Max(0f, gameOverDelay - doubleKillWindow);
        yield return new WaitForSeconds(remaining);

        EndMatch(winnerName);
    }

    void EndMatch(string winnerName)
    {
        Time.timeScale = 1f;

        MatchResult.WinnerName = winnerName;       // "Player 1", "Player 2" ou "" pra empate
        AudioManager.Instance?.StopMusic();        // para música da fase

        SceneManager.LoadScene("GameOver");
    }

    private IEnumerator PreGameRoutine()
    {
        // desabilita controles dos players
        var players = FindObjectsOfType<PlayerController>();
        foreach (var p in players)
        {
            if (p != null)
                p.SetControlsEnabled(false);
        }

        // mostra painel de "vai começar"
        if (preGamePanel != null)
            preGamePanel.SetActive(true);

        // toca som de início
        AudioManager.Instance?.PlayGameStart();

        // espera o delay
        yield return new WaitForSeconds(preGameDelaySeconds);

        // esconde painel
        if (preGamePanel != null)
            preGamePanel.SetActive(false);

        // reabilita controles
        players = FindObjectsOfType<PlayerController>();
        foreach (var p in players)
        {
            if (p != null)
                p.SetControlsEnabled(true);
        }

        AudioManager.Instance.PlayBGM(AudioManager.Instance.bgmNormal);

        gameStarted = true;
    }

#if UNITY_EDITOR
    // Gizmos: mostra onde os 4 spawns ficariam (mesmo fora do Play)
    private void OnDrawGizmos()
    {
        if (!level || !foreground) return;

        int innerW = Mathf.Max(level.outerWidth  - 2, 1);
        int innerH = Mathf.Max(level.outerHeight - 2, 1);

        Vector3Int[] corners =
        {
            new Vector3Int(0,         0,         0),
            new Vector3Int(innerW-1,  innerH-1,  0),
        };

        for (int i = 0; i < playerCount && i < corners.Length; i++)
        {
            Vector3 c = foreground.CellToWorld(corners[i]) + new Vector3(0.5f, 0.5f, 0f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(c, new Vector3(1f, 1f, 0f));
            UnityEditor.Handles.Label(c + new Vector3(0.0f, 0.35f, 0), $"P{i+1}");
        }
    }
#endif
}
