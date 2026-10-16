using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Casino.Cheating.MiniGames
{
    /// <summary>
    /// Универсальный менеджер мини-игр игрока.
    /// Вешается на префаб Игрока.
    /// </summary>
    public class PlayerMinigameManager : NetworkBehaviour
    {
        [Header("Registry")]
        [Tooltip("Список доступных мини-игр. Key должен совпадать с тем, что передает вызывающий код.")]
        [SerializeField] private List<MinigameEntry> registeredGames = new List<MinigameEntry>();

        public event Action<string, bool> OnMinigameFinished;
        private string _currentContext;
        private MinigameBase _currentActiveGame;
        
        private Dictionary<string, MinigameBase> _gamesDictionary = new Dictionary<string, MinigameBase>();

        private Action<bool> _tempCallback;


        [Serializable]
        public class MinigameEntry
        {
            [Tooltip("ScriptableObject с настройками мини-игры")]
            public MinigameDefinition definition;          
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
                if (!string.IsNullOrEmpty(entry.definition.MinigameName) && entry.gameInstance != null)
                {
                    // Убеждаемся, что панель выключена при старте
                    entry.gameInstance.gameObject.SetActive(false);
                    _gamesDictionary[entry.definition.MinigameName] = entry.gameInstance;
                    //Debug.Log($"[MinigameManager] Зарегистрирована игра: {entry.key}");
                }
            }
        }

        /// <summary>
        /// Публичный метод для запуска игры из других скриптов (CheatController, SlotMachine и т.д.).
        /// </summary>
        /// <param name="gameTypeKey">Ключ игры (должен быть в списке выше)</param>
        /// <param name="contextData">Данные для игры (имя чита, ID слота...)</param>
        /// <param name="onResult">Коллбэк результата (true/false)</param>
        public void RequestStartGame(string contextData, Action<bool> onResult)
        {
            if (!IsOwner) return;

            if (!_gamesDictionary.ContainsKey(contextData))
            {
                Debug.LogError($"[MinigameManager] Игра с ключом '{contextData}' НЕ НАЙДЕНА! Проверьте инспектор.");
                onResult?.Invoke(false);
                return;
            }

            if (_currentActiveGame != null)
            {
                Debug.LogWarning("[MinigameManager] Предыдущая игра не завершена. Закрываем принудительно.");
                _currentActiveGame.ForceClose();
            }
            _currentActiveGame = _gamesDictionary[contextData];
            // Сохраняем внешний коллбэк, чтобы вызвать его после завершения
            // Мы используем замыкание или временное поле. Проще сделать так:
            
            _currentContext = contextData; 
            _currentActiveGame = _gamesDictionary[contextData];

            // Запускаем игру
            _tempCallback = onResult;

            _currentActiveGame.StartGame(contextData, HandleGameFinishedInternal);

        }

        public List<string> GetAllGameKeys()
        {
            return new List<string>(_gamesDictionary.Keys);
        }
        
        public string GetRandomMinigame(CheatAction cheat)
        {
            if (cheat.Minigames == null || cheat.Minigames.Count == 0)
                return null;
            MinigameDefinition minigame = cheat.Minigames[Random.Range(0, cheat.Minigames.Count)];
            return minigame.MinigameName;
        }
        public string GetRandomMinigame(MinigameGroup minigameGroup)
        {
            if (minigameGroup == null || minigameGroup.Minigames.Count == 0)
                return null;
            MinigameDefinition minigame = minigameGroup.Minigames[Random.Range(0, minigameGroup.Minigames.Count)];
            return minigame.MinigameName;
        }
        /// <summary>
        /// Возвращает текущую запущенную мини-игру (или null).
        /// </summary>
        public MinigameBase GetActiveGame() => _currentActiveGame;
        
        
        [ClientRpc]
        public void StartPlayerMinigameClientRpc(string contextData)
        {
            if (!IsOwner) return;

            // Получаем все зарегистрированные игры
            var availableGames = GetAllGameKeys();

            if (availableGames.Count == 0)
            {
                Debug.LogError("[PlayerMinigameManager] Нет доступных мини-игр!");
                return;
            }

            _currentActiveGame.StartGame(contextData, HandleGameFinishedInternal);

        }
        
        private void HandleGameFinishedInternal(bool isSuccess)
        {
            // Уведомляем всех подписчиков на этом клиенте
            OnMinigameFinished?.Invoke(_currentContext, isSuccess);
            _tempCallback?.Invoke(isSuccess);

            // Очистка состояния
            _tempCallback = null; 

            _currentActiveGame = null;
            _currentContext = null;
        }

                
        /// <summary>
        /// 
        /// </summary>
        [ClientRpc]
        public void ForceCloseActiveMinigameClientRpc()
        {
            if (!IsOwner) return;
            var activeGame = GetActiveGame();
            if (activeGame != null)
            {
                activeGame.ForceClose();
                // Важно: ForceClose должен сам вызвать коллбэк с false, 
                // либо мы вызываем событие вручную, чтобы сбросить состояние чита.
            }
        }
    }
}