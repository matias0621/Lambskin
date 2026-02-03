extends Node

@onready var player = $AudioStreamPlayer

func play_track(stream: AudioStream):
	if player.stream == stream and player.playing:
		return
