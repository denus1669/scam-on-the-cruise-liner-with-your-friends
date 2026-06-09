using UnityEngine;
using Unity.Netcode;
using Blocks.Gameplay.Core;

public class FinishGameListener : MonoBehaviour
{
    [SerializeField] private GameEvent onFinishGameEvent;
    [SerializeField] private BlackGregManager blackGregManager;

    private void OnEnable() => onFinishGameEvent?.RegisterListener(OnFinish);
    private void OnDisable() => onFinishGameEvent?.UnregisterListener(OnFinish);

    private void OnFinish()
    {
        if (blackGregManager != null)
            blackGregManager.RequestFinishGameServerRpc(NetworkManager.Singleton.LocalClientId);
    }
}