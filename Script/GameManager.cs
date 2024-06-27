using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.Advertisements;

public class GameManager : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener, IUnityAdsInitializationListener
{
    public GameObject ballPrefab; // Prefab de la balle
    public GameObject blockPrefab; // Prefab du block
    public GameObject gameOverCanvas; // Prefab du block
    public Transform spawnPoint; // Point de spawn pour la nouvelle balle
    public Transform blocksParent; // Parent des blocs
    public TextMeshProUGUI levelText; // Texte pour afficher le niveau
    public TextMeshProUGUI ballsCountText; // Texte pour afficher le nombre de balles restantes
    public AudioSource musicPlayer; // This will be assigned from the Unity Inspector
    public AudioClip backgroundMusic; // This will also be assigned from the Unity Inspector



    private int initialBallCount = 0; // Nombre initial de balles
    private int initialBallCountSend = 0; // Nombre de balles envoyées
    private int blocksPerLevel = 9; // Nombre de blocs par niveau
    private float blockSpacingX = 0.5f; // Espacement horizontal entre les blocs
    private float blockSpacingY = 1f; // Espacement vertical entre les blocs
    private float initialSpawnChance = 0.2f; // Pourcentage initial de chance de spawn des blocs
    private float spawnChanceIncrement = 0.05f; // Augmentation du pourcentage de chance de spawn tous les 10 niveaux

    private Queue<GameObject> ballsQueue = new Queue<GameObject>();
    private List<GameObject> blocksList = new List<GameObject>();
    private int currentLevel = 1;
    private bool isGameOver = false;
    private bool hasBallBeenLaunched = false; // Variable pour vérifier si la première balle a été lancée
    private float ballCheckInterval = 1f; // Intervalle de vérification des balles en secondes
    private float ballMinSpeed = 0.1f; // Vitesse minimale pour considérer qu'une balle est bloquée
    private float ballStuckTime = 5f; // Temps avant de considérer qu'une balle est bloquée

    private Coroutine levelTextAnimationCoroutine;
    private Coroutine ballsCountTextAnimationCoroutine;
    private bool isLevelTextAnimating = false; // Variable pour vérifier si une animation de niveau est en cours
    private bool isBallsCountTextAnimating = false; // Variable pour vérifier si une animation de nombre de balles est en cours

    private string gameId = "5645925"; // Your Unity Ads game ID
    private string placementId = "Rewarded_Android"; // Your Unity Ads placement ID
    private bool testMode = false; // Set to false for production
    private Action onAdComplete; // Callback for when the ad is complete

    void Start()
    {
        Advertisement.Initialize(gameId, testMode, this);

        LoadAd();
        initialBallCountSend = initialBallCount;
        SpawnInitialBall();
        SpawnNewLevelBlocks();
        StartCoroutine(CheckBallsStuck());
        UpdateLevelText();
        UpdateBallsCountText();
        PlayBackgroundMusic();
    }

    private void PlayBackgroundMusic()
    {
        if (musicPlayer != null && backgroundMusic != null)
        {
            musicPlayer.clip = backgroundMusic;
            musicPlayer.loop = true; // Ensure the music loops
            musicPlayer.Play();
        }
        else
        {
            Debug.LogError("Music player or background music not set properly.");
        }
    }

    private void LoadAd()
    {
        Advertisement.Load(placementId, this);
    }

    private void SpawnInitialBall()
    {
        // Check if the game is over before launching any balls
        if (gameOverCanvas.activeSelf)
        {
            Debug.Log("Cannot launch balls, game is over!");
            return;
        }

        GameObject ball = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);
        ballsQueue.Enqueue(ball);
        UpdateBallsCountText();
    }


    public void BallLaunched(Vector2 launchDirection)
    {
        hasBallBeenLaunched = true; // Marque que la première balle a été lancée
        StartCoroutine(SpawnBallsWithDelay(launchDirection));
    }

    public void BallLost(GameObject ball)
    {
        if (ballsQueue.Contains(ball))
        {
            ballsQueue = new Queue<GameObject>(ballsQueue.Where(b => b != ball));
        }

        if (ballsQueue.Count == 0)
        {
            currentLevel++;
            initialBallCount++;
            initialBallCountSend = initialBallCount;
            UpdateLevelText();
            MoveBlocksDown();
            SpawnInitialBall();
            SpawnNewLevelBlocks();
        }
        UpdateBallsCountText();
    }

    private IEnumerator SpawnBallsWithDelay(Vector2 launchDirection)
    {
        for (int i = 0; i < initialBallCount; i++)
        {
            initialBallCountSend--;
            GameObject ball = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);
            ball.GetComponent<BallBounce>().isDragging = false;
            ball.GetComponent<BallBounce>().firstBall = false;
            ballsQueue.Enqueue(ball);
            UpdateBallsCountText();

            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            Collider2D collider = ball.GetComponent<Collider2D>();
            collider.enabled = false; // Désactive les collisions

            yield return new WaitForSeconds(0.3f); // Attendre 0.3 seconde avant de lancer la prochaine balle

            if (launchDirection != Vector2.zero)
            {
                rb.isKinematic = false; // Active le Rigidbody2D
                collider.enabled = true; // Active les collisions
                rb.AddForce(launchDirection, ForceMode2D.Impulse);
            }
        }
    }

    private void SpawnNewLevelBlocks()
    {
        float totalWidth = (blocksPerLevel - 1) * blockSpacingX;
        float startX = -totalWidth / 2;
        float spawnChance = initialSpawnChance + (currentLevel / 10) * spawnChanceIncrement;

        bool atLeastOneBlockSpawned = false;

        for (int i = 0; i < blocksPerLevel; i++)
        {
            if (UnityEngine.Random.value < spawnChance)
            {
                float xPosition = startX + i * blockSpacingX;
                Vector3 blockPosition = new Vector3(xPosition, 4, 0); // Ajuster la position en fonction de votre jeu
                GameObject block = Instantiate(blockPrefab, blockPosition, Quaternion.identity, blocksParent);
                blocksList.Add(block);
                atLeastOneBlockSpawned = true;
            }
        }

        // Assurez-vous qu'au moins un bloc est spawné
        if (!atLeastOneBlockSpawned)
        {
            int randomIndex = UnityEngine.Random.Range(0, blocksPerLevel);
            float xPosition = startX + randomIndex * blockSpacingX;
            Vector3 blockPosition = new Vector3(xPosition, 4, 0); // Ajuster la position en fonction de votre jeu
            GameObject block = Instantiate(blockPrefab, blockPosition, Quaternion.identity, blocksParent);
            blocksList.Add(block);
        }
    }

    void MoveBlocksDown()
    {
        foreach (GameObject block in blocksList.ToList())
        {
            if (block != null)
            {
                block.transform.position += Vector3.down * blockSpacingY;
                if (block.transform.position.y <= -5) // Ajuster la limite basse selon les besoins
                {
                    isGameOver = true;
                    Debug.Log("Game Over");
                    ShowGameOverCanvas(); // Appeler la fonction pour afficher le canvas de game over
                }
            }
        }
    }

    void ShowGameOverCanvas()
    {
        // Assurez-vous que vous avez un référence au canvas de Game Over dans votre GameManager
        gameOverCanvas.SetActive(true); // Active le canvas de Game Over
    }

    private IEnumerator CheckBallsStuck()
    {
        Dictionary<GameObject, float> ballStuckTimers = new Dictionary<GameObject, float>();

        while (!isGameOver)
        {
            if (hasBallBeenLaunched && initialBallCountSend < initialBallCount) // Vérifie si la première balle a été lancée et si des balles ont été envoyées
            {
                yield return new WaitForSeconds(ballCheckInterval);

                foreach (GameObject ball in ballsQueue.ToList())
                {
                    if (ball == null)
                    {
                        continue;
                    }

                    Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                    if (rb.velocity.magnitude < ballMinSpeed)
                    {
                        if (!ballStuckTimers.ContainsKey(ball))
                        {
                            ballStuckTimers[ball] = Time.time;
                        }
                        else if (Time.time - ballStuckTimers[ball] > ballStuckTime)
                        {
                            // Détruire la balle si elle est bloquée depuis plus de ballStuckTime secondes
                            BallLost(ball);
                            Destroy(ball);
                            ballStuckTimers.Remove(ball);
                        }
                    }
                    else
                    {
                        if (ballStuckTimers.ContainsKey(ball))
                        {
                            ballStuckTimers.Remove(ball);
                        }
                    }
                }
            }
            else
            {
                yield return null; // Attendre la prochaine frame si aucune balle n'a été lancée ou si aucune balle n'a été envoyée
            }
        }
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public int GetInitialBallCountSend()
    {
        return initialBallCountSend;
    }

    public int GetInitialBallCount()
    {
        return initialBallCount;
    }

    public Queue<GameObject> GetBallsQueue()
    {
        return ballsQueue;
    }

    private void UpdateLevelText()
    {
        levelText.text = "Level: " + currentLevel;
        if (!isLevelTextAnimating)
        {
            if (levelTextAnimationCoroutine != null)
            {
                StopCoroutine(levelTextAnimationCoroutine);
            }
            levelTextAnimationCoroutine = StartCoroutine(AnimateText(levelText));
        }
    }

    private void UpdateBallsCountText()
    {
        ballsCountText.text = "Balls: " + (initialBallCountSend);
        if (!isBallsCountTextAnimating)
        {
            if (ballsCountTextAnimationCoroutine != null)
            {
                StopCoroutine(ballsCountTextAnimationCoroutine);
            }
            ballsCountTextAnimationCoroutine = StartCoroutine(AnimateText(ballsCountText));
        }
    }

    private IEnumerator AnimateText(TextMeshProUGUI text)
    {
        if (text == levelText)
        {
            isLevelTextAnimating = true;
        }
        else if (text == ballsCountText)
        {
            isBallsCountTextAnimating = true;
        }

        Vector3 originalScale = text.transform.localScale;
        Vector3 targetScale = originalScale * 1.5f;
        float maxScale = 2.0f; // Taille maximale du texte
        float duration = 0.2f;

        // Agrandissement du texte
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            text.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Vérifier et ajuster la taille maximale
        if (text.transform.localScale.x > maxScale)
        {
            text.transform.localScale = new Vector3(maxScale, maxScale, maxScale);
        }

        yield return new WaitForSeconds(0.1f);

        // Rétrécissement du texte
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            text.transform.localScale = Vector3.Lerp(text.transform.localScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        text.transform.localScale = originalScale;

        if (text == levelText)
        {
            isLevelTextAnimating = false;
        }
        else if (text == ballsCountText)
        {
            isBallsCountTextAnimating = false;
        }
    }

    public void ReplayGame()
    {
        gameOverCanvas.SetActive(false); // Ferme le canvas Game Over
        ResetGame();
    }

    public void ContinueAfterAd()
    {
        ShowAd(() =>
        {
            gameOverCanvas.SetActive(false); // Ferme le canvas Game Over après la visualisation de la pub
            ContinueGame();
        });
    }

    void ResetGame()
    {
        currentLevel = 1;
        initialBallCount = 0;
        initialBallCountSend = 0;
        CleanupBallsAndBlocksLifeBlock();
        SpawnNewLevelBlocks();
        UpdateLevelText();
        UpdateBallsCountText();
        isGameOver = false;
        SpawnInitialBall();
    }

    void CleanupBallsAndBlocksLifeBlock()
    {
        // Cleanup balls
        foreach (var ball in ballsQueue)
        {
            Destroy(ball);
        }
        ballsQueue.Clear();

        // Cleanup blocks
        foreach (var block in blocksList)
        {
            Destroy(block);
        }
        blocksList.Clear();

        // Remove all objects tagged as "lifeBlock"
        GameObject[] lifeBlocks = GameObject.FindGameObjectsWithTag("lifeBlock");
        foreach (GameObject lifeBlock in lifeBlocks)
        {
            Destroy(lifeBlock);
        }
    }

    void ContinueGame()
    {
        isGameOver = false; // Reset the game over flag
        RemoveBottomBlocks(); // New method to remove only the bottom blocks
        SpawnInitialBall(); // Respawn the initial ball to continue playing
    }

    void RemoveBottomBlocks()
    {
        // Determine the bottom threshold where blocks should be removed
        float bottomThreshold = -4.0f; // Adjust this value based on your game's layout and block positioning

        // Create a list to hold blocks that need to be removed
        List<GameObject> blocksToRemove = new List<GameObject>();

        // Iterate through the blocks list and add blocks below the threshold to the removal list
        foreach (GameObject block in blocksList)
        {
            // Safeguard against null references
            if (block != null && block.transform.position.y <= bottomThreshold)
            {
                blocksToRemove.Add(block);
            }
        }

        // Iterate through the removal list to destroy the blocks and remove them from the main list
        foreach (GameObject block in blocksToRemove)
        {
            if (block != null)
            { // Check again to be safe
                blocksList.Remove(block); // Remove the block from the main list
                Destroy(block.GetComponent<Block>().healthTextInstance.gameObject);
                Destroy(block); // Destroy the block
            }
        }
    }

    void CleanupBallsAndBlocks()
    {
        foreach (var ball in ballsQueue)
        {
            Destroy(ball);
        }
        ballsQueue.Clear();

        foreach (var block in blocksList)
        {
            Destroy(block);
        }
        blocksList.Clear();
    }

    void MoveBlocksUp()
    {
        foreach (GameObject block in blocksList)
        {
            if (block != null)
            {
                block.transform.position += Vector3.up * blockSpacingY;
            }
        }
    }

    void ShowAd(Action onComplete)
    {
        this.onAdComplete = onComplete;
        Advertisement.Load(placementId, this);
        Debug.Log("Loading Ad...");
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }

    // Assurez-vous que l'annonce est chargée avant de l'afficher
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log("Ad Loaded: " + placementId);
        DisplayAd(); // Appeler DisplayAd ici seulement si vous souhaitez l'afficher automatiquement après le chargement
    }

    private void DisplayAd()
    {
        Advertisement.Show(placementId, this);
        Debug.Log("Loading Ad...");
    }



    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Failed to load ad for {placementId} with error {error}: {message}");
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Failed to show ad for {placementId} with error {error}: {message}");
    }

    public void OnUnityAdsShowStart(string placementId) { }

    public void OnUnityAdsShowClick(string placementId) { }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("Ad Finished. Give reward to player.");
            onAdComplete?.Invoke();
        }
        else if (showCompletionState == UnityAdsShowCompletionState.SKIPPED)
        {
            Debug.Log("Ad Skipped.");
        }
        else if (showCompletionState == UnityAdsShowCompletionState.UNKNOWN)
        {
            Debug.LogError("Ad Show Unknown state.");
        }
    }
}
