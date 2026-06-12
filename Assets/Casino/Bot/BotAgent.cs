using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Blocks.Gameplay.Core;

/// <summary>
/// Универсальный агент ИИ, управляющий перемещением и взаимодействием с игровыми объектами.
/// Подходит для любых игр (BlackGreg, рулетка и др.).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BotAgent : NetworkBehaviour
{
    [Header("Настройки перемещения")]
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float pathUpdateInterval = 0.2f;

    [SerializeField] private NavMeshAgent _navAgent;
    [SerializeField] private Coroutine _movementCoroutine;
    [SerializeField] private IInteractable _targetInteractable;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        _navAgent.stoppingDistance = stoppingDistance;
    }

    /// <summary>
    /// Приказывает боту подойти к объекту и взаимодействовать с ним.
    /// </summary>
    /// <param name="interactableObject">Объект для взаимодействия.</param>
    /// <param name="onInteractionComplete">Коллбек по завершению действия.</param>
    public void GoAndInteract(MonoBehaviour interactableObject, Action onInteractionComplete = null)
    {
        if (!IsServer) return;

        if (interactableObject is IInteractable interactable)
        {
            _targetInteractable = interactable;

            if (_movementCoroutine != null)
            {
                StopCoroutine(_movementCoroutine);
            }

            _movementCoroutine = StartCoroutine(MoveToAndInteractRoutine(interactableObject.transform, onInteractionComplete));
        }
        else
        {
            Debug.LogError($"Объект {interactableObject.name} не реализует интерфейс IInteractable!");
        }
    }

    /// <summary>
    /// Корутина для плавного следования к цели и последующего взаимодействия.
    /// </summary>
    private IEnumerator MoveToAndInteractRoutine(Transform target, Action onComplete)
    {
        if (!IsServer) yield return null;

        _navAgent.isStopped = false;

        // Двигаемся к цели, пока не окажемся на дистанции остановки
        while (target != null && Vector3.Distance(transform.position, target.position) > _navAgent.stoppingDistance)
        {
            _navAgent.SetDestination(target.position);
            yield return new WaitForSeconds(pathUpdateInterval);
        }

        _navAgent.isStopped = true;

        // Проверяем возможность взаимодействия
        if (_targetInteractable != null && _targetInteractable.CanInteract(gameObject))
        {
            Debug.Log($"[ИИ] {gameObject.name} начинает взаимодействие с {_targetInteractable.InteractionPromptText}");
            _targetInteractable.Interact(gameObject);
            onComplete?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[ИИ] {gameObject.name} подошел, но не смог взаимодействовать.");
        }

        _movementCoroutine = null;
    }

    /// <summary>
    /// Прервать текущее действие и остановиться.
    /// </summary>
    public void CancelCurrentAction()
    {
        if (_movementCoroutine != null)
        {
            StopCoroutine(_movementCoroutine);
            _movementCoroutine = null;
        }
        if (_navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
        }
        _targetInteractable = null;
    }
}