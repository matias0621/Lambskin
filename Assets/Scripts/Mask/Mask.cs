using System.Collections;
using UnityEngine;

public class Mask : MonoBehaviour
{
    public float speed = 20.0f; // En Unity las unidades son metros, 300 es demasiado
    public PlayerMovement ownerPlayer; // Referencia al jugador que la lanzó

    private bool _onThrow = false;
    private Vector3 _throwDirection;
    private Rigidbody _rb;

    void Awake()
    {
        // Usamos Rigidbody para el movimiento físico del proyectil
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
        
        _rb.useGravity = false; // La máscara vuela recto
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void Throw(Vector3 direction)
    {
        _throwDirection = direction.normalized;
        _onThrow = true;
        
        // Destruir o desactivar tras 2 segundos si no golpea nada
        StartCoroutine(AutoDestroyRoutine(2.0f));
    }

    void FixedUpdate()
    {
        if (_onThrow)
        {
            _rb.linearVelocity = _throwDirection * speed;
        }
    }

    private IEnumerator AutoDestroyRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        // En lugar de destruir, podrías devolverla al jugador
        // Destroy(gameObject); 
    }

    // --- DETECCIÓN DE COLISIÓN (Equivalente a _on_area_3d_body_entered) ---

    private void OnTriggerEnter(Collider other)
    {
        if (ownerPlayer == null)
        {
            Debug.LogWarning("ADVERTENCIA: La máscara no tiene referencia al jugador");
            return;
        }

        // Comprobar si el objeto tocado es un monstruo
        if (!other.CompareTag("Monster")) return;

        // Obtener el script del jugador tocado
        PlayerMovement targetPlayer = other.GetComponent<PlayerMovement>();
        if (targetPlayer == null) return;

        // Verificar inmunidad (usando la variable pública del script Player)
        if (targetPlayer._isImmune) 
        {
            Debug.Log("Monstruo es inmune");
            return;
        }

        // Probabilidad de éxito (40% éxito, 60% falla según tu randf() > 0.6)
        if (Random.value > 0.4f) 
        {
            Debug.Log("¡Transformación exitosa!");
            
            // Realizar el intercambio de roles
            targetPlayer.SetAsHuman();
            ownerPlayer.SetAsMonster();
            
            // Opcional: Desactivar la máscara tras el impacto
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Transformación fallida");
        }
    }
}
