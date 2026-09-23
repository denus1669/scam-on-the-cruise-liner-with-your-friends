using Assets.Casino.QuickOutline.Scripts;
using Blocks.Gameplay.Core;
using UnityEngine;

[RequireComponent(typeof(Outline))]
public abstract class HighlightControllerBase : MonoBehaviour, IHighlightable
{
    [Header("Outline Settings")]
    [Tooltip("Цвет обводки, когда объект доступен для взаимодействия.")]
    [SerializeField] protected Color availableColor = Color.white;
    [Tooltip("Цвет обводки, когда игрок навелся на объект (фокус).")]
    [SerializeField] protected Color focusColor = Color.yellow;
    [Tooltip("Толщина обводки. (доступность)")]
    [SerializeField, Range(0f, 10f)] protected float availableWidth = 2f;
    [Tooltip("Толщина обводки. (фокус)")]
    [SerializeField, Range(0f, 10f)] protected float focusWidth = 6f;

    [Tooltip("Опциональный источник. Если задан — CanHighlight делегирует в CanInteract.")]
    [SerializeField] protected InteractableBase _interactable;

    [Header("Availability Settings")]
    [Tooltip("Интервал проверки доступности колоды в секундах.")]
    [SerializeField, Min(0.05f)] protected float availabilityCheckInterval = 0.5f;

    protected Outline _outline;
    protected bool _available;
    protected bool _focus;

    protected virtual void Awake()
    {
        _outline = GetComponent<Outline>();
        _outline.OutlineMode = Outline.Mode.OutlineAll;

        if( _outline == null)
        {
            _interactable = GetComponent<InteractableBase>();
        }
        SetAvailableHighlight(false);
        SetFocusHighlight(false);
    }

    protected virtual void OnDestroy()
    {
        SetAvailableHighlight(false);
        SetFocusHighlight(false);
    }

    // Вызывается локальным InteractionAddon'ом
    public virtual void SetAvailableHighlight(bool state)
    { 
        if ( _available != state)
        {
            _available = state;
            ApplyState();
        }
    }
    public virtual void SetFocusHighlight(bool state) { _focus = state; ApplyState(); }

    public virtual bool CanHighlight(GameObject interactor)
    {
        // Для интерактивных — то же условие, что и для взаимодействия
        if (_interactable != null) return _interactable.CanInteract(interactor);
        return true; // для неинтерактивных подсветок переопределяется в наследнике
    }


    protected virtual void ApplyState()
    {
        bool on = _focus || _available;
        _outline.enabled = on;
        if (!on) return;
        _outline.OutlineColor = _focus ? focusColor : availableColor;
        _outline.OutlineWidth = _focus ? focusWidth : availableWidth;
    }
}