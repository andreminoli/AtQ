using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject playerPrefab;

    [Header("Grid Settings")]
    public int width = 5;
    public int height = 5;

    public Dictionary<Vector2Int, Tile> tiles = new();

    private PlayerPawn player;

    private void Start()
    {
        GenerateGrid();
        SpawnPlayer();
    }

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int coords = new(x, y);
                GameObject tileObj = Instantiate(tilePrefab, new Vector3(x, 0, y), Quaternion.identity, transform);
                Tile tile = tileObj.GetComponent<Tile>();
                tile.Init(coords);
                tiles[coords] = tile;
            }
        }
    }

    void SpawnPlayer()
    {
        Vector2Int playerStart = new Vector2Int(2, 0); // center-bottom of 5x5 board

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned in GridManager!");
            return;
        }

        GameObject pawnObj = Instantiate(playerPrefab);
        player = pawnObj.GetComponent<PlayerPawn>();

        if (player == null)
        {
            Debug.LogError("Player prefab is missing the PlayerPawn script!");
            return;
        }

        player.Init(playerStart, this);

        Debug.Log("Spawned PlayerPawn at " + playerStart);
    }

    public Tile GetTileAt(Vector2Int pos)
    {
        tiles.TryGetValue(pos, out var tile);
        return tile;
    }

    public bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }
}