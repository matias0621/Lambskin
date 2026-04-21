using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Referencias")]
    public MeshRenderer portalRenderer;
    [SerializeField] private List<Texture2D> maskTextures; // Tus 6 texturas
    
    [Header("Configuración")]
    public float timeLimitForImage = 1.0f;
    
    private bool _isAMonsterInside = false;
    private float _timer = 0f;
    private int _textureIndex = 1;
    private PlayerMovement _activePlayer;
    private Material _portalMaterial;

    void Start()
    {
        if (portalRenderer != null)
        {
            // En Unity, acceder a .material automáticamente crea una instancia única (como duplicate())
            _portalMaterial = portalRenderer.material;

            if (maskTextures.Count > 0)
            {
                _portalMaterial.mainTexture = maskTextures[0];
                _portalMaterial.color = Color.white;
            }
        }
    }

    void Update()
    {
        if (!_isAMonsterInside || _activePlayer == null) return;

        // Si el jugador deja de ser monstruo (por ejemplo, le pasaron la máscara mientras estaba en el portal)
        if (_activePlayer.isHuman)
        {
            ResetPortal();
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= timeLimitForImage)
        {
            _timer = 0f;
            UpdatePortalVisuals();
        }
    }

    private void UpdatePortalVisuals()
    {
        if (_textureIndex < maskTextures.Count)
        {
            // Cambiar textura
            _portalMaterial.mainTexture = maskTextures[_textureIndex];

            // Calcular intensidad roja (progresión hacia el rojo)
            float intensity = (float)_textureIndex / (maskTextures.Count - 1);
            // Color(R, G, B, A) -> Bajamos G y B para que solo quede R
            _portalMaterial.color = new Color(1, 1 - intensity, 1 - intensity, 1);

            _textureIndex++;

            // Si llegamos al final de la secuencia
            if (_textureIndex >= maskTextures.Count)
            {
                CompleteTransformation();
            }
        }
    }

    private void CompleteTransformation()
    {
        if (_activePlayer != null)
        {
            _activePlayer.StartImmunity(); // Activa inmunidad con timer al terminar
            _activePlayer.canMove = true;
        }
        
        Debug.Log("Secuencia de portal completada");
        Destroy(gameObject); // queue_free()
    }

    private void ResetPortal()
    {
        _isAMonsterInside = false;
        if (_activePlayer != null)
        {
            _activePlayer.canMove = true;
            _activePlayer = null;
        }
        _timer = 0f;
        _textureIndex = 1;

        if (_portalMaterial != null && maskTextures.Count > 0)
        {
            _portalMaterial.mainTexture = maskTextures[0];
            _portalMaterial.color = Color.white;
        }
    }

    // --- DETECCIÓN DE ÁREA ---

    private void OnTriggerEnter(Collider other)
    {
        // En Unity comparamos por Tag "Monster"
        if (other.CompareTag("Monster"))
        {
            PlayerMovement p = other.GetComponent<PlayerMovement>();
            if (p != null)
            {
                _activePlayer = p;
                _isAMonsterInside = true;
                
                // Bloquear movimiento y centrar (como tu script de Godot)
                _activePlayer.canMove = false;
                Vector3 targetPos = transform.position;
                targetPos.y = _activePlayer.transform.position.y; // Mantener su altura actual
                _activePlayer.transform.position = targetPos;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Monster"))
        {
            _isAMonsterInside = false;
            // Si sale del área antes de tiempo, recupera el movimiento
            if (_activePlayer != null)
            {
                _activePlayer.canMove = true;
                _activePlayer = null;
            }
        }
    }
}
