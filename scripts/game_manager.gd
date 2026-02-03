extends Node

var count_palanca = 0

func _death_human():
	var human = get_tree().get_nodes_in_group("Human")
	
	for i in human:
		i.queue_free()
	var monsters = get_tree().get_nodes_in_group("Monster")
	_select_human(monsters)

func _update_palanca():
	count_palanca += 1
	
	if count_palanca == 4:
		var monsters = get_tree().get_nodes_in_group("Monster")
		for m in monsters:
			m.start_stun()
			await get_tree().create_timer(0.2).timeout
		count_palanca = 0
		var palancas = get_tree().get_nodes_in_group("palanca")
		for p in palancas:
			p.can_active = true
			

func _select_human(list_players) -> void:
	var n_ramdom = randi_range(0, list_players.size() - 1)
	
	for i in range(list_players.size()):
		if n_ramdom == i:
			list_players[i].set_as_human()
		else:
			list_players[i].set_as_monster()
