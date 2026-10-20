using System.Collections;
using Assets.Casino.Slots;
using UnityEngine;

public class SlotAnimator : MonoBehaviour
{
    [SerializeField] Animator _animator;

    [SerializeField] private Transform reelFirstTransform;
    [SerializeField] private Transform reelSecondTransform;
    [SerializeField] private Transform reelThirdTransform;
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Spin")]
    [SerializeField] private float spinDuration = 5f;
    [SerializeField] private float reelDelay = 0.15f;      // задержка между барабанами
    [SerializeField] private int fullTurns = 3;             // сколько полных оборотов «накрутить»
    public float SpinDuration => spinDuration;
    private Coroutine _spinRoutine;

    [SerializeField] private readonly int handlerSpinSlot = Animator.StringToHash("HandlerSpin");
    [SerializeField] private readonly int handlerBrokenSlot = Animator.StringToHash("HandlerBroken");
    [SerializeField] private readonly int handlerExplodeSlot = Animator.StringToHash("HandlerExplode");
    [SerializeField] private readonly int handlerBigWinSlot = Animator.StringToHash("HandlerBigWin");
    [SerializeField] private readonly int handlerRepairIconSlot = Animator.StringToHash("HandlerIconRepair");
    [SerializeField] private readonly int handlerRepairHideIconSlot = Animator.StringToHash("HandlerIconRepairHide");
    [SerializeField] private readonly int handlerReelsSpinSlot = Animator.StringToHash("HandlerReelsSpin");

    public void HandlerSpinSlot()
    {
        _animator.SetTrigger(handlerSpinSlot);
    }
    
    public void HandlerBrokenSlot()
    {
        _animator.SetTrigger(handlerBrokenSlot);
    }

    public void HandlerExplodeSlot()
    {
        _animator.SetTrigger(handlerExplodeSlot);
    }
    
    public void HandlerBigWinSlot()
    {
        _animator.SetTrigger(handlerBigWinSlot);
    }
    
    public void HandlerRepairIconSlot(bool isBroken)
    { 
        _animator.SetBool(handlerRepairIconSlot, isBroken);
    }
    
    public void HandlerRepairIconHideSlot()
    { 
        _animator.SetTrigger(handlerRepairHideIconSlot);
    }
    
    public void HandlerReelsSpinSlot()
    {
        if (_spinRoutine != null) StopCoroutine(_spinRoutine);
        _animator.enabled = false;

        _spinRoutine = StartCoroutine(SpinAllReels());

        //_animator.SetTrigger(handlerReelsSpinSlot);
    }
        
    private IEnumerator SpinAllReels()
    {
        int running = 3;

        StartCoroutine(SpinReel(reelFirstTransform,  0f,           () => running--));
        StartCoroutine(SpinReel(reelSecondTransform, reelDelay,     () => running--));
        StartCoroutine(SpinReel(reelThirdTransform,  reelDelay*2f,  () => running--));

        // ждём, пока все три закончат
        while (running > 0) yield return null;

        _animator.enabled = true;
        _spinRoutine = null;
    }

    private IEnumerator SpinReel(Transform reel, float delay, System.Action onDone)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // Берём ТЕКУЩИЙ угол по X — это то место, где закончилась предыдущая крутка.
        // Unity нормализует localEulerAngles в [0,360), но это визуально тот же угол.
        float startX = reel.localEulerAngles.x;
        float randomFinal = Random.Range(-180000f, 0f);

        // +fullTurns*360 — мотаем «вперёд», +randomFinal — случайная остановка.
        // Знак определяет направление: +X → символы «едут» вниз/на себя.
        float endX = startX + fullTurns * 360f + randomFinal;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / spinDuration;
            float eased = spinCurve.Evaluate(Mathf.Clamp01(t));
            float currentX = Mathf.Lerp(startX, endX, eased);
            reel.localRotation = Quaternion.Euler(currentX, 0f, 0f);
            yield return null;
        }

        // Фиксируем конечный угол — это стартовая точка для следующей крутки.
        reel.localRotation = Quaternion.Euler(endX, 0f, 0f);
    }
}
