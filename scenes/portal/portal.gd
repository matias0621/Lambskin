extends Node3D


@export var area: Area3D
@export var mesh_of_portal: MeshInstance3D
@export var time_limit_for_image:int = 1
@onready var list_of_mask = [preload("uid://d33hr0q55borr"), preload("uid://c4oqlidw4jkmh"), preload("uid://dvgdwwhsbplu2"), preload("uid://d1mny1hpjedqh"), preload("uid://dtlb5rekjmqe2"), preload("uid://bdp8m6bwyvw5v")]
var is_a_monster = false
var time = 0
var i = 1
var player: Player
var portal_material: StandardMaterial3D




func _ready() -> void:
	if mesh_of_portal:
		var mat = mesh_of_portal.get_active_material(0)

		if mat:
			portal_material = mat.duplicate()
		else:
			portal_material = StandardMaterial3D.new()

		mesh_of_portal.set_surface_override_material(0, portal_material)

		portal_material.albedo_texture = list_of_mask[0]
		portal_material.albedo_color = Color.WHITE

	area.body_entered.connect(_on_monster_enter)
	area.body_exited.connect(_on_monster_exit)

func _process(delta: float) -> void:
	if !is_a_monster: return

	if player != null and player.is_in_group("Human"):
		is_a_monster = false
		player.can_move = true
		player = null
		time = 0
		if mesh_of_portal:
			print("Changing portal texture to mask 0")
			var mat = mesh_of_portal.get_surface_override_material(0)
			mat.albedo_texture = list_of_mask[0]
			mat.albedo_color = Color(1, 1, 1, 1)
		i = 1
		return
	
	time += delta

	if time >= time_limit_for_image:
		time = 0
		print("enter the code block for change texture", i)
		if mesh_of_portal:
			print("Changing portal texture to mask ", i)
			var mat = portal_material
			var red_intensity = float(i) / float(list_of_mask.size() - 1)
			mat.albedo_texture = list_of_mask[i]
			mat.albedo_color = Color(1, 1 - red_intensity, 1 - red_intensity, 1)
		i += 1
		if i >= list_of_mask.size():
			i = 0
			player.inmune = true
			player.can_move = true
	

func _on_monster_exit(body: Node) -> void:
	if body.is_in_group("monster") and body is Player:
		is_a_monster = false
		player = body

func _on_monster_enter(body: Node) -> void:
	if body.is_in_group("monster") and body is Player:
		is_a_monster = true
		player = body
		player.can_move = false
		player.position.x = position.x
		player.position.z = position.z
