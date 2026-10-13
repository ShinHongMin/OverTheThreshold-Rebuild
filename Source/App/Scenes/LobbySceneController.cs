using System;
using UnityEngine;

/// <summary>로비 편성 UI를 런 소유자와 연결한다. 확정한 파티는 출격 시 한 번 전달한다.</summary>
public sealed class LobbySceneController : MonoBehaviour
{
    [SerializeField] private LobbyView lobbyView;
    [SerializeField] private PrefabPoolManager prefabPool;
    [SerializeField] private SoundManager soundManager;

    private void Start()
    {
        var manager = RunManager.Instance;
        if (manager == null) throw new InvalidOperationException("로비의 RunManager 프리팹을 연결하세요.");
        soundManager.ConfigurePool(prefabPool);
        lobbyView.Configure(prefabPool, manager.AvailableCharacters, manager.TryStartNewRun, manager.ReturnToTitle);
        lobbyView.Show();
    }
}
