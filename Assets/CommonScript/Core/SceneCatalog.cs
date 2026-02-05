using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Common/Scene Catalog", fileName = "SceneCatalog")]
public class SceneCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SceneId id;
        public string sceneName; // Build Settings に入ってる Scene 名（.unity不要）
    }

    public List<Entry> entries = new();

    public bool TryGetSceneName(SceneId id, out string sceneName)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].id == id)
            {
                sceneName = entries[i].sceneName;
                return !string.IsNullOrEmpty(sceneName);
            }
        }
        sceneName = null;
        return false;
    }
}
