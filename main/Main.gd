class_name Main
extends Node

func _ready() -> void:
	# Extra samples are needed, as some are lost during a filter pass
	var sample_size: int = 2048 + len(Filter.LOW_PASS_RESPIRATION) * 2
	var samples: Array[PackedFloat64Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(sample_size)
	var _respiration_rate: float = %RespirationRateSensor.analyze_data(samples)
