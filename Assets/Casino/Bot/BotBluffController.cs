using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Контроллер блефа бота.
/// Управляет сетевым состоянием блефа и рассылает события всем клиентам.
/// В отличие от CheatController, используется ТОЛЬКО для ботов (не для игроков).
/// </summary>
public class BotBluffController : NetworkBehaviour
{
    [Header("Настройки блефа")]
    [Tooltip("Длительность блефа (время, в течение которого бот 'нервничает')")]
    [SerializeField] private float bluffDuration = 2.0f;

    [Tooltip("Список триггеров анимаций блефа (для будущего использования)")]
    [SerializeField] private string[] bluffAnimationTriggers = { "Bluff_Generic" };

    // Сетевое состояние блефа
    private readonly NetworkVariable<bool> _isBluffing = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Coroutine _bluffCoroutine;

    public bool IsBluffing => _isBluffing.Value;

    /// <summary>
    /// Событие изменения состояния блефа. 
    /// Срабатывает на всех клиентах (для индикаторов, аниматоров, звука).
    /// </summary>
    public event Action<bool> OnBluffStateChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _isBluffing.OnValueChanged += HandleBluffStateChanged;

        // Излучаем текущее состояние для поздних подписчиков
        OnBluffStateChanged?.Invoke(_isBluffing.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isBluffing.OnValueChanged -= HandleBluffStateChanged;
        base.OnNetworkDespawn();
    }

    private void HandleBluffStateChanged(bool previous, bool current)
    {
        OnBluffStateChanged?.Invoke(current);
    }

    /// <summary>
    /// Пытается начать блеф. Возвращает false, если блеф уже активен или вызов не с сервера.
    /// </summary>
    public bool TryBluff()
    {
        if (!IsServer) return false;
        if (_isBluffing.Value) return false;

        if (_bluffCoroutine != null)
            StopCoroutine(_bluffCoroutine);

        _bluffCoroutine = StartCoroutine(BluffRoutine());
        return true;
    }

    /// <summary>
    /// Принудительно прерывает блеф.
    /// </summary>
    public void CancelBluff()
    {
        if (!IsServer) return;
        if (_bluffCoroutine != null)
            StopCoroutine(_bluffCoroutine);

        _bluffCoroutine = null;
        _isBluffing.Value = false;
    }

    private IEnumerator BluffRoutine()
    {
        _isBluffing.Value = true;

        // Выбираем случайную анимацию (для будущей интеграции с Animator)
        string selectedTrigger = bluffAnimationTriggers.Length > 0
            ? bluffAnimationTriggers[UnityEngine.Random.Range(0, bluffAnimationTriggers.Length)]
            : null;

        NotifyBluffStartedClientRpc(selectedTrigger);

        float timer = bluffDuration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        _isBluffing.Value = false;
        _bluffCoroutine = null;
    }

    [ClientRpc]
    private void NotifyBluffStartedClientRpc(string triggerName)
    {
        // Будущие подписчики (Animator handler, звук) смогут использовать triggerName
        Debug.Log($"[BotBluffController] Блеф начат с триггером: {triggerName ?? "none"}");
    }
}