using UnityEngine;

[ExecuteAlways] // Заставляет скрипт работать прямо в редакторе (Edit Mode)
public class TextureGroupSwitcher : MonoBehaviour
{
    [Tooltip("Индекс текстуры: 0, 1, 2 или 3")]
    [Range(0, 3)]
    public int textureIndex = 0;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;

    void Start()
    {
        UpdateTextureIndex();
    }

    // Вызывается каждый раз, когда ты меняешь любое значение скрипта в Инспекторе
    private void OnValidate()
    {
        // Убрали проверку на Application.isPlaying, чтобы это работало всегда
        UpdateTextureIndex();
    }

    public void UpdateTextureIndex()
    {
        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>();

        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();

        foreach (var rnd in _renderers)
        {
            if (rnd == null) continue;

            rnd.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat("_TextureIndex", textureIndex);
            rnd.SetPropertyBlock(_propBlock);
        }
    }
}