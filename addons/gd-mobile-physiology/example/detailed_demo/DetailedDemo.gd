extends Node

# Sample code using the BreathingRateAlgorithm and HeartRateAlgorithm classes
# Plots all steps of the data process

# You may need to resize the window to get the charts to appear correctly

func _ready() -> void:
	%Start.pressed.connect(_on_pressed)

func _on_pressed() -> void:
	test_heart_rate(1024)

func test_heart_rate(sample_size: int) -> void:
	var actual_sample_size: int = HeartRateAlgorithm.GetActualSampleSize(sample_size)
	
	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]
	
	var heart_data: Dictionary = HeartRateAlgorithm.Analyze(accelerometer, gyroscope, true)
	
	plot_debug_info(heart_data)
	%RateLabel.text = "Heart Rate (bpm): " + str(heart_data["rate"]) + ", Confidence: " + str(heart_data["confidence"] * 100) + "%" 

func test_breathing_rate(sample_size: int) -> void:
	var actual_sample_size: int = BreathingRateAlgorithm.GetActualSampleSize(sample_size)
	
	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]
	
	var debug_info: Dictionary = {}
	var breathing_data: Dictionary = BreathingRateAlgorithm.Analyze(accelerometer, gyroscope, false, debug_info, true)
	print(breathing_data)
	
	plot_debug_info(debug_info)
	%RateLabel.text = "Breathing Rate (bpm): " + str(breathing_data["rate"])

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
	
	for i in range(6):
		get_node("%ICA" + str(1 + i)).line_color = Color.REBECCA_PURPLE
		get_node("%ICA" + str(1 + i)).plot(debug_info["ICAOutput%d" % i])
	get_node("%ICA" + str(1 + debug_info["SelectedICAIndex"])).line_color = Color.RED
	
	%ProbabilityDistribution.plot(debug_info["ProbabilityDistribution"].slice(0, len(debug_info["ProbabilityDistribution"]) / 8))
