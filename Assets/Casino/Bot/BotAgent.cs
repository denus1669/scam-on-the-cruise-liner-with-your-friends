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

    // КЭШИРУЕМ поведения, чтобы не искать их каждый раз
    private BaseBotBehaviour[] _cachedBehaviors;
    BaseBotBehaviour selectedBehavior = null;

    private bool isWaitingForTable;

    // Текущий стол, с которым взаимодействует бот
    private IGameTable currentTable;

    public IGameTable CurrentTable => currentTable;

    public Action<bool> OnArrivedChanged;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.stoppingDistance = stoppingDistance;

        // [ИЗМЕНЕНО] Один раз получаем все поведения при создании бота
        _cachedBehaviors = GetComponents<BaseBotBehaviour>();
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

        // Освобождаем стол при сетевом деспавне
        // Если бота кикнули или он "крашнулся", стол не должен остаться занятым навсегда
        if (IsServer && currentTable != null)
        {
            currentTable.RemoveBot();
            currentTable = null;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (IsServer && currentTable != null)
        {
            currentTable.RemoveBot();
            currentTable = null;
        }

        // Гарантированная отписка от Менеджера, если бот был в ожидании
        if (isWaitingForTable && GameTableManager.Instance != null)
        {
            GameTableManager.Instance.OnAnyTableFreed -= OnAnyTableFreed;
        }
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

        // [ИЗМЕНЕНО] Используем Менеджер вместо тяжелого поиска по сцене
        if (GameTableManager.Instance == null)
        {
            Debug.LogError("[BotAgent] GameTableManager не найден на сцене!");
            return null;
        }

        IGameTable freeTable = GameTableManager.Instance.GetRandomFreeTable();

        if (freeTable == null)
        {
            Debug.Log($"[BotAgent] Нет свободных столов для бота {gameObject.name}");
        }

        return freeTable;
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
            // [ИЗМЕНЕНО] Используем свойство TableName вместо каста к MonoBehaviour
            Debug.Log($"[BotAgent] Бот {gameObject.name} зарезервировал стол '{table.TableName}'");
        }

        // [ИЗМЕНЕНО] Используем TableTransform и BotWaitPoint (если есть)
        Transform targetTransform = table.BotWaitPoint ?? table.TableTransform;
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
        SetArrived(false);

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
    private void SetArrived(bool arrived)
    {
        if (!IsServer) return;
        if (_isArrived.Value == arrived) return;

        _isArrived.Value = arrived;
        OnArrivedChanged?.Invoke(arrived);
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
        navAgent.SetDestination(target.position);

        // 5. Возвращаем управление NavMeshAgent
        
        navAgent.updatePosition = true;
        navAgent.updateRotation = true;
        
        // 1. Ждем, пока путь построится (с таймаутом на случай зависания)
        float pathBuildTimeout = 2f;
        while (navAgent.pathPending && pathBuildTimeout > 0)
        {
            pathBuildTimeout -= Time.deltaTime;
            yield return null;
        }

        // 2. Проверка: удалось ли вообще построить путь? 
        // (Например, стол может стоять внутри collider'а, где нет NavMesh)
        if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"[BotAgent] Невозможно построить путь к {target.name}. Путь невалиден.");
            CancelMovement();
            yield break;
        }

        // 3. Переменные для детектора застревания (Stuck Detection)
        Vector3 lastPosition = transform.position;
        float stuckTimer = 0f;
        const float STUCK_THRESHOLD = 3f; // Секунд без движения, чтобы считать бота застрявшим
        const float MOVE_EPSILON = 0.05f; // Минимальное смещение, чтобы считать, что бот движется

        // 4. Основной цикл движения
        // ИСПРАВЛЕНО: Используем remainingDistance вместо Vector3.Distance
        while (navAgent.remainingDistance > navAgent.stoppingDistance)
        {
            // Обновляем путь, только если цель сдвинулась больше чем на 0.5 метра
            if (Vector3.Distance(navAgent.destination, target.position) > 0.5f)
            {
                navAgent.SetDestination(target.position);
            }
            // Если цель уничтожена во время пути
            if (target == null)
            {
                CancelMovement();
                yield break;
            }

            // Если на пути внезапно выросло препятствие (например, закрылась дверь)
            if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning($"[BotAgent] Путь к {target.name} стал недействительным во время движения.");
                CancelMovement();
                yield break;
            }

            // [STUCK DETECTION] Проверяем, движется ли бот
            if (Vector3.Distance(transform.position, lastPosition) < MOVE_EPSILON)
            {
                stuckTimer += pathUpdateInterval;
                if (stuckTimer >= STUCK_THRESHOLD)
                {
                    Debug.LogWarning($"[BotAgent] Бот {gameObject.name} застрял! Пытаемся пересчитать путь.");
                    stuckTimer = 0f;

                    // Пытаемся пересчитать путь (иногда помогает найти обходной маршрут)
                    navAgent.SetDestination(target.position);
                    yield return null; // Ждем 1 кадр

                    // Если после пересчета путь все еще невалиден - сдаемся
                    if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                    {
                        CancelMovement();
                        yield break;
                    }
                }
            }
            else
            {
                stuckTimer = 0f; // Бот движется, сбрасываем таймер
                lastPosition = transform.position;
            }

            // Экономим ресурсы сервера, проверяя статус не каждый кадр, а с интервалом
            yield return new WaitForSeconds(pathUpdateInterval);
        }

        // 5. Успешное прибытие
        navAgent.isStopped = true;
        movementCoroutine = null;
        onArrived?.Invoke();
    }
    #endregion

    #region Arrival Callbacksw

    private void OnReachedTable()
    {
        if (currentTable == null) return;
        // Запускаем плавное выравнивание вместо мгновенной установки
        StartCoroutine(SmoothAlignToTable());
    }

    private IEnumerator SmoothAlignToTable()
    {
        // 1. Определяем целевые позицию и ротацию
        Transform waitPoint = currentTable.BotWaitPoint;
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (waitPoint != null)
        {
            // Идеальный случай: есть заготовленная точка с правильной ротацией
            targetPosition = waitPoint.position;
            targetRotation = waitPoint.rotation;
        }
        else
        {
            // Fallback: BotWaitPoint не назначен.
            // Оставляем бота на текущей позиции, но поворачиваем лицом к столу.
            targetPosition = transform.position;
            Vector3 direction = currentTable.TableTransform.position - transform.position;
            direction.y = 0f;
            targetRotation = direction != Vector3.zero
                ? Quaternion.LookRotation(direction)
                : transform.rotation;
        }

        // 2. Отключаем NavMeshAgent от управления на время анимации.
        // Иначе агент будет пытаться "тянуть" бота обратно к своему рассчитанному положению.
        bool wasUpdatingPosition = navAgent.updatePosition;
        bool wasUpdatingRotation = navAgent.updateRotation;
        navAgent.updatePosition = false;
        navAgent.updateRotation = false;

        // 3. Плавная анимация (1 секунда)
        float duration = 1f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            // Если во время анимации стол исчез или бота позвали в другое место
            if (currentTable == null) yield break;

            elapsed += Time.deltaTime;

            // SmoothStep дает красивую кривую с ускорением в начале и замедлением в конце
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);

            yield return null;
        }

        // 4. Финальная точная установка (на случай микро-погрешностей)
        transform.position = targetPosition;
        transform.rotation = targetRotation;


        // 6. Запускаем игровую логику
        ActivateBehaviorForTable(currentTable);

        if (currentTable.TableType == "SlotMachine")
            currentTable.StartGame();
        if (currentTable.TableType == "BlackGreg")

        _isArrived.Value = true;
        currentTable.BotReachedTable(true);
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

        // [ИЗМЕНЕНО] Подписываемся только на ОДИН глобальный ивент Менеджера
        if (GameTableManager.Instance != null)
        {
            GameTableManager.Instance.OnAnyTableFreed += OnAnyTableFreed;
        }
    }

    private void OnAnyTableFreed()
    {
        // Отписываемся, чтобы не реагировать на следующие освобождения, 
        // так как мы уже пошли искать стол
        if (GameTableManager.Instance != null)
        {
            GameTableManager.Instance.OnAnyTableFreed -= OnAnyTableFreed;
        }

        isWaitingForTable = false;
        GoToRandomFreeTable();
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
        selectedBehavior = null;

        // [ИЗМЕНЕНО] Используем кэшированный массив
        foreach (var behavior in _cachedBehaviors)
        {
            behavior.enabled = false;
            if (IsBehaviorCompatible(behavior, table))
            {
                selectedBehavior = behavior;
            }
        }

        if (selectedBehavior == null)
        {
            GoToExit();
            return;
        }

        selectedBehavior.enabled = true;
        selectedBehavior.InitializeGame(table);

        // [ИЗМЕНЕНО] Используем TableName
        Debug.Log($"[BotAgent] Активирован {selectedBehavior.GetType().Name} для стола {table.TableName}");
    }

    /// <summary>
    /// Проверяет, подходит ли behavior для данного типа стола.
    /// Каждый behavior сам декларирует какие столы поддерживает.
    /// </summary>
    private bool IsBehaviorCompatible(BaseBotBehaviour behavior, IGameTable table)
    {
        return behavior.SupportedTableType.IsInstanceOfType(table);
    }

    #endregion

    
}