// Casino/Scripts/Runtime/Minigames/BlackjackDealerController.cs
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Клиентский контроллер ввода для Крупье.
/// Управляет запуском мини-игр мухлежа, отображает подозрение и фазы.
/// Не изменяет состояние напрямую. Только отправляет намерения на хост.
/// </summary>
public class BlackjackDealerController : MonoBehaviour
{
    [SerializeField] private IBlackjackTable _table;
    [SerializeField] private Slider _suspicionSlider;
    [SerializeField] private Text _phaseText;
    [SerializeField] private Button _cheatButton;
    [SerializeField] private GameObject _minigameOverlay;

    private ulong _localClientId;
    private bool _isDealer;
    private bool _minigameActive;

    private void Awake()
    {
        _cheatButton.onClick.AddListener(OnCheatButtonPressed);
        _cheatButton.interactable = false;
        _minigameOverlay.SetActive(false);
    }

    private void OnEnable()
    {
        if (_table == null)
        {
            return;
        }

        _localClientId = NetworkManager.Singleton.LocalClientId;
        SubscribeToEvents();

        SyncInitialUI();
    }

    private void OnDisable()
    {
        if (_table != null)
        {
            UnsubscribeFromEvents();
        }
    }

    private void SubscribeToEvents()
    {
        _table.OnPhaseChanged += OnPhaseChanged;
        _table.OnSuspicionChanged += OnSuspicionChanged;
        _table.OnTableStateChanged += OnTableStateChanged;
    }

    private void UnsubscribeFromEvents()
    {
        _table.OnPhaseChanged -= OnPhaseChanged;
        _table.OnSuspicionChanged -= OnSuspicionChanged;
        _table.OnTableStateChanged -= OnTableStateChanged;
    }

    private void SyncInitialUI()
    {
        OnPhaseChanged(_table.CurrentPhase);
        OnSuspicionChanged(_table.CurrentSuspicion);
        OnTableStateChanged();
        _isDealer = _table.DealerOwnerId == _localClientId;
    }

    private void OnPhaseChanged(BlackjackRoundPhase phase)
    {
        _phaseText.text = phase.ToString();

        if (phase != BlackjackRoundPhase.CheatWindow)
        {
            _cheatButton.interactable = false;
            CloseMinigameLocally();
        }
    }

    private void OnSuspicionChanged(float newValue)
    {
        _suspicionSlider.value = newValue / 100f;
    }

    private void OnTableStateChanged()
    {
        _isDealer = _table.DealerOwnerId == _localClientId;
        bool canCheat = _isDealer && _table.IsCheatWindowActive && !_minigameActive;
        _cheatButton.interactable = canCheat;
    }

    private void OnCheatButtonPressed()
    {
        if (!_isDealer || !_table.IsCheatWindowActive)
        {
            return;
        }

        // Запрос на хост: разрешить мухлеж
        _table.RequestCheatServerRpc(_localClientId, DealerCheatType.MarkCards);

        // Локальный запуск мини-игры (QTE/тайминг/свайп)
        StartMinigameLocally();
    }

    private void StartMinigameLocally()
    {
        _minigameActive = true;
        _minigameOverlay.SetActive(true);
        _cheatButton.interactable = false;

        // Здесь подключается реальная мини-игра.
        // По завершении вызывает CompleteMinigameLocally(success)
        // Для демо: эмуляция через Invoke
        Invoke(nameof(SimulateMinigameComplete), 2f);
    }

    private void SimulateMinigameComplete()
    {
        // В реальности результат определяется вводом игрока
        bool success = UnityEngine.Random.value > 0.3f;
        CompleteMinigameLocally(success);
    }

    private void CompleteMinigameLocally(bool success)
    {
        _minigameActive = false;
        _minigameOverlay.SetActive(false);

        // Отправка результата на хост для валидации
        _table.SubmitCheatResultServerRpc(_localClientId, DealerCheatType.MarkCards, success);
    }

    private void CloseMinigameLocally()
    {
        if (_minigameActive)
        {
            _minigameActive = false;
            _minigameOverlay.SetActive(false);
            CancelInvoke(nameof(SimulateMinigameComplete));
        }
    }

    /// <summary>
    /// Вызывается из UI или ModularInteractable при подходе к столу.
    /// </summary>
    public void TryBecomeDealer()
    {
        _table.AssignDealerServerRpc(_localClientId);
    }

    /// <summary>
    /// Покинуть стол.
    /// </summary>
    public void TryLeave()
    {
        _table.LeaveTableServerRpc(_localClientId);
    }
}