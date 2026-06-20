    /// <summary>
    /// Интерфейс для объектов, на которые можно смотреть в режиме внимания.
    /// </summary>
    public interface IAttentionTarget
    {
        /// <summary>
        /// Вызывается, когда луч внимания попадает на объект.
        /// Передает ID игрока, который смотрит.
        /// </summary>
        void OnAttentionEnter(ulong watcherClientId);

        /// <summary>
        /// Вызывается, когда луч внимания покидает объект.
        /// </summary>
        void OnAttentionExit(ulong watcherClientId);
    }
