using Blocks.Gameplay.Core;
using UnityEngine;

/// <summary>
/// Отвечает ТОЛЬКО за локальное отображение карт при просмотре.
/// Ничего не знает о боте, его недовольстве или сети.
/// </summary>

[RequireComponent(typeof(AttentionTargetReceiver))]
public class CardHandInspector : MonoBehaviour
{
    private AttentionTargetReceiver _attentionReceiver;
    [SerializeField] private CardView[] _currentCards;

    // Добавляем флаг состояния и счетчик детей
    private bool _isFocused;
    private int _lastChildCount;

    private void Awake()
    {
        _attentionReceiver = GetComponent<AttentionTargetReceiver>();
    }

    private void OnEnable()
    {
        _attentionReceiver.OnAttentionEntered += HandleAttentionEnter;
        _attentionReceiver.OnAttentionExited += HandleAttentionExit;
    }

    private void OnDisable()
    {
        _attentionReceiver.OnAttentionEntered -= HandleAttentionEnter;
        _attentionReceiver.OnAttentionExited -= HandleAttentionExit;
    }
    private void Update()
    {
        // Если игрок прямо сейчас смотрит на руку бота, следим за изменениями
        if (_isFocused && transform.childCount != _lastChildCount)
        {
            // Количество дочерних объектов изменилось (бот взял или сбросил карту)
            // Заново собираем массив и применяем видимость
            RefreshCards(true);
        }
    }

    private void HandleAttentionEnter(ulong watcherClientId)
    {
        _isFocused = true;
        RefreshCards(true);
    }

    private void HandleAttentionExit(ulong watcherClientId)
    {
        _isFocused = false;
        RefreshCards(false);
    }
    private void RefreshCards(bool isVisible)
    {
        // Обновляем массив текущих карт
        _currentCards = GetComponentsInChildren<CardView>(true);
        // Запоминаем текущее количество объектов (чтобы отловить изменения в Update)
        _lastChildCount = transform.childCount;

        // Применяем видимость (показываем или скрываем)
        if (_currentCards != null)
        {
            foreach (var card in _currentCards)
            {
                if (card != null) card.SetVisible(isVisible);
            }
        }
    }
}