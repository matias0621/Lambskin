using UnityEngine;
using UnityEngine.UI;

public class TimerMask : MonoBehaviour
{
    [Header("Sprites de la Máscara")]
    // Arrastra aquí tus 6 texturas en orden
    public Sprite[] timerSprites; 

    [Header("Configuración de Tiempo")]
    public float timeLimitForImage = 10f;
    
    private Image _imageComponent;
    private float _currentTime = 0f;
    private int _currentIndex = 0;

    void Awake()
    {
        _imageComponent = GetComponent<Image>();
    }

    void Start()
    {
        if (timerSprites.Length > 0)
        {
            _imageComponent.sprite = timerSprites[0];
            _currentIndex = 1; // Empezamos a contar para el siguiente cambio
        }
    }

    void Update()
    {
        _currentTime += Time.deltaTime;

        if (_currentTime >= timeLimitForImage)
        {
            ChangeSprite();
        }
    }

    private void ChangeSprite()
    {
        if (timerSprites.Length == 0) return;

        // Cambiar el sprite actual
        _imageComponent.sprite = timerSprites[_currentIndex];
        
        _currentIndex++;
        _currentTime = 0f;

        // Si llegamos al final de la lista de sprites
        if (_currentIndex == timerSprites.Length)
        {
            // Llamamos a la muerte del humano en el Singleton
            GameManager.Instance.DeathHuman();
            
            // Reiniciamos el timer visual
            _currentIndex = 0;
            _imageComponent.sprite = timerSprites[0];
            _currentIndex = 1; 
        }
    }
}
