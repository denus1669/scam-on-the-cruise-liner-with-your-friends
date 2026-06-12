using Unity.Netcode;
using UnityEngine;

public class GAME : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] Vector3 spawnPosition;
    void Start()
    {
        SpawnBotServerRpc(spawnPosition);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnBotServerRpc(Vector3 spawnPosition)
    {
        GameObject botPrefab = Resources.Load<GameObject>("BotPrefab");
        GameObject botInstance = Instantiate(botPrefab, spawnPosition, Quaternion.identity);
        NetworkObject netObj = botInstance.GetComponent<NetworkObject>();
        netObj.Spawn(); // спавним на всех клиентах
    }
}
