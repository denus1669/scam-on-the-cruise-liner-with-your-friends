using Assets.Casino.Games;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Bot
{
    [RequireComponent(typeof(BotAgent))]
    public class BotDispleasureController : NetworkBehaviour
    {
        [Header("Настройки недовольства")]
        [SerializeField] private float maxDispleasure = 100f;
        [SerializeField] private float baseDispleasureRate = 5f;
        [SerializeField] private float watchersDispleasureRate = 5f;
        [SerializeField] private float decayRate = 2f;


        [SerializeField] private NetworkVariable<float> currentDispleasure = new NetworkVariable<float>(0f);

        private HashSet<ulong> watchers = new HashSet<ulong>();
        private BotAgent botAgent;

        public float MaxDispleasure => maxDispleasure;
        public float CurrentDispleasure => currentDispleasure.Value;
        public event Action<float> OnDispleasureChanged;

        private void Awake()
        {
            botAgent = GetComponent<BotAgent>();
        }
        private void Update()
        {
            if (!IsServer) return;
            WatchersAddDispleasure();
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            currentDispleasure.OnValueChanged += OnDispleasureValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            currentDispleasure.OnValueChanged -= OnDispleasureValueChanged;
            watchers.Clear();
            base.OnNetworkDespawn();
        }

        private void OnDispleasureValueChanged(float oldValue, float newValue)
        {
            OnDispleasureChanged?.Invoke(newValue);
        }

        [Rpc(SendTo.Server)]
        public void AddWatcherServerRpc(ulong clientId)
        {
            watchers.Add(clientId);
        }

        [Rpc(SendTo.Server)]
        public void RemoveWatcherServerRpc(ulong clientId)
        {
            watchers.Remove(clientId);
        }

        [Rpc(SendTo.Server)]
        public void AddInstantDispleasureServerRpc(float displeasureValue)
        {
            if (!IsServer) return;
            AddInstantDispleasure(displeasureValue);
        }

        public void AddInstantDispleasure(float amount)
        {
            currentDispleasure.Value = Mathf.Clamp(currentDispleasure.Value + amount, 0f, maxDispleasure);

            if (currentDispleasure.Value >= maxDispleasure)
            {
                HandleMaxDispleasure(GetCurrentTable());
            }
        }

        private void HandleMaxDispleasure(IGameTable table)
        {
            Debug.LogWarning($"[Displeasure] Бот {gameObject.name} вышел из себя!");

            if (table != null)
            {
                table.ForceStopGame(isCheaterBot: false, reason: "Harassment");
            }

            botAgent.GoToExit();
            ResetDispleasure();
        }

        public void ResetDispleasure()
        {
            if (!IsServer) return;

            currentDispleasure.Value = 0f; // Триггерит OnValueChanged автоматически
            watchers.Clear();
            // Ручной вызов OnDispleasureChanged удалён — он уже вызван через OnValueChanged
        }

        private IGameTable GetCurrentTable()
        {
            return botAgent != null ? botAgent.CurrentTable : null;
        }

        private void WatchersAddDispleasure()
        {
            if (watchers.Count > 0)
            {
                float increase = watchersDispleasureRate * Time.deltaTime * watchers.Count;
                currentDispleasure.Value = Mathf.Clamp(currentDispleasure.Value + increase, 0f, maxDispleasure);

                if (currentDispleasure.Value >= maxDispleasure)
                {
                    HandleMaxDispleasure(GetCurrentTable());
                }
            }
        }

        private void ProcessDecay()
        {
            if (currentDispleasure.Value <= 0f) return;
            currentDispleasure.Value = Mathf.Clamp(
                currentDispleasure.Value - decayRate * Time.deltaTime,
                0f, maxDispleasure
            );
        }
    }
}