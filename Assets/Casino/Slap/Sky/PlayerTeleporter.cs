using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

public class PlayerTeleporter : NetworkBehaviour
{
    [SerializeField] private GameObject _skySphere;
    [SerializeField] private AltitudeActive altitude;
    [SerializeField] private CoreMovement _coreMovement;
    [SerializeField] private float _speedMultiplicator = 2; 

    private void Awake()
    {
        // Кэшируем один раз при инициализации
        _coreMovement = GetComponent<CoreMovement>();
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, bool isActiveSphere)
    {
        if (!IsOwner) return;

        // 1. Сохраняем скорость ДО телепортации
        Vector3 savedVelocity = _coreMovement != null
            ? _coreMovement.HorizontalVelocity
            : Vector3.zero;

        float randomMult = Random.Range(1f, _speedMultiplicator);
        Vector3 boostedVelocity = savedVelocity * randomMult;

        // 2. Телепортируем персонажа
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
            Debug.LogError("[PlayerTeleporter] CharacterController == null");
        }


        // 3. Восстанавливаем горизонтальную скорость как импульс
        if (_coreMovement != null && savedVelocity.sqrMagnitude > 0.01f)
        {
            _coreMovement.ApplyExternalForce(savedVelocity, ForceMode.Impulse);
        }

        SetSkySphere(isActiveSphere);
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