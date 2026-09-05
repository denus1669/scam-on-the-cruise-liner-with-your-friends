using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер, хранящий список всех активных столов и управляющий событиями их занятости.
/// </summary>
public class GameTableManager : MonoBehaviour
{
    public static GameTableManager Instance { get; private set; }

    // Список всех зарегистрированных столов
    private readonly List<IGameTable> _activeTables = new List<IGameTable>();

    // Глобальный ивент: срабатывает, когда ЛЮБОЙ стол освобождается
    public event Action OnAnyTableFreed;

    private void Awake()
    {
        // Реализация паттерна Синглтон
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Раскомментируй, если меню и игра на разных сценах
    }

    /// <summary>
    /// Стол регистрирует себя при спавне.
    /// </summary>
    public void RegisterTable(IGameTable table)
    {
        if (!_activeTables.Contains(table))
        {
            _activeTables.Add(table);
            Debug.Log($"[GameTableManager] Зарегистрирован стол: {table.TableName}");
        }
    }

    /// <summary>
    /// Стол удаляет себя при деспавне/уничтожении.
    /// </summary>
    public void UnregisterTable(IGameTable table)
    {
        if (_activeTables.Remove(table))
        {
            Debug.Log($"[GameTableManager] Удален стол: {table.TableName}");
        }
    }

    /// <summary>
    /// Вызывается столом, когда он освобождается.
    /// Уведомляет всех ожидающих ботов.
    /// </summary>
    public void NotifyTableFreed()
    {
        OnAnyTableFreed?.Invoke();
    }

    /// <summary>
    /// Быстро находит случайный свободный стол из кэшированного списка.
    /// </summary>
    public IGameTable GetRandomFreeTable()
    {
        List<IGameTable> freeTables = new List<IGameTable>();

        foreach (var table in _activeTables)
        {
            if (table.CanAssignBot())
            {
                freeTables.Add(table);
            }
        }

        if (freeTables.Count == 0) return null;

        int randomIndex = UnityEngine.Random.Range(0, freeTables.Count);
        return freeTables[randomIndex];
    }

    /// <summary>
    /// Возвращает стол, за которым в данный момент сидит указанный игрок.
    /// </summary>
    public IGameTable GetTableOccupiedByPlayer(ulong clientId)
    {
        foreach (var table in _activeTables)
        {
            if (table.IsOccupied && table.OccupiedByClientId == clientId)
            {
                return table;
            }
        }
        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}