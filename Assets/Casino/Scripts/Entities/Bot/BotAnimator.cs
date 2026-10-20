using UnityEngine;

public class BotAnimator : MonoBehaviour
{
    [SerializeField] Animator _animator;
    
    [SerializeField] private readonly int botHandlerSpinSlot = Animator.StringToHash("HandlerSpinSlot"); 

    
    public void HandlerSpinSlot()
    {
        _animator.SetTrigger(botHandlerSpinSlot);
    }
}
