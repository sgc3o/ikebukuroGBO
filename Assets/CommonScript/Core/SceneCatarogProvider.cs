using UnityEngine;

public class SceneCatalogProvider : MonoBehaviour
{
    public static SceneCatalogProvider I { get; private set; }

    [SerializeField] SceneCatalog catalog;
    public SceneCatalog Catalog => catalog;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
}
