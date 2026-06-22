using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestNetVar : NetworkBehaviour
{
    private NetworkVariable<int> testVar = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public void TestRpc()
    {
        Debug.Log($"[Server] Value before! {testVar.Value}");

        // Эта проверка НЕ поможет, т.к. RPC выполняется и на клиенте и на сервере
        testVar.Value = 10;
        Debug.Log($"[Server] Value changed! {testVar.Value}");
    }

    private void Update()
    {
        if (IsOwner && Keyboard.current.vKey.wasPressedThisFrame)
        {
            TestRpc();
        }

    }
}