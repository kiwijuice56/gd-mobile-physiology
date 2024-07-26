# gd-mobile-physiology

A library that processes smartphone data into physiological signals, such as heart and breathing rate.

## Installation
Note: This project requires the Mono version of Godot (last tested on version `4.2.2.stable.mono`).
1) Copy the `addons/gd-mobile-physiology/` folder into your project's `addons` folder.
2) Include the `Accord.NET` dependency in your build settings:
    - (If your project DOES NOT have a `.csproj` file): Copy `heart-rate-sensor.csproj` from the download folder
into the root of your project. Update `dotnet/project/assembly_name` in Project Settings to `heart-rate-sensor` and reload the project.
    - (If your project DOES have a `.csproj` file): Add the following dependency into the `Project` block of your `.csproj` file:
```
<ItemGroup>
	<PackageReference Include="Accord" Version="3.8.0" />
	<PackageReference Include="Accord.Statistics" Version="3.8.0" />
</ItemGroup>
```
3) Enable the plugin in Project Settings.

## Usage
To estimate heart/respiration rate, you must pass an `Array[Vector3]` of accelerometer samples and an `Array[Vector3]` of gyroscope samples into the static `Analyze` function of
`HeartRateAlgorithm` or `RespirationRateAlgorithm`.

Each item in each of the `Array[Vector3]` objects should be a measurement of the `get_accelerometer` and `get_gyroscope` methods from `Input`,
with a sampling rate of 60 Hz (once per physics process frame). See `addons/gd-mobile-physiology/sampling/Sampler.gd`
for an example implementation.

The sampling size is limited to powers of 2 (due to the algorithms utilizing FFT) along with some extra samples that are
discarded while pre-processing. To know the exact amount of samples you must feed into the algorithm, call the `GetActualSampleSize` methods
with the closest power of 2 that you want as the length of the final output signal. Higher sample sizes increase the accuracy of the prediction.

The `parallel` parameter will toggle multithreading, which may be a performance boost on newer devices.

The `debug` parameter will enable logging of intermediate signals during analysis, which may be useful to see where the algorithm is going wrong.
If `true`, the `debug_info` `Dictionary` that was passed in will be filled with labeled `String : Array[float]` pairs. Note that the `debug_info`
`Dictionary` must be passed in regardless of whether `debug` is true or not.

## Example Script
See `addons/gd-mobile-physiology/example/Main.gd` for the full example script.

```python
func test_heart_rate(sample_size: int) -> void:
	var actual_sample_size: int = HeartRateAlgorithm.GetActualSampleSize(sample_size)

	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]

	var debug_info: Dictionary = {}
	var heart_rate_bpm: float = HeartRateAlgorithm.Analyze(accelerometer, gyroscope, false, debug_info, true)

	print(heart_rate_bpm)
	print(debug_info)
```
