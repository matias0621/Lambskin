extends Control

func _on_finished() -> void:
	print("Video finished, returning to Main Menu...")
	get_tree().change_scene_to_file("res://scenes/UI/main_menu.tscn")
