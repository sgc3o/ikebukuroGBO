using UnityEngine;

public static class SceneNavigator
{
    public static void Go(SceneId id)
    {
        // Catalog取得
        if (SceneCatalogProvider.I == null || SceneCatalogProvider.I.Catalog == null)
        {
            Debug.LogError("[SceneNavigator] SceneCatalogProvider or Catalog が null");
            return;
        }

        if (!SceneCatalogProvider.I.Catalog.TryGetSceneName(id, out var sceneName))
        {
            Debug.LogError($"[SceneNavigator] SceneName SceneId={id}で見つかりません");
            return;
        }

        // Transition取得
        if (TransitionController.I == null)
        {
            Debug.LogError("[SceneNavigator] TransitionController.I がnullです");
            return;
        }

        TransitionController.I.StartTransitionToScene(sceneName);
    }
}
