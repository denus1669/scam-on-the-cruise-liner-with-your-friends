using Blocks.Gameplay.Core;
using UnityEngine;

public class PlayerCheatingGameListner : MonoBehaviour
{
    [SerializeField] private GameEvent onAttentionEvent;
    [SerializeField] private PlayerCheatController playerCheatController;



    private void OnEnable() => onAttentionEvent?.RegisterListener(OnCheatingActivated);

    private void OnDisable() => onAttentionEvent?.UnregisterListener(OnCheatingActivated);

    private void OnCheatingActivated()
    {
        playerCheatController?.RequestCheatServerRpc("");
    }
}
