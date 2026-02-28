---
name: lambskin
description: Agente especializado en el desarrollo del juego Lambskin, un party game multijugador local de terror/humor desarrollado en Unity 6. Úsalo para implementar mecánicas de juego, corregir bugs, crear nuevos scripts, optimizar rendimiento y cualquier tarea relacionada con el proyecto.
argument-hint: Una tarea a implementar, un bug a corregir, o una pregunta sobre el proyecto Lambskin.
---

# Agente de Desarrollo — Lambskin

## Descripción del Proyecto

**Lambskin** es un party game multijugador local (couch co-op) de temática terror/humor desarrollado en **Unity 6 (6000.0.63f1)** con **Universal Render Pipeline (URP)**. El juego soporta múltiples jugadores con controles por joystick y teclado usando el **New Input System**.

### Mecánica Principal (Core Loop)

1. Los jugadores se unen en un **Lobby** local (mínimo 2 jugadores).
2. Al iniciar la partida, un jugador es elegido aleatoriamente como **Humano** (lleva una máscara); el resto son **Monstruos**.
3. El **Humano** puede lanzar su máscara como proyectil hacia los monstruos. Si acierta, hay un **40% de probabilidad** de que el monstruo se convierta en humano (intercambio de roles).
4. Los **Monstruos** pueden entrar en **Portales** para completar una secuencia de transformación visual (progresión de texturas con tinte rojo). Al completarla, obtienen **inmunidad temporal**.
5. Existe un **temporizador visual** (`TimerMask`) que cuenta hacia la "muerte" del humano. Cuando se acaba el tiempo, el humano muere y se selecciona un nuevo humano aleatorio entre los monstruos restantes.
6. Hay un sistema de **palancas** (`Palanca`): cuando se activan 4 palancas, todos los monstruos quedan **stuneados** temporalmente.
7. La partida termina cuando solo quedan 1 humano y 1 monstruo, volviendo al menú principal.

## Arquitectura del Proyecto

### Estructura de Carpetas

```
Assets/
├── Scripts/
│   ├── Player/
│   │   ├── PlayerMovement.cs    — Movimiento, input, cambio de rol, lanzamiento de máscara
│   │   └── PlayerAnimations.cs  — Animaciones, visibilidad de partes del modelo
│   ├── Lobby/
│   │   └── LobbyManager.cs     — Sistema de lobby multijugador local (join/ready)
│   ├── Mask/
│   │   └── Mask.cs              — Proyectil de la máscara (colisión, intercambio de roles)
│   ├── Portal/
│   │   └── Portal.cs            — Portales de transformación de monstruos
│   ├── Singeltons/
│   │   └── GameManager.cs       — Singleton global (muerte humano, palancas, selección aleatoria)
│   └── Timer/
│       └── TimerMask.cs         — UI del temporizador visual con sprites de máscara
├── Models/                      — Modelos 3D (.glb, .fbx, .png): human_test, mask_test, test_monster, pared, puerta, pilar, piso
├── Music/                       — (vacío, pendiente)
├── SFX/                         — (vacío, pendiente)
├── Scenes/
│   └── SampleScene.unity        — Escena principal
├── Settings/                    — Configuración URP (Mobile_RPAsset, PC_RPAsset, Renderers, VolumeProfiles)
└── InputSystem_Actions.inputactions — Acciones: Move (Vector2, joystick/teclado), Attack (botón)
```

### Patrones Arquitectónicos

- **Singleton**: `GameManager` usa el patrón Singleton con `DontDestroyOnLoad`.
- **Component-Based**: Cada mecánica es un `MonoBehaviour` independiente.
- **Input System**: Se usa el New Input System con `PlayerInput` para multijugador local. Las acciones se reciben vía `OnMove()`, `OnShootMask()`, etc.
- **Coroutines**: Lanzamiento de máscara y stun usan `IEnumerator` coroutines.
- **Tags y Layers**: El juego necesita tags `Player`, `Monster`, `Human`, `Palanca` y layers `Human`, `Monster` (actualmente NO configurados en TagManager — pendiente).

### Dependencias Clave (Packages)

- `com.unity.inputsystem` 1.16.0 — New Input System
- `com.unity.render-pipelines.universal` 17.0.4 — URP
- `com.unity.ai.navigation` 2.0.9 — NavMesh (posible IA de monstruos futura)
- `org.khronos.unitygltf` — Importación de modelos GLTF
- `com.unity.ugui` 2.0.0 — UI (Canvas, Image, TextMeshPro)
- `com.unity.timeline` 1.8.9 — Cinemáticas
- `com.unity.visualscripting` 1.9.7

## Estado Actual del Desarrollo y Tareas Pendientes Conocidas

### Implementado (funcional)
- Sistema de movimiento del jugador con aceleración suave y gravedad
- Cambio de rol Humano ↔ Monstruo (`SetAsHuman`, `SetAsMonster`)
- Lanzamiento de máscara como proyectil con ida/vuelta (coroutine en `PlayerMovement`)
- Colisión de máscara con probabilidad de transformación (40%)
- Portales con secuencia visual progresiva (texturas + tinte rojo)
- Inmunidad temporal post-portal
- Timer visual con sprites de máscara
- Lobby multijugador local con sistema Ready
- GameManager singleton con lógica de muerte y selección aleatoria

### Pendiente / Incompleto
- **Tags y Layers no configurados**: `TagManager.asset` no tiene tags personalizados ni layers `Human`/`Monster`. Es necesario configurarlos.
- **Script de Palanca**: Referenciado en `GameManager.StunAllMonstersRoutine()` pero no existe. Necesita implementación.
- **Mecánica de Stun**: `_isStunned` existe en `PlayerMovement` pero `StartStun()` no está implementado.
- **Escena MainMenu**: Referenciada en `GameManager.DeathHuman()` pero no existe.
- **Escena StageTest**: Referenciada en `LobbyManager` pero no existe (solo `SampleScene`).
- **Audio**: Las carpetas Music/ y SFX/ están vacías. Los AudioClips están referenciados pero los assets no existen.
- **Binding de Input "right"**: En `InputSystem_Actions.inputactions`, el binding de "right" apunta a `stick/down` en lugar de `stick/right` (bug).
- **Acción "Ready"**: Referenciada en `LobbyManager` pero no definida en el Input Actions asset.
- **Acción "ShootMask"**: Usada en `PlayerMovement.OnShootMask()` pero el inputactions solo define "Attack".
- **Modelo de máscara**: `maskPrefab` usa lógica dual — tanto `PlayerMovement` (coroutine de ida/vuelta) como `Mask.cs` (Rigidbody + física). Podría haber conflicto. Necesita consolidar.

## Reglas y Convenciones para el Agente

### Idioma
- Los comentarios en código, nombres de variables descriptivas y mensajes de Debug.Log deben estar en **español**.
- Los nombres de clases, métodos y variables públicas deben seguir las convenciones de C# en **inglés** (PascalCase para públicos, _camelCase para privados).

### Estilo de Código
- Seguir las convenciones estándar de Unity/C#:
  - `PascalCase` para clases, métodos públicos y propiedades.
  - `_camelCase` con guion bajo para campos privados.
  - `camelCase` sin guion bajo para parámetros y variables locales.
  - `[Header("...")]` y `[SerializeField]` para campos expuestos en el Inspector.
  - `[RequireComponent]` cuando un script depende de otro componente.
- Usar `CompareTag()` en lugar de `== "tag"`.
- Preferir `TryGetComponent<T>()` sobre `GetComponent<T>()` cuando el componente puede no existir.
- Evitar `Find` y `FindGameObjectsWithTag` en `Update()` — cachear referencias.
- Usar `nameof()` para referencias a nombres de animaciones o parámetros cuando sea posible.

### Arquitectura
- Nuevos singletons deben seguir el patrón existente en `GameManager`.
- Nuevas mecánicas deben ser componentes independientes (`MonoBehaviour`) en su propia carpeta dentro de `Assets/Scripts/`.
- Para comunicación entre scripts, preferir: eventos (`UnityEvent`, `System.Action`) > referencia directa > Singleton.
- El input siempre debe pasar por el New Input System, nunca por `Input.GetKey`.

### Unity y URP
- El proyecto usa **Unity 6** (6000.0.63f1) con **URP 17**.
- Hay dos perfiles de renderizado: `Mobile_RPAsset` y `PC_RPAsset`. Tener en cuenta ambos al agregar efectos visuales.
- Los modelos 3D usan formato **GLTF** (via UnityGLTF) y **FBX**.
- La UI usa **TextMeshPro** (TMPro) y **uGUI** (Canvas, Image).

### Organización de Archivos
- Scripts nuevos van en `Assets/Scripts/{NombreDelSistema}/{NombreDelScript}.cs`.
- Modelos en `Assets/Models/`.
- Audio (cuando se agregue) en `Assets/Music/` y `Assets/SFX/`.
- Escenas en `Assets/Scenes/`.
- Configuración URP en `Assets/Settings/`.

### Antes de Implementar Cambios
1. Verificar que los tags y layers necesarios existan revisando `ProjectSettings/TagManager.asset`.
2. Verificar que las acciones de Input necesarias existan en `InputSystem_Actions.inputactions`.
3. Revisar si la mecánica interactúa con el sistema de roles (Humano/Monstruo) y respetar el flujo existente.
4. Considerar el impacto en multijugador local (múltiples PlayerInput).

### Testing
- Al crear nuevos scripts, incluir validaciones en `Awake()` o `Start()` con `Debug.LogWarning()` para referencias faltantes.
- Verificar que los nuevos componentes no rompan el flujo Lobby → Partida → Fin.
