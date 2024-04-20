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
	%RawData.initialize(acclerometer_sample_x.duplicate())
	
	detrend_samples(15)
	%DetrendedData.initialize(acclerometer_sample_x.duplicate())
	
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
		
		# Average the first few values, since these will be out of bounds for the window
		var initial_average: float = 0
		for i in range(len(sample)):
			initial_average += sample[i]
		initial_average /= len(sample)
		
		var average: float = initial_average
		for i in range(len(sample)):
			if i - window_size < 0:
				average -= initial_average / window_size
			else:
				average -= sample[i - window_size] / window_size
			average += sample[i] / window_size
			output.append(sample[i] - average)
		
		for i in range(len(output)):
			sample[i] = output[i]

func analyze_samples() -> void:
	normalize_samples()
	var magnitudes: Array[float] = flatten_samples()
	%MagnitudesChart.initialize(magnitudes)
	
	#var padded_magnitudes: Array[float] = pad_magnitudes(magnitudes)
	#%PaddedChart.initialize(padded_magnitudes)
	
	var median: float = get_median(magnitudes)
	for i in range(len(magnitudes)):
		magnitudes[i] -= median

	# The FFT plugin expects an ambiguous array, as it modifies it with
	# ComplexNumber objects in-place for performance reasons.
	var copied_data: Array = []
	for data_point in magnitudes:
		copied_data.append(data_point)
	var complex_numbers: Array = FFT.fft(copied_data)
	var fft_magnitudes: Array[float] = []
	for num in complex_numbers:
		fft_magnitudes.append(sqrt(num.re ** 2 + num.im ** 2))
	
	%FFTChart.initialize(fft_magnitudes)
	
	var filtered_magnitudes: Array[float] = apply_fir_filter(fft_magnitudes)
	%FilteredChart.initialize(filtered_magnitudes)
	
	%HeartRateLabel.text = "Heart Rate: " + str(extract_heartrate(filtered_magnitudes) * 60) + " bpm"

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

func pad_magnitudes(array: Array[float]) -> Array[float]:
	var median: float = get_median(array)
	var padding: Array[float] = []
	padding.resize(10)
	padding.fill(median)
	
	return padding + array + padding

func get_median(array: Array[float]) -> float:
	array = array.duplicate()
	array.sort()
	return array[int(len(array) / 2)]

func apply_fir_filter(magnitudes: Array[float]) -> Array[float]:
	var buffer: Array[float] = []
	buffer.resize(len(FIR_coef))
	buffer.fill(0.0)
	
	var output: Array[float] = []
	for i in range(len(magnitudes)):
		var filtered_value: float = 0.0
		buffer = ([magnitudes[i]] as Array[float]) + buffer
		for j in range(len(FIR_coef)):
			filtered_value += buffer[j] * FIR_coef[j]
		output.append(filtered_value)
	return output

func extract_heartrate(magnitudes: Array[float]):
	var m_f: float = 0
	var max_ampl: float = 0
	for i in range(len(magnitudes)):
		var f: float = (60.0/1024 * i)
		if .66 < f and  f < 2.5:
			if magnitudes[i] > max_ampl:
				max_ampl = magnitudes[i]
				m_f = f
	return m_f
