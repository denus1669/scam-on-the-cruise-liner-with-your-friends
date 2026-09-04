using UnityEngine;
using Unity.Netcode;

public class PlayerTeleporter : NetworkBehaviour
{
    [SerializeField] private GameObject _skySphere;
    [SerializeField] private AltitudeActive altitude;

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, bool isActiveSphere)
    {
        if (!IsOwner) return;

        var cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
        }
        else
        {
            transform.position = position;
        }

        SetSkySphere(isActiveSphere);

        // Сбрасываем предыдущий таймер перед новым запуском
        altitude.SetAltitudeActive(isActiveSphere);
    }

    [ClientRpc]
    public void SphereActiveClientRpc(bool isActiveSphere)
    {
        if (!IsOwner) return;
        SetSkySphere(isActiveSphere);
    }

    private void SetSkySphere(bool active)
    {
        Debug.Log($"[PlayerTeleporter] {active}");
        _skySphere.SetActive(active);
    }
}