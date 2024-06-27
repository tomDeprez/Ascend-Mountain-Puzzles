using System.Collections;
using UnityEngine;

public class BallBounce : MonoBehaviour
{
    public float bounceForce = 10f;
    public float launchForce = 15f;
    public float maxLaunchForce = 20f; // Limite de force de lancement
    public float deformDuration = 0.2f;
    public float deformAmount = 0.2f;
    public LineRenderer trajectoryLinePrefab; // Prefab du Line Renderer pour afficher la trajectoire
    public int predictionSteps = 30; // Nombre d'étapes de prédiction de la trajectoire
    public Gradient colorGradient; // Gradient de couleur en fonction de la distance

    private Rigidbody2D rb;
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    public bool isDragging = false;
    private bool hasLaunched = false; // Indique si la balle a été lancée
    private Vector3 originalScale;
    private bool isDeforming = false;
    private LineRenderer trajectoryLineInstance;
    private GameManager gameManager;
    private Vector2 launchDirection; // Ajout de la direction de lancement
    public bool firstBall = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true; // Désactive le Rigidbody2D au début
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Active le mode de détection de collision continue
        originalScale = transform.localScale; // Stocke l'échelle d'origine de la balle

        // Instancie la ligne de trajectoire depuis le prefab
        if (firstBall)
        {
            trajectoryLineInstance = Instantiate(trajectoryLinePrefab, transform);
            trajectoryLineInstance.positionCount = predictionSteps; // Définit le nombre de points de la ligne
            trajectoryLineInstance.colorGradient = colorGradient; // Assigne le gradient de couleur
        }


        gameManager = FindObjectOfType<GameManager>(); // Trouve le GameManager dans la scène
    }

    void Update()
    {
        // Début du tirage
        if (Input.GetMouseButtonDown(0) && !hasLaunched && gameManager.GetBallsQueue().Peek() == gameObject && gameManager.GetInitialBallCount() == gameManager.GetInitialBallCountSend() && firstBall)
        {
            startMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }

        // Fin du tirage
        if (Input.GetMouseButtonUp(0) && isDragging && !hasLaunched)
        {
            endMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            launchDirection = startMousePos - endMousePos;

            // Limite la force de lancement
            float launchMagnitude = Mathf.Min(launchDirection.magnitude * launchForce, maxLaunchForce);
            launchDirection = launchDirection.normalized * launchMagnitude;

            rb.isKinematic = false; // Active le Rigidbody2D
            rb.AddForce(launchDirection, ForceMode2D.Impulse);
            isDragging = false;
            hasLaunched = true; // Marque la balle comme lancée

            Destroy(trajectoryLineInstance.gameObject); // Détruit la ligne de trajectoire après le lancement

            // Informer le GameManager que la balle a été lancée
            gameManager.BallLaunched(launchDirection);
        }

        // Mise à jour de la trajectoire lors du tirage
        if (isDragging && !hasLaunched)
        {
            endMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 tempLaunchDirection = startMousePos - endMousePos;
            float launchMagnitude = Mathf.Min(tempLaunchDirection.magnitude * launchForce, maxLaunchForce);
            tempLaunchDirection = tempLaunchDirection.normalized * launchMagnitude;
            UpdateTrajectory(transform.position, tempLaunchDirection);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("paddleDie"))
        {
            gameManager.BallLost(gameObject);
            Destroy(gameObject);
            return;
        }

        Vector2 normal = collision.contacts[0].normal;
        Vector2 bounce = normal * bounceForce;
        rb.AddForce(bounce, ForceMode2D.Impulse);

        // Démarre la coroutine pour déformer la balle si elle n'est pas déjà en train de se déformer
        if (!isDeforming)
        {
            StartCoroutine(DeformBall());
        }
    }

    private IEnumerator DeformBall()
    {
        isDeforming = true;
        Vector3 deformedScale = new Vector3(
            originalScale.x * (1 - deformAmount),
            originalScale.y * (1 + deformAmount),
            originalScale.z
        );

        float elapsedTime = 0f;

        // Compression initiale
        while (elapsedTime < deformDuration / 2)
        {
            transform.localScale = Vector3.Lerp(originalScale, deformedScale, (elapsedTime / (deformDuration / 2)));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Étirement retour
        elapsedTime = 0f;
        while (elapsedTime < deformDuration / 2)
        {
            transform.localScale = Vector3.Lerp(deformedScale, originalScale, (elapsedTime / (deformDuration / 2)));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        isDeforming = false;
    }

    private void UpdateTrajectory(Vector2 startPosition, Vector2 initialVelocity)
    {
        Vector2 velocity = initialVelocity;
        Vector2 position = startPosition;
        trajectoryLineInstance.enabled = true;

        for (int i = 0; i < predictionSteps; i++)
        {
            trajectoryLineInstance.SetPosition(i, position);

            // Simule la physique pour le prochain point de la trajectoire
            position += velocity * Time.fixedDeltaTime;
            velocity += Physics2D.gravity * Time.fixedDeltaTime;

            // Vérifie les collisions potentielles
            RaycastHit2D hit = Physics2D.Raycast(position, velocity, velocity.magnitude * Time.fixedDeltaTime);
            if (hit.collider != null)
            {
                velocity = Vector2.Reflect(velocity, hit.normal);
                position = hit.point;
            }

            // Met à jour la couleur de la ligne en fonction de la distance parcourue
            float t = (float)i / (predictionSteps - 1);
            trajectoryLineInstance.startColor = colorGradient.Evaluate(t);
            trajectoryLineInstance.endColor = colorGradient.Evaluate(t);
        }
    }
}