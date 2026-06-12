using Unity.Netcode;

public interface IBotGameBehavior
{
    void InitializeGame(NetworkBehaviour tableManager);
    void StartSession();
    void EndSession();
}