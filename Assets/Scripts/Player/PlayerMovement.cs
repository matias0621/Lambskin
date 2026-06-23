using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Mask")]
    [SerializeField] private float maskDistance = 10f;
    [SerializeField] private float maskOutSpeed = 20f;
    [SerializeField] private float maskReturnSpeed = 25f;
    [SerializeField] private float maskHitRadius = 0.9f;

    [Networked] public PlayerRef OwnerPlayer { get; set; }
    [Networked] public NetworkBool IsHuman { get; set; }
    [Networked] public NetworkBool IsImmune { get; set; }
    [Networked] public NetworkBool IsEliminated { get; set; }
    [Networked] public float HumanTimeRemaining { get; set; }
    [Networked] public int MatchState { get; set; }
    [Networked] public PlayerRef WinnerPlayer { get; set; }
    [Networked] private TickTimer ImmuneTimer { get; set; }
    [Networked] private NetworkBool IsStunned { get; set; }
    [Networked] private NetworkBool IsMaskThrown { get; set; }
    [Networked] private NetworkBool IsAttacking { get; set; }
    [Networked] private Vector2 MovementInput { get; set; }
    [Networked] private Vector3 NetworkedVelocity { get; set; }

    public bool isHuman => IsHuman;
    public bool HasOwner => OwnerPlayer != PlayerRef.None;

    [Header("Animations")]
    public AnimationsHuman humanAnims;
    public AnimationsMonster monsterAnims;

    [Header("Audio Clips")]
    public AudioClip stepsSFX;
    public AudioClip maskThrowSFX;
    public AudioClip electrocutionSFX;
    public AudioClip baaaaSFX;

    private CharacterController _controller;
    private Animator _humanAnimator;
    private Animator _monsterAnimator;
    private Vector2 _moveInput;
    private string _currentAnim;
    private bool _wasAttackPressed;

    private bool HasStateAuthoritySafe => Runner == null || Object == null || Object.HasStateAuthority;
    private bool HasInputAuthoritySafe => Runner == null || Object == null || Object.HasInputAuthority;
    private bool CanAct => MatchState == (int)NetworkMatchPhase.Playing && !IsEliminated && canMove && !IsStunned;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (humanModel != null)
        {
            _humanAnimator = humanModel.GetComponent<Animator>();
        }

        if (monsterModel != null)
        {
            _monsterAnimator = monsterModel.GetComponent<Animator>();
        }

        TrySetObjectTag("Player");
    }

    public override void Spawned()
    {
        if (maskPrefab != null)
        {
            maskPrefab.SetActive(false);
        }

        if (_controller != null)
        {
            _controller.enabled = Object.HasStateAuthority;
        }

        if (Object.HasStateAuthority)
        {
            IsHuman = false;
            IsImmune = false;
            IsEliminated = false;
            IsStunned = false;
            IsMaskThrown = false;
            IsAttacking = false;
            HumanTimeRemaining = 0f;
            MatchState = (int)NetworkMatchPhase.Waiting;
            WinnerPlayer = PlayerRef.None;
            NetworkedVelocity = Vector3.zero;
        }

        UpdateVisuals();
    }

    public override void FixedUpdateNetwork()
    {
        ReadNetworkInput();
        HandleImmunity();

        if (!HasStateAuthoritySafe)
        {
            return;
        }

        if (IsEliminated)
        {
            NetworkedVelocity = Vector3.zero;
            MovementInput = Vector2.zero;
            return;
        }

        if (CanAct)
        {
            ApplyMovement();
        }
        else
        {
            ApplyGravityOnly();
        }
    }

    public override void Render()
    {
        UpdateVisuals();
        UpdateAnimations();
    }

    public void OnMove(InputValue value)
    {
        if (HasInputAuthoritySafe)
        {
            _moveInput = value.Get<Vector2>();
        }
    }

    public void OnAttack(InputValue value)
    {
        if (!HasInputAuthoritySafe || !value.isPressed)
        {
            return;
        }

        if (Runner == null && IsHuman && !IsMaskThrown && !IsAttacking)
        {
            StartCoroutine(ThrowMaskRoutine());
        }
    }

    public void InitializeForPlayer(PlayerRef ownerPlayer)
    {
        if (!HasStateAuthoritySafe)
        {
            return;
        }

        OwnerPlayer = ownerPlayer;
        MatchState = (int)NetworkMatchPhase.Waiting;
        WinnerPlayer = PlayerRef.None;
        IsEliminated = false;
        SetAsMonster();
        SetHumanTimer(0f);
    }

    public void PrepareForMatch(NetworkMatchPhase phase)
    {
        if (!HasStateAuthoritySafe)
        {
            return;
        }

        MatchState = (int)phase;
        WinnerPlayer = PlayerRef.None;
        IsEliminated = false;
        IsImmune = false;
        IsStunned = false;
        IsMaskThrown = false;
        IsAttacking = false;
        canMove = true;
        NetworkedVelocity = Vector3.zero;
    }

    public void SetMatchResult(PlayerRef winner)
    {
        if (!HasStateAuthoritySafe)
        {
            return;
        }

        MatchState = (int)NetworkMatchPhase.Finished;
        WinnerPlayer = winner;
        IsAttacking = false;
        IsMaskThrown = false;
        canMove = false;
        NetworkedVelocity = Vector3.zero;
    }

    public void SetEliminated()
    {
        if (!HasStateAuthoritySafe)
        {
            return;
        }

        IsEliminated = true;
        IsHuman = false;
        IsAttacking = false;
        IsMaskThrown = false;
        HumanTimeRemaining = 0f;
        canMove = false;
        NetworkedVelocity = Vector3.zero;
        RPC_UpdateVisuals();
    }

    public void SetHumanTimer(float seconds)
    {
        if (HasStateAuthoritySafe)
        {
            HumanTimeRemaining = Mathf.Max(0f, seconds);
        }
    }

    public void SetAsHuman()
    {
        if (HasStateAuthoritySafe)
        {
            IsHuman = true;
            IsEliminated = false;
            RPC_UpdateVisuals();
        }
    }

    public void SetAsMonster()
    {
        if (HasStateAuthoritySafe)
        {
            IsHuman = false;
            RPC_UpdateVisuals();
        }
    }

    public bool CanBeConvertedByMask(PlayerMovement owner)
    {
        return owner != null && owner != this && !IsEliminated && !IsHuman && !IsImmune;
    }

    public string GetDisplayName()
    {
        if (!HasOwner)
        {
            return "Jugador";
        }

        return $"Jugador {OwnerPlayer.AsIndex + 1}";
    }

    public void StartImmunity()
    {
        if (HasStateAuthoritySafe && Runner != null)
        {
            IsImmune = true;
            ImmuneTimer = TickTimer.CreateFromSeconds(Runner, immuneTime);
            return;
        }

        if (Runner == null)
        {
            IsImmune = true;
        }
    }

    private void ReadNetworkInput()
    {
        if (!GetInput(out NetworkInputData input))
        {
            if (HasStateAuthoritySafe)
            {
                MovementInput = Vector2.zero;
            }

            return;
        }

        _moveInput = input.movementInput;

        if (HasStateAuthoritySafe)
        {
            MovementInput = CanAct ? _moveInput : Vector2.zero;

            if (input.attackPressed && !_wasAttackPressed && CanAct && IsHuman && !IsMaskThrown && !IsAttacking)
            {
                RPC_ThrowMask();
            }
        }

        _wasAttackPressed = input.attackPressed;
    }

    private void ApplyMovement()
    {
        float deltaTime = Runner != null ? Runner.DeltaTime : Time.deltaTime;
        Vector3 velocity = NetworkedVelocity;
        Vector3 direction = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;

        if (!IsAttacking)
        {
            if (direction.magnitude > 0.1f)
            {
                float targetX = direction.x * speed;
                float targetZ = direction.z * speed;
                velocity.x = Mathf.Lerp(velocity.x, targetX, acceleration * deltaTime);
                velocity.z = Mathf.Lerp(velocity.z, targetZ, acceleration * deltaTime);

                Quaternion targetRotation = Quaternion.LookRotation(-direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * deltaTime);

                if (HasInputAuthoritySafe && audioSource != null && _controller != null && _controller.isGrounded && !audioSource.isPlaying)
                {
                    PlaySFX(stepsSFX, true);
                }
            }
            else
            {
                velocity.x = Mathf.MoveTowards(velocity.x, 0, acceleration * deltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0, acceleration * deltaTime);
            }
        }

        if (_controller != null && !_controller.isGrounded)
        {
            velocity.y -= gravity * deltaTime;
        }
        else
        {
            velocity.y = -0.5f;
        }

        NetworkedVelocity = velocity;

        if (_controller != null)
        {
            _controller.Move(velocity * deltaTime);
        }
    }

    private void ApplyGravityOnly()
    {
        if (_controller == null)
        {
            return;
        }

        float deltaTime = Runner != null ? Runner.DeltaTime : Time.deltaTime;
        Vector3 velocity = NetworkedVelocity;

        velocity.x = 0f;
        velocity.z = 0f;

        if (!_controller.isGrounded)
        {
            velocity.y -= gravity * deltaTime;
        }
        else
        {
            velocity.y = -0.5f;
        }

        NetworkedVelocity = velocity;
        _controller.Move(new Vector3(0f, velocity.y, 0f) * deltaTime);
    }

    private void HandleImmunity()
    {
        if (HasStateAuthoritySafe && Runner != null && IsImmune && ImmuneTimer.Expired(Runner))
        {
            IsImmune = false;
        }

        UpdateShineEffect(IsImmune);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ThrowMask()
    {
        StartCoroutine(ThrowMaskRoutine());
    }

    private IEnumerator ThrowMaskRoutine()
    {
        if (maskPrefab == null)
        {
            yield break;
        }

        if (HasStateAuthoritySafe)
        {
            IsAttacking = true;
            IsMaskThrown = true;
        }

        maskPrefab.SetActive(true);

        if (HasInputAuthoritySafe)
        {
            PlaySFX(maskThrowSFX);
        }

        Transform maskTransform = maskPrefab.transform;
        Transform originalParent = transform;
        maskTransform.SetParent(null);

        Vector3 targetPosition = transform.position + transform.forward * maskDistance;
        bool hitResolved = false;

        while (Vector3.Distance(maskTransform.position, targetPosition) > 0.2f)
        {
            maskTransform.position = Vector3.MoveTowards(maskTransform.position, targetPosition, maskOutSpeed * Time.deltaTime);

            if (HasStateAuthoritySafe && !hitResolved)
            {
                hitResolved = TryResolveMaskHit(maskTransform.position);
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.25f);

        while (Vector3.Distance(maskTransform.position, transform.position) > 0.6f)
        {
            maskTransform.position = Vector3.MoveTowards(maskTransform.position, transform.position, maskReturnSpeed * Time.deltaTime);
            yield return null;
        }

        maskTransform.SetParent(originalParent);
        maskTransform.localPosition = new Vector3(0f, 0.5f, -1.5f);
        maskTransform.localRotation = Quaternion.identity;
        maskPrefab.SetActive(false);

        if (HasStateAuthoritySafe)
        {
            IsMaskThrown = false;
            IsAttacking = false;
        }
    }

    private bool TryResolveMaskHit(Vector3 maskPosition)
    {
        Collider[] hits = Physics.OverlapSphere(maskPosition, maskHitRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            PlayerMovement target = hit.GetComponentInParent<PlayerMovement>();
            if (target == null || target == this)
            {
                continue;
            }

            if (NetworkMatchController.Instance != null && NetworkMatchController.Instance.TryResolveMaskHit(this, target))
            {
                return true;
            }
        }

        return false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateVisuals()
    {
        UpdateVisuals();
    }

    private void UpdateAnimations()
    {
        if (IsEliminated || IsAttacking)
        {
            return;
        }

        Vector2 netInput = Runner != null ? MovementInput : _moveInput;
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

    private void UpdateVisuals()
    {
        if (IsEliminated)
        {
            if (humanModel != null) humanModel.SetActive(false);
            if (monsterModel != null) monsterModel.SetActive(false);
            if (maskPrefab != null) maskPrefab.SetActive(false);
            TrySetObjectTag("Player");
            return;
        }

        int layer = LayerMask.NameToLayer(IsHuman ? "Human" : "Monster");
        if (layer >= 0)
        {
            gameObject.layer = layer;
        }

        if (humanModel != null)
        {
            humanModel.SetActive(IsHuman);
        }

        if (monsterModel != null)
        {
            monsterModel.SetActive(!IsHuman);
        }

        if (!IsHuman && maskPrefab != null && !IsMaskThrown)
        {
            maskPrefab.SetActive(false);
        }

        TrySetObjectTag(IsHuman ? "Human" : "Monster");
    }

    private void PlayAnimation(string clipName)
    {
        if (string.IsNullOrEmpty(clipName) || clipName == _currentAnim)
        {
            return;
        }

        _currentAnim = clipName;
        Animator activeAnimator = IsHuman ? _humanAnimator : _monsterAnimator;

        if (activeAnimator != null)
        {
            activeAnimator.CrossFadeInFixedTime(clipName, 0.15f);
        }
    }

    private void PlaySFX(AudioClip clip, bool randomPitch = false)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.pitch = randomPitch ? Random.Range(0.8f, 1.2f) : 1.0f;
        audioSource.PlayOneShot(clip);
    }

    private void UpdateShineEffect(bool enable)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rendererComponent in renderers)
        {
            if (rendererComponent.material.HasProperty("_ShineIntensity"))
            {
                rendererComponent.material.SetFloat("_ShineIntensity", enable ? 1.0f : 0.0f);
            }
        }
    }

    private void TrySetObjectTag(string tagName)
    {
        try
        {
            gameObject.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[PlayerMovement] Falta configurar el tag {tagName} en Project Settings.");
        }
    }
}
