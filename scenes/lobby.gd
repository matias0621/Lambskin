extends Node3D

var player_scene = load("res://scenes/player/player.tscn")
var joined_players = [] # Lista de IDs de dispositivos que ya entraron

func _input(event):
	if event is InputEventJoypadButton or event is InputEventKey:
		if event.is_pressed() and not event.is_echo():
			var id = event.device
			
			# Temporary "Force Unique" for testing:
			# If it's a key, let's pretend it's ID -1 so it doesn't clash with Controller 0
			if event is InputEventKey:
				id = -1 

			if not joined_players.has(id) and joined_players.size() < 4:
				spawn_player(id)

func spawn_player(id):
	joined_players.append(id)
	var new_player = player_scene.instantiate()
	new_player.device_id = id
	
	add_child(new_player)
	
	var spawn_index = joined_players.size() - 1
	var spacing = 12.5
	var start_x = 50.0
	var target_pos = Vector3(start_x - (spawn_index * spacing), 10, 40)
	new_player.global_position = target_pos
	
	print("Lobby commanded position: ", target_pos)
	print("Actual position after setting: ", new_player.global_position)

func _process(_delta):
	if joined_players.size() > 0:
		check_all_ready()

func check_all_ready():
	var players = get_tree().get_nodes_in_group("Player")
	
	# If this is 0, the game will NEVER start
	if players.size() == 0: 
		return 

	var all_ready = true
	for p in players:
		if "is_ready" in p:
			if p.is_ready == false:
				all_ready = false
		else:
			all_ready = false # If a node lacks the variable, we aren't ready
			
	if all_ready:
		print("SUCCESS: EVERYONE IS READY. STARTING GAME...")
		start_game()

func start_game():
	print("Everyone is ready! Moving to the Dungeon...")
	# Prevent this from firing multiple times
	set_process(false) 
	# Change scene to your actual gameplay map
	get_tree().change_scene_to_file("res://stage/stage_test.tscn")
