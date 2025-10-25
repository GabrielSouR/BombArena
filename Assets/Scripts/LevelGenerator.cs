using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator : MonoBehaviour
{
    [Header("Board size (INCLUDES the outer walls)")]
    public int outerWidth  = 15;   // total com paredes
    public int outerHeight = 13;

    private int innerWidth  => Mathf.Max(outerWidth  - 2, 1);
    private int innerHeight => Mathf.Max(outerHeight - 2, 1);

    [Header("Tilemaps")]
    public Tilemap background;    // chão
    public Tilemap foreground;    // paredes + pilares
    public Tilemap destructible;  // blocos destruíveis (brick)

    [Header("Tiles")]
    public TileBase groundTile;
    public TileBase wallTile;
    public TileBase pillarTile;       // se null, usa wallTile
    public TileBase destructibleTile; // brick

    [Header("Camera Fit")]
    public Camera mainCamera;
    public float cameraMargin = 0.1f;
    public float zoomBias     = 0.0f;

    [Header("Spawn safe radius (corners)")]
    [Tooltip("Pillars: 0=1x1 livre; 1=2x2; 2=3x3...")]
    public int spawnSafeRadiusPillars = 0;       // 1x1
    [Tooltip("Destructibles: 0=1x1 livre; 1=2x2; 2=3x3...")]
    public int spawnSafeRadiusDestructibles = 1; // 2x2

    [Header("Destructibles")]
    [Range(0f,1f)] public float destructibleFillPercent = 0.55f;
    public int randomSeed = 0;  // 0 = aleatório; >0 = reprodutível

    [ContextMenu("Generate Level")]
    public void GenerateLevel()
    {
        if (!background || !foreground || !groundTile || !wallTile)
        {
            Debug.LogError("Assign background, foreground, groundTile and wallTile.");
            return;
        }
        if (pillarTile == null) pillarTile = wallTile;
        if (mainCamera == null) mainCamera = Camera.main;

        background.ClearAllTiles();
        foreground.ClearAllTiles();
        if (destructible) destructible.ClearAllTiles();

        // Conjuntos de células seguras por tipo
        var safePillars = BuildSpawnSafeSet(spawnSafeRadiusPillars);
        var safeBricks  = BuildSpawnSafeSet(spawnSafeRadiusDestructibles);

        // 1) Chão interno (0..inner-1)
        for (int y = 0; y < innerHeight; y++)
            for (int x = 0; x < innerWidth; x++)
                background.SetTile(new Vector3Int(x, y, 0), groundTile);

        // 2) Paredes externas (-1..inner)
        for (int x = -1; x <= innerWidth; x++)
        {
            foreground.SetTile(new Vector3Int(x, -1, 0), wallTile);
            foreground.SetTile(new Vector3Int(x, innerHeight, 0), wallTile);
        }
        for (int y = -1; y <= innerHeight; y++)
        {
            foreground.SetTile(new Vector3Int(-1, y, 0), wallTile);
            foreground.SetTile(new Vector3Int(innerWidth, y, 0), wallTile);
        }

        // 3) Pilares (xadrez: 1,3,5,...) respeitando safePillars
        for (int y = 1; y <= innerHeight - 2; y += 2)
        {
            for (int x = 1; x <= innerWidth - 2; x += 2)
            {
                var cell = new Vector3Int(x, y, 0);
                if (safePillars.Contains(cell)) continue;
                foreground.SetTile(cell, pillarTile);
            }
        }

        // 4) Destrutíveis aleatórios respeitando safeBricks
        PlaceDestructiblesRandom(safeBricks);

        // 5) Câmera
        FitCameraToBoard();

        Debug.Log($"Level generated: OUTER {outerWidth}x{outerHeight} | INNER {innerWidth}x{innerHeight}");
    }

    // --------- Destrutíveis ----------
    private void PlaceDestructiblesRandom(HashSet<Vector3Int> spawnSafe)
    {
        if (!destructible || !destructibleTile || destructibleFillPercent <= 0f) return;

        var candidates = new List<Vector3Int>(innerWidth * innerHeight);

        for (int y = 0; y < innerHeight; y++)
        {
            for (int x = 0; x < innerWidth; x++)
            {
                var cell = new Vector3Int(x, y, 0);

                // Não ocupar áreas seguras de spawn
                if (spawnSafe.Contains(cell)) continue;

                // Não sobrescrever paredes/pilares
                if (foreground.HasTile(cell)) continue;

                candidates.Add(cell);
            }
        }

        // Embaralhar (Fisher-Yates)
        System.Random rng = (randomSeed == 0) ? new System.Random() : new System.Random(randomSeed);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int target = Mathf.RoundToInt(candidates.Count * destructibleFillPercent);
        for (int i = 0; i < target; i++)
            destructible.SetTile(candidates[i], destructibleTile);
    }

    // --------- Áreas seguras (cantos) ----------
    private HashSet<Vector3Int> BuildSpawnSafeSet(int radius)
    {
        var safe = new HashSet<Vector3Int>();
        int r = Mathf.Max(radius, 0);

        // bottom-left
        for (int y = 0; y <= r; y++)
            for (int x = 0; x <= r; x++)
                safe.Add(new Vector3Int(x, y, 0));

        // bottom-right
        for (int y = 0; y <= r; y++)
            for (int x = innerWidth - 1 - r; x <= innerWidth - 1; x++)
                safe.Add(new Vector3Int(x, y, 0));

        // top-left
        for (int y = innerHeight - 1 - r; y <= innerHeight - 1; y++)
            for (int x = 0; x <= r; x++)
                safe.Add(new Vector3Int(x, y, 0));

        // top-right
        for (int y = innerHeight - 1 - r; y <= innerHeight - 1; y++)
            for (int x = innerWidth - 1 - r; x <= innerWidth - 1; x++)
                safe.Add(new Vector3Int(x, y, 0));

        return safe;
    }

    // --------- Câmera ----------
    private void FitCameraToBoard()
    {
        if (mainCamera == null) return;

        Vector2 centerCell = new Vector2((innerWidth - 1) / 2f, (innerHeight - 1) / 2f);
        Vector3 centerWorld = foreground.CellToWorld(new Vector3Int(Mathf.RoundToInt(centerCell.x), Mathf.RoundToInt(centerCell.y), 0))
                            + new Vector3(0.5f, 0.5f, 0f);

        mainCamera.orthographic = true;
        mainCamera.transform.position = new Vector3(centerWorld.x, centerWorld.y, -10f);

        float totalW = outerWidth;
        float aspect = (float)Screen.width / Screen.height;
        float sizeByWidth = ((totalW / 2f) + cameraMargin) / aspect;
        mainCamera.orthographicSize = Mathf.Max(1f, sizeByWidth - zoomBias);
    }

#if UNITY_EDITOR
    // Gizmos: cianos = área segura para pilares; amarelos = área segura para bricks
    private void OnDrawGizmosSelected()
    {
        if (background == null) return;

        DrawSafeGizmos(BuildSpawnSafeSet(spawnSafeRadiusPillars), new Color(0f,1f,1f,0.9f)); // cyan
        DrawSafeGizmos(BuildSpawnSafeSet(spawnSafeRadiusDestructibles), new Color(1f,1f,0f,0.9f)); // yellow
    }

    private void DrawSafeGizmos(HashSet<Vector3Int> set, Color color)
    {
        Gizmos.color = color;
        foreach (var cell in set)
        {
            Vector3 c = background.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
            Gizmos.DrawWireCube(c, new Vector3(1f, 1f, 0f));
        }
    }
#endif
}
