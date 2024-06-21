class_name Main
extends Node

var state: String = "heart_rate"

func _ready() -> void:
	if state == "heart_rate":
		var sample_size: int = 2048 + len(Filter.BANDPASS_FILTER_BCG) + len(Filter.BANDPASS_FILTER_HR)
		var samples: Array[PackedFloat64Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(sample_size)
		var _heart_rate: float = %HeartRateSensor.analyze_data(samples)
	elif state == "respiration_rate":
		var sample_size: int = 2048 + len(Filter.LOW_PASS_RESPIRATION) * 2
		var samples: Array[PackedFloat64Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(sample_size)
		var _respiration_rate: float = %RespirationRateSensor.analyze_data(samples)
