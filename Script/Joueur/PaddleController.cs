using UnityEngine;

public class PaddleController : MonoBehaviour
{
    public float speed = 10f;  // Vitesse du d�placement de la barre
    public float boundaryPadding = 0.5f;  // Marge par rapport aux bords de l'�cran

    private float screenWidth;
    private float paddleWidth;

    void Start()
    {
        screenWidth = Camera.main.aspect * Camera.main.orthographicSize * 2;
        paddleWidth = transform.localScale.x;
    }

    void Update()
    {
        Vector3 pos = transform.position;

        // G�rer les entr�es du clavier pour les tests dans l'�diteur
        float input = Input.GetAxis("Horizontal");
        pos.x += input * speed * Time.deltaTime;

        // G�rer les entr�es tactiles pour les mobiles
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
            pos.x = Mathf.Clamp(touchPosition.x, -screenWidth / 2 + paddleWidth / 2 + boundaryPadding, screenWidth / 2 - paddleWidth / 2 - boundaryPadding);
        }

        // Limiter la position de la barre pour �viter qu'elle ne sorte de l'�cran
        pos.x = Mathf.Clamp(pos.x, -screenWidth / 2 + paddleWidth / 2 + boundaryPadding, screenWidth / 2 - paddleWidth / 2 - boundaryPadding);

        transform.position = pos;
    }
}
