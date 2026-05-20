using UnityEngine;
using Mirror;
using System.Collections;

public class CaterpillarSpawner : NetworkBehaviour
{
    public GameObject caterpillarPrefab; // Assign in Inspector
    public Transform[] spawnPoints; // Assign multiple spawn points
    public int caterpillarsPerWave = 3; // Number of caterpillars per wave
    public float waveInterval = 10f; // Time between waves

    private int waveNumber = 0; // Tracks the current wave

    public override void OnStartServer()
    {
        StartCoroutine(SpawnWaves());
    }

    IEnumerator SpawnWaves()
    {
        while (true)
        {
            yield return new WaitForSeconds(waveInterval); // Wait before spawning the next wave
            SpawnWave();
        }
    }

    [Server]
    void SpawnWave()
    {
        waveNumber++; // Increase wave count
        Debug.Log($"Spawning Wave {waveNumber}");

        for (int i = 0; i < caterpillarsPerWave; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)]; // Random spawn location
            GameObject caterpillar = Instantiate(caterpillarPrefab, spawnPoint.position, Quaternion.identity);
            NetworkServer.Spawn(caterpillar); // ✅ Spawns across network
        }

        // Optional: Increase difficulty by adding more caterpillars per wave
        caterpillarsPerWave += 1;
    }
}
