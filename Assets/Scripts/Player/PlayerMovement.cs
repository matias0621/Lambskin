using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Fusion;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 10f;
    public float acceleration = 20f;
    public float gravity = 9.81f;

    [Header("References (Unity Transforms)")]
    public GameObject humanModel;
    public GameObject monsterModel;
    public GameObject maskPrefab;
    public AudioSource audioSource;

    [Header("Gameplay State")]
    public float immuneTime = 5.0f;
    public bool canMove = true;

    // Propiedades sincronizadas en red
    [Networked] public NetworkBool IsHuman { get; set; }
    [Networked] public NetworkBool IsImmune { get; set; }
    [Networked] private TickTimer ImmuneTimer { get; set; }
    [Networked] private NetworkBool IsStunned { get; set; }
    [Networked] private NetworkBool IsMaskThrown { get; set; }
    [Networked] private NetworkBool IsAttacking { get; set; }
    [Networked] private Vector2 MovementInput { get; set; }
    [Networked] private Vector3 NetworkedVelocity { get; set; }

    // Variable local para compatibilidad con código existente
    public bool isHuman => IsHuman;

    [Header("Animations")]
    public AnimationsHuman humanAnims;
    public AnimationsMonster monsterAnims;

    [Header("Audio Clips")]
    public AudioClip stepsSFX;
    public AudioClip maskThrowSFX;
    public AudioClip electrocutionSFX;
    public AudioClip baaaaSFX;

    // Privados para lógica interna
    private CharacterController _controller;
    private Animator _humanAnimator;
    private Animator _monsterAnimator;
    private Vector2 _moveInput;
    private string _currentAnim;
    private bool _wasAttackPressed = false;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _humanAnimator = humanModel.GetComponent<Animator>();
        _monsterAnimator = monsterModel.GetComponent<Animator>();
        gameObject.tag = "Player";
    }

    public override void Spawned()
    {
        maskPrefab.SetActive(false);
        
        Debug.Log($"[PlayerMovement] Spawned. HasInputAuthority: {Object.HasInputAuthority}, HasStateAuthority: {Object.HasStateAuthority}");
        
        // Evitar que el CharacterController pelee con el NetworkTransform en los proxies
        if (_controller != null)
        {
            _controller.enabled = Object.HasStateAuthority || Object.HasInputAuthority;
        }

        // Solo el servidor inicializa el estado
        if (Object.HasStateAuthority)
        {
            // Por defecto todos son monstruos al inicio
            IsHuman = false;
            IsImmune = false;
            IsStunned = false;
            IsMaskThrown = false;
            IsAttacking = false;
        }
        
        // Todos los clientes actualizan su visualización
        UpdateVisuals();
    }

    public override void FixedUpdateNetwork()
    {
        // Obtener input del jugador si tiene autoridad de input
        if (GetInput(out NetworkInputData input))
        {
            _moveInput = input.movementInput;
            
            if (_moveInput.magnitude > 0.1f)
            {
                Debug.Log($"[PlayerMovement] Input recibido: {_moveInput}, HasStateAuthority: {Object.HasStateAuthority}");
            }
            
            // Detectar ataque (solo en flanco ascendente)
            if (input.attackPressed && !_wasAttackPressed && !IsMaskThrown && IsHuman && !IsAttacking)
            {
                // Iniciar lanzamiento de máscara
                if (Object.HasStateAuthority)
                {
                    RPC_ThrowMask();
                }
            }
            _wasAttackPressed = input.attackPressed;
        }
        else if (Object.HasInputAuthority)
        {
            Debug.LogWarning($"[PlayerMovement] NO se recibió input pero tengo InputAuthority");
        }

        // Solo el servidor gestiona el estado de red
        if (Object.HasStateAuthority)
        {
            // Escribir el input en la propiedad de red para que los clientes puedan leerlo
            MovementInput = _moveInput;
        }

        HandleImmunity();

        // El servidor y el cliente local (para predicción) procesan la física
        if (Object.HasStateAuthority || Object.HasInputAuthority)
        {
            if (canMove && !IsStunned)
            {
                ApplyMovement();
            }
            else
            {
                ApplyGravityOnly();
            }
        }

        // Todos los clientes actualizan sus animaciones usando el estado de red sincronizado
        UpdateAnimations();
    }

    // Estos métodos se mantienen para compatibilidad con modo local (sin red)
    public void OnMove(InputValue value)
    {
        // Solo procesar si NO estamos en red o si tenemos input authority
        if (Object == null || Object.HasInputAuthority)
        {
            _moveInput = value.Get<Vector2>();
        }
    }
    
    public void OnAttack(InputValue value)
    {
        // Solo procesar si NO estamos en red o si tenemos input authority
        if (Object == null || Object.HasInputAuthority)
        {
            if (value.isPressed && !IsMaskThrown && IsHuman && !IsAttacking)
            {
                if (Object == null)
                {
                    // Modo local sin red
                    StartCoroutine(ThrowMaskRoutine());
                }
                else if (Object.HasStateAuthority)
                {
                    // Modo red con autoridad
                    RPC_ThrowMask();
                }
            }
        }
    }

    private void ApplyMovement()
    {
        // Usar el deltaTime correcto (Runner para red, Time para local)
        float deltaTime = Runner != null ? Runner.DeltaTime : Time.deltaTime;
        
        // Convertir el input 2D a movimiento 3D
        Vector3 direction = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;

        if (!IsAttacking)
        {
            if (direction.magnitude > 0.1f)
            {
                // Aceleración suave (Lerp)
                float targetX = direction.x * speed;
                float targetZ = direction.z * speed;
                Vector3 currentVelocity = NetworkedVelocity;
                currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetX, acceleration * deltaTime);
                currentVelocity.z = Mathf.Lerp(currentVelocity.z, targetZ, acceleration * deltaTime);
                NetworkedVelocity = currentVelocity;

                // Rotación hacia donde mira
                Quaternion targetRotation = Quaternion.LookRotation(-direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * deltaTime); 
                
                // Sonido de pasos (solo para el jugador local)
                if (Object == null || Object.HasInputAuthority)
                {
                    if (_controller.isGrounded && !audioSource.isPlaying)
                    {
                        PlaySFX(stepsSFX, true);
                    }
                }
            }
            else
            {
                Vector3 currentVelocity = NetworkedVelocity;
                currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0, acceleration * deltaTime);
                currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, 0, acceleration * deltaTime);
                NetworkedVelocity = currentVelocity;
            }
        }

        // Gravedad constante
        Vector3 vel = NetworkedVelocity;
        if (!_controller.isGrounded)
        {
            vel.y -= gravity * deltaTime;
        }
        else
        {
            vel.y = -0.5f; // Mantener pegado al suelo
        }
        NetworkedVelocity = vel;

        _controller.Move(NetworkedVelocity * deltaTime);
    }

    /// <summary>
    /// Actualiza las animaciones en todos los clientes usando el estado sincronizado en red.
    /// Se llama fuera del bloque HasStateAuthority para que todos los peers la ejecuten.
    /// </summary>
    private void UpdateAnimations()
    {
        if (IsAttacking) return;

        // Leer el input de movimiento desde la propiedad de red (escrita por el servidor)
        Vector2 netInput = Object != null ? MovementInput : _moveInput;
        Vector3 direction = new Vector3(netInput.x, 0, netInput.y).normalized;

        if (direction.magnitude > 0.1f)
        {
            PlayAnimation(IsHuman ? humanAnims.walk : monsterAnims.run);
        }
        else
        {
            PlayAnimation(IsHuman ? humanAnims.idle : monsterAnims.idle);
        }
    }

    private void HandleImmunity()
    {
        if (Object.HasStateAuthority)
        {
            if (IsImmune && ImmuneTimer.Expired(Runner))
            {
                IsImmune = false;
            }
        }
        
        // Actualizar visuales para todos los clientes
        UpdateShineEffect(IsImmune);
    }

    // --- MECÁNICA CORE: LANZAR MÁSCARA ---

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ThrowMask()
    {
        StartCoroutine(ThrowMaskRoutine());
    }

    private IEnumerator ThrowMaskRoutine()
    {
        IsAttacking = true;
        IsMaskThrown = true;

        // Ocultar partes del humano (animación)
        // humanModel.GetComponent<MonsterAnimation>().HideParts(); 
        maskPrefab.SetActive(true);
        
        // Sonido solo para el jugador local
        if (Object == null || Object.HasInputAuthority)
        {
            PlaySFX(maskThrowSFX);
        }

        // Desacoplar máscara al mundo
        maskPrefab.transform.SetParent(null);
        Vector3 targetPos = transform.position + transform.forward * 10f;

        // Trayectoria de ida
        while (Vector3.Distance(maskPrefab.transform.position, targetPos) > 0.2f)
        {
            maskPrefab.transform.position = Vector3.MoveTowards(maskPrefab.transform.position, targetPos, 20f * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Trayectoria de vuelta (sigue al jugador)
        while (Vector3.Distance(maskPrefab.transform.position, transform.position) > 0.6f)
        {
            maskPrefab.transform.position = Vector3.MoveTowards(maskPrefab.transform.position, transform.position, 25f * Time.deltaTime);
            yield return null;
        }

        // Re-acoplar al jugador
        maskPrefab.transform.SetParent(this.transform);
        maskPrefab.transform.localPosition = new Vector3(0, 0.5f, -1.5f); // Ajustar según tu modelo
        maskPrefab.transform.localRotation = Quaternion.identity;

        IsMaskThrown = false;
        IsAttacking = false;
        maskPrefab.SetActive(false);
    }

    // --- CAMBIOS DE ESTADO ---

    public void SetAsHuman()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            IsHuman = true;
            RPC_UpdateVisuals();
        }
        else if (Object == null)
        {
            // Modo local sin red
            IsHuman = true;
            UpdateVisuals();
        }
    }

    public void SetAsMonster()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            IsHuman = false;
            RPC_UpdateVisuals();
        }
        else if (Object == null)
        {
            // Modo local sin red
            IsHuman = false;
            UpdateVisuals();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateVisuals()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        int layer;
        
        if (IsHuman)
        {
            layer = LayerMask.NameToLayer("Human");
            if (layer >= 0) gameObject.layer = layer;
            else Debug.LogWarning("Layer 'Human' not found. Add it in Edit > Project Settings > Tags and Layers.");
            
            humanModel.SetActive(true);
            monsterModel.SetActive(false);
            
            // Sonido solo para el jugador local
            if (Object == null || Object.HasInputAuthority)
            {
                PlaySFX(baaaaSFX);
            }
        }
        else
        {
            layer = LayerMask.NameToLayer("Monster");
            if (layer >= 0) gameObject.layer = layer;
            else Debug.LogWarning("Layer 'Monster' not found. Add it in Edit > Project Settings > Tags and Layers.");
            
            humanModel.SetActive(false);
            monsterModel.SetActive(true);
            maskPrefab.SetActive(false);
        }
    }

    public void StartImmunity()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            IsImmune = true;
            ImmuneTimer = TickTimer.CreateFromSeconds(Runner, immuneTime);
        }
        else if (Object == null)
        {
            IsImmune = true;
        }
    }

    // --- UTILS ---

    private void PlayAnimation(string clipName)
    {
        if (clipName == _currentAnim) return;
        _currentAnim = clipName;

        Animator activeAnim = IsHuman ? _humanAnimator : _monsterAnimator;
        if (activeAnim != null) activeAnim.CrossFadeInFixedTime(clipName, 0.15f);
    }

    private void PlaySFX(AudioClip clip, bool randomPitch = false)
    {
        if (clip == null) return;
        audioSource.pitch = randomPitch ? Random.Range(0.8f, 1.2f) : 1.0f;
        audioSource.PlayOneShot(clip);
    }

    private void UpdateShineEffect(bool enable)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            // Asegúrate de que tu Shader en Unity tenga la propiedad "_ShineIntensity"
            r.material.SetFloat("_ShineIntensity", enable ? 1.0f : 0.0f);
        }
    }

    private void ApplyGravityOnly()
    {
        float deltaTime = Runner != null ? Runner.DeltaTime : Time.deltaTime;
        Vector3 vel = NetworkedVelocity;
        if (!_controller.isGrounded) vel.y -= gravity * deltaTime;
        NetworkedVelocity = vel;
        _controller.Move(new Vector3(0, NetworkedVelocity.y, 0) * deltaTime);
    }
}
