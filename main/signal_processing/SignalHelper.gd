class_name SignalHelper
extends Node

static func detrend_samples(samples: Array[PackedFloat64Array], window_size: int) -> void:
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

static func normalize_samples(samples: Array[PackedFloat64Array], scale: bool) -> void:
	for sample in samples:
		var average: float = get_average(sample)
		for i in range(len(sample)):
			sample[i] -= average
		
		if scale:
			var stdev: float = get_standard_deviation(sample)
			for i in range(len(sample)):
				sample[i] /= stdev

static func apply_fir_filter(sample: PackedFloat64Array, filter: PackedFloat64Array) -> PackedFloat64Array:
	var output: PackedFloat64Array = []
	for i in range(len(filter), len(sample)):
		var weighted_value: float = 0.0
		for k in range(len(filter)):
			weighted_value += sample[i - k] * filter[k]
		output.append(weighted_value)
	return output

static func apply_hanning_window(samples: PackedFloat64Array) -> PackedFloat64Array:
	var output: PackedFloat64Array = []
	var N: int = len(samples)
	for i in range(N):
		var m = 0.5 * (1.0 - cos(2 * PI * i / (N - 1)))
		output.append(samples[i] * m)
	return output

static func get_fourier_transform(sample: PackedFloat64Array) -> PackedFloat64Array:
	var complex_data: Array[Complex] = []
	for val in sample:
		complex_data.append(Complex.new(val, 0.0))
	var complex_result: Array[Complex] = fft_helper(complex_data)
	var fft_magnitudes: PackedFloat64Array = []
	for i in range(len(complex_result) / 2):
		var num: Complex = complex_result[i]
		fft_magnitudes.append(sqrt(num.re ** 2 + num.im ** 2))
	return fft_magnitudes

static func fft_helper(a: Array[Complex]) -> Array[Complex]:
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
	a0 = fft_helper(a0)
	a1 = fft_helper(a1)
	var ang: float = -2.0 * PI / N 
	var w: Complex = Complex.new(1.0, 0.0)
	var wn: Complex = Complex.new(cos(ang), sin(ang))
	for i in range(0, hN):
		a[i] = w.mul(a1[i]).add(a0[i])
		a[i + N / 2] = a0[i].sub(w.mul(a1[i]))
		w = w.mul(wn)
	return a

static func get_average(arr: PackedFloat64Array) -> float:
	var average: float = 0
	for val in arr:
		average += val
	average /= len(arr)
	return average

static func get_standard_deviation(arr: PackedFloat64Array) -> float:
	var average: float = get_average(arr)
	var stdev: float = 0
	for val in arr:
		stdev += pow(val - average, 2)
	return sqrt(stdev / len(arr))

static func extract_rate(fft: PackedFloat64Array, min_bpm: float, max_bpm: float) -> int:
	var max_amplitude: float = 0
	var output_index: int = -1
	for i in range(len(fft)):
		var frequency: float = 60.0 / len(fft) * i
		if min_bpm/60.0 < frequency and frequency < max_bpm/60.0 and fft[i] > max_amplitude:
			max_amplitude = fft[i]
			output_index = i
	return output_index
