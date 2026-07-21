using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Отвечает ТОЛЬКО за сетевое состояние режима внимания.
    /// Является "Издателем" событий для других компонентов.
    /// </summary>
    public class PlayerAttentionController : NetworkBehaviour
    {
        // Делаем переменную приватной, чтобы никто снаружи не мог её случайно сломать
        private readonly NetworkVariable<bool> _isAttention = new NetworkVariable<bool>(
            value: false,
            writePerm: NetworkVariableWritePermission.Server,
            readPerm: NetworkVariableReadPermission.Everyone
        );
        // Событие для локального игрока (Камера, UI, Звуки в ушах игрока)
        public event Action<bool> OnLocalAttentionChanged;

        // Событие для всех (Анимации персонажа, которые должны видеть другие игроки)
        public event Action<bool> OnGlobalAttentionChanged;

        public bool IsAttentionActive => _isAttention.Value;


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isAttention.OnValueChanged += HandleAttentionStateChanged;

            // Принудительно вызываем обновление при спавне, чтобы инициализировать локальные компоненты
            HandleAttentionStateChanged(false, _isAttention.Value);
        }

        public override void OnNetworkDespawn()
        {
            _isAttention.OnValueChanged -= HandleAttentionStateChanged;
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Вызывается локально по кнопке инпута.
        /// </summary>
        public void ToggleAttentionLocal()
        {
            if (!IsOwner) return;

            ToggleAttention(!_isAttention.Value);
        }

        //[Rpc(SendTo.Server)]
        public void ToggleAttention(bool newState)
        {

                _isAttention.Value = newState;
        }

        private void HandleAttentionStateChanged(bool previous, bool current)
        {
            // 1. Оповещаем глобальные системы (например, скрипт анимаций)
            OnGlobalAttentionChanged?.Invoke(current);

            // 2. Оповещаем только локальные системы (камера, локальный сканер)
            if (IsOwner)
            {
                OnLocalAttentionChanged?.Invoke(current);
            }
        }
    }
}