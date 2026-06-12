using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BotSpawner : NetworkBehaviour
{
    [Header("Префаб бота")]
    [SerializeField] private GameObject botPrefab;

    [Header("Позиции спавна")]
    [SerializeField] private Vector3[] spawnPositions;

    [Header("Привязка к столам (УНИВЕРСАЛЬНО)")]
    [SerializeField] private NetworkBehaviour[] availableTables; // Теперь любой NetworkBehaviour

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
            Debug.LogError("Сетевая сессия не активна");
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
        ConfigureBot(bot, index);
        spawnedBots.Add(bot);

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

    private void ConfigureBot(GameObject bot, int index)
    {
        // Ищем интерфейс, а не конкретный класс!
        IBotGameBehavior behavior = bot.GetComponent<IBotGameBehavior>();
        if (behavior != null)
        {
            NetworkBehaviour targetTable = GetTableForIndex(index);
            if (targetTable != null)
            {
                // Универсальная инициализация
                behavior.InitializeGame(targetTable);

                // Дополнительно настраиваем личность, если это BlackGreg бот
                // Это компромисс: спавнер знает о существовании BlackGregBotBehavior,
                // но только для дополнительной настройки. Вся основная логика — через интерфейс.
                if (behavior is BlackGregBotBehavior blackGregBehavior)
                {
                    BotPersonality personality = GetPersonalityForIndex(index);
                    blackGregBehavior.SetPersonality(personality);
                }
            }
            else
            {
                Debug.LogWarning($"[BotSpawner] Для бота {index} не найден стол.");
            }
        }
        else
        {
            Debug.LogWarning($"[BotSpawner] У бота {index} отсутствует IBotGameBehavior.");
        }
    }

    private NetworkBehaviour GetTableForIndex(int index)
    {
        if (availableTables == null || availableTables.Length == 0)
            return null;
        int tableIndex = index % availableTables.Length;
        return availableTables[tableIndex];
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