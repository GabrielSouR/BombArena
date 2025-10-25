using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public LevelGenerator level;      // arraste o LevelGenerator
    public Tilemap foreground;        // arraste o Foreground (p/ CellToWorld)
    public List<GameObject> playerPrefabs = new List<GameObject>(); // 1 a 4 prefabs

    [Header("Players")]
    [Range(1,4)] public int playerCount = 2;
    public bool regenerateLevelOnStart = false; // gera o tabuleiro no Start

    private readonly Vector3Int[] cornerCellsOrder = new Vector3Int[4];
    private readonly List<Transform> spawnedPlayers = new List<Transform>();

    void Start()
    {
        if (!level || !foreground)
        {
            Debug.LogError("Assign LevelGenerator and Foreground tilemap.");
            return;
        }

        if (regenerateLevelOnStart)
            level.GenerateLevel();

        // área interna (caminhável)
        int innerW = Mathf.Max(level.outerWidth  - 2, 1);
        int innerH = Mathf.Max(level.outerHeight - 2, 1);

        // ordem: BL, BR, TL, TR
        cornerCellsOrder[0] = new Vector3Int(0,         0,         0);
        cornerCellsOrder[1] = new Vector3Int(innerW-1,  0,         0);
        cornerCellsOrder[2] = new Vector3Int(0,         innerH-1,  0);
        cornerCellsOrder[3] = new Vector3Int(innerW-1,  innerH-1,  0);

        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        spawnedPlayers.Clear();

        for (int i = 0; i < playerCount && i < 4; i++)
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
                go.transform.position = center; // fallback
            }

            spawnedPlayers.Add(go.transform);
        }

        Debug.Log($"Spawned {spawnedPlayers.Count} player(s).");
    }

    private GameObject GetPrefabForIndex(int i)
    {
        if (playerPrefabs == null || playerPrefabs.Count == 0) return null;
        if (i < playerPrefabs.Count && playerPrefabs[i] != null) return playerPrefabs[i];

        // se só tiver 1 prefab preenchido, usa ele pra todos
        return playerPrefabs[0];
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
            new Vector3Int(innerW-1,  0,         0),
            new Vector3Int(0,         innerH-1,  0),
            new Vector3Int(innerW-1,  innerH-1,  0),
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3 c = foreground.CellToWorld(corners[i]) + new Vector3(0.5f, 0.5f, 0f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(c, new Vector3(1f, 1f, 0f));
            // etiqueta com o índice (1..4)
            UnityEditor.Handles.Label(c + new Vector3(0.0f, 0.35f, 0), $"P{i+1}");
        }
    }
#endif
}
