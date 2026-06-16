using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
#if USING_CINEMACHINE
using Cinemachine;
#endif

/// <summary>
/// Компонент управления Вниманием (приближением) для игрока.
/// Добавляется на префаб игрока. Управляет энергией, камерой, рейкастом и локальным раскрытием карт.
/// </summary>
public class PlayerAttentionController : NetworkBehaviour
{
    [Header("Настройки управления")]
    [Tooltip("Клавиша для активации режима внимания (удерживать)")]
    [SerializeField] private KeyCode attentionKey = KeyCode.Q;
    [Tooltip("Клавиша для обвинения во время прицеливания")]
    [SerializeField] private KeyCode accuseKey = KeyCode.E;

    [Header("Настройки камеры и зума")]
    [SerializeField] private Camera playerCamera;
#if USING_CINEMACHINE
    [Tooltip("Специфичная виртуальная камера Cinemachine для зума")]
    [SerializeField] private CinemachineVirtualCamera zoomVirtualCamera;
#endif
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float zoomedFOV = 25f;
    [SerializeField] private float zoomSpeed = 8f;

    [Header("Параметры энергии и баланса")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float energyDrainRate = 3f;   // тратится за секунду зума 
    [SerializeField] private float energyRegenRate = 5f;   // регенерация в секунду вне зума
    [SerializeField] private float cooldownDuration = 3f;   // штрафная перезарядка при полной разрядке

    [Header("Параметры Рэйкаста")]
    [SerializeField] private float maxAttentionDistance = 4.5f;
    [SerializeField] private LayerMask attentionLayerMask = ~0; // все слои по умолчанию
    [SerializeField] private float lookAtBotDispleasureRate = 8f;   // базовый прирост раздражения бота в сек
    [SerializeField] private float lookAtCardsDispleasureRate = 22f; // боты ОЧЕНЬ злятся, когда смотрят на их карты (22 в сек)

    // Текущее состояние
    private float currentEnergy;
    private bool isAttentionActive;
    private bool isCooldownActive;
    private float cooldownTimer;

    // Ссылки на локально раскрытые карты для последующего скрытия
    private List<CardVisualController> revealedCardsThisFrame = new List<CardVisualController>();
    private List<CardVisualController> previouslyHiddenCards = new List<CardVisualController>();

    private void Start()
    {
        currentEnergy = maxEnergy;
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        // Логику ввода и визуализацию обрабатывает только владелец этого персонажа
        if (!IsOwner) return;

        HandleFocusEnergy();
        HandleInput();
        ApplyCameraZoom();

        if (isAttentionActive)
        {
            PerformAttentionRaycast();
        }
        else
        {
            ResetRevealedCards();
        }
    }

    /// <summary>
    /// Управляет шкалой фокуса (энергией внимания) и штрафным кулдауном.
    /// </summary>
    private void HandleFocusEnergy()
    {
        if (isCooldownActive)
        {
            cooldownTimer -= Time.deltaTime;
            currentEnergy += energyRegenRate * Time.deltaTime;
            if (cooldownTimer <= 0 && currentEnergy >= maxEnergy * 0.3f) // выходит из кд при накоплении 30%
            {
                isCooldownActive = false;
                Debug.Log("[Attention] Способность снова готова к использованию!");
            }
            return;
        }

        if (isAttentionActive)
        {
            currentEnergy = Mathf.Max(0f, currentEnergy - energyDrainRate * Time.deltaTime);
            if (currentEnergy <= 0f)
            {
                isAttentionActive = false;
                isCooldownActive = true;
                cooldownTimer = cooldownDuration;
                Debug.Log("[Attention] Внимание перегрето! Активирован кулдаун.");
            }
        }
        else
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenRate * Time.deltaTime);
        }
    }

    private void HandleInput()
    {
        if (isCooldownActive) return;

        // Активация внимания по удержанию клавиши
        if (Input.GetKeyDown(attentionKey))
        {
            isAttentionActive = true;
        }
        if (Input.GetKeyUp(attentionKey))
        {
            isAttentionActive = false;
        }
    }

    /// <summary>
    /// Изменяет FOV или переключает виртуальные камеры Cinemachine.
    /// </summary>
    private void ApplyCameraZoom()
    {
        bool zoomState = isAttentionActive && !isCooldownActive;

#if USING_CINEMACHINE
        if (zoomVirtualCamera != null)
        {
            zoomVirtualCamera.Priority = zoomState ? 20 : 5;
            return;
        }
#endif

        // Плавный резервный FOV-зум на обычной камере
        if (playerCamera != null)
        {
            float targetFOV = zoomState ? zoomedFOV : defaultFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
    }

    /// <summary>
    /// Пускает луч из центра экрана для обнаружения карт или ботов.
    /// </summary>
    private void PerformAttentionRaycast()
    {
        if (playerCamera == null) return;

        revealedCardsThisFrame.Clear();

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, maxAttentionDistance, attentionLayerMask))
        {
            // 1. Проверяем попадание по картам
            CardVisualController cardVisual = hit.collider.GetComponentInParent<CardVisualController>();
            if (cardVisual != null)
            {
                HandleCardFocus(cardVisual);

                // Начисляем недовольство владельцу карт (боту) через BotDispleasureController
                BotDispleasureController targetDispleasure = hit.collider.GetComponentInParent<BotDispleasureController>();
                if (targetDispleasure != null)
                {
                    targetDispleasure.TickDispleasureServerRpc(lookAtCardsDispleasureRate * Time.deltaTime, OwnerClientId);
                }
            }
            else
            {
                // 2. Проверяем попадание просто по боту (телу, голове)
                BotDispleasureController targetDispleasure = hit.collider.GetComponentInParent<BotDispleasureController>();
                if (targetDispleasure != null)
                {
                    HandleBotFocus(targetDispleasure);
                }
            }
        }

        // Ппрячем карты, на которые мы перестали смотреть в этом кадре
        for (int i = previouslyHiddenCards.Count - 1; i >= 0; i--)
        {
            CardVisualController oldCard = previouslyHiddenCards[i];
            if (!revealedCardsThisFrame.Contains(oldCard))
            {
                oldCard.isVisible = false;
                oldCard.UpdateCardVisuals();
                previouslyHiddenCards.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Локально раскрывает карты при наведении луча.
    /// </summary>
    private void HandleCardFocus(CardVisualController cardVisual)
    {
        revealedCardsThisFrame.Add(cardVisual);

        if (!cardVisual.isVisible)
        {
            cardVisual.isVisible = true;
            cardVisual.UpdateCardVisuals();
            previouslyHiddenCards.Add(cardVisual);

            // Проигрываем локальный тихий звук подглядывания (опционально)
            // AudioSource.PlayClipAtPoint(peekSound, transform.position);
        }
    }

    /// <summary>
    /// Обработка фокусировки взгляда на самом боте.
    /// </summary>
    private void HandleBotFocus(BotDispleasureController targetDispleasure)
    {
        // Начисляем базовое раздражение боту на сервере через выделенный контроллер
        targetDispleasure.TickDispleasureServerRpc(lookAtBotDispleasureRate * Time.deltaTime, OwnerClientId);

        // Если бот сейчас реально мухлюет, подсвечиваем его и позволяем обвинить (запрос идет к CheatController)
        CheatController targetCheat = targetDispleasure.GetComponent<CheatController>();
        if (targetCheat != null && targetCheat.IsCheating)
        {
            // Здесь можно вызвать UI-подсказку: "НАЖМИТЕ Е, ЧТОБЫ ПОЙМАТЬ!"
            if (Input.GetKeyDown(accuseKey))
            {
                targetCheat.AccuseServerRpc(OwnerClientId);
            }
        }
    }

    /// <summary>
    /// Возвращает все раскрытые карты к закрытому состоянию при выходе из Внимания.
    /// </summary>
    private void ResetRevealedCards()
    {
        if (previouslyHiddenCards.Count > 0)
        {
            foreach (var card in previouslyHiddenCards)
            {
                if (card != null)
                {
                    card.isVisible = false;
                    card.UpdateCardVisuals();
                }
            }
            previouslyHiddenCards.Clear();
        }
    }
}