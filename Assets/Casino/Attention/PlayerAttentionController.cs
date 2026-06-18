using Blocks.Gameplay.Core;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAttentionController : NetworkBehaviour
{
    [Header("Режим внимания")]
    public NetworkVariable<bool> IsAttention { get; private set; } = new NetworkVariable<bool>(false);

    [Header("Настройки камеры и зума")]
    [SerializeField] private CoreCameraController cameraController;
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float attentionFOV = 25f;
    [SerializeField] private float zoomSpeed = 8f;

    [Header("Параметры Рэйкаста")]
    [SerializeField] private float maxAttentionDistance = 4.5f;
    [SerializeField] private LayerMask attentionLayerMask = ~0;
    [SerializeField] private float lookAtBotDispleasureRate = 8f;
    [SerializeField] private float lookAtCardsDispleasureRate = 22f;

    // Текущее состояние
    private bool isAttentionActive;
    private bool isCooldownActive;
    private float cooldownTimer;

    // Ссылки на локально раскрытые карты
    private List<CardVisualController> revealedCardsThisFrame = new List<CardVisualController>();
    private List<CardVisualController> previouslyHiddenCards = new List<CardVisualController>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            IsAttention.OnValueChanged += OnAttentionStateChanged;
            // Принудительно синхронизируем начальное состояние
            OnAttentionStateChanged(IsAttention.Value, IsAttention.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            IsAttention.OnValueChanged -= OnAttentionStateChanged;
            ResetRevealedCards();
        }
        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        // Дополнительная страховка, если деспавн не вызвался
        if (IsOwner)
        {
            IsAttention.OnValueChanged -= OnAttentionStateChanged;
        }
        base.OnDestroy();
    }

    /// <summary>
    /// Единственный метод, который дёргает AttentionGameListener.
    /// </summary>
    public void ToggleAttention()
    {
        if (IsServer)
        {
            SetAttentionState(!IsAttention.Value);
        }
        else
        {
            ToggleAttentionServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void ToggleAttentionServerRpc()
    {
        SetAttentionState(!IsAttention.Value);
    }

    private void SetAttentionState(bool newState)
    {
        if (IsAttention.Value == newState) return;
        IsAttention.Value = newState;
    }

    private void OnAttentionStateChanged(bool previous, bool current)
    {
        isAttentionActive = current;

        // Применяем зум камеры
        float targetFOV = current ? attentionFOV : defaultFOV;
        ApplyCameraZoom(targetFOV);

        // При выходе из режима – сбрасываем все раскрытые карты
        if (!current)
        {
            ResetRevealedCards();
        }

        // Здесь можно добавить звук, UI, блокировку ввода и т.д.
    }

    /// <summary>
    /// Изменяет FOV камеры.
    /// </summary>
    public void ApplyCameraZoom(float FOV)
    {
        if (cameraController != null && cameraController.ActiveCameraMode?.CinemachineCamera != null)
        {
            cameraController.ActiveCameraMode.CinemachineCamera.Lens.FieldOfView = FOV;
        }
    }

    /// <summary>
    /// Локально раскрывает карту при наведении луча.
    /// </summary>
    public void HandleCardFocus(CardVisualController cardVisual)
    {
        if (cardVisual == null) return;

        revealedCardsThisFrame.Add(cardVisual);

        if (!cardVisual.isVisible)
        {
            cardVisual.isVisible = true;
            cardVisual.UpdateCardVisuals();
            previouslyHiddenCards.Add(cardVisual);
        }
    }

    /// <summary>
    /// Скрывает конкретную карту, когда луч уходит с неё.
    /// </summary>
    public void ReleaseCard(CardVisualController cardVisual)
    {
        if (cardVisual == null) return;

        if (cardVisual.isVisible)
        {
            cardVisual.isVisible = false;
            cardVisual.UpdateCardVisuals();
        }

        previouslyHiddenCards.Remove(cardVisual);
        revealedCardsThisFrame.Remove(cardVisual);
    }


    /// <summary>
    /// Возвращает все раскрытые карты к закрытому состоянию при выходе из Внимания.
    /// </summary>
    public void ResetRevealedCards()
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
        revealedCardsThisFrame.Clear();
    }
}