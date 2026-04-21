using UnityEngine;
using Fusion;

/// <summary>
/// Estructura de datos para sincronizar el input de los jugadores en red
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput;
    public NetworkBool attackPressed;
}