using Blocks.Gameplay.Core;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Виджет, отображающий подсказку взаимодействия (например, "Нажмите E").
/// Сам выполняет Raycast и определяет, на какой объект смотрит игрок.
/// </summary>
public class InteractionWidget : UIWidget
{
    [Header("Raycast Settings")]
    [SerializeField] private float maxDistance = 10f;
    // Рекомендуется создать отдельный слой "Interactable" и выбрать его здесь, а не использовать ~0
    [SerializeField] private LayerMask interactableLayerMask = ~0;

    [Header("UI")]
    [SerializeField] private TMP_Text promptText;

    private GameObject playerObject;
    private bool isActive = false;

    private void Awake()
    {
        if (promptText == null)
            Debug.LogError("[InteractionWidget] promptText не назначен в инспекторе!");
    }

    private void Start()
    {
        Hide();

        if (NetworkManager.Singleton != null)
        {
            // Подписываемся на обновление состояния локального клиента
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            TryUpdateLocalPlayer();
        }
        else
        {
            Debug.LogWarning("[InteractionWidget] NetworkManager.Singleton == null");
        }

        // Проверка регистрации в UIManager
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.GetWidget<InteractionWidget>() != null)
                Debug.Log("[InteractionWidget] Виджет зарегистрирован в UIManager");
            else
                Debug.LogWarning("[InteractionWidget] Виджет НЕ НАЙДЕН в UIManager! Проверьте список Widgets.");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            TryUpdateLocalPlayer();
        }
    }

    /// <summary>
    /// Безопасное и легковесное обновление ссылки на игрока
    /// </summary>
    private void TryUpdateLocalPlayer()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
        {
            // Самый надежный способ получить локального игрока в NGO
            var playerNetObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerNetObj != null)
            {
                playerObject = playerNetObj.gameObject;
            }
        }
    }

    private void Update()
    {
        // Если виджет не активен – сразу скрываем и выходим (экономим производительность)
        if (!isActive)
        {
            if (promptText != null && promptText.gameObject.activeSelf)
                HidePrompt();
            return;
        }

        // Пытаемся обновить ссылку на игрока, если её нет (без спама логами!)
        if (playerObject == null)
        {
            TryUpdateLocalPlayer();
        }

        // Если игрока все еще нет, взаимодействовать не с кем
        if (playerObject == null)
        {
            HidePrompt();
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            HidePrompt();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        // Используем Raycast (один хит), а не RaycastAll, если нам нужен только ближайший/лучший объект.
        // Но если логика приоритетов требует проверки всех, оставляем RaycastAll.
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, interactableLayerMask);

        if (hits.Length == 0)
        {
            HidePrompt();
            return;
        }

        IInteractable bestInteractable = null;
        float bestDistance = float.MaxValue;
        int bestPriority = int.MinValue;

        foreach (var hit in hits)
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                // Дополнительная защита от null
                if (playerObject == null) break;

                if (interactable.CanInteract(playerObject))
                {
                    int priority = interactable.Priority;
                    float distance = hit.distance;

                    if (priority > bestPriority || (priority == bestPriority && distance < bestDistance))
                    {
                        bestPriority = priority;
                        bestDistance = distance;
                        bestInteractable = interactable;
                    }
                }
            }
        }

        if (bestInteractable != null)
        {
            string text = bestInteractable.InteractionPromptText;
            promptText.text = text;
            if (!promptText.gameObject.activeSelf)
            {
                promptText.gameObject.SetActive(true);
            }
        }
        else
        {
            HidePrompt();
        }
    }

    private void HidePrompt()
    {
        if (promptText != null && promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    public override void Show()
    {
        isActive = true;
    }

    public override void Hide()
    {
        isActive = false;
        HidePrompt();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}