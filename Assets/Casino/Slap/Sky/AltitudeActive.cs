using TMPro;
using UnityEngine;

public class AltitudeActive : MonoBehaviour
{
    [SerializeField] private GameObject altitudeRoot;
    [SerializeField] private TextMeshProUGUI altitudeTime;
    [SerializeField] private TextMeshProUGUI altitudeMax;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float disableDelay = 10f;

    private bool _isActive;
    private float _startTime;
    private float _maxHeight;

    public void SetAltitudeActive(bool isActive)
    {
        if (isActive) Enable();
        else StartDisableTimer();
    }

    private void Enable()
    {
        _isActive = true;
        _startTime = Time.time;
        _maxHeight = 0f;

        altitudeRoot.SetActive(true);
        UpdateUI();
    }

    private void StartDisableTimer()
    {
        Invoke(nameof(Disable), disableDelay);
    }

    private void Disable()
    {
        
    }

    private void Update()
    {
        if (!_isActive) return;

        float currentY = playerTransform.position.y;
        if (currentY > _maxHeight)
            _maxHeight = currentY;

        UpdateUI();
    }

    private void UpdateUI()
    {
        float elapsed = Time.time - _startTime;
        altitudeTime.text = $"{elapsed:F1}s";
        altitudeMax.text = $"{_maxHeight:F1}m";
    }
}