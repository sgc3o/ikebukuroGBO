using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public enum State { Title, GameSelect, Transition, Opening, HowToPlay,FirstStage }

    [Header("Panels")]
    public GameObject titlePanel;
    public GameObject gameSelectPanel;

    [Header("Input")]
    public LogicalUiClicker uiClicker; // 既存
    public TapFeedbackSpawner tapFx;   // 任意
    public TouchGate gate;
    public GameObject transitionPanel;
    public GameObject[] openingPanels; // size 4 推奨

    [Header("Gate Presets")]
    [Range(0f, 1f)] public float titleActiveMinY01 = 0.0f;     // Titleは全画面OKにしたいなら0
    [Range(0f, 1f)] public float selectActiveMinY01 = 0.5f;    // GameSelectは下半分のみ、など

    [Header("Game Select")]
    public int selectedGameIndex = 0;
    
    public State Current { get; private set; } = State.Title;

    public GameObject confirmPanel; // ConfirmPanelをドラッグで入れる

    public TransitionController transition; // InspectorでTransitionPanelのTransitionControllerを入れる

    [Header("1stStage")]
    public GameObject firstStagePanel;

    void SetActiveAll(GameObject[] arr, bool active)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i]) arr[i].SetActive(active);
        }

    void HideAll()
    {
        if (titlePanel) titlePanel.SetActive(false);
        if (gameSelectPanel) gameSelectPanel.SetActive(false);
        if (transitionPanel) transitionPanel.SetActive(false);
        if (confirmPanel) confirmPanel.SetActive(false);
        if (firstStagePanel) firstStagePanel.SetActive(false);
    
    }

    void Start()
    {
        HideAll();
        GoTitle();
    }

    void Update()
    {
        //デバック用ショートカットは必要な時に使う
        if (Input.GetKeyDown(KeyCode.Alpha1)) GoTitle();
        //if (Input.GetKeyDown(KeyCode.Alpha2)) GoGameSelect();
        //if (Input.GetKeyDown(KeyCode.Alpha3)) GoTransition();
    }

    public void GoTitle()
    {
        Current = State.Title;
        
        HideAll();
        if (titlePanel) titlePanel.SetActive(true);
        if (gate) gate.activeMinY01 = titleActiveMinY01;
        SetInputEnabled(true);
    }

    void SetInputEnabled(bool enabled)
    {
        if (uiClicker) uiClicker.enabled = enabled;
        if (tapFx) tapFx.enabled = enabled;
    }

    public void GoGameSelect()
    {
        Current = State.GameSelect;
        HideAll();
        if (gameSelectPanel) gameSelectPanel.SetActive(true);
        if (gate) gate.activeMinY01 = selectActiveMinY01;
        SetInputEnabled(true);

        if (gate) gate.activeMinY01 = selectActiveMinY01;
        SetInputEnabled(true);
    }


//シーンID参照
    SceneId GetSceneIdByIndex(int idx)
    {
        return idx switch
        {
            0 => SceneId.S_MemoryGameRoot,
            1 => SceneId.S_PuzzleGameRoot,
            2 => SceneId.S_PuzzleGameRoot,
            3 => SceneId.S_PuzzleGameRoot,
            _ => SceneId.S_PuzzleGameRoot,
        };
    }

    public void GoTransition()
    {
        Current = State.Transition;
        SetInputEnabled(false);

        var sceneId = GetSceneIdByIndex(selectedGameIndex);
        Debug.Log($"[GSM] GoTransition idx={selectedGameIndex} -> {sceneId}");

        SceneNavigator.Go(sceneId);
    }

}