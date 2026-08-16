using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.Timeline.Actions;
using UnityEngine;

public class PlayerCheatController : CheatController
{
    // --- СОБЫТИЯ ТОЛЬКО ДЛЯ ЛОКАЛЬНОГО ИГРОКА (Владельца) ---
    public event Action<CheatAction> OnLocalPlayerCheatUIRequested;

    /// <summary>
    /// Вызывается локальным игроком, чтобы запросить запуск мухлежа.
    /// Сервер сам определит, за каким столом сидит игрок, используя GameTableManager.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void RequestCheatServerRpc(string specificCheatName = "")
    {
        Debug.Log($"[CheatController] Игрок {OwnerClientId} запросил мухлеж. specificCheat={specificCheatName}");
        if (!CanCheat()) return;
        Debug.Log($"[CheatController] Сервер обрабатывает запрос мухлежа от {OwnerClientId}.");
        if (!IsServer || isCheating.Value) return;

        // 1. Ищем стол игрока через кешированный список менеджера 
        IGameTable currentTable = null;
        if (GameTableManager.Instance != null)
        {
            currentTable = GameTableManager.Instance.GetTableOccupiedByPlayer(OwnerClientId);
        }

        // Если игрок нажал "мухлевать", но он не за столом — игнорируем
        if (currentTable == null)
        {
            Debug.LogWarning($"[CheatController] Игрок {OwnerClientId} пытается мухлевать, но он не сидит за столом!");
            return;
        }

        // 2. Определяем конкретный чит
        CheatAction cheatToExecute = null;
        if (!string.IsNullOrEmpty(specificCheatName))
            cheatToExecute = availableCheats.Find(c => c.name == specificCheatName);


        if (cheatToExecute == null) return;

        // 3. Запускаем серверную логику мухлежа
        TryInitiateCheat(currentTable, cheatToExecute);
    }

    /// <summary>
    /// Пытается начать мухлеж. 
    /// Бот будет передавать specificCheat = null, чтобы выбрать случайный мухлеж.
    /// Игрок может передавать конкретный specificCheat, выбранный через UI.
    /// </summary>
    public override bool TryInitiateCheat(IGameTable table, CheatAction specificCheat)
    {
        if (!IsServer || isCheating.Value) return false;
        currentTable = table;

        // 1. Формируем правильный контекст

        if (!specificCheat.CanExecute(currentTable)) return false;

        // 3. Запускаем процесс
        CheatRoutine(specificCheat);
        return true;
    }

    protected override void CheatRoutine(CheatAction cheat)
    {
        base.CheatRoutine(cheat);
        PlayerCheatRoutine(cheat);
    }

    private void PlayerCheatRoutine(CheatAction cheat)
    {
        Debug.Log($"[CheatController] Игрок начинает мухлевать: {cheat.CheatName}");

        // Оповещаем всех клиентов (запуск подозрительной анимации и звуков на клиентах)
        NotifyCheatStartedClientRpc(cheat.AnimationTriggerName);

        //Даем команду локальному клиенту открыть UI мини - игры
        StartPlayerMinigameClientRpc(cheat.name);
    }

    [ClientRpc]
    private void StartPlayerMinigameClientRpc(string cheatActionName)
    {
        // Открываем UI только у владельца этого персонажа
        if (!IsOwner) return;

        Debug.Log("[CheatController] Получен запрос от сервера: Открыть UI мини-игры.");

        // Находим нужный SO по имени в нашем локальном списке, чтобы передать его в UI
        CheatAction match = availableCheats.Find(c => c.name == cheatActionName);
        if (match != null)
        {
            OnLocalPlayerCheatUIRequested?.Invoke(match);
        }
    }

    /// <summary>
    /// Вызывается клиентом (из UI Менеджера), когда он успешно прошел или завалил мини-игру.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void FinishPlayerCheatServerRpc(bool isSuccess)
    {
        if (!IsServer || !isCheating.Value) return;

        // Останавливаем корутину ожидания (тайм-аут)
        if (cheatCoroutine != null) StopCoroutine(cheatCoroutine);

        if (isSuccess)
        {
            CompleteCheat(currentCheatAction);
        }
        else
        {
            Debug.Log($"[CheatController] Игрок {OwnerClientId} завалил мини-игру.");
            // Игрок ошибся — это приравнивается к поимке за руку (побеждает казино/оппонент)
            HandleCheatCaught(ulong.MaxValue);
        }
    }

}
