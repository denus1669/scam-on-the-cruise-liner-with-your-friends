using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class BotMovement : NetworkBehaviour
{
    [Header("Target (можно указать вручную или найти по тегу)")]
    [SerializeField] private Transform targetObject;   // цель, к которой идти
    [SerializeField] private string targetTag = "Target"; // если цель ищется по тегу

    private NavMeshAgent agent;

    public override void OnNetworkSpawn()
    {
        // Получаем компонент NavMeshAgent
        agent = GetComponent<NavMeshAgent>();

        if (!IsServer)
        {
            // Клиентам NavMeshAgent не нужен – отключаем
            if (agent != null) agent.enabled = false;
            return;
        }

        // ========== ТОЛЬКО НА СЕРВЕРЕ ==========
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent отсутствует!", this);
            return;
        }

        // Включаем агента
        agent.enabled = true;

        // Определяем целевую точку
        Transform target = targetObject;
        if (target == null && !string.IsNullOrEmpty(targetTag))
        {
            GameObject go = GameObject.FindGameObjectWithTag(targetTag);
            if (go != null) target = go.transform;
        }

        if (target != null)
        {
            // Устанавливаем destination для NavMeshAgent
            agent.SetDestination(target.position);
            Debug.Log($"Бот {gameObject.name} идёт к {target.name}");
        }
        else
        {
            Debug.LogWarning("Цель не найдена! Бот стоит на месте.");
        }
    }

    // Необязательно: обновление destination, если цель движется
    private void Update()
    {
        if (!IsServer) return;
        if (agent == null || !agent.enabled) return;
        if (targetObject == null) return;

        // Например, если цель перемещается – перенаправляем бота
        if (agent.destination != targetObject.position)
            agent.SetDestination(targetObject.position);
    }
}