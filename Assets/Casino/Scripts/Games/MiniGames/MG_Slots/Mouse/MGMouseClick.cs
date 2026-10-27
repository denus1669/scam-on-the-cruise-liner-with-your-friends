using System;
using System.Collections;
using Assets.Casino.Cheating.MiniGames.MG_Slots.Mouse;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public enum MouseState { Hidden, Emerging, Visible, Hiding, Dead }

public class MGMouseClick : MonoBehaviour, IPointerClickHandler
{
    public event Action<MGMouseClick> OnKilled;

    [Header("Анимация")]
    [SerializeField] private float hideOffset = 80f;
    [SerializeField] private float emergeDuration = 0.2f;
    [SerializeField] private float hideDuration = 0.2f;

    [Header("Тайминги (рандом)")]
    [SerializeField] private float minVisibleTime = 0.6f;
    [SerializeField] private float maxVisibleTime = 1.8f;
    [SerializeField] private float minHiddenTime = 0.3f;
    [SerializeField] private float maxHiddenTime = 1.2f;

    [SerializeField] private CanvasGroup canvasGroup;

    public MouseState State { get; private set; }
    public Hole CurrentHole { get; private set; }

    private HoleList _holes;
    private RectTransform _rt;
    private Coroutine _loop;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        State = MouseState.Hidden;
        SetVisual(0f, -hideOffset);
    }

    public void StartLife(HoleList holes, Hole initialHole)
    {
        _holes = holes;
        AttachTo(initialHole);
        _loop = StartCoroutine(LifeLoop());
    }

    private void AttachTo(Hole hole)
    {
        CurrentHole = hole;
        hole.currentMouse = this;
        _rt.SetParent(hole.holeRect, false);
        _rt.anchoredPosition = new Vector2(0, -hideOffset);
        SetVisual(0f, -hideOffset);
    }

    private IEnumerator LifeLoop()
    {
        while (State != MouseState.Dead)
        {
            // ждём перед вылазом
            yield return new WaitForSeconds(Random.Range(minHiddenTime, maxHiddenTime));
            if (State == MouseState.Dead) yield break;

            // если сейчас не привязаны к дырке — ищем свободную
            if (CurrentHole == null)
            {
                Hole h = _holes != null ? _holes.GetRandomFreeHole() : null;
                if (h == null) { yield return null; continue; }
                AttachTo(h);
            }

            yield return EmergeRoutine();
            if (State == MouseState.Dead) yield break;

            yield return new WaitForSeconds(Random.Range(minVisibleTime, maxVisibleTime));
            if (State == MouseState.Dead) yield break;

            yield return HideRoutine();
        }
    }

    private IEnumerator EmergeRoutine()
    {
        State = MouseState.Emerging;
        float t = 0f;
        while (t < emergeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / emergeDuration);
            SetVisual(k, -hideOffset * (1f - k));
            yield return null;
        }
        SetVisual(1f, 0f);
        canvasGroup.blocksRaycasts = true;
        State = MouseState.Visible;
    }

    private IEnumerator HideRoutine()
    {
        State = MouseState.Hiding;
        canvasGroup.blocksRaycasts = false;
        float t = 0f;
        while (t < hideDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / hideDuration);
            SetVisual(1f - k, -hideOffset * k);
            yield return null;
        }
        SetVisual(0f, -hideOffset);

        // освобождаем дырку и отвязываемся
        if (CurrentHole != null)
        {
            CurrentHole.currentMouse = null;
            CurrentHole = null;
        }
        if (_holes != null) _rt.SetParent(_holes.transform, false);

        State = MouseState.Hidden;
    }

    private void SetVisual(float alpha, float y)
    {
        canvasGroup.alpha = alpha;
        var p = _rt.anchoredPosition;
        p.y = y;
        _rt.anchoredPosition = p;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (State != MouseState.Visible) return;
        Die();
    }

    private void Die()
    {
        State = MouseState.Dead;
        if (_loop != null) StopCoroutine(_loop);
        if (CurrentHole != null) { CurrentHole.currentMouse = null; CurrentHole = null; }

        OnKilled?.Invoke(this);
        Destroy(gameObject);
    }
}