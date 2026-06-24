using Unity.Netcode;
using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// Обрабатывает удары по рукам для БОТА.
/// Маршрутизирует взаимодействие в зависимости от текущего состояния бота (мухлеж, блеф, покой).
/// </summary>
[RequireComponent(typeof(BotAgent))]
public class BotHandInteractionHandler : NetworkBehaviour, IHandInteractable
{
    // Ссылки на контроллеры для проверки состояний
    [SerializeField] private CheatController cheatController;
    [SerializeField] private BlackGregBotBehavior botBehavior;

    private void Reset()
    {
        // Автопоиск при добавлении компонента в редакторе
        cheatController = GetComponent<CheatController>();
        botBehavior = GetComponent<BlackGregBotBehavior>();
    }

    public void ExecuteHandInteraction(ulong interactorClientId)
    {
        // В NGO DA взаимодействие с правилами игры должно валидироваться на сервере
        if (!IsServer)
        {
            RequestInteractionServerRpc(interactorClientId);
            return;
        }

        ResolveInteraction(interactorClientId);
    }

    [Rpc(SendTo.Server)]
    private void RequestInteractionServerRpc(ulong interactorClientId)
    {
        ResolveInteraction(interactorClientId);
    }

    /// <summary>
    /// Серверная логика определения ситуации и вызова конкретного эффекта.
    /// </summary>
    private void ResolveInteraction(ulong interactorClientId)
    {
        // 1. Проверяем, мухлюет ли бот прямо сейчас
        if (cheatController != null && cheatController.IsCheating)
        {
            HandleBotCatchingCheat(interactorClientId);
            return;
        }

        // 2. Проверяем, блефует ли бот (проигрывает пустую анимацию)
        if (botBehavior != null && botBehavior.IsBotBluffingActive)
        {
            HandleBotBluffingCaught(interactorClientId);
            return;
        }

        // 3. Бот ничего не делает (сидит спокойно)
        HandleBotIdleCaught(interactorClientId);
    }

    #region Заготовки для будущих эффектов (Stubs)

    protected virtual void HandleBotCatchingCheat(ulong interactorClientId)
    {
        Debug.Log($"[ЗАГЛУШКА] Игрок {interactorClientId} поймал бота {gameObject.name} за МУХЛЕЖОМ!");
        // TODO: Остановить игру, засчитать победу игроку, вызвать RPC для VFX/UI
    }

    protected virtual void HandleBotBluffingCaught(ulong interactorClientId)
    {
        Debug.Log($"[ЗАГЛУШКА] Игрок {interactorClientId} ударил бота {gameObject.name} по рукам во время БЛЕФА!");
        // TODO: Прервать анимацию блефа, возможно наложить штраф на игрока за ложное обвинение или просто сломать блеф
    }

    protected virtual void HandleBotIdleCaught(ulong interactorClientId)
    {
        Debug.Log($"[ЗАГЛУШКА] Игрок {interactorClientId} ударил бота {gameObject.name} по рукам БЕЗ ПРИЧИНЫ!");
        // TODO: Сильный штраф игроку (рост раздражения бота, потеря репутации/фишек)
    }

    #endregion
}