class_name Mask
extends CharacterBody3D

const SPEED = 300.0

var onThrow = false
var throwDirection = Vector2.ZERO
@export var player:Player

func _ready() -> void:
	$Area3D.body_entered.connect(_on_area_3d_body_entered)

func _physics_process(_delta):
	if onThrow:
		# 3D position
		var direction_3d = Vector3(-throwDirection.x, 0, -throwDirection.y).normalized()
		velocity = direction_3d * SPEED
		onThrow = false

	move_and_slide()

func throw(direction):
	onThrow = true
	throwDirection = direction

	await get_tree().create_timer(2.0).timeout
	queue_free()


func _on_area_3d_body_entered(body: Node3D) -> void:
	if not player:
		print("ADVERTENCIA: La máscara no tiene referencia al jugador")
		return
	
	if not body.is_in_group("monster"):
		return
	
	if not player.attacking:
		print("Jugador no está atacando")
		return
	
	# Verificar inmunidad
	if body.has_method("get") and body.get("inmune"):
		print("Monstruo es inmune, no se puede transformar")
		return
	
	if randf() > 0.6:
		print("Transformación fallida")
		return
	
	print("¡Transformación exitosa! Monster -> Human, Player -> Monster")
	body.set_as_human()
	player.set_as_monster()
