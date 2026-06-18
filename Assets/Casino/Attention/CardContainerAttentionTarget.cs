using Blocks.Gameplay.Core;
using System.Collections;
using UnityEngine;

/// <summary>
/// Вешается на объект-контейнер с коллайдером-триггером (например, рука бота).
/// При попадании луча режима внимания раскрывает все дочерние карты (CardVisualController),
/// при выходе – скрывает их.
/// </summary>
public class CardContainerAttentionTarget : MonoBehaviour, IAttentionTarget
{
    [Header("Отладка")]
    [SerializeField] private bool debugLog = true;

    // Кэшируем список карт, чтобы не искать каждый раз при входе/выходе
    private CardVisualController[] childCards;

    private Coroutine displeasureCoroutine;


    public void OnAttentionEnter(GameObject instigator)
    {
        if (debugLog)
            Debug.Log($"[Внимание] Луч вошёл в контейнер карт: {gameObject.name}");

        GetPlayerAttentionController(instigator);
        GetBotDispleasureController();
    }


    public void OnAttentionExit(GameObject instigator)
    {
        if (debugLog)
            Debug.LogWarning($"[Внимание] Луч покинул контейнер карт: {gameObject.name}");

        var attentionCtrl = instigator.GetComponent<PlayerAttentionController>();
        if (attentionCtrl == null) return;

        foreach (var card in childCards)
        {
            if (card != null)
                attentionCtrl.ReleaseCard(card);
        }

        BotDispleasureController botDispleasureController = GetComponentInParent<BotDispleasureController>();
        if (botDispleasureController != null)
        {
            if (debugLog) Debug.Log($"[Внимание] Сброс недовольства бота {botDispleasureController.gameObject.name}");

            // ВЫЗЫВАЕМ СЕТЕВОЙ МЕТОД
            botDispleasureController.ResetDispleasureServerRpc();
        }
        else
        {
            Debug.LogWarning($"[Внимание] BotDispleasureController не найден в {gameObject.name}");
        }

        // ОСТАНАВЛИВАЕМ КОРУТИНУ
        if (displeasureCoroutine != null)
        {
            StopCoroutine(displeasureCoroutine);
            displeasureCoroutine = null;
        }
    }

    private IEnumerator DispleasureRoutine(BotDispleasureController botDispleasureController)
    {
        // Бесконечный цикл, пока игрок в триггере
        while (true)
        {
            // Тикаем раздражение каждые 0.1 секунды (или каждый кадр, если yield return null)
            botDispleasureController.TickDispleasureServerRpc(); // amount = 0.1 за тик

            // Ждем 0.1 секунды до следующего тика
            yield return new WaitForSeconds(0.1f);

            // Если хотите каждый кадр:
            // yield return null;
        }
    }

    private void GetPlayerAttentionController(GameObject instigator)
    {
        var attentionCtrl = instigator.GetComponent<PlayerAttentionController>();
        if (attentionCtrl == null)
        {
            Debug.LogWarning($"[Внимание] У объекта {instigator.name} нет PlayerAttentionController");
            return;
        }
        // Если карты ещё не найдены или включен динамический поиск - обновляем список
        if (childCards == null || childCards.Length == 0)
        {
            childCards = GetComponentsInChildren<CardVisualController>(true);

            if (childCards.Length == 0)
            {
                Debug.LogWarning($"[Внимание] В объекте {gameObject.name} не найдено дочерних CardVisualController при входе.");
                return;
            }

            if (debugLog)
                Debug.Log($"[Внимание] Найдено {childCards.Length} карт в контейнере {gameObject.name}");
        }

        foreach (var card in childCards)
        {
            if (card != null)
            {
                if (debugLog)
                    Debug.Log($"[Внимание] Фокусируем карту: {card.gameObject.name}");
                attentionCtrl.HandleCardFocus(card);
            }
        }
    }

    private void GetBotDispleasureController()
    {
        // Получаем родительский BotDispleasureController
        BotDispleasureController botDispleasureController = GetComponentInParent<BotDispleasureController>();
        if (botDispleasureController != null)
        {
            if (debugLog)
                Debug.Log($"[Внимание] Найден родительский BotDispleasureController: {botDispleasureController.gameObject.name}");
            botDispleasureController.NotifySuspiciousActionStarted();


            // Запускаем корутину, если она еще не запущена
            if (displeasureCoroutine == null)
            {
                displeasureCoroutine = StartCoroutine(DispleasureRoutine(botDispleasureController));
            }

        }
    }

    public void OnAccuse(GameObject instigator)
    {
        // Будет реализовано позже, когда дойдём до обвинения
    }
}