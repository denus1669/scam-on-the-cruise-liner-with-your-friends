using System;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Вешается на коллайдер объекта, на который можно смотреть.
    /// Перехватывает вызов от луча (IAttentionTarget) и превращает его в C# события 
    /// для других компонентов на этом же объекте.
    /// </summary>
    public class AttentionTargetReceiver : MonoBehaviour, IAttentionTarget
    {
        // События, на которые будут подписываться другие скрипты на этом объекте
        public event Action<ulong> OnAttentionEntered;
        public event Action<ulong> OnAttentionExited;

        public void OnAttentionEnter(ulong watcherClientId)
        {
            OnAttentionEntered?.Invoke(watcherClientId);
        }

        public void OnAttentionExit(ulong watcherClientId)
        {
            OnAttentionExited?.Invoke(watcherClientId);
        }
    }
}