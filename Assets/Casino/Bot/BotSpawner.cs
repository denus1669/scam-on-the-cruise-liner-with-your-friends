using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BotSpawner : NetworkBehaviour
{
    [Header("Префаб бота")]
    [SerializeField] private GameObject botPrefab;

    [Header("Позиции спавна")]
    [SerializeField] private Vector3[] spawnPositions;

    [Header("Настройки личностей (опционально)")]
    [SerializeField] private BotPersonality[] forcedPersonalities;

    private List<GameObject> spawnedBots = new List<GameObject>();

    public GameObject SpawnBot(int index)
    {
        if (!IsServer)
        {
            Debug.LogError("[BotSpawner] SpawnBot может вызываться только на сервере!");
            return null;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogError("[BotSpawner] Сетевая сессия не активна");
            return null;
        }

        if (botPrefab == null)
        {
            Debug.LogError("[BotSpawner] botPrefab не назначен!");
            return null;
        }

        if (index < 0 || index >= spawnPositions.Length)
        {
            Debug.LogError($"[BotSpawner] Индекс {index} выходит за пределы spawnPositions");
            return null;
        }

        GameObject bot = Instantiate(botPrefab, spawnPositions[index], Quaternion.identity);
        NetworkObject netObj = bot.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[BotSpawner] Префаб бота не содержит NetworkObject!");
            Destroy(bot);
            return null;
        }

        netObj.Spawn();
        spawnedBots.Add(bot);

        // Настройка личности, если применимо
        ApplyPersonality(bot, index);

        Debug.Log($"[BotSpawner] Бот {index} заспавнен.");
        return bot;
    }

    public void DespawnBot(GameObject bot)
    {
        if (!IsServer || bot == null) return;

        NetworkObject netObj = bot.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn();

        spawnedBots.Remove(bot);
        Destroy(bot);
    }

    public void DespawnAllBots()
    {
        if (!IsServer) return;
        var botsCopy = new List<GameObject>(spawnedBots);
        foreach (var bot in botsCopy)
        {
            DespawnBot(bot);
        }
        spawnedBots.Clear();
    }

    /// <summary>
    /// Применяет личность к компоненту BlackGregBotBehavior, если он есть.
    /// Остальную инициализацию бот выполнит сам после выбора стола.
    /// </summary>
    private void ApplyPersonality(GameObject bot, int index)
    {
        if (bot.TryGetComponent<BlackGregBotBehavior>(out var blackGregBehavior))
        {
            BotPersonality personality = GetPersonalityForIndex(index);
            blackGregBehavior.SetPersonality(personality);
        }
    }

    private BotPersonality GetPersonalityForIndex(int index)
    {
        if (forcedPersonalities != null && index < forcedPersonalities.Length)
            return forcedPersonalities[index];
        return (BotPersonality)Random.Range(0, 3);
    }

    public List<GameObject> GetSpawnedBots() => new List<GameObject>(spawnedBots);
    public int GetSpawnPositionsCount() => spawnPositions.Length;
}