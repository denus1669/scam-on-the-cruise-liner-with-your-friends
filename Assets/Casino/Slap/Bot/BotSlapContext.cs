using Unity.Netcode;

public struct BotSlapContext
{
    public CheatController CheatController;
    public BaseBotBehavior BotBehavior;
    public BotDispleasureController DispleasureController;
    public BotSlapRouter Router;
    public NetworkObject TargetNetworkObject;

    // НОВОЕ: ссылка на агента бота для динамического получения стола
    public BotAgent BotAgent;

    // Вспомогательный метод для получения актуального TableType
    public string GetCurrentTableType()
    {
        return BotAgent?.CurrentTable?.TableType ?? "Unknown";
    }

    // Вспомогательный метод для получения актуального стола
    public IGameTable GetCurrentTable()
    {
        return BotAgent?.CurrentTable;
    }
}