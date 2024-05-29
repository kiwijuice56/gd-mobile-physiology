class_name RespirationRateSensor
extends Node

var acclerometer_sample_x: PackedFloat64Array
var acclerometer_sample_y: PackedFloat64Array
var acclerometer_sample_z: PackedFloat64Array

var sample_count: int 
var frame: int = 0

func _ready() -> void:
	set_physics_process(false)
	
	start_detection(1024)

func _physics_process(_delta: float) -> void:
	var sample: Vector3 = Input.get_accelerometer()
	
	acclerometer_sample_x.append(sample.x)
	acclerometer_sample_y.append(sample.y)
	acclerometer_sample_z.append(sample.z)
	
	if frame % 64 == 0:
		%RawDataX.plot(acclerometer_sample_x)
		%RawDataY.plot(acclerometer_sample_y)
		%RawDataZ.plot(acclerometer_sample_z)
	frame += 1
	
	if len(acclerometer_sample_x) > sample_count:
		analyze_data([acclerometer_sample_x.duplicate(), acclerometer_sample_y.duplicate(), acclerometer_sample_z.duplicate()])
		
		acclerometer_sample_x = acclerometer_sample_x.slice(256)
		acclerometer_sample_y = acclerometer_sample_y.slice(256)
		acclerometer_sample_z = acclerometer_sample_z.slice(256)

func start_detection(max_sample_count: int) -> void:
	set_physics_process(true)
	sample_count = max_sample_count

func analyze_data(samples: Array[PackedFloat64Array]) -> void:
	detrend_samples(samples, 128)
	normalize_samples(samples, true) # z-scoring
	
	%FixedDataX.plot(samples[0].duplicate())
	%FixedDataY.plot(samples[1].duplicate())
	%FixedDataZ.plot(samples[2].duplicate())
	
	# apply ICA
	# extract data

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

func normalize_samples(samples: Array[PackedFloat64Array], scale: bool) -> void:
	for sample in samples:
		var average: float = get_average(sample)
		for i in range(len(sample)):
			sample[i] -= average
		
		if scale:
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

func get_fourier_transform(sample: PackedFloat64Array) -> PackedFloat64Array:
	var complex_data: Array[Complex] = []
	for val in sample:
		complex_data.append(Complex.new(val, 0.0))
	var complex_result: Array[Complex] = fft(complex_data)
	var fft_magnitudes: PackedFloat64Array = []
	for i in range(len(complex_result) / 2):
		var num: Complex = complex_result[i]
		fft_magnitudes.append(sqrt(num.re ** 2 + num.im ** 2))
	return fft_magnitudes

func fft(a: Array[Complex]) -> Array[Complex]:
	var N: int = len(a)
	if N <= 1:
		return a
	var hN: int = N / 2
	var a0: Array[Complex] = []
	var a1: Array[Complex]  = []
	a0.resize(hN)
	a1.resize(hN)
	for i in range(0, hN):
		a0[i] = a[i * 2]
		a1[i] = a[i * 2 + 1]
	a0 = fft(a0)
	a1 = fft(a1)
	var ang: float = -2.0 * PI / N 
	var w: Complex = Complex.new(1.0, 0.0)
	var wn: Complex = Complex.new(cos(ang), sin(ang))
	for i in range(0, hN):
		a[i] = w.mul(a1[i]).add(a0[i])
		a[i + N / 2] = a0[i].sub(w.mul(a1[i]))
		w = w.mul(wn)
	return a

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
