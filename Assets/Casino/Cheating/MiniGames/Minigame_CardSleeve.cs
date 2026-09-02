using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Мини-игра: игрок достает карту из рукава.
/// Нужно зажать карту мышью и дотащить до целевой зоны, не касаясь стенок.
/// При касании стенки карта возвращается в начальное положение.
/// </summary>
public class Minigame_CardSleeve : MinigameBase
{
    [Header("Ссылки UI")]
    [Tooltip("Карта, которую игрок перетаскивает мышью.")]
    [SerializeField] private RectTransform card;

    [Tooltip("Зона, куда нужно дотащить карту.")]
    [SerializeField] private RectTransform targetZone;

    [Tooltip("Стенки. Если карта касается любой из них, мини-игра начинается заново.")]
    [SerializeField] private List<RectTransform> walls = new List<RectTransform>();

    [Tooltip("Камера для Screen Space - Camera. Для Screen Space - Overlay можно оставить пустым.")]
    [SerializeField] private Camera uiCamera;

    [Header("Правила победы")]
    [Tooltip("Если включено: победа сразу, когда карта попала в зону.\nЕсли выключено: нужно отпустить кнопку мыши в зоне.")]
    [SerializeField] private bool winWhenTargetReached = true;

    [Tooltip("Если включено: карта должна полностью находиться внутри целевой зоны.\nЕсли выключено: достаточно пересечения.")]
    [SerializeField] private bool requireFullOverlap = false;

    [Header("Перезапуск")]
    [Tooltip("Задержка перед возвратом карты после касания стенки.")]
    [SerializeField] private float restartDelay = 0.2f;

    private RectTransform _cardParent;
    private Vector3 _startLocalPosition;
    private Vector2 _dragOffset;

    private bool _dragging;
    private bool _resetting;

    private Camera _camera;

    public override void StartGame(string contextData, Action<bool> onComplete)
    {
        // Дополнительная страховка, если менеджер выключил GameObject префаба/панели.
        gameObject.SetActive(true);
        base.StartGame(contextData, onComplete);
    }

    protected override void OnGameStarted()
    {
        StopAllCoroutines();

        _isPlaying = true;
        _dragging = false;
        _resetting = false;
        _dragOffset = Vector2.zero; // Сбрасываем оффсет перетаскивания


        if (card != null)
        {
            _cardParent = card.parent as RectTransform;
            _startLocalPosition = card.localPosition;
        }

        SetupCamera();
        ResetCard();
    }

    public override void ForceClose()
    {
        StopAllCoroutines();
        _dragging = false;
        _resetting = false;
        ResetCard(); // Возвращаем карту на место перед скрытием

        base.ForceClose();
    }

    public override void WinGame()
    {
        StopAllCoroutines();
        _dragging = false;
        _resetting = false;
        _isPlaying = false;
        ResetCard(); // Возвращаем карту на место перед скрытием

        base.WinGame();
    }

    public override void LoseGame()
    {
        StopAllCoroutines();
        _dragging = false;
        _resetting = false;
        _isPlaying = false;
        ResetCard(); // Возвращаем карту на место перед скрытием

        base.LoseGame();
    }

    private void Update()
    {
        if (!_isPlaying || _resetting || card == null || _cardParent == null)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 screenPosition = mouse.position.ReadValue();

        if (!_dragging)
        {
            if (mouse.leftButton.wasPressedThisFrame && IsPointerOverCard(screenPosition))
            {
                BeginDrag(screenPosition);
            }
        }
        else
        {
            MoveCard(screenPosition);

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                EndDrag();
            }
        }
    }

    private void SetupCamera()
    {
        _camera = uiCamera;

        if (_camera != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _camera = canvas.worldCamera;
        else
            _camera = null;
    }

    private bool IsPointerOverCard(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(card, screenPosition, _camera);
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _cardParent,
                screenPosition,
                _camera,
                out Vector2 localPoint))
        {
            return;
        }

        _dragOffset = (Vector2)card.localPosition - localPoint;
        _dragging = true;
    }

    private void MoveCard(Vector2 screenPosition)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _cardParent,
                screenPosition,
                _camera,
                out Vector2 localPoint))
        {
            return;
        }

        Vector2 nextPosition = localPoint + _dragOffset;
        card.localPosition = new Vector3(nextPosition.x, nextPosition.y, card.localPosition.z);

        CheckCollisions();
    }

    private void EndDrag()
    {
        _dragging = false;

        if (!_isPlaying || _resetting)
            return;

        if (targetZone != null && !winWhenTargetReached && IsCardInsideTarget())
        {
            WinGame();
        }
    }

    private void CheckCollisions()
    {
        if (!_isPlaying || _resetting || card == null)
            return;

        for (int i = 0; i < walls.Count; i++)
        {
            if (walls[i] == null)
                continue;

            if (RectsOverlap(card, walls[i]))
            {
                StartCoroutine(RestartRoutine());
                return;
            }
        }

        if (targetZone != null && winWhenTargetReached && IsCardInsideTarget())
        {
            WinGame();
        }
    }

    private bool IsCardInsideTarget()
    {
        if (targetZone == null || card == null)
            return false;

        if (!requireFullOverlap)
            return RectsOverlap(card, targetZone);

        Rect targetRect = GetWorldRect(targetZone);

        Vector3[] corners = new Vector3[4];
        card.GetWorldCorners(corners);

        for (int i = 0; i < corners.Length; i++)
        {
            if (!targetRect.Contains(corners[i]))
                return false;
        }

        return true;
    }

    private bool RectsOverlap(RectTransform a, RectTransform b)
    {
        if (a == null || b == null)
            return false;

        Rect rectA = GetWorldRect(a);
        Rect rectB = GetWorldRect(b);

        return rectA.Overlaps(rectB);
    }

    private Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        float width = Mathf.Abs(corners[2].x - corners[0].x);
        float height = Mathf.Abs(corners[2].y - corners[0].y);

        return new Rect(corners[0].x, corners[0].y, width, height);
    }

    private void ResetCard()
    {
        if (card != null)
            card.localPosition = _startLocalPosition;
    }

    private IEnumerator RestartRoutine()
    {
        _resetting = true;
        _dragging = false;

        if (restartDelay > 0f)
            yield return new WaitForSeconds(restartDelay);

        ResetCard();

        _resetting = false;
    }
}