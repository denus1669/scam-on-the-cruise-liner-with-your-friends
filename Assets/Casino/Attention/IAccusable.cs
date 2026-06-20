/// <summary>
/// Интерфейс для объектов, которые можно обвинить в мухлеже (нажатие кнопки действия).
/// </summary>
public interface IAccusable
{
    /// <summary>
    /// Вызывается при попытке обвинить объект.
    /// </summary>
    void OnAccuse(ulong accuserClientId);
}