extends CharacterBody3D

const SPEED = 300.0

var onThrow = false
var throwDirection = Vector2.ZERO
@export var player:Player

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
	if body.is_in_group("Monster") and body.stun and player.attacking:
		body.set_as_human()
		player.set_as_monster()
	elif body.is_in_group("Monster") and randf() < 0.4 and player.attacking:
		body.set_as_human()
		player.set_as_monster()
