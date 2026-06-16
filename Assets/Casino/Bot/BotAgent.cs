using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

/// <summary>
/// Универсальный агент ИИ, управляющий перемещением и взаимодействием с игровыми столами.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BotAgent : NetworkBehaviour
{
    [Header("Настройки перемещения")]
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float pathUpdateInterval = 0.2f;

    [Header("Точка выхода")]
    [SerializeField] private Transform exitPoint;   // куда уходит бот, покидая казино

    private NavMeshAgent navAgent;
    private Coroutine movementCoroutine;

    private bool isWaitingForTable;
    private List<IGameTable> subscribedTables = new List<IGameTable>();

    // Текущий стол, с которым взаимодействует бот
    private IGameTable currentTable;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.stoppingDistance = stoppingDistance;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            // При спавне бот сам ищет стол и идёт к нему
            FindAndGoToRandomTable();
            exitPoint = FindExitPoint();
        }
        else
        {
            // На клиентах NavMeshAgent не нужен
            if (navAgent != null) navAgent.enabled = false;
        }
    }

    /// <summary>
    /// Находит все столы на сцене, выбирает случайный свободный и отправляет бота к нему.
    /// </summary>
    public void FindAndGoToRandomTable()
    {
        if (!IsServer) return;

        List<IGameTable> freeTables = new List<IGameTable>();

        // Используем современный API, без сортировки
        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (var behaviour in allBehaviours)
        {
            if (behaviour is IGameTable table && !table.IsBotOccupied)
            {
                freeTables.Add(table);
            }
        }

        if (freeTables.Count == 0)
        {
            Debug.Log($"[BotAgent] Нет свободных столов. Бот {gameObject.name} ожидает освобождения.");
            WaitForFreeTable();
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, freeTables.Count);
        IGameTable selectedTable = freeTables[randomIndex];
        currentTable = selectedTable;

        Debug.Log($"[BotAgent] Бот {gameObject.name} выбрал стол {((MonoBehaviour)selectedTable).name}");

        Transform targetTransform = selectedTable.BotWaitPoint ?? ((MonoBehaviour)selectedTable).transform;

        MoveToTarget(targetTransform, OnReachedTable);
    }

    /// <summary>
    /// Поворачивает бота лицом к текущему столу (игнорируя наклон по вертикали).
    /// </summary>
    private void FaceCurrentTable()
    {
        if (currentTable == null) return;

        Transform tableTransform = ((MonoBehaviour)currentTable).transform;
        Vector3 direction = tableTransform.position - transform.position;
        direction.y = 0f; // чтобы бот не задирал голову

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <summary>
    /// Отправляет бота к точке выхода из казино.
    /// </summary>
    public void GoToExit()
    {
        if (!IsServer) return;

        if (currentTable != null)
        {
            currentTable.RemoveBot();
            currentTable = null;
        }

        if (exitPoint != null)
        {
            Debug.Log($"[BotAgent] Бот {gameObject.name} уходит к выходу.");
            MoveToTarget(exitPoint, OnReachedExit);
        }
        else
        {
            Debug.LogWarning($"[BotAgent] Точка выхода не назначена. Бот {gameObject.name} остановлен.");
        }
    }

    /// <summary>
    /// Универсальный метод движения к цели. После прибытия вызывает указанный колбэк.
    /// </summary>
    private void MoveToTarget(Transform target, Action onArrived)
    {
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        movementCoroutine = StartCoroutine(MoveToTargetRoutine(target, onArrived));
    }

    private IEnumerator MoveToTargetRoutine(Transform target, Action onArrived)
    {
        if (!IsServer || target == null) yield break;

        navAgent.isStopped = false;

        while (Vector3.Distance(transform.position, target.position) > navAgent.stoppingDistance)
        {
            navAgent.SetDestination(target.position);
            yield return new WaitForSeconds(pathUpdateInterval);

            if (target == null)
            {
                Debug.LogWarning($"[BotAgent] Цель исчезла во время движения. Бот {gameObject.name} останавливается.");
                navAgent.isStopped = true;
                movementCoroutine = null;
                yield break;
            }
        }

        navAgent.isStopped = true;
        movementCoroutine = null;

        onArrived?.Invoke();
    }

    private void OnReachedTable()
    {
        if (currentTable != null)
        {
            // Поворачиваемся к столу
            FaceCurrentTable();

            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null)
            {
                currentTable.AssignBot(netObj);
                Debug.Log($"[BotAgent] Бот {gameObject.name} занял место за столом.");
            }

            // Ищем у себя IBotGameBehavior и инициализируем его текущим столом
            if (TryGetComponent<IBotGameBehavior>(out var behavior))
            {
                behavior.InitializeGame(currentTable);
            }
        }
    }
    private void OnReachedExit()
    {
        Debug.Log($"[BotAgent] Бот {gameObject.name} покинул казино.");
        Destroy(gameObject);
        // Здесь можно запустить деспавн или дальнейшее поведение
    }

    /// <summary>
    /// Прерывает текущее движение (если есть).
    /// </summary>
    public void CancelMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }
    }

    private void WaitForFreeTable()
    {
        if (isWaitingForTable) return;
        isWaitingForTable = true;

        // Находим все столы на сцене
        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var behaviour in allBehaviours)
        {
            if (behaviour is IGameTable table)
            {
                table.OnBotOccupancyChanged += OnTableBotOccupancyChanged;
                subscribedTables.Add(table);
            }
        }
    }

    private void OnTableBotOccupancyChanged(bool isOccupied)
    {
        if (!isOccupied) // Стол освободился
        {
            // Отписываемся от всех столов
            foreach (var table in subscribedTables)
            {
                table.OnBotOccupancyChanged -= OnTableBotOccupancyChanged;
            }
            subscribedTables.Clear();
            isWaitingForTable = false;

            // Пытаемся снова найти стол
            FindAndGoToRandomTable();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (IsServer)
        {
            foreach (var table in subscribedTables)
            {
                table.OnBotOccupancyChanged -= OnTableBotOccupancyChanged;
            }
            subscribedTables.Clear();
        }
    }

    /// <summary>
    /// Находит объект ExitPoint: сначала по тэгу, затем по имени.
    /// Возвращает первый найденный активный объект или null.
    /// </summary>
    public Transform FindExitPoint()
    {
        // 1. Быстрый поиск по тэгу
        GameObject exitPoint = GameObject.FindWithTag("ExitPoint");
        if (exitPoint != null)
            return exitPoint.transform;

        // 2. Медленный поиск по имени (fallback)
        exitPoint = GameObject.Find("ExitPoint");
        if (exitPoint != null)
        {
            Debug.LogWarning("[ExitPointFinder] Объект найден по имени, рекомендуется назначить тэг 'ExitPoint' для повышения производительности.");
            return exitPoint.transform;
        }

        Debug.LogError("[ExitPointFinder] Объект с тэгом или именем 'ExitPoint' не найден на сцене!");
        return null;
    }


}