class_name HeartRateSensor
extends Node

# Returns heart rate in beats per minute
# For debug purposes, also plots intermediate data
func analyze_data(samples: Array[PackedFloat64Array]) -> float:
	%RawDataX.plot(samples[0].duplicate())
	%RawDataY.plot(samples[1].duplicate())
	%RawDataZ.plot(samples[2].duplicate())
	
	# Step 1) 
	# Use a sliding window average to detrend the samples; Window size
	# chosen arbitrarily 
	SignalHelper.detrend_samples(samples, 128)
	
	# Step 2) 
	# Set mean and variance to 0 (z-scoring)
	SignalHelper.normalize_samples(samples, true) 
	
	# Step 3) 
	# Use a band pass filter (7-13 Hz) to isolate BCG motions
	for i in range(3):
		samples[i] = SignalHelper.apply_fir_filter(samples[i], Filter.BANDPASS_FILTER_BCG)
	
	%FixedDataX.plot(samples[0].duplicate())
	%FixedDataY.plot(samples[1].duplicate())
	%FixedDataZ.plot(samples[2].duplicate())
	
	# Step 4)
	# Aggregate the different axes into one filtered signal
	var aggregated_signal: PackedFloat64Array = []
	for i in range(len(samples[0])):
		aggregated_signal.append(sqrt(pow(samples[0][i], 2) + pow(samples[1][i], 2) + pow(samples[2][i], 2)))
	aggregated_signal = SignalHelper.apply_fir_filter(aggregated_signal, Filter.BANDPASS_FILTER_HR)
	
	%ICA1.plot(aggregated_signal)
	%ICA2.visible = false
	%ICA3.visible = false
	%ICA4.visible = false
	%ICA5.visible = false
	%ICA6.visible = false
	
	# Step 5)
	# Use FFT (using external C# Accord library) to find the strongest frequency within heart rate ranges
	var fft: PackedFloat64Array = AccordWrapper.fft(aggregated_signal, len(aggregated_signal))
	var index: int = SignalHelper.extract_rate(fft, 0.66 * 60, 2.5 * 60)
	var rate: float = 60.0 / len(fft) * index
	
	%FFT.plot(fft)
	
	%RespirationRateLabel.text = "Heart Rate: %.03f Hz, %.01f bpm" % [rate, rate * 60]
	return rate * 60


