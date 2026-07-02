using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

/// <summary>
/// Координатор индикатора мухлежа. Живёт на CardHandPosition (ближе к рукам бота).
/// </summary>
public class BotCheatingReactionHandler : MonoBehaviour
{
    [Header("Источники данных (прокидываются из корня бота)")]
    [Tooltip("Ссылка на CheatController на корневом объекте бота")]
    [SerializeField] private CheatController cheatController;

    [Tooltip("Ссылка на AttentionTargetReceiver на этом же объекте CardHandPosition")]
    [SerializeField] private AttentionTargetReceiver attentionReceiver;

    [Header("Визуальный индикатор")]
    [Tooltip("Компонент, реализующий ICheatVisualIndicator (на этом же объекте или рядом)")]
    [SerializeField] private MonoBehaviour cheatIndicator;

    private ICheatVisualIndicator _indicator;
    private bool _isBotCheating;
    private bool _isLocalPlayerWatching;

    private void Awake()
    {
        _indicator = cheatIndicator as ICheatVisualIndicator;
        if (_indicator == null && cheatIndicator != null)
        {
            Debug.LogWarning($"[BotCheatingReaction] Компонент {cheatIndicator.GetType().Name} " +
                             $"не реализует ICheatVisualIndicator.");
        }
    }

    private void OnEnable()
    {
        if (cheatController != null)
            cheatController.OnCheatingStateChanged += HandleCheatingStateChanged;

        if (attentionReceiver != null)
        {
            attentionReceiver.OnAttentionEntered += HandleAttentionEntered;
            attentionReceiver.OnAttentionExited += HandleAttentionExited;
        }
    }

    private void OnDisable()
    {
        if (cheatController != null)
            cheatController.OnCheatingStateChanged -= HandleCheatingStateChanged;

        if (attentionReceiver != null)
        {
            attentionReceiver.OnAttentionEntered -= HandleAttentionEntered;
            attentionReceiver.OnAttentionExited -= HandleAttentionExited;
        }

        _indicator?.HideCheatIndicator();
    }

    private void HandleCheatingStateChanged(bool isCheating)
    {
        _isBotCheating = isCheating;
        UpdateIndicator();
    }

    private void HandleAttentionEntered(ulong watcherClientId)
    {
        if (NetworkManager.Singleton == null) return;
        if (watcherClientId != NetworkManager.Singleton.LocalClientId) return;

        _isLocalPlayerWatching = true;
        UpdateIndicator();
    }

    private void HandleAttentionExited(ulong watcherClientId)
    {
        if (NetworkManager.Singleton == null) return;
        if (watcherClientId != NetworkManager.Singleton.LocalClientId) return;

        _isLocalPlayerWatching = false;
        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        if (_indicator == null) return;

        if (_isBotCheating && _isLocalPlayerWatching)
            _indicator.ShowCheatIndicator();
        else
            _indicator.HideCheatIndicator();
    }
}