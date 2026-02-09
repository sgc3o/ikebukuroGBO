using UnityEngine;

public class IdleRecoveryIncentiveGuard : MonoBehaviour
{
    [Header("IdleRecoveryのルート（HierarchyのIdleRecovery）")]
    [SerializeField] private GameObject idleRecoveryRoot;

    private bool prevActive;

    public void Suspend()
    {
        if (idleRecoveryRoot == null) return;
        prevActive = idleRecoveryRoot.activeSelf;
        idleRecoveryRoot.SetActive(false);
    }

    public void Resume()
    {
        if (idleRecoveryRoot == null) return;
        // もともとONだった時だけ戻す（安全）
        if (prevActive) idleRecoveryRoot.SetActive(true);
    }
}
