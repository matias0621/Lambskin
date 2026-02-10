extends Node3D

var player_scene = load("res://scenes/player/player.tscn")
var joined_players = [] # Lista de IDs de dispositivos que ya entraron
var ready_players = {} # Dictionary to track {device_id: bool}
@onready var status_label = $UI/LobbyStatusLabel

func _input(event):
	if event is InputEventJoypadButton or event is InputEventKey:
		if event.is_pressed() and not event.is_echo():
			var id = event.device
			if event is InputEventKey:
				id = -1 

			# 1. JOIN LOGIC
			if not joined_players.has(id):
				if joined_players.size() < 4:
					spawn_player(id)
					ready_players[id] = false # Initialize as not ready
			
			# 2. READY LOGIC (If already joined)
			else:
				var is_ready_button = false
				# Check for Button 0 (A/Cross) or 'R' Key
				if event is InputEventJoypadButton and event.button_index == 0:
					is_ready_button = true
				elif event is InputEventKey and event.keycode == KEY_R:
					is_ready_button = true
				
				if is_ready_button:
					toggle_player_ready(id)

func toggle_player_ready(id):
	ready_players[id] = !ready_players[id]
	print("Device ", id, " Ready Status: ", ready_players[id])
	
	# Find the specific node to show visual feedback
	for child in get_children():
		# This works because you set child.device_id in spawn_player()
		if "device_id" in child and child.device_id == id:
			var status_label = child.find_child("Label3D")
			if ready_players[id]:
				status_label.text = "✅"
			else:
				status_label.text = "" # Hide the checkmark

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
	update_lobby_text()

func update_lobby_text():
	if joined_players.size() < 2:
		status_label.text = "LOBBY_TEXT1"
	else:
		status_label.text = "LOBBY_TEXT2"

func _process(_delta):
	if joined_players.size() > 0:
		check_all_ready()

func check_all_ready():
	var players_count = joined_players.size()
	if players_count < 1: return

	var all_ready = true
	for id in joined_players:
		if ready_players[id] == false:
			all_ready = false
			break
			
	if all_ready and players_count >= 2:
		status_label.text = "Starting Game..."
		start_game()

func start_game():
	Global.active_players = joined_players.duplicate() # Use .duplicate() to be safe
	print("CRITICAL: Lobby is saving these IDs: ", Global.active_players)
	get_tree().change_scene_to_file("res://test/stage_test.tscn")
