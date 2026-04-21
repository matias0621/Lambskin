using Fusion;
using UnityEngine;

/// <summary>
/// Script temporal para asegurar que el NetworkInputProvider esté en el NetworkRunner
/// Agrégalo a un GameObject en la escena
/// </summary>
public class EnsureNetworkInputProvider : MonoBehaviour
{
    private void Update()
    {
        // Buscar el NetworkRunner en cada frame hasta que exista
        var runner = FindObjectOfType<NetworkRunner>();
        
        if (runner != null)
        {
            // Verificar si ya tiene el NetworkInputProvider
            var provider = runner.GetComponent<NetworkInputProvider>();
            
            if (provider == null)
            {
                Debug.Log("[EnsureNetworkInputProvider] ✅ Agregando NetworkInputProvider al NetworkRunner");
                runner.gameObject.AddComponent<NetworkInputProvider>();
                
                // Destruir este script después de hacer su trabajo
                Destroy(this);
            }
            else
            {
                Debug.Log("[EnsureNetworkInputProvider] NetworkInputProvider ya existe en el Runner");
                Destroy(this);
            }
        }
    }
}
