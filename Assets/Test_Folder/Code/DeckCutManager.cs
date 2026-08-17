using UnityEngine;

public class DeckCutManager : MonoBehaviour
{
    public static DeckCutManager Instance { get; private set; }

    [Header("Визуальные настройки")]
    [SerializeField] private float distanceFromCamera = 0.5f;

    private BlackGregTable currentTable;
    private ulong currentClientId;
    private Camera mainCam;

    private void Awake()
    {
        Debug.Log("PENIS1");
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCam = Camera.main;
   
    }

    // Вызывается из вашего DeckInteractable
    public void ShowSelection(BlackGregTable table, ulong clientId)
    {
        Debug.Log("PENIS2");
        currentTable = table;
        currentClientId = clientId;

        // Размещаем префаб прямо перед лицом игрока
        Transform camTransform = mainCam.transform;
        transform.position = camTransform.position + camTransform.forward * distanceFromCamera;

        // Поворачиваем колоду "лицом" к камере
        transform.rotation = Quaternion.LookRotation(camTransform.forward);

     
    }

    // Этот метод мы привяжем к событиям клика в инспекторе
    public void OnZoneSelected()
    {
        // Отправляем запрос на сервер
        if (currentTable != null)
        {
            Debug.Log("PENIS3");
            currentTable.RequestDrawCardServerRpc(currentClientId);
        }

        Debug.Log("PENIS4");
        currentTable = null;
    }
}