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
    [SerializeField] private Transform exitPoint;

    private readonly NetworkVariable<bool> _isArrived = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    public bool IsArrived => _isArrived.Value;

    private NavMeshAgent navAgent;
    private Coroutine movementCoroutine;

    private bool isWaitingForTable;
    private List<IGameTable> subscribedTables = new List<IGameTable>();

    // Текущий стол, с которым взаимодействует бот
    private IGameTable currentTable;

    public IGameTable CurrentTable => currentTable;

    public Action<bool> OnArrivedChanged;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.stoppingDistance = stoppingDistance;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isArrived.OnValueChanged += HandleOnArrivedChanged;
        if (IsServer)
        {
            exitPoint = FindExitPoint();
            GoToRandomFreeTable();
        }
        else
        {
            if (navAgent != null) navAgent.enabled = false;
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isArrived.OnValueChanged -= HandleOnArrivedChanged;
    }

    public void HandleOnArrivedChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"[BotAgent] OnArrivedChanged: {previousValue} -> {newValue}");
        OnArrivedChanged?.Invoke(newValue);
    }
    #region Public Navigation Methods

    /// <summary>
    /// Находит любой свободный стол на сцене с учётом CanAssignBot().
    /// Возвращает null, если свободных столов нет.
    /// </summary>
    public IGameTable FindFreeTable()
    {
        if (!IsServer) return null;

        List<IGameTable> freeTables = new List<IGameTable>();

        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (var behaviour in allBehaviours)
        {
            if (behaviour is IGameTable table && table.CanAssignBot())
            {
                freeTables.Add(table);
            }
        }

        if (freeTables.Count == 0)
        {
            Debug.Log($"[BotAgent] Нет свободных столов для бота {gameObject.name}");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, freeTables.Count);
        return freeTables[randomIndex];
    }

    /// <summary>
    /// Отправляет бота к указанному столу.
    /// При прибытии: поворачивает лицом, назначает бота, инициализирует поведение.
    /// </summary>
    public void GoToTable(IGameTable table)
    {
        if (!IsServer) return;
        if (table == null) return;

        CancelMovement();

        // Освобождаем предыдущий стол
        if (currentTable != null)
        {
            currentTable.RemoveBot();
        }

        currentTable = table;

        // РЕЗЕРВИРУЕМ СТОЛ СРАЗУ — чтобы другие боты не выбрали его
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            currentTable.AssignBot(netObj);
            Debug.Log($"[BotAgent] Бот {gameObject.name} зарезервировал стол '{((MonoBehaviour)table).name}'");
        }

        Transform targetTransform = table.BotWaitPoint ?? ((MonoBehaviour)table).transform;
        MoveToTarget(targetTransform, OnReachedTable);
    }

    /// <summary>
    /// Convenience-метод: находит свободный стол и идёт к нему.
    /// Если столов нет — переходит в режим ожидания.
    /// </summary>
    public void GoToRandomFreeTable()
    {
        if (!IsServer) return;

        IGameTable freeTable = FindFreeTable();

        if (freeTable != null)
        {
            GoToTable(freeTable);
        }
        else
        {
            Debug.Log($"[BotAgent] Бот {gameObject.name} ожидает освобождения стола.");
            WaitForFreeTable();
        }
    }

    /// <summary>
    /// Отправляет бота к точке выхода из казино.
    /// </summary>
    public void GoToExit()
    {
        if (!IsServer) return;

        CancelMovement();

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

    #endregion

    #region Movement

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

    /// <summary>
    /// Поворачивает бота лицом к текущему столу (игнорируя наклон по вертикали).
    /// </summary>
    private void FaceCurrentTable()
    {
        if (currentTable == null) return;

        Transform tableTransform = ((MonoBehaviour)currentTable).transform;
        Vector3 direction = tableTransform.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    #endregion

    #region Arrival Callbacks

    private void OnReachedTable()
    {
        if (currentTable == null) return;

            // Проверка: стол мог стать недоступным (сломался, взорвался)
            /*
        if (!currentTable.CanAssignBot())
        {   
            Debug.LogWarning($"[BotAgent] Стол '{((MonoBehaviour)currentTable).name}' стал недоступен после прибытия. Ищем новый.");
            currentTable = null;
            GoToRandomFreeTable();
            return;
        }*/


        // Только поворот лицом к столу
        FaceCurrentTable();

        // Включаем нужный behavior под тип стола
        ActivateBehaviorForTable(currentTable);

        _isArrived.Value = true;

    }

    private void OnReachedExit()
    {
        Debug.Log($"[BotAgent] Бот {gameObject.name} покинул казино.");
        
        // Сетевой деспавн — безопаснее чем Destroy
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Waiting for Free Table

    private void WaitForFreeTable()
    {
        if (isWaitingForTable) return;
        isWaitingForTable = true;

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
        if (!isOccupied)
        {
            foreach (var table in subscribedTables)
            {
                table.OnBotOccupancyChanged -= OnTableBotOccupancyChanged;
            }
            subscribedTables.Clear();
            isWaitingForTable = false;

            GoToRandomFreeTable();
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

    #endregion

    #region Utility

    /// <summary>
    /// Находит объект ExitPoint: сначала по тэгу, затем по имени.
    /// </summary>
    public Transform FindExitPoint()
    {
        GameObject exitObj = GameObject.FindWithTag("ExitPoint");
        if (exitObj != null)
            return exitObj.transform;

        exitObj = GameObject.Find("ExitPoint");
        if (exitObj != null)
        {
            Debug.LogWarning("[ExitPointFinder] Объект найден по имени, рекомендуется назначить тэг 'ExitPoint'.");
            return exitObj.transform;
        }

        Debug.LogError("[ExitPointFinder] Объект с тэгом или именем 'ExitPoint' не найден на сцене!");
        return null;
    }

    /// <summary>
    /// Включает подходящий behavior для данного типа стола.
    /// Все остальные behaviors отключает.
    /// </summary>
    private void ActivateBehaviorForTable(IGameTable table)
    {
        // Получаем все behaviors на боте
        var allBehaviors = GetComponents<BaseBotBehavior>();

        BaseBotBehavior selectedBehavior = null;
        foreach (var behavior in allBehaviors)
        {
            // Выключаем все
            behavior.enabled = false;
            // Проверяем совместимость с текущим столом
            if (IsBehaviorCompatible(behavior, table))
            {
                selectedBehavior = behavior;
            }
        }

        if (selectedBehavior == null)
        {
            Debug.LogError($"[BotAgent] Нет подходящего behavior для стола {table.TableType}");
            GoToExit();
            return;
        }

        // Включаем выбранный
        selectedBehavior.enabled = true;
        selectedBehavior.InitializeGame(table);

        Debug.Log($"[BotAgent] Активирован {selectedBehavior.GetType().Name} для стола {table.TableType}");
    }

    /// <summary>
    /// Проверяет, подходит ли behavior для данного типа стола.
    /// Каждый behavior сам декларирует какие столы поддерживает.
    /// </summary>
    private bool IsBehaviorCompatible(BaseBotBehavior behavior, IGameTable table)
    {
        return behavior.SupportedTableType.IsInstanceOfType(table);
    }

    #endregion
}