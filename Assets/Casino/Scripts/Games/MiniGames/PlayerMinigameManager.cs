using System;
using System.Collections.Generic;
using Assets.Casino.Games;
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
        [SerializeField] private List<MinigameBase> registeredGames = new List<MinigameBase>();

        public event Action<bool> OnMinigameFinished;
        private MinigameBase _currentActiveGame;
        
        private Dictionary<MinigameType, MinigameBase> _gamesDictionary = new Dictionary<MinigameType, MinigameBase>();

        private Action<bool> _tempCallback;

        public override void OnNetworkSpawn()
        {
            // Работаем только у локального владельца
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            // Инициализация словаря
            foreach (var miniGame in registeredGames)
            {
                if (miniGame == null) continue;
                if (miniGame.MinigameKind == MinigameType.None)
                {
                    Debug.LogError($"[MinigameManager] {miniGame.name} не задал MinigameType", miniGame);
                    continue;
                }

                if (_gamesDictionary.ContainsKey(miniGame.MinigameKind))
                {
                    
                }
                miniGame.gameObject.SetActive(false);

                _gamesDictionary[miniGame.MinigameKind] = miniGame;
            }
        }

        /// <summary>
        /// Публичный метод для запуска игры из других скриптов (CheatController, SlotMachine и т.д.).
        /// </summary>
        /// <param name="gameTypeKey">Ключ игры (должен быть в списке выше)</param>
        /// <param name="contextData">Данные для игры (имя чита, ID слота...)</param>
        /// <param name="onResult">Коллбэк результата (true/false)</param>
        public void RequestStartGame(MinigameType minigameType, Action<bool> onResult)
        {
            if (!IsOwner) return;

            if (!_gamesDictionary.ContainsKey(minigameType))
            {
                Debug.LogError($"[MinigameManager] Игра с ключом '{minigameType}' НЕ НАЙДЕНА! Проверьте инспектор.");
                onResult?.Invoke(false);
                return;
            }

            if (_currentActiveGame != null)
            {
                Debug.LogWarning("[MinigameManager] Предыдущая игра не завершена. Закрываем принудительно.");
                _currentActiveGame.ForceClose();
            }
            _currentActiveGame = _gamesDictionary[minigameType];
            // Сохраняем внешний коллбэк, чтобы вызвать его после завершения
            // Мы используем замыкание или временное поле. Проще сделать так:
            
            _currentActiveGame = _gamesDictionary[minigameType];

            // Запускаем игру
            _tempCallback = onResult;

            _currentActiveGame.StartGame(HandleGameFinishedInternal);

        }
        
        public MinigameType GetRandomMinigame(CheatAction cheat)
            => PickRandom(cheat != null ? cheat.MinigameTypes : null);

        public MinigameType GetRandomMinigame(MinigameGroup group)
            => PickRandom(group != null ? group.Minigames : null);

        private MinigameType PickRandom(IReadOnlyList<MinigameType> candidates)
        {
            if (candidates == null || candidates.Count == 0) return MinigameType.None;
            var available = new List<MinigameType>(candidates.Count);
            foreach (var minigameType in candidates)
                if (minigameType != MinigameType.None && _gamesDictionary.ContainsKey(minigameType))
                    available.Add(minigameType);
            return available.Count == 0 ? MinigameType.None : available[Random.Range(0, available.Count)];
        }
        
        
        /// <summary>
        /// Возвращает текущую запущенную мини-игру (или null).
        /// </summary>
        public MinigameBase GetActiveGame() => _currentActiveGame;
        
        
        [ClientRpc]
        public void StartPlayerMinigameClientRpc(MinigameType minigameType)
        {
            if (!IsOwner) return;

            // Получаем все зарегистрированные игры
            var availableGames = GetAllGameTypes();

            if (availableGames.Count == 0)
            {
                Debug.LogError("[PlayerMinigameManager] Нет доступных мини-игр!");
                return;
            }

            _currentActiveGame.StartGame(HandleGameFinishedInternal);
        }
        
        public IReadOnlyCollection<MinigameType> GetAllGameTypes() => _gamesDictionary.Keys;

        
        private void HandleGameFinishedInternal(bool isSuccess)
        {
            // Уведомляем всех подписчиков на этом клиенте
            OnMinigameFinished?.Invoke(isSuccess);
            _tempCallback?.Invoke(isSuccess);

            // Очистка состояния
            _tempCallback = null; 

            _currentActiveGame = null;
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