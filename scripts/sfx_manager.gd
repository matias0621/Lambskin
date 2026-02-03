extends Node

# Create a pool of players to handle simultaneous sounds
func play_sfx(stream: AudioStream): # pitch_variance: float = 0.1
	var asp = AudioStreamPlayer.new()
	add_child(asp)
	asp.stream = stream
	
	# Add variety so sounds don't feel robotic
	# asp.pitch_scale = randf_range(1.0 - pitch_variance, 1.0 + pitch_variance)
	
	asp.play()
	# Automatically delete the node when the sound finishes
	asp.finished.connect(asp.queue_free)
