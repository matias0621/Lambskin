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

    private void OnTriggerEnter(Collider other)
    {
        // La resolucion de impacto vive en PlayerMovement para que solo el host
        // decida cambios de rol y todos los clientes vean el mismo resultado.
    }
}
