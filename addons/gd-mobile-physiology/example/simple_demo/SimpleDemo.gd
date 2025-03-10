extends Node

# Sample code using the BreathingRateAlgorithm and HeartRateAlgorithm classes
# Simple and interactive example 

# You may need to resize the window to get the charts to appear correctly

func _ready() -> void:
	%RecordingCover.modulate.a = 0
	%RecordButton.pressed.connect(_on_record)

func _on_record() -> void:
	get_tree().create_tween().tween_property(%RecordingCover, "modulate:a", 1.0, 1.0)
	
	%RecordButton.disabled = true
	%RecordButton.text = "Collecting data..."
	%LengthSlider.editable = false
	
	%HRLabel.text = "-"
	%BRLabel.text = "-"
	
	var sample_size: int = 1024
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
	
	var debug: Dictionary = {}
	var heart_data: Dictionary = HeartRateAlgorithm.Analyze(accelerometer.slice(0, heart_sample_size), gyroscope.slice(0, heart_sample_size), false, {}, true)
	
	var breathing_data: Dictionary = BreathingRateAlgorithm.Analyze(accelerometer.slice(0, breath_sample_size), gyroscope.slice(0, breath_sample_size), false, debug, true)
	
	print(debug)
	
	%HRLabel.text = str(int(heart_data["rate"]))
	%BRLabel.text = str(int(breathing_data["rate"]))
	
	%RecordButton.disabled = false
	%RecordButton.text = "Start measuring"
	%ProgressBar.modulate.a = 0.0
	%LengthSlider.editable = true
	
	get_tree().create_tween().tween_property(%RecordingCover, "modulate:a", 0.0, 0.5)
