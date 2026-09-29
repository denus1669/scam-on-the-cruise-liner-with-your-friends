using UnityEngine;

namespace Assets.Casino.Test_Folder
{
    public class CardVisualController : MonoBehaviour
    {
        public Vector2 faceIndex;
        public bool isVisible = true;
        public Vector2 placeholderIndex;

        [Range(0f, 1f)]
        [SerializeField] private float cheatGlow = 0f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private static readonly int FaceId = Shader.PropertyToID("_FaceIndex");
        private static readonly int VisibleId = Shader.PropertyToID("_IsVisible");
        private static readonly int PlaceholderId = Shader.PropertyToID("_PlaceholderIndex");
        private static readonly int CheatId = Shader.PropertyToID("_IsCheated");


        void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            UpdateCardVisuals();
        }

        void OnValidate()
        {
            UpdateCardVisuals();
        }

        /// <summary>
        /// Устанавливает интенсивность свечения читерской карты (0 = выключено).
        /// </summary>
        public void SetCheatGlow(float value)
        {
            cheatGlow = Mathf.Max(0f, value);
            UpdateCardVisuals();
        }

        /// <summary>
        /// Возвращает текущую интенсивность свечения.
        /// </summary>
        public float GetCheatGlow() => cheatGlow;

        public void UpdateCardVisuals()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_propBlock);

            _propBlock.SetVector(FaceId, faceIndex);
            _propBlock.SetVector(PlaceholderId, placeholderIndex);
            _propBlock.SetFloat(VisibleId, isVisible ? 1f : 0f);
            _propBlock.SetFloat(CheatId, cheatGlow);

            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}