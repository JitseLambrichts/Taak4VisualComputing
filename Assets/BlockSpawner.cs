using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    public GameObject blockPrefab;
    public float spawnHeight = 3f;
    public float spawnInterval = 1.5f;
    public int maxBlocks = 6;

    public Vector2 xRange = new Vector2(-3.7f, -1.3f);
    public Vector2 zRange = new Vector2(-1.2f, 1.2f);

    private int blocksSpawned = 0;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnBlock), 1f, spawnInterval);
    }

    private void SpawnBlock()
    {
        if (blocksSpawned >= maxBlocks)
        {
            CancelInvoke(nameof(SpawnBlock));
            return;
        }

        float x = Random.Range(xRange.x, xRange.y);
        float z = Random.Range(zRange.x, zRange.y);

        Vector3 spawnPosition = new Vector3(x, spawnHeight, z);
        Instantiate(blockPrefab, spawnPosition, Quaternion.identity);

        blocksSpawned++;
    }
}
