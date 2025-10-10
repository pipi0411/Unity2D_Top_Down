using UnityEngine;
using System.Collections.Generic;

public class SceneManagement : Singleton<SceneManagement>
{
    private string sceneTransitionName;
    private HashSet<string> clearedScenes = new HashSet<string>();

    public string CurrentSceneName { get; set; }

    public string SceneTransitionName => sceneTransitionName;

    public void SetTransitionName(string sceneTransitionName)
    {
        this.sceneTransitionName = sceneTransitionName;
    }

    // 🔑 Đánh dấu scene đã clear
    public void MarkSceneCleared(string sceneName)
    {
        if (!clearedScenes.Contains(sceneName))
            clearedScenes.Add(sceneName);
    }

    // 🔑 Kiểm tra scene đã clear chưa
    public bool IsSceneCleared(string sceneName)
    {
        return clearedScenes.Contains(sceneName);
    }

    public bool IsCurrentSceneCleared()
    {
        return IsSceneCleared(CurrentSceneName);
    }

    // 🔑 Reset khi New Game
    public void ResetClearedScenes()
    {
        clearedScenes.Clear();
    }

    // 🔑 Xóa trạng thái clear của 1 scene (nếu muốn replay)
    public void UnclearScene(string sceneName)
    {
        if (clearedScenes.Contains(sceneName))
            clearedScenes.Remove(sceneName);
    }
}
