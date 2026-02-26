using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerAnimations : MonoBehaviour
{
    [Header("Unity Components")]
    [SerializeField] private Animator animator;
    
    [Header("Visual Settings")]
    // En Unity, para ocultar/mostrar partes específicas usamos MeshRenderer o el GameObject
    [SerializeField] private List<GameObject> partMask; 
    
    // Evento equivalente al signal animation_finished
    public UnityEvent<string> onAnimationFinished;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void PlayAnimation(string animationName)
    {
        if (animator == null) return;

        animator.Play(animationName);
    }

    // --- MÉTODOS DE VISIBILIDAD ---

    public void HideParts()
    {
        foreach (GameObject part in partMask)
        {
            if (part != null) part.SetActive(false);
        }
    }

    public void ShowParts()
    {
        foreach (GameObject part in partMask)
        {
            if (part != null) part.SetActive(true);
        }
    }

    // --- MANEJO DE SEÑALES (ANIMATION EVENTS) ---

    /// <summary>
    /// Unity no tiene un signal automático de "terminó animación" como Godot.
    /// Debes crear un "Animation Event" al final del clip en el editor de Unity
    /// que llame a esta función.
    /// </summary>
    public void InvokeAnimationFinished(string animName)
    {
        onAnimationFinished?.Invoke(animName);
    }
}
