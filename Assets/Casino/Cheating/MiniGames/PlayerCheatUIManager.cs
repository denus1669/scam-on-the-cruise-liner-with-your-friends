using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Компонент, который отвечает за показ мини-игр мухлежа локальному игроку.
/// Должен висеть на префабе игрока (чтобы иметь доступ к CheatController) 
/// ИЛИ на Canvas менеджере, но тогда его нужно связать с локальным игроком.
/// </summary>
public class PlayerCheatUIManager : NetworkBehaviour
{
    [SerializeField] private CheatController playerCheatController;

    [Header("Список доступных мини-игр")]
    [Tooltip("Ссылки на компоненты мини-игр, которые лежат в Canvas")]
    [SerializeField] private List<CheatMinigameBase> availableMinigames;

    private CheatMinigameBase activeMinigame;

    public override void OnNetworkSpawn()
    {
        // Логика UI нужна только владельцу (со стороны клиента)
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        playerCheatController = GetComponent<CheatController>();
        if (playerCheatController != null)
        {
            // Подписываемся на приказ от сервера открыть интерфейс
            playerCheatController.OnLocalPlayerCheatUIRequested += HandleCheatUIRequest;
            // Подписываемся на отмену (если нас поймали ДО того, как мы прошли игру)
            playerCheatController.OnCheatCanceled += HandleCheatCanceled;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (playerCheatController != null)
        {
            playerCheatController.OnLocalPlayerCheatUIRequested -= HandleCheatUIRequest;
            playerCheatController.OnCheatCanceled -= HandleCheatCanceled;
        }
    }

    private void HandleCheatUIRequest(CheatAction cheatAction)
    {
        if (availableMinigames == null || availableMinigames.Count == 0)
        {
            Debug.LogError("[UI] Нет доступных мини-игр! Автоматический провал.");
            playerCheatController.FinishPlayerCheatServerRpc(false);
            return;
        }


        // Выбираем случайную мини-игру из пула
        int randomIndex = Random.Range(0, availableMinigames.Count);
        activeMinigame = availableMinigames[randomIndex];

        Debug.Log($"[UI] Запуск мини-игры: {activeMinigame.gameObject.name}");

        // Запускаем и передаем коллбэк, который сработает при завершении
        activeMinigame.StartMinigame(cheatAction, OnMinigameFinished);
    }

    /// <summary>
    /// Коллбэк от мини-игры.
    /// </summary>
    private void OnMinigameFinished(bool isSuccess)
    {
        activeMinigame = null;

        // Отправляем результат на сервер
        playerCheatController.FinishPlayerCheatServerRpc(isSuccess);
    }

    /// <summary>
    /// Отмена мухлежа сервером (нас поймали). Нужно закрыть мини-игру, если она была запущена.
    /// </summary>
    private void HandleCheatCanceled()
    {
        if (activeMinigame != null)
        {
            Debug.Log("[UI] Мухлеж прерван сервером (нас поймали!). Закрываем мини-игру.");
            activeMinigame.ForceClose();
            activeMinigame = null;
        }
    }
}