class_name HeartRateSensor
extends Node

signal sampling_complete

var acclerometer_sample_x: Array[float] 
var acclerometer_sample_y: Array[float] 
var acclerometer_sample_z: Array[float] 

var sample_count: int 

const FIR_coef: Array[float] = [
	-0.030558025513308964,
	-0.0444457674165741,
	-0.02702148514505239,
	0.04617464410595246,
	0.1617586061668395,
	0.2702328451984712,
	0.31473643267885837,
	0.2702328451984712,
	0.1617586061668395,
	0.04617464410595246,
	-0.02702148514505239,
	-0.0444457674165741,
	-0.030558025513308964
]

func _ready() -> void:
	set_physics_process(false)
	
	# Delay to make it easier to measure data
	await get_tree().create_timer(1.0).timeout
	
	await sample(256)
	print("Samples collected.")
	
	%RawDataX.initialize(acclerometer_sample_x.duplicate())
	%RawDataY.initialize(acclerometer_sample_y.duplicate())
	%RawDataZ.initialize(acclerometer_sample_z.duplicate())
	
	detrend_samples(15)
	%DetrendedDataX.initialize(acclerometer_sample_x.duplicate())
	%DetrendedDataY.initialize(acclerometer_sample_y.duplicate())
	%DetrendedDataZ.initialize(acclerometer_sample_z.duplicate())
	
	normalize_samples()
	%NormalizedDataX.initialize(acclerometer_sample_x.duplicate())
	%NormalizedDataY.initialize(acclerometer_sample_y.duplicate())
	%NormalizedDataZ.initialize(acclerometer_sample_z.duplicate())
	
	print("Done!")

func _physics_process(_delta: float) -> void:
	var sample: Vector3 = Input.get_accelerometer()
	
	if len(acclerometer_sample_x) < sample_count:
		acclerometer_sample_x.append(sample.x)
		acclerometer_sample_y.append(sample.y)
		acclerometer_sample_z.append(sample.z)
	else:
		sampling_complete.emit()

func sample(count: int):
	sample_count = count
	
	acclerometer_sample_x = []
	acclerometer_sample_y = []
	acclerometer_sample_y = []
	
	set_physics_process(true)
	
	await sampling_complete
	
	set_physics_process(false)

func detrend_samples(window_size: int) -> void:
	for sample in [acclerometer_sample_x, acclerometer_sample_y, acclerometer_sample_z]:
		var output: Array[float] = []
		var total_average: float = get_average(sample)
		var average: float = total_average
		for i in range(len(sample)):
			if i - window_size < 0:
				average -= total_average / window_size
			else:
				average -= sample[i - window_size] / window_size
			average += sample[i] / window_size
			output.append(sample[i] - average)
		
		for i in range(len(output)):
			sample[i] = output[i]

func normalize_samples() -> void:
	for sample in [acclerometer_sample_x, acclerometer_sample_y, acclerometer_sample_z]:
		var average: float = get_average(sample)
		for i in range(len(sample)):
			sample[i] -= average
		
		var stdev: float = get_standard_deviation(sample)
		
		for i in range(len(sample)):
			sample[i] /= stdev

func get_average(arr: Array[float]) -> float:
	var average: float 
	for val in arr:
		average += val
	average /= len(arr)
	return average

func get_standard_deviation(arr: Array[float]) -> float:
	var average: float = get_average(arr)
	var stdev: float 
	for val in arr:
		stdev += pow(val - average, 2)
	return sqrt(stdev / len(arr))
