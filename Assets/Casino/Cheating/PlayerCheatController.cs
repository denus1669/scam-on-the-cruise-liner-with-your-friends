using Assets.Casino;
using Assets.Casino.Bot;
using Assets.Casino.Cheating.MiniGames;
using Assets.Casino.Exposure;
using Assets.Casino.Games;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Cheating
{
    public class PlayerCheatController : CheatController
    {
        [Header("Minigame Integration")]
        [Tooltip("Ссылка на менеджер мини-игр на этом же игроке")]
        [SerializeField] private PlayerMinigameManager _minigameManager;

        [Header("Раздражение ботов во время чита")]
        [SerializeField] private float displeasurePerSecond = 5f;

        [Tooltip("Если указать конкретного бота, раздражаться будет только он. Если пусто — боты за текущим столом.")]
        [SerializeField] private BotDispleasureController specificTargetBot;

        private Coroutine _irritationRoutine;
        private readonly List<BotDispleasureController> _affectedBots = new();
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                OnCheatingStateChanged += HandleCheatingStateChanged;
            }

            base.OnNetworkSpawn();

            if (IsServer && IsCheating)
            {
                StartIrritation();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                StopIrritation();
                OnCheatingStateChanged -= HandleCheatingStateChanged;
            }

            base.OnNetworkDespawn();
        }

        private void Awake()
        {
            if (_minigameManager == null)
            {
                _minigameManager = GetComponent<PlayerMinigameManager>();
                if (_minigameManager == null)
                    Debug.LogError("[PlayerCheatController] PlayerMinigameManager не найден на объекте игрока!");
            }
        }

        /// <summary>
        /// Запрос от клиента на сервер о начале мухлежа.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void RequestCheatServerRpc(string specificCheatName = "")
        {
            if (!IsServer) return;
            if (IsCheating) return;

            Debug.Log($"[CheatController] Игрок {OwnerClientId} запросил мухлеж: {specificCheatName}");

            IGameTable currentTable = GameTableManager.Instance?.GetTableOccupiedByPlayer(OwnerClientId);

            if (currentTable == null)
            {
                Debug.LogWarning($"[CheatController] Игрок {OwnerClientId} не сидит за столом.");
                return;
            }

            CheatAction cheatToExecute = null;
            if (!string.IsNullOrEmpty(specificCheatName))
                // ИСПРАВЛЕНИЕ 1: Ищем по CheatName, а не по name ассета
                cheatToExecute = availableCheats.Find(c => c.name == specificCheatName);
            else
                Debug.LogError("specificCheatName is null or empty!");

            if (cheatToExecute != null)
            {
                TryInitiateCheat(currentTable, cheatToExecute);
            }
            else
                Debug.LogError($"cheatToExecute == null for name: {specificCheatName}");
        }

        public override bool TryInitiateCheat(IGameTable table, CheatAction specificCheat)
        {
            if (!IsServer || IsCheating) return false;

            if (!specificCheat.CanExecute(table, "Player"))
            {
                Debug.LogWarning($"[CheatController] Чит {specificCheat.name} нельзя выполнить за этим столом.");
                return false;
            }

            currentTable = table;
            CheatRoutine(specificCheat);
            return true;
        }

        protected override void CheatRoutine(CheatAction cheat)
        {
            base.CheatRoutine(cheat);
            PlayerCheatRoutine(cheat);
        }

        private void PlayerCheatRoutine(CheatAction cheat)
        {
            Debug.Log($"[CheatController] Сервер запустил рутину мухлежа: {cheat.CheatName}");

            // Уведомляем клиентов об анимации
            NotifyCheatStartedClientRpc(cheat.AnimationTriggerName);

            // Запускаем мини-игру у владельца
            // Передаем имя чита как контекст, чтобы мини-игра знала, что именно отображать/проверять
            StartPlayerMinigameClientRpc(cheat.name);
        }

        [ClientRpc]
        private void StartPlayerMinigameClientRpc(string cheatActionName)
        {
            if (!IsOwner) return;

            if (_minigameManager == null)
            {
                Debug.LogError("[PlayerCheatController] MinigameManager отсутствует, невозможно запустить UI.");
                return;
            }

            // Получаем все зарегистрированные игры
            var availableGames = _minigameManager.GetAllGameKeys();

            if (availableGames.Count == 0)
            {
                Debug.LogError("[PlayerCheatController] Нет доступных мини-игр!");
                return;
            }

            // Выбираем случайную
            int randomIndex = UnityEngine.Random.Range(0, availableGames.Count);
            string minigameTypeKey = availableGames[randomIndex];

            Debug.Log($"[PlayerCheatController] Выбрана случайная мини-игра: '{minigameTypeKey}'");
            Debug.Log($"[PlayerCheatController] Запрос мини-игры '{minigameTypeKey}' через менеджер. Контекст: {cheatActionName}");

            _minigameManager.RequestStartGame(minigameTypeKey, cheatActionName, HandleMinigameResult);
        }

        private void HandleMinigameResult(bool isSuccess)
        {
            Debug.Log($"[PlayerCheatController] Мини-игра завершена локально. Успех: {isSuccess}. Отправка на сервер...");
            FinishPlayerCheatServerRpc(isSuccess);
        }

        /// <summary>
        /// Вызывается сервером через RPC, когда чит прерван инспектором или другим игроком.
        /// </summary>
        public void ForceCloseActiveMinigame()
        {
            if (_minigameManager == null) return;

            var activeGame = _minigameManager.GetActiveGame();
            if (activeGame != null)
            {
                Debug.Log("[PlayerCheatController] Мини-игра принудительно закрыта из-за поимки.");
                activeGame.ForceClose();
            }
        }

        [Rpc(SendTo.Server)]
        public void FinishPlayerCheatServerRpc(bool isSuccess)
        {
            if (!IsServer) return;

            if (!IsCheating)
            {
                Debug.LogWarning($"[CheatController] Игрок {OwnerClientId} прислал результат, но флаг isCheating = false (возможно таймаут).");
                return;
            }

            Debug.Log($"[CheatController] Сервер получил результат от {OwnerClientId}: Успех={isSuccess}");

            if (cheatCoroutine != null) StopCoroutine(cheatCoroutine);

            if (isSuccess)
            {
                Debug.Log("[CheatController] Игрок успешно совершил чит");
                CompleteCheat(currentCheatAction, "Player");
            }
            else
            {
                Debug.Log($"[CheatController] Игрок {OwnerClientId} провалил мини-игру.");
                HandleCheatCaught(ulong.MaxValue);
            }

        }


        public override void HandleCheatCaught(ulong accuserClientId)
        {
            base.HandleCheatCaught(accuserClientId);
            ForceCloseMinigameClientRpc();
            ExposureManager.Instance?.AddExposure();
        }

        private void HandleCheatingStateChanged(bool isCheating)
        {
            if (!IsServer) return;

            if (isCheating)
            {
                StartIrritation();
            }
            else
            {
                StopIrritation();
            }
        }

        private void StartIrritation()
        {
            if (_irritationRoutine != null) return;

            CacheAffectedBots();
            _irritationRoutine = StartCoroutine(IrritationRoutine());
        }

        private void StopIrritation()
        {
            if (_irritationRoutine != null)
            {
                StopCoroutine(_irritationRoutine);
                _irritationRoutine = null;
            }

            _affectedBots.Clear();
        }
        private IEnumerator IrritationRoutine()
        {
            var wait = new WaitForSeconds(1f);

            while (IsCheating)
            {
                AddDispleasureToBots();
                yield return wait;
            }

            _irritationRoutine = null;
        }

        private void AddDispleasureToBots()
        {
            if (currentTable == null)
            {
                StopIrritation();
                return;
            }

            for (int i = _affectedBots.Count - 1; i >= 0; i--)
            {
                if (_affectedBots[i] == null)
                {
                    _affectedBots.RemoveAt(i);
                    continue;
                }

                // Вызываем напрямую, потому что мы уже на сервере.
                _affectedBots[i].AddInstantDispleasureServerRpc(displeasurePerSecond);
            }
        }

        private void CacheAffectedBots()
        {
            _affectedBots.Clear();

            if (specificTargetBot != null)
            {
                _affectedBots.Add(specificTargetBot);
                return;
            }

            if (currentTable == null) return;

            // Лучше, если IGameTable сам отдаст список ботов за столом.
            // Ниже — универсальный запасной вариант.
            var botAgents = FindObjectsByType<BotAgent>(FindObjectsSortMode.None);

            foreach (var botAgent in botAgents)
            {
                if (botAgent == null) continue;
                if (botAgent.CurrentTable != currentTable) continue;

                if (botAgent.TryGetComponent(out BotDispleasureController displeasure))
                {
                    _affectedBots.Add(displeasure);
                }
            }
        }
    }
}