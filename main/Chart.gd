class_name Chart
extends Control

@export var line_color: Color = Color(0.5, 0.2, 0.9)
@export var line_width: float = 1.0

var y: Array[float]

func _draw() -> void:
	if len(y) > 0:
		plot()

func initialize(data: Array[float]) -> void:
	y = data
	queue_redraw()

func plot() -> void:
	var max_y: float = y.max()
	var min_y: float = y.min()
	for i in range(1, len(y)):
		var from: Vector2 = Vector2(
			fit(i - 1, 0, len(y) - 1, size.x), 
			size.y - fit(y[i - 1], min_y, max_y, size.y))
		var to: Vector2 = Vector2(
			fit(i, 0, len(y) - 1, size.x), 
			size.y - fit(y[i], min_y, max_y, size.y))
		draw_line(from, to, line_color, line_width, true)
	%Max.text = str(max_y)
	%Min.text = str(min_y)

func fit(v: float, min_v: float, max_v: float, scalar: float) -> float:
	return (v - min_v) / (max_v - min_v) * scalar
