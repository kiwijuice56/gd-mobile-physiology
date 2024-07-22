extends Node

# Sample code using the BreathingRateAlgorithm class

func _ready() -> void:
	test_breathing_rate(2048)

func test_heart_rate(sample_size: int) -> void:
	var actual_sample_size: int = HeartRateAlgorithm.GetActualSampleSize(sample_size)
	
	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]
	
	var debug_info: Dictionary = {}
	var heart_rate_bpm: float = HeartRateAlgorithm.Analyze(accelerometer, gyroscope, false, debug_info, true)
	
	plot_debug_info(debug_info, false)
	%RateLabel.text = "Heart Rate (bpm): " + str(heart_rate_bpm) 

func test_breathing_rate(sample_size: int) -> void:
	var actual_sample_size: int = BreathingRateAlgorithm.GetActualSampleSize(sample_size)
	
	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]
	
	var debug_info: Dictionary = {}
	var breathing_rate_bpm: float = BreathingRateAlgorithm.Analyze(accelerometer, gyroscope, false, debug_info, true)
	
	plot_debug_info(debug_info, true)
	%RateLabel.text = "Breathing Rate (bpm): " + str(breathing_rate_bpm)


func plot_debug_info(debug_info: Dictionary, has_ica: bool) -> void:
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
	
	# Heart/respiration plot differently for the bottom left graph
	if has_ica:
		%ICATitle.text = "ICA Output"
		
		get_node("%ICA" + str(1 + debug_info["SelectedICAIndex"])).line_color = Color.RED
		
		%ICA1.plot(debug_info["ICAOutput0"])
		%ICA2.plot(debug_info["ICAOutput1"])
		%ICA3.plot(debug_info["ICAOutput2"])
		%ICA4.plot(debug_info["ICAOutput3"])
		%ICA5.plot(debug_info["ICAOutput4"])
		%ICA6.plot(debug_info["ICAOutput5"])
	else:
		%ICATitle.text = "Combined Signal"
		
		%ICA1.plot(debug_info["CombinedSignal"])
		%ICA2.visible = false
		%ICA3.visible = false
		%ICA4.visible = false
		%ICA5.visible = false
		%ICA6.visible = false
	
	%FFT.plot(debug_info["FFT"].slice(0, len(debug_info["FFT"]) / 6))
