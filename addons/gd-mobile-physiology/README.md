# gd-mobile-physiology

A library that processes smartphone data into physiological signals, such as heart and breathing rate.
Also includes wobble measurement to detect brain trauma.

The intended usage is to record data by holding the device up to your chest. Good readings
require about 15-30 seconds of recording. The wobble recording should be done standing up.

Requires Godot 4.4.stable.mono or later.

## Installation
Note: This project requires the Mono version of Godot.
1) Copy the `addons/gd-mobile-physiology/` folder into your project's `addons` folder.
2) Include the `Accord.NET` dependency in your build settings:
	- (If your project DOES NOT have a `.csproj` file): Copy the `heart-rate-sensor.csproj` and `heart-rate-sensor.sln` files from the download folder
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
### Heart and breathing
To estimate heart/respiration rate, you must pass an `Array[Vector3]` of accelerometer samples and an `Array[Vector3]` of gyroscope samples into the static `Analyze` function of
`HeartRateAlgorithm` or `RespirationRateAlgorithm`.

Both methods return a dictionary with the following entries:
- "rate": Estimated rate in beats per minute.
- "confidence": The confidence that the given rate is the actual rate. Generally, the lower the magnitude, the weaker the pulse waveform.
- "kurtosis": the "peak"-ness of the pulse waveform in the frequency domain. Generally, values >> 0 indicate a strong pulse waveform.

Each item in each of the `Array[Vector3]` objects should be a measurement of the `get_accelerometer` and `get_gyroscope` methods from `Input`,
with a sampling rate of 60 Hz (once per physics process frame). See `addons/gd-mobile-physiology/sampling/Sampler.gd`
for an example implementation.

Both algorithms require some extra samples that are discarded while pre-processing. To know the exact amount of samples you must feed into the algorithm, call the `GetActualSampleSize` methods
with the amount of real samples you want. Higher sample sizes increase the accuracy of the prediction.

The `debugOutput` parameter will enable logging of intermediate signals during analysis, which may be useful to see where the algorithm is going wrong.
These debug values are returned in the output dictionary.

### Wobble/wiggle index
The `WiggleIndexAlgorithm` is similar to the previous two algorithms, but only requires accelerometer samples.

## Example 
See `addons/gd-mobile-physiology/example/Demo.gd` for the full example script. 

```python
func test_heart_rate(sample_size: int) -> void:
	var actual_sample_size: int = HeartRateAlgorithm.GetActualSampleSize(sample_size)

	# Array of Array[Vector3] (accelerometer and gyroscope samples, respectively)
	var samples: Array[Array] = await %Sampler.get_accelerometer_and_gyroscope_samples(actual_sample_size)
	var accelerometer: Array[Vector3] = samples[0]
	var gyroscope: Array[Vector3] = samples[1]

	var heart_data: Dictionary = HeartRateAlgorithm.Analyze(accelerometer, gyroscope, true)

	print("heart rate (bpm): ", heart_data["rate"])
```
