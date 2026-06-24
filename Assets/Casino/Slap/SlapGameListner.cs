using Blocks.Gameplay.Core;
using UnityEngine;

public class SlapGameListner : MonoBehaviour
{
    [SerializeField] private GameEvent onSlapEvent;
    [SerializeField] private PlayerSlapController playerSlapController;

    private void OnEnable() => onSlapEvent?.RegisterListener(OnSlapPressed);

    private void OnDisable() => onSlapEvent?.UnregisterListener(OnSlapPressed);


    private void OnSlapPressed()
    {
        playerSlapController?.ExecuteSlap();
    }
}

