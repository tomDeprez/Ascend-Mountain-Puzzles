using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    private int health;
    private GameManager gameManager;
    public GameObject healthTextPrefab; // Prefab du TextMeshPro
    private Canvas canvas; // R�f�rence au Canvas
    public TextMeshProUGUI healthTextInstance;
    private Coroutine currentAnimation; // R�f�rence � l'animation en cours
    public AudioClip ballSpawnSound; // Assign this from the Unity Inspector
    public AudioClip blockSpawnSound; // Assign this from the Unity Inspector

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        int currentLevel = gameManager.GetCurrentLevel();
        float chance = (currentLevel < 50) ? 0.01f : 0.35f;
        bool doubleHealth = Random.value < chance;

        health = currentLevel;
        if (doubleHealth) // Double la sant� avec une certaine probabilit�
        {
            health *= 2;
        }

        // R�cup�re ou cr�e un canvas
        canvas = GameObject.FindGameObjectWithTag("canvasLife")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found. Creating a new one.");
            // Cr�e un nouveau Canvas si aucun n'est trouv�
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        // Instancie le texte de sant� dans le canvas
        GameObject healthTextObject = Instantiate(healthTextPrefab, canvas.transform);
        healthTextInstance = healthTextObject.GetComponent<TextMeshProUGUI>();
        if (healthTextInstance == null)
        {
            Debug.LogError("Failed to create Health Text Instance.");
            return;
        }

        if (doubleHealth)
        {
            healthTextInstance.color = Color.red; // Change la couleur du texte en rouge si la sant� est doubl�e
        }

        UpdateHealthText();
    }

    void Update()
    {
        // Mettre � jour la position du texte pour qu'il suive le bloc
        Vector2 screenPosition = transform.position + new Vector3(0, 0, 0);
        healthTextInstance.transform.position = screenPosition; // Positionne le texte en fonction de la position � l'�cran
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ball"))
        {
            health--; // R�duit la sant� du bloc de 1
            UpdateHealthText();

            if (currentAnimation == null)
            {
                currentAnimation = StartCoroutine(AnimateHealthText()); // D�marre une nouvelle animation
            }

            if (health <= 0)
            {
                PlayBlockSpawnSound();
                Destroy(healthTextInstance.gameObject); // D�truit le texte de sant�
                Destroy(gameObject); // D�truit le bloc si sa sant� est inf�rieure ou �gale � 0
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

        // R�tr�cissement du texte
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            healthTextInstance.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        healthTextInstance.transform.localScale = originalScale;
        currentAnimation = null; // R�initialise la r�f�rence � l'animation en cours
    }
}
