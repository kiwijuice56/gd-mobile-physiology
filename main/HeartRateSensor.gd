class_name HeartRateSensor
extends Node

var acclerometer_sample_x: PackedFloat64Array
var acclerometer_sample_y: PackedFloat64Array
var acclerometer_sample_z: PackedFloat64Array

var sample_count: int 
var frame_index: int 

func _ready() -> void:
	set_physics_process(false)
	
	start_detection(1024 + len(Filter.BANDPASS_FILTER_BCG) + len(Filter.BANDPASS_FILTER_HR))

func _physics_process(_delta: float) -> void:
	var sample: Vector3 = Input.get_accelerometer()
	
	acclerometer_sample_x.append(sample.x)
	acclerometer_sample_y.append(sample.y)
	acclerometer_sample_z.append(sample.z)
	
	if len(acclerometer_sample_x) > sample_count:
		acclerometer_sample_x.remove_at(0)
		acclerometer_sample_y.remove_at(0)
		acclerometer_sample_z.remove_at(0)
		
		if frame_index % 256 == 0:
			analyze_data([acclerometer_sample_x.duplicate(), acclerometer_sample_y.duplicate(), acclerometer_sample_z.duplicate()])
	
	%RawDataX.plot(acclerometer_sample_x)
	%RawDataY.plot(acclerometer_sample_y)
	%RawDataZ.plot(acclerometer_sample_z)
	
	frame_index += 1

func start_detection(max_sample_count: int) -> void:
	set_physics_process(true)
	sample_count = max_sample_count

func analyze_data(samples: Array[PackedFloat64Array]) -> void:
	# Remove trends, set variance of each signal to 1 and mean to 0
	detrend_samples(samples, 15)
	normalize_samples(samples)
	
	%FixedDataX.plot(samples[0].duplicate())
	%FixedDataY.plot(samples[1].duplicate())
	%FixedDataZ.plot(samples[2].duplicate())
	
	# Isolate BCG movements with bandpass filter, cutoffs at 7 and 13 Hz
	for i in range(len(samples)):
		samples[i] = apply_fir_filter(samples[i], Filter.BANDPASS_FILTER_BCG)
	
	# Get the magnitudes of the filtered signals
	var magnitudes: PackedFloat64Array = []
	for i in range(len(samples[0])):
		var value: float = 0
		for j in range(len(samples)):
			value += pow(samples[j][i], 2)
		magnitudes.append(sqrt(value))
	
	%Pulse1.plot(magnitudes)
	
	# Isolate the heartrate again with a bandpass filter, cutoffs at 0.66 and 2.50 Hz
	magnitudes = apply_fir_filter(magnitudes, Filter.BANDPASS_FILTER_HR)
	
	normalize_samples([magnitudes])
	
	%Pulse2.plot(magnitudes)
	
	var fft: Array = get_fourier_transform(magnitudes)
	
	# Normalize amplitude
	for i in range(len(fft)):
		fft[i] /= len(fft)
	
	%FFT1.plot(fft)
	
	%HeartRateLabel1.text = "Heart Rate (bpm): " + str(extract_heartrate(fft))

func detrend_samples(samples: Array[PackedFloat64Array], window_size: int) -> void:
	for sample in samples:
		var output: PackedFloat64Array = []
		var total_average: float = sample[0]
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

func normalize_samples(samples: Array[PackedFloat64Array]) -> void:
	for sample in samples:
		var average: float = get_average(sample)
		for i in range(len(sample)):
			sample[i] -= average
		
		var stdev: float = get_standard_deviation(sample)
		for i in range(len(sample)):
			sample[i] /= stdev

func apply_fir_filter(sample: PackedFloat64Array, filter: PackedFloat64Array) -> PackedFloat64Array:
	var output: PackedFloat64Array = []
	for i in range(len(filter), len(sample)):
		var weighted_value: float = 0.0
		for k in range(len(filter)):
			weighted_value += sample[i - k] * filter[k]
		output.append(weighted_value)
	return output

func extract_heartrate(fft: PackedFloat64Array, threshold: float = 0.1):
	var max_amplitude_frequency: float = 0
	var max_amplitude: float = 0
	for i in range(len(fft)):
		var frequency: float = 60.0 / len(fft) * i
		if .66 < frequency and frequency < 2.5:
			if fft[i] > max_amplitude and fft[i] > threshold:
				max_amplitude = fft[i]
				max_amplitude_frequency = frequency
	return max_amplitude_frequency * 60

func get_fourier_transform(sample: PackedFloat64Array) -> PackedFloat64Array:
	# The FFT function expects an ambiguous array, as it modifies it with
	# ComplexNumber objects in-place for performance reasons
	var data: Array = []
	for val in sample:
		data.append(val)
	var complex_numbers: Array = fft(data)
	var fft_magnitudes: PackedFloat64Array = []
	for i in range(len(complex_numbers) / 2):
		var num: Complex = complex_numbers[i]
		fft_magnitudes.append(sqrt(num.re ** 2 + num.im ** 2))
	return fft_magnitudes

func fft(amplitudes: Array) -> Array:
	var N = len(amplitudes)
	if N <= 1:
		return amplitudes
	var hN = N / 2
	var even = []
	var odd = []
	# Divide
	even.resize(hN)
	odd.resize(hN)
	for i in range(0, hN):
		even[i] = amplitudes[i * 2]
		odd[i] = amplitudes[i * 2 + 1]
	# And conquer
	even = fft(even)
	odd = fft(odd)
	var a := -2.0 * PI
	for k in range(0, hN):
		if not even[k] is Complex:
			even[k] = Complex.new(even[k], 0)
		if not odd[k] is Complex:
			odd[k] = Complex.new(odd[k], 0)
		var p = k / float(N)
		var t = Complex.new(0, a * p)
		t.cexp(t).mul(odd[k], t)
		amplitudes[k] = even[k].add(t, odd[k])
		amplitudes[k + hN] = even[k].sub(t, even[k])
	return amplitudes

func get_average(arr: PackedFloat64Array) -> float:
	var average: float = 0
	for val in arr:
		average += val
	average /= len(arr)
	return average

func get_standard_deviation(arr: PackedFloat64Array) -> float:
	var average: float = get_average(arr)
	var stdev: float = 0
	for val in arr:
		stdev += pow(val - average, 2)
	return sqrt(stdev / len(arr))
