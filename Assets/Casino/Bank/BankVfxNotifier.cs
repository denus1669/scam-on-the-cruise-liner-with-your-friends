using System;
using Unity.Netcode;
using UnityEngine;

    /// <summary>
    /// Лёгкая структура для передачи данных о транзакции по сети.
    /// Реализует INetworkSerializable для эффективной сериализации в ClientRpc.
    /// </summary>
    public struct TransactionVfxPacket : INetworkSerializable
    {
        public ulong OperatorClientId;
        public int Amount;
        public TransactionType Type;
        public string Reason;
        public string TableType;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref OperatorClientId);
            serializer.SerializeValue(ref Amount);

            // Enum сериализуем как byte для экономии трафика
            byte typeByte = (byte)Type;
            serializer.SerializeValue(ref typeByte);
            if (serializer.IsReader) Type = (TransactionType)typeByte;

            serializer.SerializeValue(ref Reason);
            serializer.SerializeValue(ref TableType);
        }
    }

    /// <summary>
    /// Отвечает ТОЛЬКО за рассылку VFX-событий о транзакциях всем клиентам.
    /// Сервер: слушает OnTransactionCompleted → рассылает ClientRpc.
    /// Клиент: получает RPC → излучает локальное событие OnTransactionVfxReceived.
    /// </summary>
    public class BankVfxNotifier : NetworkBehaviour
    {
        [Header("Зависимости")]
        [SerializeField] private CasinoBank casinoBank;

        /// <summary>
        /// Срабатывается на КАЖДОМ клиенте при получении уведомления о транзакции.
        /// UI/VFX системы подписываются на это событие для отображения "+1$" / "-1$".
        /// </summary>
        public event Action<TransactionVfxPacket> OnTransactionVfxReceived;

        private void Reset()
        {
            casinoBank = GetComponentInParent<CasinoBank>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                if (casinoBank == null)
                {
                    Debug.LogError("[BankVfxNotifier] CasinoBank не назначен!");
                    return;
                }

                casinoBank.OnTransactionCompleted += HandleTransactionCompleted;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && casinoBank != null)
            {
                casinoBank.OnTransactionCompleted -= HandleTransactionCompleted;
            }

            base.OnNetworkDespawn();
        }

        private void HandleTransactionCompleted(BankTransaction transaction)
        {
            // Преобразуем в пакет для сети и рассылаем всем клиентам
            var packet = new TransactionVfxPacket
            {
                OperatorClientId = transaction.OperatorClientId,
                Amount = transaction.Amount,
                Type = transaction.Type,
                Reason = transaction.Reason,
                TableType = transaction.TableType
            };

            BroadcastTransactionVfxClientRpc(packet);
        }

        [ClientRpc]
        private void BroadcastTransactionVfxClientRpc(TransactionVfxPacket packet)
        {
            // Этот метод выполняется на ВСЕХ клиентах
            OnTransactionVfxReceived?.Invoke(packet);

            Debug.Log($"[VFX] Клиент получил транзакцию: {packet.Type} {packet.Amount} ({packet.Reason})");
        }
    }
