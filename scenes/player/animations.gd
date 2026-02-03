class_name MonsterAnimation
extends Node3D

@export var _animationPlayer:AnimationPlayer
signal animation_finished

func _ready() -> void:
	_animationPlayer.animation_finished.connect(_on_animation_finished)


func play_animation(animation: String, _loop = false):
	if animation == "Corriendo":
		_animationPlayer.speed_scale = 1.8
	else :
		_animationPlayer.speed_scale = 1
	if _loop:
		_animationPlayer.get_animation(animation).loop_mode = Animation.LOOP_LINEAR
	else:
		_animationPlayer.get_animation(animation).loop_mode = Animation.LOOP_NONE
	_animationPlayer.play(animation)
	

func _on_animation_finished(anim_name: String):
	emit_signal("animation_finished", anim_name)
