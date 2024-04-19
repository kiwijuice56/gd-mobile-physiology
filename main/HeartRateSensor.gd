class_name HeartRateSensor
extends Node

var acclerometer_sample_x: Array[float] 
var acclerometer_sample_y: Array[float] 
var acclerometer_sample_z: Array[float] 

var sample_count: int 

func _ready() -> void:
	set_physics_process(false)
	
	await get_tree().create_timer(1.0).timeout
	
	start_sampling()
	await get_tree().create_timer(5.0).timeout
	stop_sampling()
	
	analyze_samples()

func _physics_process(_delta: float) -> void:
	var sample: Vector3 = Input.get_accelerometer()
	acclerometer_sample_x.append(sample.x)
	acclerometer_sample_y.append(sample.y)
	acclerometer_sample_z.append(sample.z)

func analyze_samples() -> void:
	normalize_samples()
	var magnitudes: Array[float] = flatten_samples()
	%MagnitudesChart.initialize(magnitudes)

func start_sampling() -> void:
	set_physics_process(true)
	sample_count = 0
	acclerometer_sample_x = []
	acclerometer_sample_y = []
	acclerometer_sample_y = []

func stop_sampling() -> void:
	set_physics_process(false)
	sample_count = len(acclerometer_sample_x)

func normalize_samples() -> void:
	for axis in [acclerometer_sample_x, acclerometer_sample_y, acclerometer_sample_z]:
		var median: float = get_median(axis)
		for i in range(len(axis)):
			axis[i] -= median

func flatten_samples() -> Array[float]:
	var magnitudes: Array[float] = []
	for i in range(sample_count):
		var sum: float = 0
		for axis in [acclerometer_sample_x, acclerometer_sample_y, acclerometer_sample_z]:
			sum += pow(axis[i], 2)
		magnitudes.append(sqrt(sum))
	return magnitudes

func get_median(array: Array[float]) -> float:
	array = array.duplicate()
	array.sort()
	return array[int(len(array) / 2)]
