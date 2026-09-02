using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// Универсальный менеджер мини-игр игрока.
/// Вешается на префаб Игрока.
/// </summary>
public class PlayerMinigameManager : NetworkBehaviour
{
    [Header("Registry")]
    [Tooltip("Список доступных мини-игр. Key должен совпадать с тем, что передает вызывающий код.")]
    [SerializeField] private List<MinigameEntry> registeredGames = new List<MinigameEntry>();

    private Dictionary<string, MinigameBase> _gamesDictionary = new Dictionary<string, MinigameBase>();
    private MinigameBase _currentActiveGame;

    private Action<bool> _tempCallback;


    [Serializable]
    public class MinigameEntry
    {
        public string key;             // Например: "ReactionGame"
        public MinigameBase gameInstance; // Ссылка на компонент в сцене (Canvas)
    }

    public override void OnNetworkSpawn()
    {
        // Работаем только у локального владельца
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        // Инициализация словаря
        foreach (var entry in registeredGames)
        {
            if (!string.IsNullOrEmpty(entry.key) && entry.gameInstance != null)
            {
                // Убеждаемся, что панель выключена при старте
                entry.gameInstance.gameObject.SetActive(false);
                _gamesDictionary[entry.key] = entry.gameInstance;
                Debug.Log($"[MinigameManager] Зарегистрирована игра: {entry.key}");
            }
        }
    }

    /// <summary>
    /// Публичный метод для запуска игры из других скриптов (CheatController, SlotMachine и т.д.).
    /// </summary>
    /// <param name="gameTypeKey">Ключ игры (должен быть в списке выше)</param>
    /// <param name="contextData">Данные для игры (имя чита, ID слота...)</param>
    /// <param name="onResult">Коллбэк результата (true/false)</param>
    public void RequestStartGame(string gameTypeKey, string contextData, Action<bool> onResult)
    {
        if (!IsOwner) return;

        if (!_gamesDictionary.ContainsKey(gameTypeKey))
        {
            Debug.LogError($"[MinigameManager] Игра с ключом '{gameTypeKey}' НЕ НАЙДЕНА! Проверьте инспектор.");
            onResult?.Invoke(false);
            return;
        }

        if (_currentActiveGame != null)
        {
            Debug.LogWarning("[MinigameManager] Предыдущая игра не завершена. Закрываем принудительно.");
            _currentActiveGame.ForceClose();
        }

        _currentActiveGame = _gamesDictionary[gameTypeKey];

        // Запускаем игру
        _currentActiveGame.StartGame(contextData, HandleGameFinishedInternal);

        // Сохраняем внешний коллбэк, чтобы вызвать его после завершения
        // Мы используем замыкание или временное поле. Проще сделать так:
        _tempCallback = onResult;
    }

    private void HandleGameFinishedInternal(bool isSuccess)
    {
        if (_tempCallback != null)
        {
            _tempCallback.Invoke(isSuccess);
            _tempCallback = null;
        }
        _currentActiveGame = null;
    }

    public List<string> GetAllGameKeys()
    {
        return new List<string>(_gamesDictionary.Keys);
    }
    /// <summary>
    /// Возвращает текущую запущенную мини-игру (или null).
    /// </summary>
    public MinigameBase GetActiveGame() => _currentActiveGame;
}
