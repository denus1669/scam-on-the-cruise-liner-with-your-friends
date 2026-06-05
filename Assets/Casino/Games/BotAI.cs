/*

using System.Collections;
using UnityEngine;

/// <summary>
/// Простейший ИИ бота для игры в BlackGreg.
/// Следит за состоянием стола и принимает решения во время своего хода.
/// </summary>
public class BotAI : MonoBehaviour
{
    [Header("Связи")]
    [SerializeField, Tooltip("Стол, за которым играет бот")]
    private BlackGregManager tableManager;

    [Header("Настройки логики")]
    [SerializeField, Tooltip("Количество очков, при котором бот перестанет брать карты (пас)")]
    private int stopTakingCardsThreshold = 16;

    [SerializeField, Tooltip("Задержка перед действием (имитация раздумий)")]
    private float thinkDelay = 1.5f;

    private bool isTakingTurn = false;

    private void Update()
    {
        if (tableManager == null) return;

        // Если сейчас ход бота, и он еще не начал действовать
        if (tableManager.CurrentState == BlackGregManager.GameState.BotTurn && !isTakingTurn)
        {
            StartCoroutine(PerformTurnRoutine());
        }
    }

    /// <summary>
    /// Корутина хода бота. Позволяет делать паузы между взятием карт.
    /// </summary>
    private IEnumerator PerformTurnRoutine()
    {
        isTakingTurn = true;

        // Ждем немного перед первым действием
        yield return new WaitForSeconds(thinkDelay);

        // Бот должен взять минимум 2 карты по правилам
        // Здесь мы передаем список карт бота из менеджера. 
        // Так как переменная приватная в менеджере, нам нужно либо сделать публичный метод получения очков бота, 
        // либо бот будет просто "запрашивать" карту, пока стол не скажет хватит.

        bool botWantsMoreCards = true;

        while (botWantsMoreCards && tableManager.CurrentState == BlackGregManager.GameState.BotTurn)
        {
            // Берем карту
            tableManager.BotDrawCard();

            // Имитируем время на анализ новой карты
            yield return new WaitForSeconds(thinkDelay);

            // Решаем, брать ли еще. 
            // Для этого мы немного схитрим и используем логику стола для оценки текущих очков.
            // В идеале бот должен иметь свой инвентарь карт, но для MVP YAGNI мы оцениваем через менеджер:
            // Примечание: Для этого потребуется публичный метод GetBotScore() в BlackGregManager, 
            // но мы можем просто передать текущие очки через делегат или сделать счетчик внутри самого бота.
            // Давай сделаем простую симуляцию: бот просто берет 2-3 карты наугад или мы добавим публичный счетчик.

            // Заглушка: Бот делает пас после 2-3 карт (чтобы не усложнять код сейчас). 
            // Позже мы свяжем это с реальными очками.
            int currentCardsCount = Random.Range(2, 5); // временная заглушка

            if (currentCardsCount >= 3) // Условная логика для прототипа
            {
                botWantsMoreCards = false;
            }
        }

        tableManager.BotPass();
        isTakingTurn = false;
    }
}

*/