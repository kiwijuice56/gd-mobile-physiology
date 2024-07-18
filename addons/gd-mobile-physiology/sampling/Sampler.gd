class_name Sampler
extends Node

# Samples accelerometer and gyroscope data to feed into breathing/heart rate algorithms

var accelerometer: Array[Vector3]
var gyroscope: Array[Vector3]

var sample_amount_target: int 
var frame: int = 0

signal sampling_complete

func _ready() -> void:
	set_physics_process(false)

func _physics_process(_delta: float) -> void:
	accelerometer.append(Input.get_accelerometer())
	gyroscope.append(Input.get_gyroscope())
	
	if len(accelerometer) == sample_amount_target:
		sampling_complete.emit()
		stop_detection()

func start_detection() -> void:
	set_physics_process(true)

func stop_detection() -> void:
	set_physics_process(false)

func get_accelerometer_and_gyroscope_samples(sample_size: int) -> Array[Array]:
	sample_amount_target = sample_size
	start_detection()
	await sampling_complete
	return [accelerometer, gyroscope]
