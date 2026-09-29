using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Games.BlackGreg
{
    /// <summary>
    /// Компонент стола для игры в блэкджек.
    /// Наследует базовую логику PlayerGameTable и реализует ICardGameTable.
    /// </summary>
    /// 
    public class BlackGregTable : PlayerGameTable, ICardGameTable
    {
        [Header("Конфигурация")]
        [SerializeField] private BlackGregTableConfig config;
        [Header("Card Settings")]
        [SerializeField] private CardView cardViewPrefab;
        [SerializeField] private Transform cardTablePosition; // точка для сброшенных/нераспределённых карт
        [Header("Reveal Settings")]
        [SerializeField] private Transform revealBotPosition; // Куда выкладывать карты боту (це    нтр стола)
        [SerializeField] private Transform revealPlayerPosition; // Куда выкладывать карты игроку (центр стола)

        [Header("Positioning")]
        [SerializeField] private Transform discardPosition;
        [SerializeField] private Transform deckPosition; 

        [Header("End Game Sequence Settings")]
        [SerializeField] private CardAnimationConfig animationConfig;

        [Header("Score Display")]
        [SerializeField] private HandScoreDisplay botScoreDisplay;
        [SerializeField] private HandScoreDisplay playerScoreDisplay;

        private readonly NetworkVariable<bool> _isRevealed = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Состояние бота
        private readonly NetworkVariable<bool> _botHasStood = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public bool IsRevealed => _isRevealed.Value;

        public bool BotHasStood => _botHasStood.Value;


        // Данные рук (сервер)
        [SerializeField] private List<CardData> playerHandData = new List<CardData>();
        [SerializeField] private List<CardData> botHandData = new List<CardData>();

        private Transform playerHandParent;
        private Transform botHandParent;

        // Визуал на клиентах
        private List<CardView> _botSpawnedCardViews = new List<CardView>();
        private List<CardView> _playerSpawnedCardViews = new List<CardView>();
        private bool placeNextCardOnLeft = true;

        // Копия руки бота до мухлежа. Хранится только пока активен мухлеж в текущем раунде.
        private List<CardData> _originalBotHand;
        private List<CardData> _originalPlayerHand;

        public override string TableType => "BlackGreg";

        // ---------- Реализация ICardGameTable ----------
        public int GetBotScore() => CalculateHandValue(botHandData);
        public int GetPlayerScore() => CalculateHandValue(playerHandData);
        public int GetBotCardCount() => botHandData.Count;
        public int GetPlayerCardCount() => playerHandData.Count;

        public List<CardData> GetBotHandCopy()
        {
            return new List<CardData>(botHandData);
        }

        public void BotDrawCard()
        {
            if (!IsServer || !IsGameStarted) return;

            if (AddCardToHand(botHandData))
            {
                CardData lastCard = botHandData[botHandData.Count - 1];
                Debug.Log($"[BlackGregTable] Бот взял карту: {lastCard.rank} {lastCard.suit}. Текущий счёт: {CalculateHandValue(botHandData)}");

                DrawCardAnimatedClientRpc(lastCard, false, botHandData.Count - 1, OccupiedByClientId);
            }
        }

        [Rpc(SendTo.Server)]
        public void BotStandServerRpc()
        {
            if (!IsServer) return;

            Debug.Log("BotStand");
            _botHasStood.Value = true;
            PutOnTableBotCards();
        }
        public void BotStand()
        {
            if (!IsServer) return;

            BotStandServerRpc();
        }

        /// <summary>
        /// Полностью перезаписывает руку бота (вызывается из системы мухлежа).
        /// </summary>
        public void OverwriteBotHand(List<CardData> newHand)
        {
            if (!IsServer) return;

            // Сохраняем копию перед первым мухлежом в раунде
            if (_originalBotHand == null)
            {
                _originalBotHand = new List<CardData>(botHandData);
                Debug.Log($"[BlackGregTable] Сохранена оригинальная рука бота ({_originalBotHand.Count} карт)");
            }

            botHandData = new List<CardData>(newHand);
            SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
        }

        private void RevertBotHand()
        {
            if (_originalBotHand == null)
            {
                Debug.LogWarning("[BlackGregTable] Попытка откатить руку, но копия отсутствует.");
                return;
            }

            botHandData = _originalBotHand;
            _originalBotHand = null;
            Debug.Log($"[BlackGregTable] Рука бота откачена до оригинальной ({botHandData.Count} карт)");
            SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
        }

        public void OverwritePlayerHand(List<CardData> newHand)
        {
            if (!IsServer) return;

            // Сохраняем копию перед первым мухлежом в раунде
            if (_originalPlayerHand == null)
            {
                _originalPlayerHand = new List<CardData>(playerHandData);
                Debug.Log($"[BlackGregTable] Сохранена оригинальная рука игрока ({_originalPlayerHand.Count} карт)");
            }

            playerHandData = new List<CardData>(newHand);
            SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
        }

        // ---------- Переопределение жизненного цикла ----------
        public override void StartGame()
        {
            if (!IsServer || !CanStartGame()) return;

            // Сброс состояния для новой игры
            playerHandData.Clear();
            botHandData.Clear();
            _botHasStood.Value = false;
            _originalBotHand = null;
            _isRevealed.Value = false;

            Debug.Log("CanStartGame " + CanStartGame());
            base.StartGame(); // устанавливает gameInProgress.Value = true
        }

        public override void EndGame()
        {
            if (!IsServer || !IsGameStarted) return;

            _isRevealed.Value = false;
            _originalBotHand = null;
            _originalPlayerHand = null;

            base.EndGame(); // устанавливает gameInProgress.Value = false

            ClearHandsData(); // Очищаем только данные

            // Оставляем вызов, если игрок должен покинуть стол после игры
            LeaveServerRpc(OccupiedByClientId);
        }
        private void ClearHandsData()
        {
            playerHandData.Clear();
            botHandData.Clear();
        }

        // ---------- Методы для игрока (вызываются UI) ----------
        public void PlayerDrawCard()
        {
            if (!IsServer) return;

            if (!IsGameStarted)
            {
                if (!CanStartGame()) return;
                StartGame();
            }

            if (AddCardToHand(playerHandData))
            {
                CardData lastCard = playerHandData[playerHandData.Count - 1];
                Debug.Log($"[BlackGregTable] Игрок взял карту: {lastCard.rank} {lastCard.suit}. Текущий счёт: {CalculateHandValue(playerHandData)}");

                DrawCardAnimatedClientRpc(lastCard, true, playerHandData.Count - 1, OccupiedByClientId);
            }
        }

        [ClientRpc]
        private void DrawCardAnimatedClientRpc(CardData newCard, bool isPlayer, int cardIndex, ulong playerClientId)
        {
            // Определяем родителя (рука игрока или бота)
            Transform handParent = null;
            if (isPlayer)
            {
                if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerClientId) is { } playerObj)
                    handParent = FindChildByName(playerObj.transform, "CardHandPosition");
                if (handParent == null) handParent = playerHandParent;
            }
            else
            {
                if (botNetworkObjectRef.Value.TryGet(out NetworkObject botObj))
                    handParent = FindChildByName(botObj.transform, "CardHandPosition");
                if (handParent == null) handParent = botHandParent;
            }

            if (handParent == null || cardViewPrefab == null || deckPosition == null) return;

            // Обновляем данные на клиенте
            if (!IsServer)
            {
                if (isPlayer)
                    playerHandData.Add(newCard);
                else
                    botHandData.Add(newCard);
            }

            // Вычисляем позиции в локальных координатах родителя
            Vector3 deckLocalPos = handParent.InverseTransformPoint(deckPosition.position);
            Vector3 targetLocalPos = GetNextCardPosition(cardIndex);
            Quaternion targetLocalRot = Quaternion.Euler(config.startRotation);

            // Спавним карту
            CardView view = Instantiate(cardViewPrefab, handParent);
            view.SetCardData(newCard);
            view.SetVisible(isPlayer);

            // Запускаем анимацию
            view.FlyFromDeck(deckLocalPos, targetLocalPos, targetLocalRot, animationConfig.drawCardDuration, animationConfig.drawCardCurve);

            // Добавляем в список визуалов
            if (isPlayer)
                _playerSpawnedCardViews.Add(view);
            else
                _botSpawnedCardViews.Add(view);
        }



        public void FinishGame()
        {
            if (!IsServer || !IsGameStarted || !_isRevealed.Value) return;

            if (playerHandData.Count < config.minCardsToFinish || botHandData.Count < config.minCardsToFinish) return;
            if (!BotHasStood) return;

            int playerScore = CalculateHandValue(playerHandData);
            int botScore = CalculateHandValue(botHandData);
            string winner = DetermineWinner(playerScore, botScore);

            // Отправляем на клиент команду начать визуальную последовательность
            PlayEndGameSequenceClientRpc(winner, playerScore, botScore);

            if (casinoBank != null)
            {
                if (winner == "player") casinoBank.TryDeposit(2, OccupiedByClientId, "Win", TableType);
                else if (winner == "draw") casinoBank.TryDeposit(1, OccupiedByClientId, "Draw", TableType);
            }
        }

        private IEnumerator PlayScoreCountSequence(List<CardView> cards, List<CardData> handData, HandScoreDisplay scoreDisplay)
        {
            scoreDisplay.ResetScore();
            scoreDisplay.SetVisible(true);

            // Создаём пары (вид, данные) и сортируем по позиции на столе
            var cardPairs = new List<(CardView view, CardData data)>();
            for (int i = 0; i < cards.Count && i < handData.Count; i++)
            {
                if (cards[i] != null)
                    cardPairs.Add((cards[i], handData[i]));
            }

            // Сортировка слева направо по локальной оси X стола
            // Если направление окажется наоборот, замените CompareTo на обратный порядок
            cardPairs.Sort((a, b) => a.view.transform.localPosition.x.CompareTo(b.view.transform.localPosition.x));

            int runningTotal = 0;

            for (int i = 0; i < cardPairs.Count; i++)
            {
                CardView cardView = cardPairs[i].view;
                CardData cardData = cardPairs[i].data;
                int cardValue = cardData.BaseValue;

                // Подсветка и пульсация
                cardView.SetHighlight(true, animationConfig.countingHighlightColor);
                cardView.PlayCountingPulse();

                // Летящая цифра
                bool numberArrived = false;
                cardView.ShowFlyingNumber(cardValue, scoreDisplay.GetTargetPosition(), () =>
                {
                    runningTotal += cardValue;
                    scoreDisplay.UpdateScore(runningTotal);
                    numberArrived = true;
                });

                // Ждём прибытия цифры
                while (!numberArrived)
                    yield return null;

                // Задержка между картами
                if (i < cardPairs.Count - 1)
                    yield return new WaitForSeconds(animationConfig.countingDelayBetweenCards);
            }
        }

        [ClientRpc]
        private void PlayEndGameSequenceClientRpc(string winner, int playerScore, int botScore)
        {
            StartCoroutine(PlayEndGameSequenceRoutine(winner, playerScore, botScore));
        }

        private IEnumerator PlayEndGameSequenceRoutine(string winner, int playerScore, int botScore)
        {
            // 1. Подсчёт очков бота
            yield return PlayScoreCountSequence(_botSpawnedCardViews, botHandData, botScoreDisplay);

            // 2. Подсчёт очков игрока
            yield return PlayScoreCountSequence(_playerSpawnedCardViews, playerHandData, playerScoreDisplay);

            // 2. Визуализации результата
            VisualiseResult(winner);

            // 4. Пауза для демонстрации результата (настраивается в инспекторе стола)
            yield return new WaitForSeconds(config.highlightPauseDuration);

            // 5. Подтверждение завершения игры на сервере (очистка логических данных)
            ConfirmGameEndServerRpc();
            
            // 6. Сброс карт
            DiscardCards();

            if (botScoreDisplay != null) botScoreDisplay.ResetScore();
            if (playerScoreDisplay != null) playerScoreDisplay.ResetScore();
        }
        
        private void VisualiseResult(string winner)
        {
            // 1. Определяем карты победителя для акцента
            List<CardView> winnerCards = null;
            // 3. Визуализация результата
            if (winner == "draw")
            {
                // Ничья: обе руки подсвечиваются и плавно приподнимаются
                foreach (var card in _botSpawnedCardViews)
                {
                    if (card != null)
                    {
                        card.SetHighlight(true, animationConfig.drawHighlightColor);
                        card.PlayDrawAnimation();
                    }
                }
                foreach (var card in _playerSpawnedCardViews)
                {
                    if (card != null)
                    {
                        card.SetHighlight(true, animationConfig.drawHighlightColor);
                        card.PlayDrawAnimation();
                    }
                }
            }
            else
            {
                // Победа: карты победителя подсвечиваются и подпрыгивают
                if (winner == "player") winnerCards = _playerSpawnedCardViews;
                else if (winner == "bot") winnerCards = _botSpawnedCardViews;
                // 2. Подсветка и прыжок
                if (winnerCards != null)
                {
                    foreach (var card in winnerCards)
                    {
                        if (card != null)
                        {
                            card.SetHighlight(true, animationConfig.winnerHighlightColor);
                            card.PlayJumpAnimation();
                        }

                    }

                }
            }
        }

        private void DiscardCards()
        {
            // 5. Запуск анимации сброса карт в колоду с каскадной задержкой
            List<CardView> allCards = new List<CardView>(_playerSpawnedCardViews);
            allCards.AddRange(_botSpawnedCardViews);

            // Очищаем локальные списки, так как карты сейчас уничтожат сами себя
            _playerSpawnedCardViews.Clear();
            _botSpawnedCardViews.Clear();

            // Сортировка по расстоянию до точки сброса
            allCards.Sort((a, b) =>
            {
                float distA = Vector3.Distance(a.transform.position, discardPosition.position);
                float distB = Vector3.Distance(b.transform.position, discardPosition.position);
                return distA.CompareTo(distB);
            });

            for (int i = 0; i < allCards.Count; i++)
            {
                if (allCards[i] != null)
                {
                    float delay = i * animationConfig.discardStaggerDelay;
                    allCards[i].FlyToDiscard(discardPosition, delay);
                }
            }
        }

        [Rpc(SendTo.Server)]
        public void ConfirmGameEndServerRpc()
        {
            if (!IsServer) return;
            EndGame();
        }

        // ---------- RPC для запросов от игрока ----------
        [Rpc(SendTo.Server)]
        public void RequestDrawCardServerRpc(ulong clientId)
        {
            if (IsOccupied)
            {
                if (IsServer && clientId == OccupiedByClientId)
                {
                    PlayerDrawCard();
                }

            }
            else
            {
                if (IsServer && playersInGameArea.Contains(clientId))
                {
                    Occupy(clientId);
                    PlayerDrawCard();
                }
            }

        }

        // ---------- Сетевая синхронизация рук ----------

        [ClientRpc]
        private void SyncHandsClientRpc(ulong playerClientId, CardData[] syncedPlayerHand, CardData[] syncedBotHand)
        {

            if (IsRevealed) return;

            playerHandData = new List<CardData>(syncedPlayerHand);
            botHandData = new List<CardData>(syncedBotHand);

            CardViewCleaner(_playerSpawnedCardViews);

            placeNextCardOnLeft = true;

            // Определяем, должны ли карты игрока быть открытыми

            bool isPlayerFaceUp = (playerClientId != ulong.MaxValue);

            // Получаем руку игрока
            Transform playerHand = null;
            if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerClientId) is { } playerObj)
            {
                playerHand = FindChildByName(playerObj.transform, "CardHandPosition");
            }
            if (playerHand == null) playerHand = cardTablePosition ? cardTablePosition : playerHandParent;

            if (playerHand != null)
            {
                for (int i = 0; i < playerHandData.Count; i++)
                    SpawnCardVisual(playerHandData[i], playerHand, i, isPlayerFaceUp, _playerSpawnedCardViews);
            }

            if (!BotHasStood)
            {
                CardViewCleaner(_botSpawnedCardViews);

                bool isBotFaceUp = false;

                // Получаем руку бота
                Transform botHand = null;
                if (botNetworkObjectRef.Value.TryGet(out NetworkObject botObj))
                {
                    botHand = FindChildByName(botObj.transform, "CardHandPosition");
                }
                if (botHand == null) botHand = botHandParent;

                if (botHand != null)
                {
                    for (int i = 0; i < botHandData.Count; i++)
                        SpawnCardVisual(botHandData[i], botHand, i, isBotFaceUp, _botSpawnedCardViews);
                }
            }
        }

        private void CardViewCleaner(List<CardView> spawnedViews)
        {
            foreach (var view in spawnedViews)
                if (view != null) Destroy(view.gameObject);
            spawnedViews.Clear();
        }

        // ---------- Вспомогательные методы ----------
        /// <summary>
        /// Добавляет случайную карту в указанную руку, если не достигнут лимит.
        /// </summary>
        /// <returns>true, если карта успешно добавлена.</returns>
        private bool AddCardToHand(List<CardData> hand)
        {
            if (hand.Count >= config.cardLimit) return false;

            Card newCard = CardFactory.CreateRandomCard();
            hand.Add(new CardData(newCard.CardSuit, newCard.CardRank, newCard.CardType));
            return true;
        }

        private int CalculateHandValue(List<CardData> hand)
        {
            int sum = 0;
            int aceCount = 0;
            foreach (var card in hand)
            {
                sum += card.BaseValue;
                if (card.rank == CardRank.Ace) aceCount++;
            }
            while (sum > 21 && aceCount > 0)
            {
                sum -= 10;
                aceCount--;
            }
            return sum;
        }

        private string DetermineWinner(int playerScore, int botScore)
        {
            bool playerBust = playerScore > 21;
            bool botBust = botScore > 21;

            if (playerBust && botBust) return "draw";
            if (playerBust) return "bot";
            if (botBust) return "player";
            if (playerScore > botScore) return "player";
            if (botScore > playerScore) return "bot";
            return "draw";
        }

        // ---------- Визуализация ----------
        private void SpawnCardVisual(CardData cardData, Transform parent, int cardIndex, bool isFaceUp, List<CardView> spawnedViews)
        {
            if (cardViewPrefab == null) return;

            CardView view = Instantiate(cardViewPrefab, parent);

            view.transform.localPosition = GetNextCardPosition(cardIndex);
            view.transform.localRotation = Quaternion.Euler(config.startRotation);
            view.SetCardData(cardData);
            view.SetVisible(isFaceUp);
            spawnedViews.Add(view);
        }

        private Vector3 GetNextCardPosition(int currentCardIndex)
        {
            Vector3 position = config.startPosition;
            if (currentCardIndex == 0)
            {
                placeNextCardOnLeft = true;
                return position;
            }
            if (placeNextCardOnLeft)
            {
                int leftCount = (currentCardIndex + 1) / 2;
                position.x = config.startPosition.x - leftCount * config.spreadDistance;
                placeNextCardOnLeft = false;
            }
            else
            {
                int rightCount = (currentCardIndex + 1) / 2;
                position.x = config.startPosition.x + rightCount * config.spreadDistance;
                placeNextCardOnLeft = true;
            }
            return position;
        }

        private Transform FindChildByName(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Переопределённый метод освобождения стола игроком.
        /// После базового освобождения синхронизирует руки так, чтобы карты игрока
        /// переместились в cardTablePosition (если он задан).
        /// </summary>
        public override void Leave(ulong clientId)
        {
            base.Leave(clientId);
                        if (IsServer)
            {
                SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
            }
        }
        /// <summary>
        /// При занятии стола игроком (возвращение) синхронизируем руки с актуальным ID,
        /// чтобы карты игрока переместились из cardTablePosition к его руке.
        /// </summary>
        public override void Occupy(ulong clientId)
        {
            base.Occupy(clientId);

            if (IsServer)
            {
                SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
            }
        }

        public override void OnCheaterCaught(ulong accuserClientId, bool isCheaterBot)
        {
            if (!isCheaterBot)
            {
                // Игрок-читер — пока используем поведение по умолчанию (форс-стоп).
                // В будущем здесь будет мини-игра или другая механика.
                base.OnCheaterCaught(accuserClientId, isCheaterBot);
                RevertBotHand();
                return;
            }

            // Бот пойман — откатываем руку и продолжаем игру.
            Debug.Log($"[BlackGregTable] Бот пойман на мухлеже игроком {accuserClientId}. Рука откачена, игра продолжается.");
            RevertBotHand();

            // Игра НЕ останавливается — бот и игрок доигрывают партию.
            // Бот продолжит свою сессию в PlaySessionRoutine, так как gameInProgress остался true.
        }

        public void PutOnTableBotCards()
        {
            if (!IsServer) return;

            PutOnTableBotCardsClientRpc();
        }

        public void PutOnTablePlayerCards()
        {
            if (!IsServer) return;
            PutOnTablePlayerCardsClientRpc();
        }

        [ClientRpc]
        public void PutOnTablePlayerCardsClientRpc()
        {
            // Очистить старые визуалы
            CardViewCleaner(_playerSpawnedCardViews);

            if (revealPlayerPosition != null)
            {
                for (int i = 0; i < playerHandData.Count; i++)
                    SpawnCardVisual(playerHandData[i], revealPlayerPosition, i, false, _playerSpawnedCardViews);
            }

            Debug.Log("[BlackGregTable] Карты на столе. Ожидание подтверждения игрока.");
        }

        [ClientRpc]
        public void PutOnTableBotCardsClientRpc()
        {
            // Очистить старые визуалы
            CardViewCleaner(_botSpawnedCardViews);

            if (revealBotPosition != null)
            {
                for (int i = 0; i < botHandData.Count; i++)
                    SpawnCardVisual(botHandData[i], revealBotPosition, i, false, _botSpawnedCardViews);
            }

            Debug.Log("[BlackGregTable] Карты на столе. Ожидание подтверждения игрока.");
        }

        /// <summary>
        /// Первый шаг: вскрыть карты.
        /// </summary>
        public void RevealHands()
        {
            if (!IsServer || !IsGameStarted || _isRevealed.Value) return;

            _isRevealed.Value = true;
            RevealHandsClientRpc();
            Debug.Log("[BlackGregTable] Карты вскрыты. Ожидание подтверждения игрока.");
        }

        [ClientRpc]
        private void RevealHandsClientRpc()
        {
            for (int i = 0; i < _playerSpawnedCardViews.Count; i++)
                _playerSpawnedCardViews[i].SetVisible(true);
            for (int i = 0; i < _botSpawnedCardViews.Count; i++)
                _botSpawnedCardViews[i].SetVisible(true);
        }

        /// <summary>
        /// Проверяет, может ли игрок завершить игру (для UI-подсказки).
        /// Дублируется на сервере в PlayerFinishGame для безопасности.
        /// </summary>

        public bool CanPlayerReveal()
        {
            if (!IsGameStarted) return false;
            if (_isRevealed.Value) return false;
            if (playerHandData.Count < config.minCardsToFinish) return false;
            if (botHandData.Count < config.minCardsToFinish) return false;
            if (!BotHasStood) return false;
            return true;
        }

        [Rpc(SendTo.Server)]
        public void RevealHandsServerRpc(ulong clientId)
        {
            if (IsServer && IsOccupied && clientId == OccupiedByClientId)
            {
                PutOnTablePlayerCards();
                RevealHands();
                Invoke("FinishGame", config.timeBeforeEvaluateResult);
            }
        }
        public bool CanPlayerFinish()
        {
            return IsGameStarted && _isRevealed.Value;
        }


        [Rpc(SendTo.Server)]
        public void FinishGameServerRpc(ulong clientId)
        {
            if (IsServer && IsOccupied && clientId == OccupiedByClientId)
                FinishGame();
        }
        public override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            ulong exitingClientId = netObj.OwnerClientId;
            if (IsGameStarted)
            {
                OccupyServerRpc(exitingClientId);
            }
        }
    }
}