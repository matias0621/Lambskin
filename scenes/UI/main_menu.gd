extends Control

@onready var iniciar = $Iniciar
@onready var salir = $Salir

func iniciar_juego():
	if iniciar.pressed():
		get_tree().change_scene_to_file("Escena de juego")

func salir_del_juego():
	if salir.pressed():
		get_tree().quit()
