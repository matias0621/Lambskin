extends Node3D

@export var player_scene: PackedScene = preload("res://scenes/player/player.tscn")

func _ready():
	if Global.active_players.size() == 0:
		return

	# 1. Randomize the player list
	var roles = Global.active_players.duplicate()
	roles.shuffle() # Mixes the IDs randomly
	var human_id = roles[0] # The first ID in the shuffled list is our Human

	var start_x = 20.0 
	var spacing = 25.0
	var index = 0

	for id in Global.active_players:
		var p = player_scene.instantiate()
		p.device_id = id
		p.can_move = true 
		
		# 2. Position logic (same as we fixed before)
		var x_pos = start_x + ((index % 2) * spacing)
		var z_pos = 40.0 if index < 2 else 50.0
		p.position = Vector3(x_pos, 10, z_pos)
		
		# 3. Add to scene BEFORE calling group functions
		add_child(p)

		# 4. Role Assignment
		if id == human_id:
			p.set_as_human()
			print("Player ", id, " is the HUMAN")
		else:
			p.set_as_monster()
			print("Player ", id, " is a MONSTER")
		
		index += 1
