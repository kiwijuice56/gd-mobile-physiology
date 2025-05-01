class_name Demo extends Node

# Sample code using the BreathingRateAlgorithm and HeartRateAlgorithm classes
# Plots all steps of the data process

# You may need to resize the window to get the charts to appear correctly

var sample_size: int = 2048
var heart_data: Dictionary = {}
var breathing_data: Dictionary = {}
var showing_heart_data: bool = true
var showing_detailed: bool = false

func _ready() -> void:
	%ShowHeartButton.pressed.connect(_on_pressed_heart)
	%ShowBreathingButton.pressed.connect(_on_pressed_breathing)
	
	%RecordingCover.modulate.a = 0
	%RecordButton.pressed.connect(_on_record)
	
	%ToggleButton.pressed.connect(_on_toggle)
	
	%LengthSlider.editable = true
	%LengthSlider.value_changed.connect(_on_length_changed)
	_on_length_changed(%LengthSlider.value)
	
	show_interface()

func _on_length_changed(value: float) -> void:
	sample_size = int(value * 60)

func _on_toggle() -> void:
	showing_detailed = not showing_detailed
	show_interface()

func _on_record() -> void:
	get_tree().create_tween().tween_property(%RecordingCover, "modulate:a", 1.0, 1.0)
	
	%RecordButton.disabled = true
	%RecordButton.text = "Collecting data..."
	%LengthSlider.editable = false
	
	%HRLabel.text = "-"
	%BRLabel.text = "-"
	
	var heart_sample_size: int = HeartRateAlgorithm.GetActualSampleSize(sample_size)
	var breath_sample_size: int = BreathingRateAlgorithm.GetActualSampleSize(sample_size)
	var actual_sample_size: int = max(heart_sample_size, breath_sample_size)
	
	%ProgressBar.value = 0
	
	var tween: Tween = get_tree().create_tween()
	tween.tween_property(%ProgressBar, "value", 1.0, actual_sample_size / 60.0)
	
	%ProgressBar.modulate.a = 1.0
	
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1] 
	
	heart_data = HeartRateAlgorithm.Analyze(accelerometer.slice(0, heart_sample_size), gyroscope.slice(0, heart_sample_size), true)
	breathing_data = BreathingRateAlgorithm.Analyze(accelerometer.slice(0, breath_sample_size), gyroscope.slice(0, breath_sample_size), true)
	
	%HRLabel.text = str(int(heart_data["rate"]))
	%BRLabel.text = str(int(breathing_data["rate"]))
	
	if showing_heart_data:
		plot_debug_info(heart_data)
	else:
		plot_debug_info(breathing_data)
	
	%RecordButton.disabled = false
	%RecordButton.text = "Start measuring"
	%ProgressBar.modulate.a = 0.0
	%LengthSlider.editable = true
	
	get_tree().create_tween().tween_property(%RecordingCover, "modulate:a", 0.0, 0.5)

func _on_pressed_heart() -> void:
	showing_heart_data = true
	plot_debug_info(heart_data)

func _on_pressed_breathing() -> void:
	showing_heart_data = false
	plot_debug_info(breathing_data)

func show_interface() -> void:
	%DetailedInterface.visible = showing_detailed
	%BasicInterface.visible = not showing_detailed

func test_heart_rate(sample_size: int) -> void:
	var actual_sample_size: int = HeartRateAlgorithm.GetActualSampleSize(sample_size)
	
	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]
	
	heart_data = HeartRateAlgorithm.Analyze(accelerometer, gyroscope, true)

func test_breathing_rate(sample_size: int) -> void:
	var actual_sample_size: int = BreathingRateAlgorithm.GetActualSampleSize(sample_size)
	
	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]
	
	breathing_data = BreathingRateAlgorithm.Analyze(accelerometer, gyroscope, true)

func plot_debug_info(debug_info: Dictionary) -> void:
	%RateLabel.text = " "
	
	if debug_info.size() == 0:
		return
	
	if showing_heart_data:
		%RateLabel.text = "Heart Rate (bpm): %.2f, Confidence: %.2f, Kurtosis: %.2f" % [heart_data["rate"], heart_data["confidence"], heart_data["kurtosis"]]
	else:
		%RateLabel.text = "Breathing Rate (bpm): %.2f, Confidence: %.2f, Kurtosis: %.2f" % [breathing_data["rate"], breathing_data["confidence"], breathing_data["kurtosis"]]
	
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
