using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Интерактивный компонент для начала мухлежа игроком.
/// Вешается на префаб игрока. Работает только во время DayActive и если игрок сидит за столом.
/// </summary>
public class CheatInteractable : NetworkBehaviour, IInteractable
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 5; // Приоритет ниже, чем у двери (10), чтобы не перебивать её
    [SerializeField] private string promptText = "Мухлевать (E)";

    // Ссылка на контроллер мухлежа (ищем на том же объекте при старте)
    [SerializeField] private PlayerCheatController _cheatController;

    // Кэш для оптимизации обновления текста подсказки
    private string m_CachedPrompt;
    private bool m_LastCanCheat;

    public InteractionTriggerMode TriggerMode => triggerMode;
    public int Priority => priority;
    public float HoldDuration => 0f; // Мгновенное взаимодействие

    private void Awake()
    {
        // Ищем CheatController на этом же GameObject (так как оба висят на префабе игрока)
        _cheatController = GetComponent<PlayerCheatController>();
    }

    /// <summary>
    /// Динамический текст подсказки.
    /// </summary>
    public string InteractionPromptText
    {
        get
        {
            // Проверяем, может ли игрок сейчас мухлевать (не мухлюет ли он уже)
            bool canCheatNow = _cheatController != null && _cheatController.CanCheat();

            if (canCheatNow != m_LastCanCheat || m_CachedPrompt == null)
            {
                m_LastCanCheat = canCheatNow;
                m_CachedPrompt = canCheatNow ? promptText : "Вы уже мухлюете...";
            }

            return m_CachedPrompt;
        }
    }

    /// <summary>
    /// Взаимодействие доступно, если идёт день, игрок за столом и ещё не мухлюет.
    /// </summary>
    public bool CanInteract(GameObject interactor)
    {
        var manager = GameSessionManager.Instance;
        if (manager == null) return false;

        // 1. Проверка фазы (мухлевать можно только в активный день)
        if (manager.CurrentState != GameState.DayActive) return false;

        // 2. Проверка, что взаимодействует именно владелец этого объекта (локальный игрок)
        if (!IsOwner) return false;

        // 3. Проверка контроллера (свободен ли он от другого мухлежа)
        if (_cheatController == null || !_cheatController.CanCheat()) return false;

        // 4. Проверка, сидит ли игрок вообще за каким-то столом
        if (GameTableManager.Instance == null) return false;
        IGameTable currentTable = GameTableManager.Instance.GetTableOccupiedByPlayer(OwnerClientId);

        return currentTable != null;
    }

    /// <summary>
    /// Запускает процесс мухлежа через RPC.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        if (!IsSpawned) return;

        // Отправляем запрос на сервер. 
        // Сервер сам проверит все условия внутри RequestCheatServerRpc.
        // specificCheatName оставляем пустым, чтобы сервер выбрал случайный чит из доступных.
        Debug.Log("[CheatInteractable] Игрок нажал кнопку мухлежа!");
        _cheatController.RequestCheatServerRpc();
    }
}