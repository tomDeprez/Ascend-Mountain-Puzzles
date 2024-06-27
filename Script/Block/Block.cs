using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    private int health;
    private GameManager gameManager;
    public GameObject healthTextPrefab; // Prefab du TextMeshPro
    private Canvas canvas; // Référence au Canvas
    public TextMeshProUGUI healthTextInstance;
    private Coroutine currentAnimation; // Référence à l'animation en cours
    public AudioClip ballSpawnSound; // Assign this from the Unity Inspector
    public AudioClip blockSpawnSound; // Assign this from the Unity Inspector

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        int currentLevel = gameManager.GetCurrentLevel();
        float chance = (currentLevel < 50) ? 0.01f : 0.35f;
        bool doubleHealth = Random.value < chance;

        health = currentLevel;
        if (doubleHealth) // Double la santé avec une certaine probabilité
        {
            health *= 2;
        }

        // Récupère ou crée un canvas
        canvas = GameObject.FindGameObjectWithTag("canvasLife")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found. Creating a new one.");
            // Crée un nouveau Canvas si aucun n'est trouvé
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        // Instancie le texte de santé dans le canvas
        GameObject healthTextObject = Instantiate(healthTextPrefab, canvas.transform);
        healthTextInstance = healthTextObject.GetComponent<TextMeshProUGUI>();
        if (healthTextInstance == null)
        {
            Debug.LogError("Failed to create Health Text Instance.");
            return;
        }

        if (doubleHealth)
        {
            healthTextInstance.color = Color.red; // Change la couleur du texte en rouge si la santé est doublée
        }

        UpdateHealthText();
    }

    void Update()
    {
        // Mettre à jour la position du texte pour qu'il suive le bloc
        Vector2 screenPosition = transform.position + new Vector3(0, 0, 0);
        healthTextInstance.transform.position = screenPosition; // Positionne le texte en fonction de la position à l'écran
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ball"))
        {
            health--; // Réduit la santé du bloc de 1
            UpdateHealthText();

            if (currentAnimation == null)
            {
                currentAnimation = StartCoroutine(AnimateHealthText()); // Démarre une nouvelle animation
            }

            if (health <= 0)
            {
                PlayBlockSpawnSound();
                Destroy(healthTextInstance.gameObject); // Détruit le texte de santé
                Destroy(gameObject); // Détruit le bloc si sa santé est inférieure ou égale à 0
            }
            else
            {
                PlayBallSpawnSound();
            }
        }
    }

    private void PlayBlockSpawnSound()
    {
        if (gameManager.musicPlayer != null && blockSpawnSound != null)
        {
            gameManager.musicPlayer.PlayOneShot(blockSpawnSound); // Play the sound once using the existing music player
        }
        else
        {
            Debug.LogError("Music player or ball spawn sound not set properly.");
        }
    }

    private void PlayBallSpawnSound()
    {
        if (gameManager.musicPlayer != null && ballSpawnSound != null)
        {
            gameManager.musicPlayer.PlayOneShot(ballSpawnSound); // Play the sound once using the existing music player
        }
        else
        {
            Debug.LogError("Music player or ball spawn sound not set properly.");
        }
    }

    void UpdateHealthText()
    {
        healthTextInstance.text = health.ToString();
        AdjustTextSize();
    }

    private void AdjustTextSize()
    {
        int healthLength = health.ToString().Length;

        if (healthLength <= 2)
        {
            healthTextInstance.fontSize = 90;
        }
        else if (healthLength == 3)
        {
            healthTextInstance.fontSize = 120;
        }
        else
        {
            healthTextInstance.fontSize = 200;
        }
    }

    private IEnumerator AnimateHealthText()
    {
        Vector3 originalScale = healthTextInstance.transform.localScale;
        Vector3 targetScale = originalScale * 2f;
        float duration = 0.1f;

        // Agrandissement du texte
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            healthTextInstance.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Maintien de la taille agrandie pendant un court moment
        yield return new WaitForSeconds(0.1f);

        // Rétrécissement du texte
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            healthTextInstance.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        healthTextInstance.transform.localScale = originalScale;
        currentAnimation = null; // Réinitialise la référence à l'animation en cours
    }
}
