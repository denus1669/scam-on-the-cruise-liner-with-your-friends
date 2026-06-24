using Unity.Netcode;
public struct BotSlapContext
{
    public CheatController CheatController;
    public BaseBotBehavior BotBehavior;
    public BotDispleasureController DispleasureController;
    public BotSlapRouter Router; 
    public IGameTable GameTable;
    public NetworkObject TargetNetworkObject;
}