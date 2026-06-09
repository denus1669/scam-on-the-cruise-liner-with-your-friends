using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Интерактивный объект "Место за столом". 
/// Логически закрепляет стол за игроком без телепортации.
/// </summary>
public class TableInteractable : NetworkBehaviour, IInteractable
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 0;
    [SerializeField] private string promptText = "Занять стол (E)";

    // ЯВНО указываем права: Читают все, пишет только Сервер
    [SerializeField]
    private NetworkVariable<bool> isOccupied = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField]
    private NetworkVariable<ulong> occupiedByClientId = new NetworkVariable<ulong>(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public InteractionTriggerMode TriggerMode => triggerMode;
    public int Priority => priority;
    public string InteractionPromptText => promptText;

    // Свойство для подписки на изменения владельца извне
    public NetworkVariable<ulong> OccupiedByClientIdVar => occupiedByClientId;

    public bool IsOccupied() => isOccupied.Value;
    public ulong GetOccupyingClientId() => occupiedByClientId.Value;

    public bool CanInteract(GameObject interactor)
    {
        // Взаимодействовать можно только если стол свободен
        return !isOccupied.Value;
    }

    public void Interact(GameObject interactor)
    {
        if (!IsSpawned || isOccupied.Value) return;

        // Получаем ID клиента, который нажал (E)
        ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;
        OccupyTableServerRpc(clientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void OccupyTableServerRpc(ulong clientId)
    {
        // ЗАЩИТА: Гарантируем, что этот код выполнится ТОЛЬКО на сервере
        if (!IsServer) return;

        if (isOccupied.Value) return;

        // Сервер фиксирует, что стол занят этим клиентом
        isOccupied.Value = true;
        occupiedByClientId.Value = clientId;

        Debug.Log($"Стол занят клиентом: {clientId}");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void LeaveTableServerRpc(ulong clientId)
    {
        if (!IsServer) return;

        // Убеждаемся, что запрос на уход отправляет именно текущий владелец стола
        if (occupiedByClientId.Value == clientId)
        {
            isOccupied.Value = false;
            occupiedByClientId.Value = ulong.MaxValue;
            Debug.Log($"Стол освобожден клиентом: {clientId}");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        // Проверяем, что из триггера вышел игрок (сетевой объект)
        NetworkObject netObj = other.GetComponent<NetworkObject>();

        // Только сам локальный клиент фиксирует свой уход и сообщает серверу
        if (netObj != null && netObj.IsLocalPlayer)
        {
            // Если этот клиент сейчас является владельцем стола
            if (occupiedByClientId.Value == netObj.OwnerClientId)
            {
                LeaveTableServerRpc(netObj.OwnerClientId);
            } 
        }
    }
}