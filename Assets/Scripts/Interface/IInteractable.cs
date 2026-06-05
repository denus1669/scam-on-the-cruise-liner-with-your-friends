/// <summary>
/// Базовый интерфейс для всех объектов в игре, с которыми игрок может взаимодействовать.
/// (Столы, колоды, игровые автоматы, двери и т.д.)
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Выполняет логику взаимодействия.
    /// </summary>
    /// <param name="interactor">Объект, который инициировал взаимодействие (сам Игрок).</param>
    void Interact(UnityEngine.GameObject interactor);

    /// <summary>
    /// Возвращает текст подсказки для UI, когда игрок смотрит на объект.
    /// </summary>
    /// <returns>Строка с подсказкой (например, "Нажмите E, чтобы сесть").</returns>
    string GetInteractPrompt();
}