using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputSystem_Actions))]
public class PlayerMovement : MonoBehaviour
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
    public bool isHuman = false;
    public bool canMove = true;

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
    private Vector3 _velocity;
    private Vector2 _moveInput;
    private bool _isAttacking = false;
    public bool _isImmune = false;
    private float _immuneTimer = 0.0f;
    private bool _isStunned = false;
    private bool _isMaskThrown = false;
    private string _currentAnim;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _humanAnimator = humanModel.GetComponent<Animator>();
        _monsterAnimator = monsterModel.GetComponent<Animator>();
        gameObject.tag = "Player";
    }

    private void Start()
    {
        maskPrefab.SetActive(false);
        // Inicializar estado según grupo inicial (por defecto monstruo)
        if (isHuman) SetAsHuman(); else SetAsMonster();
    }

    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !_isMaskThrown && isHuman && !_isAttacking)
        {
            StartCoroutine(ThrowMaskRoutine());
        }
    }

    private void Update()
    {
        HandleImmunity();

        if (canMove && !_isStunned)
        {
            ApplyMovement();
        }
        else
        {
            ApplyGravityOnly();
        }
    }

    private void ApplyMovement()
    {
        // Convertir el input 2D a movimiento 3D
        Vector3 direction = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;

        if (!_isAttacking)
        {
            if (direction.magnitude > 0.1f)
            {
                // Aceleración suave (Lerp)
                float targetX = direction.x * speed;
                float targetZ = direction.z * speed;
                _velocity.x = Mathf.Lerp(_velocity.x, targetX, acceleration * Time.deltaTime);
                _velocity.z = Mathf.Lerp(_velocity.z, targetZ, acceleration * Time.deltaTime);

                // Rotación hacia donde mira
                Quaternion targetRotation = Quaternion.LookRotation(-direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime); 

                PlayAnimation(isHuman ? humanAnims.walk : monsterAnims.run);
                
                // Sonido de pasos
                if (_controller.isGrounded && !audioSource.isPlaying)
                {
                    PlaySFX(stepsSFX, true);
                }
            }
            else
            {
                _velocity.x = Mathf.MoveTowards(_velocity.x, 0, acceleration * Time.deltaTime);
                _velocity.z = Mathf.MoveTowards(_velocity.z, 0, acceleration * Time.deltaTime);
                PlayAnimation(isHuman ? humanAnims.idle : monsterAnims.idle);
            }
        }

        // Gravedad constante
        if (!_controller.isGrounded)
        {
            _velocity.y -= gravity * Time.deltaTime;
        }
        else
        {
            _velocity.y = -0.5f; // Mantener pegado al suelo
        }

        _controller.Move(_velocity * Time.deltaTime);
    }

    private void HandleImmunity()
    {
        if (_isImmune)
        {
            _immuneTimer += Time.deltaTime;
            UpdateShineEffect(true);
            if (_immuneTimer >= immuneTime)
            {
                _isImmune = false;
                _immuneTimer = 0.0f;
                UpdateShineEffect(false);
            }
        }
    }

    // --- MECÁNICA CORE: LANZAR MÁSCARA ---

    private IEnumerator ThrowMaskRoutine()
    {
        _isAttacking = true;
        _isMaskThrown = true;

        // Ocultar partes del humano (animación)
        // humanModel.GetComponent<MonsterAnimation>().HideParts(); 
        maskPrefab.SetActive(true);
        PlaySFX(maskThrowSFX);

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

        _isMaskThrown = false;
        _isAttacking = false;
        maskPrefab.SetActive(false);
    }

    // --- CAMBIOS DE ESTADO ---

    public void SetAsHuman()
    {
        isHuman = true;
        gameObject.layer = LayerMask.NameToLayer("Human"); // Opcional para colisiones
        humanModel.SetActive(true);
        monsterModel.SetActive(false);
        PlaySFX(baaaaSFX);
    }

    public void SetAsMonster()
    {
        isHuman = false;
        gameObject.layer = LayerMask.NameToLayer("Monster");
        humanModel.SetActive(false);
        monsterModel.SetActive(true);
        maskPrefab.SetActive(false);
    }

    // --- UTILS ---

    private void PlayAnimation(string clipName)
    {
        if (clipName == _currentAnim) return;
        _currentAnim = clipName;

        Animator activeAnim = isHuman ? _humanAnimator : _monsterAnimator;
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
        if (!_controller.isGrounded) _velocity.y -= gravity * Time.deltaTime;
        _controller.Move(new Vector3(0, _velocity.y, 0) * Time.deltaTime);
    }
}
