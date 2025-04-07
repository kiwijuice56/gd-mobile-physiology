extends Node

# Sample code using the BreathingRateAlgorithm and HeartRateAlgorithm classes
# Plots all steps of the data process

# You may need to resize the window to get the charts to appear correctly

var sample_size: int = 2048

func _ready() -> void:
	%StartHeart.pressed.connect(_on_pressed_heart)
	%StartBreathing.pressed.connect(_on_pressed_breathing)
	%SampleSizePicker.item_selected.connect(_on_sample_size_selected)

func _on_sample_size_selected(index: int) -> void:
	sample_size = [512, 1024, 2048][index]

func _on_pressed_heart() -> void:
	test_heart_rate(sample_size)

func _on_pressed_breathing() -> void:
	test_breathing_rate(sample_size)

func test_heart_rate(sample_size: int) -> void:
	var actual_sample_size: int = HeartRateAlgorithm.GetActualSampleSize(sample_size)
	
	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]
	
	var heart_data: Dictionary = HeartRateAlgorithm.Analyze(accelerometer, gyroscope, true)
	
	plot_debug_info(heart_data)
	%RateLabel.text = "Heart Rate (bpm): %.2f, Confidence: %.2f, Kurtosis: %.2f" % [heart_data["rate"], heart_data["confidence"], heart_data["kurtosis"]]

func test_breathing_rate(sample_size: int) -> void:
	var actual_sample_size: int = BreathingRateAlgorithm.GetActualSampleSize(sample_size)
	
	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]
	
	var breathing_data: Dictionary = BreathingRateAlgorithm.Analyze(accelerometer, gyroscope, true)
	
	plot_debug_info(breathing_data)
	%RateLabel.text = "Breathing Rate (bpm): %.2f, Confidence: %.2f, Kurtosis: %.2f" % [breathing_data["rate"], breathing_data["confidence"], breathing_data["kurtosis"]]

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
		if "ICAOutput%d" % i in debug_info:
			get_node("%ICA" + str(1 + i)).line_color = Color.REBECCA_PURPLE
			get_node("%ICA" + str(1 + i)).plot(debug_info["ICAOutput%d" % i])
	get_node("%ICA" + str(1 + debug_info["SelectedICAIndex"])).line_color = Color.RED
	
	%ProbabilityDistribution.plot(debug_info["ProbabilityDistribution"].slice(0, len(debug_info["ProbabilityDistribution"]) / 8))
