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
    public int predictionSteps = 30; // Nombre d'�tapes de pr�diction de la trajectoire
    public Gradient colorGradient; // Gradient de couleur en fonction de la distance

    private Rigidbody2D rb;
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    public bool isDragging = false;
    private bool hasLaunched = false; // Indique si la balle a �t� lanc�e
    private Vector3 originalScale;
    private bool isDeforming = false;
    private LineRenderer trajectoryLineInstance;
    private GameManager gameManager;
    private Vector2 launchDirection; // Ajout de la direction de lancement
    public bool firstBall = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true; // D�sactive le Rigidbody2D au d�but
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Active le mode de d�tection de collision continue
        originalScale = transform.localScale; // Stocke l'�chelle d'origine de la balle

        // Instancie la ligne de trajectoire depuis le prefab
        if (firstBall)
        {
            trajectoryLineInstance = Instantiate(trajectoryLinePrefab, transform);
            trajectoryLineInstance.positionCount = predictionSteps; // D�finit le nombre de points de la ligne
            trajectoryLineInstance.colorGradient = colorGradient; // Assigne le gradient de couleur
        }


        gameManager = FindObjectOfType<GameManager>(); // Trouve le GameManager dans la sc�ne
    }

    void Update()
    {
        // D�but du tirage
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
            hasLaunched = true; // Marque la balle comme lanc�e

            Destroy(trajectoryLineInstance.gameObject); // D�truit la ligne de trajectoire apr�s le lancement

            // Informer le GameManager que la balle a �t� lanc�e
            gameManager.BallLaunched(launchDirection);
        }

        // Mise � jour de la trajectoire lors du tirage
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

        // D�marre la coroutine pour d�former la balle si elle n'est pas d�j� en train de se d�former
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

        // �tirement retour
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

            // V�rifie les collisions potentielles
            RaycastHit2D hit = Physics2D.Raycast(position, velocity, velocity.magnitude * Time.fixedDeltaTime);
            if (hit.collider != null)
            {
                velocity = Vector2.Reflect(velocity, hit.normal);
                position = hit.point;
            }

            // Met � jour la couleur de la ligne en fonction de la distance parcourue
            float t = (float)i / (predictionSteps - 1);
            trajectoryLineInstance.startColor = colorGradient.Evaluate(t);
            trajectoryLineInstance.endColor = colorGradient.Evaluate(t);
        }
    }
}