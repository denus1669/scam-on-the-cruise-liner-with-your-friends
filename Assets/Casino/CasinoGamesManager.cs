using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CasinoGamesManager : NetworkBehaviour
{
    [Header("Ссылки на спавнер")]
    [SerializeField] private BotSpawner botSpawner;

    [Header("Настройки спавна")]
    [SerializeField] private int maxBotsInCasino = 3;
    [SerializeField] private float spawnDelayMin = 55f;
    [SerializeField] private float spawnDelayMax = 155f;

    private Coroutine _spawnRoutine;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            // Запускаем бесконечный цикл спавна ботов, пока казино не заполнится
            _spawnRoutine = StartCoroutine(SpawnBotsOverTimeRoutine());
        }
    }
    private IEnumerator SpawnBotsOverTimeRoutine()
    {
        Debug.Log("[Casino] Старт корутины спавна ботов");
        int currentBotIndex = 0;

        while (currentBotIndex < maxBotsInCasino)
        {
            Debug.Log($"[Casino] Попытка спавна бота {currentBotIndex}");
            if (botSpawner == null)
            {
                Debug.LogError("[Casino] botSpawner = null! Назначьте ссылку в инспекторе.");
                yield break;
            }

            int positionsCount = botSpawner.GetSpawnPositionsCount();
            if (currentBotIndex >= positionsCount)
            {
                Debug.LogWarning($"[Casino] Достигнут лимит позиций: {positionsCount}");
                yield break;
            }

            botSpawner.SpawnBot(currentBotIndex);
            currentBotIndex++;

            yield return new WaitForSeconds(Random.Range(spawnDelayMin, spawnDelayMax));
        }
        Debug.Log("[Casino] Все боты заспавнены");
    }

    /// <summary>
    /// Убирает одного случайного бота (например, ему надоело играть).
    /// </summary>
    public void DespawnRandomBot()
    {
        if (!IsServer || botSpawner == null) return;

        List<GameObject> activeBots = botSpawner.GetSpawnedBots();
        if (activeBots.Count > 0)
        {
            int randomIndex = Random.Range(0, activeBots.Count);
            botSpawner.DespawnBot(activeBots[randomIndex]);
        }
    }

    /// <summary>
    /// Массовая уборка всех ботов (например, закрытие казино или конец раунда).
    /// </summary>
    public void ForceDespawnAllBots()
    {
        if (!IsServer || botSpawner == null) return;

        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        botSpawner.DespawnAllBots();
    }

    public override void OnDestroy()
    {
        if (IsServer) ForceDespawnAllBots();
        base.OnDestroy();
    }
}