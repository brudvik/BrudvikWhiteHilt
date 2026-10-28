using UnityEngine;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Lives on the game object for one world session: registers the loading wait's RPCs and, on the server, answers the
/// players' questions about their destination (<see cref="LoadingWait"/>).
/// </summary>
public class LoadingWaitService : MonoBehaviour
{
    private void Start()
    {
        LoadingWait.RegisterRpc();
    }

    private void Update()
    {
        LoadingWait.UpdateServer();
    }
}
