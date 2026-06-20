using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Компонент вешается на коллайдер руки бота (там где спавнятся карты).
/// Отвечает ТОЛЬКО за локальное отображение карт при просмотре.
/// </summary>
public class CardHandInspector : MonoBehaviour, IAttentionTarget
{
    [Header("Настройки")]
    [Tooltip("Ссылка на контроллер недовольства бота (если есть)")]
    [SerializeField] private BotDispleasureController botDispleasure;

    private CardView[] currentCards;

    private void Reset()
    {
        // Автоматически пытаемся найти контроллер на родителе при добавлении скрипта
        botDispleasure = GetComponentInParent<BotDispleasureController>();
    }

    public void OnAttentionEnter(ulong watcherClientId)
    {
        // 1. Сообщаем боту, что на него начали смотреть
        if (botDispleasure != null)
        {
            botDispleasure.AddWatcherServerRpc(watcherClientId);
        }

        // 2. Ищем все актуальные карты в руке в данный момент (решает проблему с добавлением новых карт)
        currentCards = GetComponentsInChildren<CardView>(true);

        // 3. Локально показываем их игроку
        foreach (var card in currentCards)
        {
            if (card != null)
            {
                card.SetVisible(true);
            }
        }
    }

    public void OnAttentionExit(ulong watcherClientId)
    {
        // 1. Сообщаем боту, что мы перестали смотреть
        if (botDispleasure != null)
        {
            botDispleasure.RemoveWatcherServerRpc(watcherClientId);
        }

        // 2. Скрываем карты обратно
        if (currentCards != null)
        {
            foreach (var card in currentCards)
            {
                if (card != null)
                {
                    card.SetVisible(false);
                }
            }
        }
    }
}