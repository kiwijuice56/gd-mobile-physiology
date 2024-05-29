class_name Sampler
extends Node

var accelerometer_x: PackedFloat64Array
var accelerometer_y: PackedFloat64Array
var accelerometer_z: PackedFloat64Array

var sample_amount_target: int 
var frame: int = 0

signal sampling_complete

func _ready() -> void:
	set_physics_process(false)

func _physics_process(_delta: float) -> void:
	var sample: Vector3 = Input.get_accelerometer()
	
	accelerometer_x.append(sample.x)
	accelerometer_y.append(sample.y)
	accelerometer_z.append(sample.z)
	
	if len(accelerometer_x) == sample_amount_target:
		sampling_complete.emit()
		stop_detection()

func start_detection() -> void:
	set_physics_process(true)

func stop_detection() -> void:
	set_physics_process(false)

func get_accelerometer_samples(sample_size: int) -> Array[PackedFloat64Array]:
	sample_amount_target = sample_size
	start_detection()
	await sampling_complete
	return [accelerometer_x, accelerometer_y, accelerometer_z]
