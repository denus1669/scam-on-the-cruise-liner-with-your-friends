using Blocks.Gameplay.Core;
using UnityEngine;

public class SlapGameListner : MonoBehaviour
{
    [SerializeField] private GameEvent onAttentionEvent;
    [SerializeField] private PlayerSlapController playerSlapController;

    private void OnEnable() => onAttentionEvent?.RegisterListener(OnSlapActivated);

    private void OnDisable() => onAttentionEvent?.UnregisterListener(OnSlapActivated);


    private void OnSlapActivated()
    {
        playerSlapController?.ToggleSlap();
    }
}

