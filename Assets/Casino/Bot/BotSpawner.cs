using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
//using static UnityEditor.FilePathAttribute;

public class BotSpawner : NetworkBehaviour
{
    [Header("Префаб бота")]
    [SerializeField] private GameObject botPrefab;

    [Header("Позиции спавна")]
    [SerializeField] private List<GameObject> spawnPositions;

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
        // Проверка наличия точек спавна
        if (spawnPositions == null || spawnPositions.Count == 0)
        {
            Debug.LogError("[BotSpawner] Список spawnPositions пуст!");
            return null;
        }

        // Выбираем случайную точку из списка
        int randomIndex = Random.Range(0, spawnPositions.Count);
        Vector3 position = spawnPositions[randomIndex].transform.position;
        Quaternion rotation = spawnPositions[randomIndex].transform.rotation; // Берем поворот маркера

        GameObject bot = Instantiate(botPrefab, position, rotation);
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
        if (bot.TryGetComponent<BaseBotBehaviour>(out var baseBotBehavior))
        {
            BotPersonality personality = GetPersonalityForIndex(index);
            baseBotBehavior.SetPersonality(personality);
        }
    }

    private BotPersonality GetPersonalityForIndex(int index)
    {
        if (forcedPersonalities != null && index < forcedPersonalities.Length)
            return forcedPersonalities[index];
        return (BotPersonality)Random.Range(0, 3);
    }

    public List<GameObject> GetSpawnedBots() => new List<GameObject>(spawnedBots);
}