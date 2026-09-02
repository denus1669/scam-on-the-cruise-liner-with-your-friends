using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(NavMeshAgent))]
public class InspectorAgent : NetworkBehaviour   
{
    [Header("Патруль")]
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
    [SerializeField] private float patrolStopDistance = 0.25f;
    [SerializeField] private float pathRefreshInterval = 0.2f;

    [Header("Обнаружение читеров")]
    [SerializeField] private float detectionRadius = 8f;
    [SerializeField] private LayerMask playerLayerMask = ~0;
    [SerializeField] private float catchDistance = 1.6f;

    [Header("Взаимодействие игрока с инспектором")]
    [SerializeField] private float stopDuration = 3f;
    [SerializeField] private float interactionCooldown = 10f;

    private NavMeshAgent _agent;

    private int _currentPatrolIndex = -1;
    private Transform _currentPatrolPoint;

    private PlayerCheatController _lockedTarget;
    private bool _hasSelectedTarget;

    private readonly List<PlayerCheatController> _detectedCheaters = new List<PlayerCheatController>();
    private Collider[] _overlapBuffer = new Collider[64];


    private readonly NetworkVariable<float> _nextInteractionTime = new(
    0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> _isStoppedByPlayer = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float _lastPathRefreshTime = -999f;
    private Coroutine _stopCoroutine;

    public bool HasSelectedTarget => _hasSelectedTarget;
    public bool IsStoppedByPlayer => _isStoppedByPlayer.Value;

    public bool CanBeStoppedByPlayer =>
        IsServer && !_isStoppedByPlayer.Value && Time.time >= _nextInteractionTime.Value;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = patrolStopDistance;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            _agent.enabled = true;
            GoToRandomPatrolPoint();
        }
        else
        {
            if (_agent != null)
                _agent.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (_stopCoroutine != null)
        {
            StopCoroutine(_stopCoroutine);
            _stopCoroutine = null;
        }

        _isStoppedByPlayer.Value = false;
        _hasSelectedTarget = false;
        _lockedTarget = null;
        _detectedCheaters.Clear();
    }

    private void Update()
    {
        if (!IsServer || _agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            return;

        if (_isStoppedByPlayer.Value)
            return;

        UpdateDetectedCheaters();

        if (_hasSelectedTarget)
        {
            if (!IsCheaterValid(_lockedTarget))
            {
                _detectedCheaters.Remove(_lockedTarget);
                ClearLockedTarget();

                if (!TrySelectNextCheater())
                    GoToRandomPatrolPoint();
            }
            else
            {
                UpdateChase();
            }

            return;
        }

        if (TrySelectNextCheater())
            return;

        UpdatePatrol();
    }

    private void UpdateDetectedCheaters()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            _overlapBuffer,
            playerLayerMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            if (_overlapBuffer[i] == null)
                continue;

            PlayerCheatController cheater = _overlapBuffer[i].GetComponentInParent<PlayerCheatController>();

            if (cheater == null)
                continue;

            if (!cheater.IsSpawned)
                continue;

            if (!cheater.IsCheating)
                continue;

            if (!_detectedCheaters.Contains(cheater))
                _detectedCheaters.Add(cheater);
        }

        for (int i = _detectedCheaters.Count - 1; i >= 0; i--)
        {
            if (!IsCheaterValid(_detectedCheaters[i]))
                _detectedCheaters.RemoveAt(i);
        }
    }

    private bool IsCheaterValid(PlayerCheatController cheater)
    {
        return cheater != null
               && cheater.IsSpawned
               && cheater.isActiveAndEnabled
               && cheater.IsCheating;
    }

    private bool TrySelectNextCheater()
    {
        if (_hasSelectedTarget)
            return true;

        for (int i = _detectedCheaters.Count - 1; i >= 0; i--)
        {
            if (!IsCheaterValid(_detectedCheaters[i]))
                _detectedCheaters.RemoveAt(i);
        }

        if (_detectedCheaters.Count == 0)
            return false;

        SetLockedTarget(_detectedCheaters[0]);
        return true;
    }

    private void SetLockedTarget(PlayerCheatController target)
    {
        _lockedTarget = target;
        _hasSelectedTarget = true;

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(target.transform.position);
            _lastPathRefreshTime = Time.time;
        }
    }

    private void ClearLockedTarget()
    {
        _lockedTarget = null;
        _hasSelectedTarget = false;
    }

    private void UpdateChase()
    {
        if (_lockedTarget == null)
            return;

        Vector3 targetPosition = _lockedTarget.transform.position;

        if (Time.time - _lastPathRefreshTime >= pathRefreshInterval ||
            Vector3.Distance(_agent.destination, targetPosition) > 0.5f)
        {
            _agent.SetDestination(targetPosition);
            _lastPathRefreshTime = Time.time;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _lockedTarget.transform.position);

        if (distanceToTarget <= catchDistance)
            CatchCheater();
    }

    private void CatchCheater()
    {
        if (_lockedTarget == null)
            return;

        PlayerCheatController target = _lockedTarget;

        _detectedCheaters.Remove(target);

        if (target.IsCheating)
        {
            ulong inspectorId = NetworkObject != null ? OwnerClientId : ulong.MaxValue;

            // Существующий метод отмены чита и уведомления стола.
            target.HandleCheatCaught(inspectorId);
        }

        ClearLockedTarget();

        if (!TrySelectNextCheater())
            GoToRandomPatrolPoint();
    }

    private void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
        {
            if (_agent.isOnNavMesh)
                _agent.isStopped = true;

            return;
        }

        if (_currentPatrolPoint == null)
        {
            GoToRandomPatrolPoint();
            return;
        }

        if (!_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            GoToRandomPatrolPoint();
            return;
        }

        if (!_agent.pathPending &&
            _agent.remainingDistance <= Mathf.Max(_agent.stoppingDistance, patrolStopDistance))
        {
            GoToRandomPatrolPoint();
        }
    }

    private void GoToRandomPatrolPoint()
    {
        if (!IsServer || patrolPoints == null || patrolPoints.Count == 0)
            return;

        int validCount = 0;

        for (int i = 0; i < patrolPoints.Count; i++)
        {
            if (patrolPoints[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return;

        int selectedIndex = -1;
        int attempts = 0;

        do
        {
            int randomValidIndex = Random.Range(0, validCount);
            int currentValidIndex = 0;

            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] == null)
                    continue;

                if (currentValidIndex == randomValidIndex)
                {
                    selectedIndex = i;
                    break;
                }

                currentValidIndex++;
            }

            attempts++;
        }
        while (validCount > 1 && selectedIndex == _currentPatrolIndex && attempts < 10);

        _currentPatrolIndex = selectedIndex;
        _currentPatrolPoint = patrolPoints[_currentPatrolIndex];

        if (_currentPatrolPoint == null)
            return;

        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_currentPatrolPoint.position);
            _lastPathRefreshTime = Time.time;
        }
    }

    public float GetInteractionCooldownRemaining()
    {
        return Mathf.Max(0f, _nextInteractionTime.Value - Time.time);
    }

    /// <summary>
    /// Вызывается другим классом, когда игрок заполнил виджет взаимодействия.
    /// </summary>
    public bool TryStopByPlayerInteraction(ulong playerId)
    {
        if (!IsServer)
            return false;

        if (!CanBeStoppedByPlayer)
            return false;

        if (_stopCoroutine != null)
            StopCoroutine(_stopCoroutine);

        _stopCoroutine = StartCoroutine(StopByPlayerRoutine());
        return true;
    }

    [Rpc(SendTo.Server)]
    public void RequestStopByPlayerInteractionServerRpc(ulong playerId)
    {
        TryStopByPlayerInteraction(playerId);
    }

    private IEnumerator StopByPlayerRoutine()
    {
        _isStoppedByPlayer.Value = true;

        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = true;

        yield return new WaitForSeconds(stopDuration);

        _isStoppedByPlayer.Value = false;

        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = false;

        _nextInteractionTime.Value = Time.time + interactionCooldown;
        _lastPathRefreshTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}