using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

public class FinishGameListener : MonoBehaviour
{
    [SerializeField] private GameEvent onFinishGameEvent;
    [SerializeField] private BlackGregTable blackGregTable; 

    private void OnEnable() => onFinishGameEvent?.RegisterListener(OnFinish); 
    private void OnDisable() => onFinishGameEvent?.UnregisterListener(OnFinish);

    private void OnFinish()
    {
        if (blackGregTable != null)
            blackGregTable.RequestFinishGameServerRpc(NetworkManager.Singleton.LocalClientId);
    }
}