using Blocks.Gameplay.Core;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Компонент мухлежа, висящий на СТОЛЕ (или конкретном месте за столом).
/// Теперь он не привязан к владельцу игрока, а проверяет входящего игрока.
/// </summary>
public class TableCheatInteractable : NetworkBehaviour, IInteractable
{
    [SerializeField] GameTable thisTableComponent;

    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 5;
    [SerializeField] private string promptText = "Мухлевать (E)";

    [SerializeField] private BlackGregCheatAction availableCheats;


    // Ссылка на контроллер мухлежа теперь берется динамически у игрока, который взаимодействует
    // Или можно иметь ссылку на общий менеджер стола, если логика общая

    public InteractionTriggerMode TriggerMode => triggerMode;
    public int Priority => priority;
    public float HoldDuration => 0f;

    // Кэш для текста подсказки (опционально, можно упростить)
    private string m_CachedPrompt;
    private bool m_LastCanCheat;
    private GameObject m_LastTarget;

    public string InteractionPromptText
    {
        get
        {
            // Текст может зависеть от состояния конкретного игрока, но для простоты
            // часто используют статический текст, а проверку делают в CanInteract.
            // Если нужно динамически менять текст ("Вы уже мухлюете"), нужно знать, кто смотрит.
            // В текущей архитектуре InteractionAddon передает цель, но не всегда контекст для UI.
            // Для начала оставим статический или простой текст.
            return promptText;
        }
    }

    /// <summary>
    /// Проверка возможности взаимодействия.
    /// interactor — этоGameObject игрока, который нажал кнопку или навел курсор.
    /// </summary>
    public bool CanInteract(GameObject interactor)
    {
        if (interactor == null) return false;

        // 2. Получаем компоненты игрока, который взаимодействует
        // Предполагаем, что у игрока есть CorePlayerManager или аналогичный идентификатор
        var playerManager = interactor.GetComponent<CorePlayerManager>();
        if (playerManager == null)
        {
            // Пробуем найти в детях, если интерактор - корень, а менеджер ниже
            playerManager = interactor.GetComponentInChildren<CorePlayerManager>();
        }

        if (playerManager == null) return false;

        // 3. Проверка: сидит ли этот игрок за ЭТИМ столом
        if (GameTableManager.Instance == null) return false;

        // Получаем ID игрока, который пытается взаимодействовать
        ulong interactorId = playerManager.OwnerClientId;

        // Важно: проверяем, что игрок сидит именно за этим столом (или любым, если логика позволяет)
        // Если GetTableOccupiedByPlayer возвращает интерфейс стола, сравните его с этим объектом

        // Если у вас несколько столов, убедитесь, что игрок сидит именно за этим конкретным столом
      
         if (!thisTableComponent.IsGameStarted || interactorId != thisTableComponent.OccupiedByClientId) return false;

        // 4. Проверка контроллера мухлежа у этого конкретного игрока
        var cheatController = playerManager.GetComponent<PlayerCheatController>(); // Или FindObjectOfType/GetChild
        
        if (cheatController == null || !cheatController.CanCheat())
        {
            return false;
        }

        return true;
    }

    public void Interact(GameObject interactor)
    {
        if (!IsSpawned) return;

        var playerManager = interactor.GetComponent<CorePlayerManager>();
        if (playerManager == null) playerManager = interactor.GetComponentInChildren<CorePlayerManager>();

        if (playerManager == null) return;

        var cheatController = playerManager.GetComponent<PlayerCheatController>();
        if (cheatController == null) return;

        Debug.Log($"[TableCheatInteractable] Игрок {playerManager.OwnerClientId} начал мухлеж! {availableCheats.CheatName} ");

        // Вызываем метод на контроллере игрока
        cheatController.RequestCheatServerRpc(availableCheats.CheatForCodeName);
    }
}
