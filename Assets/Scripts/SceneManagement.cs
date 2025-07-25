using UnityEngine;

public class SceneManagement : Singleton<SceneManagement>
{
    private string sceneTransitionName;

    public string SceneTransitionName
    {
        get { return sceneTransitionName; }
    }

    public void SetTransitionName(string sceneTransitionName)
    {
        this.sceneTransitionName = sceneTransitionName;
    }
}