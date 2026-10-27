using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Casino.Cheating.MiniGames.MG_Slots.Wire
{
    public class DragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private NodeConnectionList list;

        [SerializeField] private LineRenderer lineRenderer;
        private NodeConnection _conn;

        private RectTransform nodeStart;
        
        private Camera cam;
        private bool _locked;

        private void Awake()
        {
            nodeStart = (RectTransform)transform;

            cam = Camera.main;
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (_locked) return;
            _conn = list.FindByStart(nodeStart);
            if (_conn == null || _conn.isOccupied) return;


            lineRenderer.enabled = true;
            Canvas startCanvas = _conn.start.GetComponentInParent<Canvas>();
            Camera uiCam = startCanvas != null ? startCanvas.worldCamera : null;

            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(
                uiCam, _conn.start.transform.position);
            
            lineRenderer.SetPosition(0, GetWorldPos(startScreen));
        }

        public void OnDrag(PointerEventData e)
        {
            if (_locked) return;

            lineRenderer.SetPosition(1, GetWorldPos(e.position));
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (_locked) return;

            var hit = e.pointerCurrentRaycast.gameObject;
            RectTransform hitRt = hit != null ? hit.GetComponentInParent<RectTransform>() : null;
            
            bool valid = hitRt != null
                         && hitRt == _conn.finish
                         && !_conn.isOccupied;

            if (valid)
            {
                Canvas targetCanvas = hitRt.GetComponentInParent<Canvas>();
                Camera uiCam = targetCanvas != null ? targetCanvas.worldCamera : null;

                Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(
                    uiCam, hitRt.transform.position);

                lineRenderer.SetPosition(1, GetWorldPos(targetScreen));
                _locked = true;
                _conn.SetVisual(true);
                _conn.isOccupied = true;
                list.NotifyChanged();   
            }
            else
            {
                lineRenderer.enabled = false;
            }
            _conn = null;
        }

        private Vector3 GetWorldPos(Vector2 screenPos)
        {
            Vector3 p = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            p.z = 0f;
            return p;
        }
        
        public void ResetState()
        {
            _locked = false;
            _conn = null;
            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }
    }
}
