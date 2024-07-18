extends Node

# Sample code using the BreathingRateAlgorithm class

func _ready() -> void:
	var sample_size: int = 2048 + len(Filter.LOW_PASS_RESPIRATION) 
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(sample_size)
	
	var debug_info: Dictionary = {}
	var start: int = Time.get_ticks_usec()
	var breathing_rate: float = BreathingRateAlgorithm.Analyze(samples[0], samples[1], false, debug_info, true)
	var end: int = Time.get_ticks_usec()
	
	plot_debug_info(debug_info)
	%RateLabel.text = "Breathing Rate (bpm): " + str(breathing_rate) + ", Time (s): " + str((end - start) / 1000000.)

func plot_debug_info(debug_info: Dictionary) -> void:
	%RawDataX.plot(debug_info["RawAccelX"])
	%RawDataY.plot(debug_info["RawAccelY"])
	%RawDataZ.plot(debug_info["RawAccelZ"])
	%RawDataX2.plot(debug_info["RawGyroX"])
	%RawDataY2.plot(debug_info["RawGyroY"])
	%RawDataZ2.plot(debug_info["RawGyroZ"])
	
	%FixedDataX.plot(debug_info["PreprocessedAccelX"])
	%FixedDataY.plot(debug_info["PreprocessedAccelY"])
	%FixedDataZ.plot(debug_info["PreprocessedAccelZ"])
	%FixedDataX2.plot(debug_info["PreprocessedGyroX"])
	%FixedDataY2.plot(debug_info["PreprocessedGyroY"])
	%FixedDataZ2.plot(debug_info["PreprocessedGyroZ"])
	
	get_node("%ICA" + str(1 + debug_info["SelectedICAIndex"])).line_color = Color.RED
	
	%ICA1.plot(debug_info["ICAOutput0"])
	%ICA2.plot(debug_info["ICAOutput1"])
	%ICA3.plot(debug_info["ICAOutput2"])
	%ICA4.plot(debug_info["ICAOutput3"])
	%ICA5.plot(debug_info["ICAOutput4"])
	%ICA6.plot(debug_info["ICAOutput5"])
	
	%FFT.plot(debug_info["FFT"].slice(0, len(debug_info["FFT"]) / 6))
