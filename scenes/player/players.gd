class_name Player
extends CharacterBody3D

@export var speed:float = 10
@export var acceleration:float = 20
@export var device_id:int = -1
@export var human_model: MonsterAnimation
@export var monster_animation: MonsterAnimation
@export var mask_node: Mask 
@export var audio_stream_player_3d: AudioStreamPlayer3D
@export var inmune_time: float = 5.0
var gravity: float = ProjectSettings.get_setting("physics/3d/default_gravity")
var _name_animations:NameAnimationMonster = NameAnimationMonster.new()
var _name_animations_human:NameAnimationHuman = NameAnimationHuman.new()
var attacking = false
var can_move: bool = true # New toggle to enable/disable movement
var inmune = false
var inmune_timer = 0.0
var current_material: ShaderMaterial = null

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
	add_to_group("Player")
	mask_node.visible = false

func _process(delta: float) -> void:
	if MultiplayerInput.is_action_just_pressed(-1,"a"):
		set_as_human()
	if MultiplayerInput.is_action_just_pressed(-1,"b"):
		set_as_monster()
	
	if not is_on_floor():
		velocity.y -= gravity * delta
	
	if inmune:
		print("Player is inmune, timer: ", inmune_timer)
		inmune_timer += delta
		_update_shine_effect(true)
		if inmune_timer >= inmune_time:
			print("Player is no longer inmune")
			inmune = false
			inmune_timer = 0.0
			_update_shine_effect(false)
	
	if not stun:
		var input_dir = MultiplayerInput.get_vector(device_id ,"player_right", "player_left", "player_down", "player_up")
		
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
				if is_in_group("monster"):
					monster_animation.play_animation(_name_animations.run)
				else:
					human_model.play_animation(_name_animations_human.run)
				
				# Play footstep sounds only when not already playing
				audio_stream_player_3d.pitch_scale = randf_range(0.8, 1.2) # Randomize pitch for variety
				if is_on_floor() and not audio_stream_player_3d.playing:
					play_sfx(PASOS)
					
			else:
				velocity.x = 0
				velocity.z = 0
				if is_on_floor() and audio_stream_player_3d.playing:
					audio_stream_player_3d.stop()
				if is_in_group("monster"):
					monster_animation.play_animation(_name_animations.idle)
				else:
					human_model.play_animation(_name_animations_human.idle)
	else:
		velocity.x = 0
		velocity.z = 0
	
	# If we are in the lobby, just play the idle animation and stop here
	if not can_move:
		velocity.x = 0
		velocity.z = 0
		# Check for groups to play the right idle
		if is_in_group("monster"):
			monster_animation.play_animation(_name_animations.idle)
		else:
			human_model.play_animation(_name_animations_human.idle)
		
		move_and_slide() # Allow them to fall to the floor
		return # STOP HERE during lobby
	
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
	add_to_group("human")
	remove_from_group("monster")
	#mask_node.show()
	human_model.show()
	monster_animation.hide()

func set_as_monster():
	add_to_group("monster")
	remove_from_group("human")
	mask_node.hide()
	human_model.hide()
	monster_animation.show()

func _update_shine_effect(enable: bool):
	# Get the active model (human or monster)
	var active_model = monster_animation if is_in_group("monster") else human_model
	if active_model == null:
		print("No active model found")
		return
	
	# Find the MeshInstance3D in the active model
	var mesh_instance = _find_mesh_instance(active_model)
	if mesh_instance == null:
		print("No MeshInstance3D found in model")
		return
	
	# Get the shader material (don't cache it, fetch it fresh each time)
	var material = mesh_instance.get_active_material(0)
	if material == null:
		print("No material found on mesh")
		return
	
	if not material is ShaderMaterial:
		print("Material is not a ShaderMaterial")
		return
	
	var shader_mat = material as ShaderMaterial
	
	# Check if it has the shine_intensity parameter
	if shader_mat.shader == null:
		print("No shader attached to material")
		return
	
	# Update shine intensity
	shader_mat.set_shader_parameter("shine_intensity", 1.0 if enable else 0.0)
	print("Shine effect ", "enabled" if enable else "disabled", " for ", "monster" if is_in_group("monster") else "human")

func _find_mesh_instance(node: Node) -> MeshInstance3D:
	if node is MeshInstance3D:
		return node
	
	for child in node.get_children():
		var result = _find_mesh_instance(child)
		if result != null:
			return result
	
	return null

func play_sfx(sound: AudioStream):
	audio_stream_player_3d.stream = sound
	audio_stream_player_3d.play()
