using UnityEngine;

public class SlotAnimator : MonoBehaviour
{
    [SerializeField] Animator _animator;
    
    [SerializeField] private readonly int handlerSpinSlot = Animator.StringToHash("HandlerSpin");
    [SerializeField] private readonly int handlerBrokenSlot = Animator.StringToHash("HandlerBroken");
    [SerializeField] private readonly int handlerExplodeSlot = Animator.StringToHash("HandlerExplode");
    [SerializeField] private readonly int handlerBigWinSlot = Animator.StringToHash("HandlerBigWin");
    [SerializeField] private readonly int handlerRepairIconSlot = Animator.StringToHash("HandlerRepairIcon");
    [SerializeField] private readonly int handlerGambleSlot = Animator.StringToHash("HandlerGamble");

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
    
    public void HandlerRepairIconSlot(bool value)
    { 
            _animator.SetBool(handlerRepairIconSlot, value);
    }

    public void HandlerGambleSlot()
    {
        _animator.SetTrigger(handlerGambleSlot);
    }
}
