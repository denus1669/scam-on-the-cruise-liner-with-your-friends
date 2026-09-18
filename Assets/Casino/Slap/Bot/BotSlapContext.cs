using Assets.Casino.Bot;
using Assets.Casino.Cheating;
using Assets.Casino.Games;
using Unity.Netcode;

namespace Assets.Casino.Slap.Bot
{
    public struct BotSlapContext
    {
        public CheatController CheatController;
        public BotBluffController BluffController;
        public BaseBotBehaviour BotBehaviour;
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
}