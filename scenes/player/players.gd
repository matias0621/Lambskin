class_name Player
extends CharacterBody3D

@export var speed:float = 10
@export var acceleration:float = 20
@export var device_id:int = -1
@export var monster_animation:MonsterAnimation
var gravity: float = ProjectSettings.get_setting("physics/3d/default_gravity")
var _name_animations:NameAnimationMonster = NameAnimationMonster.new()
var _name_animations_human:NameAnimationHuman = NameAnimationHuman.new()
@onready var mask_node: CharacterBody3D = $Mask
@onready var audio_stream_player_3d: AudioStreamPlayer3D = $AudioStreamPlayer3D
@onready var human_model: MonsterAnimation = $HumanModel
var attacking = false



const BAAAAA = preload("uid://b5hfraugwaaer")
const CLICK = preload("uid://dmshxdxcen5pm")
const ELECTROCUCION = preload("uid://btjb5u414by16")
const GOLPE = preload("uid://dy2fdbsffxq4n")
const PASARMASCARA = preload("uid://fr472forldlq")
const PASOS = preload("uid://dkixvsfuwcrfk")
const TICTAC = preload("uid://c6fopqy82f6nm")

var is_mask_thrown: bool = false
var stun = false
var is_stunning := false

func _ready() -> void:
	print(device_id)

func _process(delta: float) -> void:
	if not is_on_floor():
		velocity.y -= gravity * delta
	
	if not stun:
		var input_dir = MultiplayerInput.get_vector(device_id ,"player_one_right", "player_one_left", "player_one_down", "player_one_up")
		
		if MultiplayerInput.is_action_just_pressed(device_id,"shoot_mask") and not is_mask_thrown and is_in_group("Human"):
			attacking = true
			human_model.play_animation(_name_animations_human.attack)
			_throw_mask()
		
		
		if not attacking:
			if input_dir != Vector2.ZERO:
				velocity.x = lerp(velocity.x, input_dir.x * speed, acceleration * delta)
				velocity.z = lerp(velocity.z, input_dir.y * speed ,acceleration * delta)
				var dir_3d = Vector3(input_dir.x, 0, input_dir.y).normalized()
				var target_rotation = atan2(dir_3d.x, dir_3d.z) + PI
				rotation.y = lerp_angle(rotation.y, target_rotation, 8 * delta) 
				if is_in_group("Monster"):
					monster_animation.play_animation(_name_animations.run)
				else:
					human_model.play_animation(_name_animations_human.run)
			else:
				velocity.x = 0
				velocity.z = 0
				if is_in_group("Monster"):
					monster_animation.play_animation(_name_animations.idle)
				else:
					human_model.play_animation(_name_animations_human.idle)
	else:
		velocity.x = 0
		velocity.z = 0
	
	move_and_slide()
	


func start_stun() -> void:

	if is_stunning:
		return
	
	is_stunning = true
	stun = true

	await _do_stun()

	is_stunning = false
	stun = false

func _do_stun() -> void:

	# Animación stunning
	play_sfx(ELECTROCUCION)
	monster_animation.play_animation(_name_animations.shock)

	await monster_animation.animation_finished


	# Animación shock (loop)
	monster_animation.play_animation(_name_animations.stunning, true)

	await get_tree().create_timer(4).timeout

func _throw_mask():
	is_mask_thrown = true
	play_sfx(PASARMASCARA)
	
	# 1. Detach from player and add to the world so it doesn't move WITH the player
	var world = get_parent()
	var start_pos = mask_node.global_position
	remove_child(mask_node)
	world.add_child(mask_node)
	mask_node.global_position = start_pos

	# 2. Logic for throwing
	var throw_distance = 10
	var target_position = global_transform.origin - global_transform.basis.z * throw_distance


	while mask_node.global_position.distance_to(target_position) > 0.1:
		mask_node.global_position = mask_node.global_position.move_toward(target_position, 20 * get_process_delta_time())
		await get_tree().process_frame

	await get_tree().create_timer(1.0).timeout
	_return_mask()

func _return_mask():
	mask_node.set_collision_layer(0)
	mask_node.set_collision_mask(0)

	while mask_node.global_position.distance_to(global_transform.origin) > 0.5:
		# Move toward the player's current position
		mask_node.global_position = mask_node.global_position.move_toward(global_transform.origin, 25 * get_process_delta_time())
		await get_tree().process_frame

	# 3. Re-attach to player and reset local position
	mask_node.get_parent().remove_child(mask_node)
	add_child(mask_node)
	
	# Reset to your specific coordinates
	mask_node.position = Vector3(0, 0.5, -2.5)
	mask_node.rotation = Vector3.ZERO
	
	mask_node.set_collision_layer(1)
	mask_node.set_collision_mask(1)
	is_mask_thrown = false
	attacking = false

func set_as_human():
	play_sfx(BAAAAA)
	add_to_group("Human")
	remove_from_group("Monster")
	mask_node.show()
	human_model.show()
	monster_animation.hide()

func set_as_monster():
	add_to_group("Monster")
	remove_from_group("Human")
	mask_node.hide()
	human_model.hide()
	monster_animation.show()

func play_sfx(sound: AudioStream):
	audio_stream_player_3d.stream = sound
	audio_stream_player_3d.play()
