using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Casino
{
    public class ClickDebugger : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = Input.mousePosition;

                System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                if (results.Count > 0)
                    Debug.Log($"Клик попал в: {results[0].gameObject.name}");
                else
                    Debug.Log("Клик в пустоту (ничего не заблокировано)");
            }
        }
    }
}