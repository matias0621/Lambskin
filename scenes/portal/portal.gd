extends Node3D


@export var area: Area3D
@export var mesh_of_portal: MeshInstance3D
@export var time_limit_for_image:int = 8
var list_of_mask = ["uid://d33hr0q55borr", "uid://c4oqlidw4jkmh", "uid://dvgdwwhsbplu2", "uid://d1mny1hpjedqh", "uid://dtlb5rekjmqe2", "uid://bdp8m6bwyvw5v"]
var is_a_monster = false
var time = 0
var i = 1
var player: Player



func _ready() -> void:
    mesh_of_portal.texture = list_of_mask[0]
    area.body_entered.connect(_on_monster_enter)
    area.body_exited.connect(_on_monster_exit)

func _process(delta: float) -> void:
    if not is_a_monster or player.inmune: return

    if player != null and player.is_in_group("Human"):
        is_a_monster = false
        player.can_move = true
        player = null


    
    time += delta

    if time >= time_limit_for_image:
        time = 0
        mesh_of_portal.texture = list_of_mask[i]
        i += 1
        if i >= list_of_mask.size():
            i = 0
            player.inmune = true
            player.can_move = true
    

func _on_monster_exit(body: Node) -> void:
    if body.is_in_group("monster") and body is Player:
        is_a_monster = false
        player = body
        player.can_move = true

func _on_monster_enter(body: Node) -> void:
    if body.is_in_group("monster") and body is Player:
        is_a_monster = true
        player = body
        player.can_move = false