class_name RespirationRateSensor
extends Node

# Returns respiration rate in breaths per minute
# For debug purposes, also plots intermediate data
func analyze_data(samples: Array[PackedFloat64Array]) -> float:
	%RawDataX.plot(samples[0].duplicate())
	%RawDataY.plot(samples[1].duplicate())
	%RawDataZ.plot(samples[2].duplicate())
	%RawDataX2.plot(samples[3].duplicate())
	%RawDataY2.plot(samples[4].duplicate())
	%RawDataZ2.plot(samples[5].duplicate())
	
	# Step 1) 
	# Use a sliding window average to detrend the samples; Window size
	# chosen arbitrarily 
	SignalHelper.detrend_samples(samples, 128)
	
	# Step 2) 
	# Set mean and variance to 0 (z-scoring)
	SignalHelper.normalize_samples(samples, true) 
	
	# Step 3) 
	# [not in paper] Use a low pass filter to isolate signals < 1 Hz
	for i in range(len(samples)):
		samples[i] = SignalHelper.apply_fir_filter(samples[i], Filter.LOW_PASS_RESPIRATION)
	
	%FixedDataX.plot(samples[0].duplicate())
	%FixedDataY.plot(samples[1].duplicate())
	%FixedDataZ.plot(samples[2].duplicate())
	%FixedDataX2.plot(samples[3].duplicate())
	%FixedDataY2.plot(samples[4].duplicate())
	%FixedDataZ2.plot(samples[5].duplicate())
	
	# Step 4)
	# Run ICA (using external C# Accord library) 
	var ica_result: Array = %ICA.analyze(samples[0], samples[1], samples[2], samples[3], samples[4], samples[5], len(samples[0]))
	var ica_signals: Array[PackedFloat64Array] = []
	for signal_idx in range(6):
		var output: PackedFloat64Array = []
		for sample_idx in range(len(samples[0])):
			output.append(ica_result[sample_idx][signal_idx])
		var filtered_ica_signal: PackedFloat64Array = SignalHelper.apply_fir_filter(output, Filter.LOW_PASS_RESPIRATION)
		ica_signals.append(filtered_ica_signal)
	
	# Step 5)
	# Use FFT to find the strongest signal within respiration rate ranges
	var maximum_confidence: float = 0.0
	var cleanest_rate: float = 0.0
	var cleanest_index: int = 0
	for i in range(len(ica_signals)):
		var ica_signal: PackedFloat64Array = ica_signals[i]
		var fft: PackedFloat64Array = %ICA.fft(ica_signal, len(ica_signal))
		var index: int = extract_respiration_rate(fft, 5, 18)
		
		if fft[index] > maximum_confidence:
			cleanest_index = i
			maximum_confidence = fft[index]
			cleanest_rate = 60.0 / len(fft) * index
			%FFT.plot(fft.slice(0, int(len(fft) / 6)))
	
	get_node("%ICA" + str(cleanest_index + 1)).line_color = Color("#FF0032")
	
	%ICA1.plot(ica_signals[0].duplicate())
	%ICA2.plot(ica_signals[1].duplicate())
	%ICA3.plot(ica_signals[2].duplicate())
	%ICA4.plot(ica_signals[3].duplicate())
	%ICA5.plot(ica_signals[4].duplicate())
	%ICA6.plot(ica_signals[5].duplicate())
	
	%RespirationRateLabel.text = "Respiration Rate: %.03f Hz, %.01f bpm" % [cleanest_rate, cleanest_rate * 60]
	return cleanest_rate * 60

# Returns the index that corresponds to the strongest signal in the FFT
# within respiration rate ranges
func extract_respiration_rate(fft: PackedFloat64Array, min_bpm: float, max_bpm: float) -> int:
	var max_amplitude: float = 0
	var output_index: int = -1
	for i in range(len(fft)):
		var frequency: float = 60.0 / len(fft) * i
		if min_bpm/60.0 < frequency and frequency < max_bpm/60.0 and fft[i] > max_amplitude:
			max_amplitude = fft[i]
			output_index = i
	return output_index

