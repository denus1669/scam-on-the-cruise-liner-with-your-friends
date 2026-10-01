using UnityEngine;

public class PurchaseVisualController : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material purchasedMaterial;
    [SerializeField] private Material notPurchasedMaterial;

    [Header("Interaction")]
    [SerializeField] private Collider targetCollider;
    [SerializeField] private Behaviour[] disableWhenHidden;

    private bool _visible = true;
    private bool _purchased;

    public bool IsVisible => _visible;
    public bool IsPurchased => _purchased;

    private void Awake()
    {
        AutoResolveReferences();
        Apply();
    }

    private void OnValidate()
    {
        AutoResolveReferences();

        if (Application.isPlaying)
            Apply();
    }

    public void SetPurchased(bool purchased)
    {
        if (_purchased == purchased) return;

        _purchased = purchased;
        Apply();
    }

    public void SetVisible(bool visible)
    {
        if (_visible == visible) return;

        _visible = visible;
        Apply();
    }

    private void AutoResolveReferences()
    {
        if (targetRenderer == null)
            TryGetComponent(out targetRenderer);

        if (targetCollider == null)
            TryGetComponent(out targetCollider);
    }

    private void Apply()
    {
        if (targetRenderer != null)
        {
            targetRenderer.enabled = _visible;
            targetRenderer.sharedMaterial = _purchased ? purchasedMaterial : notPurchasedMaterial;
        }

        if (targetCollider != null)
            targetCollider.enabled = _visible;

        if (disableWhenHidden == null) return;

        foreach (var component in disableWhenHidden)
        {
            if (component == null) continue;
            component.enabled = _visible;
        }
    }
}