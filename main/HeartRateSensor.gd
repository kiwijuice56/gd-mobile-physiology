class_name HeartRateSensor
extends Node

signal sampling_complete


var acclerometer_sample_x_raw: Array[float] 
var acclerometer_sample_y_raw: Array[float] 
var acclerometer_sample_z_raw: Array[float] 

var acclerometer_sample_x: Array[float] 
var acclerometer_sample_y: Array[float] 
var acclerometer_sample_z: Array[float] 

var sample_count: int = 512
var frame_index: int = 0

func _physics_process(_delta: float) -> void:
	var sample: Vector3 = Input.get_accelerometer()
	
	acclerometer_sample_x_raw.append(sample.x)
	acclerometer_sample_y_raw.append(sample.y)
	acclerometer_sample_z_raw.append(sample.z)
	
	if len(acclerometer_sample_x_raw) > sample_count:
		acclerometer_sample_x_raw.pop_front()
		acclerometer_sample_y_raw.pop_front()
		acclerometer_sample_z_raw.pop_front()
	
	%RawDataX.initialize(acclerometer_sample_x_raw)
	%RawDataY.initialize(acclerometer_sample_y_raw)
	%RawDataZ.initialize(acclerometer_sample_z_raw)

	
	frame_index += 1
	if frame_index % 64 == 0:
		analyze_data()
		frame_index = 0

func analyze_data() -> void:
	acclerometer_sample_x = acclerometer_sample_x_raw.duplicate()
	acclerometer_sample_y = acclerometer_sample_y_raw.duplicate()
	acclerometer_sample_z = acclerometer_sample_z_raw.duplicate()
	
	detrend_samples(15)
	normalize_samples()
	%DetrendedDataX.initialize(acclerometer_sample_x.duplicate())
	%DetrendedDataY.initialize(acclerometer_sample_y.duplicate())
	%DetrendedDataZ.initialize(acclerometer_sample_z.duplicate())
	
	var ica_result: Array = %ICA.analyze(acclerometer_sample_x, acclerometer_sample_y, acclerometer_sample_z, len(acclerometer_sample_x))
	var ica_signals: Array = []
	
	# Transpose the ICA signals
	for si in range(3):
		var output: Array = []
		for i in range(len(acclerometer_sample_x)):
			output.append(ica_result[i][si])
		ica_signals.append(output)
	
	%ICA1.initialize(ica_signals[0])
	%ICA2.initialize(ica_signals[1])
	%ICA3.initialize(ica_signals[2])
	
	var ffts: Array = []
	for component in ica_signals:
		ffts.append(get_fourier_transform(component))
	
	%FFT1.initialize(ffts[0])
	%FFT2.initialize(ffts[1])
	%FFT3.initialize(ffts[2])
	
	%HeartRateLabel1.text = "Heart Rate (bpm): " + str(extract_heartrate(ffts[0]))
	%HeartRateLabel2.text = "Heart Rate (bpm): " + str(extract_heartrate(ffts[1]))
	%HeartRateLabel3.text = "Heart Rate (bpm): " + str(extract_heartrate(ffts[2]))

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

func extract_heartrate(fft: Array[float]):
	var max_amplitude_frequency: float = 0
	var max_amplitude: float = 0
	for i in range(len(fft)):
		var frequency: float = 60.0 / len(fft) * i
		if 1 < frequency and frequency < 2:
			if fft[i] > max_amplitude:
				max_amplitude = fft[i]
				max_amplitude_frequency = frequency

	return max_amplitude_frequency * 60

func get_fourier_transform(sample: Array) -> Array[float]:
	# The FFT plugin expects an ambiguous array, as it modifies it with
	# ComplexNumber objects in-place for performance reasons
	var complex_numbers: Array = FFT.fft(sample.duplicate())
	var fft_magnitudes: Array[float] = []
	for num in complex_numbers:
		fft_magnitudes.append(sqrt(num.re ** 2 + num.im ** 2))
	return fft_magnitudes

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
