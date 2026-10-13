using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>씬 사이에 유지되는 로더 하나. 화면 닫기→비동기 로딩→화면 열기의 순서를 담당한다.</summary>
public sealed class SceneLoader : MonoBehaviour
{
    [SerializeField] private LoadingView loadingView;
    [SerializeField, Min(0f)] private float minimumLoadingTime = 0.5f;
    public static SceneLoader Instance { get; private set; }
    public bool IsLoading { get; private set; }
    public event Action LoadingCompleted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        loadingView.Hide();
    }

    public bool CanLoad(string sceneName)
        => isActiveAndEnabled && !IsLoading && !string.IsNullOrEmpty(sceneName)
            && Application.CanStreamedLevelBeLoaded(sceneName);

    /// <summary>화면이 완전히 닫힌 후 beforeLoad를 호출한다. 전투 뷰도 이 시점에 반납한다.</summary>
    public bool TryLoad(string sceneName, Action beforeLoad = null)
    {
        if (!CanLoad(sceneName)) return false;
        IsLoading = true;
        StartCoroutine(LoadScene(sceneName, beforeLoad));
        return true;
    }

    private IEnumerator LoadScene(string sceneName, Action beforeLoad)
    {
        loadingView.CloseScreen();
        while (!loadingView.IsClosed) yield return null;
        beforeLoad?.Invoke();

        float loadingStartedAt = Time.realtimeSinceStartup;
        var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        operation.allowSceneActivation = false;
        while (operation.progress < 0.9f || Time.realtimeSinceStartup - loadingStartedAt < minimumLoadingTime)
            yield return null;
        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
        // sceneLoaded의 런 전달과 새 씬 Start의 UI·유닛 준비가 끝난 다음 화면을 연다.
        yield return null;

        loadingView.OpenScreen();
        while (!loadingView.IsOpen) yield return null;
        loadingView.Hide();
        IsLoading = false;
        LoadingCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
