class_name Demo extends Node

# Sample code using the BreathingRateAlgorithm and HeartRateAlgorithm classes
# Plots all steps of the data process

# You may need to resize the window to get the charts to appear correctly

enum Measurement { Heart, Breathing, Wiggle }

var sample_size: int = 2048

var heart_data: Dictionary = {}
var breathing_data: Dictionary = {}
var wiggle_data: Dictionary = {}

var shown_measurement: Measurement = Measurement.Heart
var showing_detailed: bool = false

func _ready() -> void:
	%ShowHeartButton.pressed.connect(_on_pressed_heart)
	%ShowBreathingButton.pressed.connect(_on_pressed_breathing)
	%ShowWiggleButton.pressed.connect(_on_pressed_wiggle)
	
	%RecordingCover.modulate.a = 0
	%RecordButton.pressed.connect(_on_record)
	
	%ToggleButton.pressed.connect(_on_toggle)
	
	%LengthSlider.editable = true
	%LengthSlider.value_changed.connect(_on_length_changed)
	_on_length_changed(%LengthSlider.value)
	
	show_interface()

func _on_length_changed(value: float) -> void:
	sample_size = int(value * 60)
	%TimeLabel.text = str(int(value))

func _on_toggle() -> void:
	showing_detailed = not showing_detailed
	show_interface()

func _on_record() -> void:
	%StartPlayer.play()
	
	get_tree().create_tween().tween_property(%RecordingCover, "modulate:a", 1.0, 1.0)
	
	%RecordButton.disabled = true
	%RecordButton.text = "Collecting data..."
	%LengthSlider.editable = false
	
	%HRLabel.text = "-"
	%BRLabel.text = "-"
	%WILabel.text = "-"
	
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
	wiggle_data = WiggleIndexAlgorithm.Analyze(accelerometer, true)
	
	%HRLabel.text = str(int(heart_data["rate"]))
	%BRLabel.text = str(int(breathing_data["rate"]))
	%WILabel.text = "%.1f" % [wiggle_data["wiggle"]]
	
	match shown_measurement:
		Measurement.Heart:
			plot_debug_info(heart_data)
		Measurement.Breathing:
			plot_debug_info(breathing_data)
		Measurement.Wiggle:
			plot_debug_info(wiggle_data)
	
	%RecordButton.disabled = false
	%RecordButton.text = "Start measuring"
	%ProgressBar.modulate.a = 0.0
	%LengthSlider.editable = true
	
	get_tree().create_tween().tween_property(%RecordingCover, "modulate:a", 0.0, 0.5)
	
	Input.vibrate_handheld(1000, 0.5)
	%EndPlayer.play()

func _on_pressed_heart() -> void:
	shown_measurement = Measurement.Heart
	plot_debug_info(heart_data)

func _on_pressed_breathing() -> void:
	shown_measurement = Measurement.Breathing
	plot_debug_info(breathing_data)

func _on_pressed_wiggle() -> void:
	shown_measurement = Measurement.Wiggle
	plot_debug_info(wiggle_data)

func show_interface() -> void:
	%DetailedInterface.visible = showing_detailed
	%BasicInterface.visible = not showing_detailed

func plot_debug_info(debug_info: Dictionary) -> void:
	%RateLabel.text = " "
	%IndexLabel.text = " "
	
	# Show and hide the correct plots
	if shown_measurement == Measurement.Wiggle:
		%ProcessedContainer.visible = false
		%IcaContainer.visible = false
		%ProbabilityContainer.visible = false
		%TotalWiggleContainer.visible = true
		%DerivativeContainer.visible = true
		%SmoothedContainer.visible = true
		
		%RawDataX2.visible = false
		%RawDataY2.visible = false
		%RawDataZ2.visible = false
	else:
		%ProcessedContainer.visible = true
		%IcaContainer.visible = true
		%ProbabilityContainer.visible = true
		%TotalWiggleContainer.visible = false
		%DerivativeContainer.visible = false
		%SmoothedContainer.visible = false
		
		%RawDataX2.visible = true
		%RawDataY2.visible = true
		%RawDataZ2.visible = true
	
	# Skip plotting if there is no data
	if debug_info.size() == 0:
		return
	
	# Show the result
	match shown_measurement:
		Measurement.Heart:
			%RateLabel.text = "Heart Rate (bpm): %.2f, Confidence: %.2f, Kurtosis: %.2f" % [heart_data["rate"], heart_data["confidence"], heart_data["kurtosis"]]
		Measurement.Breathing:
			%RateLabel.text = "Breathing Rate (bpm): %.2f, Confidence: %.2f, Kurtosis: %.2f" % [breathing_data["rate"], breathing_data["confidence"], breathing_data["kurtosis"]]
		Measurement.Wiggle:
			%IndexLabel.text = "Wobble Index: %.4f" % [wiggle_data["wiggle"]]
	
	%RawDataX.plot(debug_info["RawAccelX"])
	%RawDataY.plot(debug_info["RawAccelY"])
	%RawDataZ.plot(debug_info["RawAccelZ"])
	
	if shown_measurement == Measurement.Wiggle:
		%ProcessedContainer.visible = false
		%IcaContainer.visible = false
		%ProbabilityContainer.visible = false
		%TotalWiggleContainer.visible = true
		
		%DerivativeDataX.plot(debug_info["DerivativeAccelX"])
		%DerivativeDataY.plot(debug_info["DerivativeAccelY"])
		%DerivativeDataZ.plot(debug_info["DerivativeAccelZ"])
		
		%SmoothDataX.plot(debug_info["SmoothedX"])
		%SmoothDataY.plot(debug_info["SmoothedY"])
		%SmoothDataZ.plot(debug_info["SmoothedZ"])
		
		%WiggleData.plot(debug_info["WiggleTotal"])
	else:
		%ProcessedContainer.visible = true
		%IcaContainer.visible = true
		%ProbabilityContainer.visible = true
		%TotalWiggleContainer.visible = false
		
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
