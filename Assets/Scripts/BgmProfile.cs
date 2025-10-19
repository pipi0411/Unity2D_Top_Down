using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BgmProfile", menuName = "Audio/BGM Profile")]
public class BgmProfile : ScriptableObject
{
    [System.Serializable]
    public class SceneBgmData
    {
        public string sceneName;
        public AudioClip bgmClip;
    }

    [Header("Danh sách BGM cho từng Scene")]
    public List<SceneBgmData> bgmList = new List<SceneBgmData>();

    public AudioClip GetClipForScene(string sceneName)
    {
        foreach (var entry in bgmList)
        {
            if (entry.sceneName == sceneName)
                return entry.bgmClip;
        }
        return null;
    }
}

