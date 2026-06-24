using System;
using Unity.Netcode;
using UnityEngine;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Отвечает за выполнение шлепка и рассылку сетевых событий.
    /// Является "Издателем" событий для других локальных компонентов (Звуки, UI, Тряска камеры).
    /// </summary>
    [RequireComponent(typeof(SlapTargetDetector))]
    public class PlayerSlapController : NetworkBehaviour
    {
        [Header("Зависимости")]
        [SerializeField] private SlapTargetDetector targetDetector;

        // Событие для локального игрока (Звук взмаха, локальная анимация руки)
        public event Action OnLocalSlapExecuted;

        // Событие для всех (Анимация взмаха, которую видят другие игроки)
        public event Action OnGlobalSlapExecuted;

        private void Reset()
        {
            targetDetector = GetComponent<SlapTargetDetector>();
        }

        /// <summary>
        /// Точка входа. Вызывается из SlapGameListener при нажатии кнопки.
        /// </summary>
        public void ExecuteSlap()
        {

            if (targetDetector != null && targetDetector.HasTarget)
            {
                Debug.Log(targetDetector.CurrentTarget);
                targetDetector.CurrentTarget.ExecuteSlap(NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                Debug.Log("[Slap] Удар в воздух! Цели нет.");
            }
        }

        private void HandleSlap(SlapTargetDetector slapTargetDetector)
        {
            OnGlobalSlapExecuted?.Invoke();

            if (IsOwner)
            {
                OnLocalSlapExecuted?.Invoke();
            }
        }       
    }
}





/*
 * 1) Нажатие кнопки 
 * 2) Поиск таргета 
 * 3) Обработчик бота который вызывает нужный класс 
 * 4) Обработик персонажа который вызывавет нужный класс 
 * 5) Классы бота и персонадж
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 */