extends TextureRect
const TIMERS = [
	preload("uid://d33hr0q55borr"), preload("uid://c4oqlidw4jkmh"), preload("uid://dvgdwwhsbplu2"), preload("uid://d1mny1hpjedqh"), preload("uid://dtlb5rekjmqe2"), preload("uid://bdp8m6bwyvw5v")
]


@export var time_limit_for_image:int = 10
var time = 0
var i = 1

func _ready() -> void:
	self.texture = TIMERS[0]

func _process(delta: float) -> void:
	
	time += delta
	
	if time >= time_limit_for_image:
		self.texture = TIMERS[i]
		i += 1
		time = 0
		if i == TIMERS.size():
			GameManager._death_human()
			self.texture = TIMERS[0]
			i = 0
	
