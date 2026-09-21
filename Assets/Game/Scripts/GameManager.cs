using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Resolve the deadline before combat Updates in the frame where time expires.
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public enum RunState { SelectingDifficulty, Playing, Won, Lost }
    public const string TotalKillsKey = "Skyloft.LifetimeEnemyKills";

    [SerializeField, Min(0.1f)] private float matchDuration = 180f;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerController playerMovement;
    [SerializeField] private AutoAttackController playerAttack;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Transform enemies;
    [SerializeField] private Transform projectiles;
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private GameObject hud;
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultTitle;
    [SerializeField] private Text resultKills;

    public RunState State { get; private set; } = RunState.SelectingDifficulty;
    public float RemainingTime { get; private set; }
    public int RunKills => enemySpawner.KillCount;
    public int TotalKills { get; private set; }
    public DifficultyConfig SelectedDifficulty { get; private set; }

    private bool replayRequested;

    private void Start()
    {
        RemainingTime = matchDuration;
        TotalKills = PlayerPrefs.GetInt(TotalKillsKey, 0);
        playerHealth.Died += HandlePlayerDeath;
        SetGameplayActive(false);
        difficultyPanel.SetActive(true);
        resultPanel.SetActive(false);
    }

    public void StartRun(DifficultyConfig difficulty)
    {
        if (State != RunState.SelectingDifficulty || difficulty == null)
            return;

        SelectedDifficulty = difficulty;
        enemySpawner.ApplyDifficulty(difficulty);
        RemainingTime = Mathf.Max(0.1f, matchDuration);
        State = RunState.Playing;
        difficultyPanel.SetActive(false);
        SetGameplayActive(true);
    }

    private void Update()
    {
        if (State != RunState.Playing)
            return;

        if (!playerHealth.IsAlive)
        {
            FinishRun(RunState.Lost);
            return;
        }

        RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
        if (RemainingTime <= 0f)
            FinishRun(RunState.Won);
    }

    private void HandlePlayerDeath(Health health) => FinishRun(RunState.Lost);

    private void FinishRun(RunState result)
    {
        if (State != RunState.Playing)
            return;

        // Latch the result before stopping objects or saving: callbacks cannot count twice.
        State = result;
        SetGameplayActive(false);
        DestroyChildren(enemies);
        DestroyChildren(projectiles);
        TotalKills = PlayerPrefs.GetInt(TotalKillsKey, 0) + RunKills;
        PlayerPrefs.SetInt(TotalKillsKey, TotalKills);
        PlayerPrefs.Save();

        resultTitle.text = result == RunState.Won ? "YOU SURVIVED" : "YOU DIED";
        resultKills.text = $"Run Kills: {RunKills}\nTotal Kills: {TotalKills}";
        resultPanel.SetActive(true);
    }

    private void SetGameplayActive(bool active)
    {
        playerHealth.enabled = active;
        playerMovement.enabled = active;
        playerAttack.enabled = active;
        enemySpawner.enabled = active;
        enemies.gameObject.SetActive(active);
        projectiles.gameObject.SetActive(active);
        joystick.gameObject.SetActive(active);
        hud.SetActive(active);
    }

    private static void DestroyChildren(Transform parent)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
    }

    public void Replay()
    {
        if ((State != RunState.Won && State != RunState.Lost) || replayRequested)
            return;

        replayRequested = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.Died -= HandlePlayerDeath;
    }
}
