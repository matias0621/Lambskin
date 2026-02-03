extends MeshInstance3D


var _human_in_area:bool = false
var _device_id:int
var can_active:bool = true

func _process(_delta: float) -> void:
	if _human_in_area and _device_id != -9 and can_active:
		if MultiplayerInput.is_action_just_pressed(_device_id, "pruebas"):
			GameManager._update_palanca()
			can_active = false
			print("Se activo uno")

func _on_area_3d_body_entered(body: Node3D) -> void:
	if body.is_in_group("Human"):
		_human_in_area = true
		_device_id = body.device_id

func _on_area_3d_body_exited(body: Node3D) -> void:
	if body.is_in_group("Human"):
		_human_in_area = false
		_device_id = -9
