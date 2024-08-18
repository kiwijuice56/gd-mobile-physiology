using Godot;
using System;

using System.Threading.Tasks;

// Calculates breathing rate from gyroscope and accelerometer samples

[GlobalClass]
public partial class BreathingRateAlgorithm : GodotObject {
	private const int DetrendWindowSize = 128;
	
	// Accepts accelerometer and gyroscope samples and returns a dictionary with the entries:
	// - "rate": Estimated breathing rate in beats per minute.
	// - "magnitude": The strength of the given rate in the frequency domain. This value varies based on sample size, but you can use this value to filter out noise/bad readings. Generally, the lower the magnitude, the weaker the signal.
	// Algorithm modified from `BioPhone: Physiology Monitoring from Peripheral Smartphone Motions: Javier Hernandez, Daniel J. McDuff and Rosalind W. Picard.`
	// If `debug` is true, `debugInfo` will be filled with the following items:
	// - Raw[Accel/Gyro][X/Y/Z]: Signals before any processing
	// - Preprocessed[Accel/Gyro][X/Y/Z]: Signals after basic processing
	// - ICAOutput[0/1/2/3/4/5]: Output of independent component analysis in random order
	// - SelectedICAIndex: The ICA signal that was used for determining breathing rate
	// - FFT: The Fourier transform for the selected ICA signal
	public static Godot.Collections.Dictionary Analyze(Godot.Collections.Array<Vector3> accel, Godot.Collections.Array<Vector3> gyro, bool parallel, Godot.Collections.Dictionary debugInfo, bool debug) {
		int sampleSize = accel.Count;
		
		// Load data into arrays
		double[][] data = new double[6][];
		for (int i = 0; i < 6; i++) {
			data[i] = new double[sampleSize];
		}
		for (int i = 0; i < sampleSize; i++) {
			data[0][i] = accel[i].X;
			data[1][i] = accel[i].Y;
			data[2][i] = accel[i].Z;
			data[3][i] = gyro[i].X;
			data[4][i] = gyro[i].Y;
			data[5][i] = gyro[i].Z;
		}
		
		if (debug) {
			debugInfo["RawAccelX"] = new Godot.Collections.Array<double>(data[0]);
			debugInfo["RawAccelY"] = new Godot.Collections.Array<double>(data[1]);
			debugInfo["RawAccelZ"] = new Godot.Collections.Array<double>(data[2]);
			debugInfo["RawGyroX"] = new Godot.Collections.Array<double>(data[3]);
			debugInfo["RawGyroY"] = new Godot.Collections.Array<double>(data[4]);
			debugInfo["RawGyroZ"] = new Godot.Collections.Array<double>(data[5]);
		}
		
		// Note: Signal will have extraneous samples at the end due to FIR filtering.
		// These are removed as the array is recreated in the ICA step
		if (parallel) {
			Parallel.For(0, 6, delegate(int i) {
				PreprocessSignal(data[i]);
			});
		} else {
			for (int i = 0; i < 6; i++) {
				PreprocessSignal(data[i]);
			}
		}
		
		if (debug) {
			debugInfo["PreprocessedAccelX"] = new Godot.Collections.Array<double>(data[0]);
			debugInfo["PreprocessedAccelY"] = new Godot.Collections.Array<double>(data[1]);
			debugInfo["PreprocessedAccelZ"] = new Godot.Collections.Array<double>(data[2]);
			debugInfo["PreprocessedGyroX"] = new Godot.Collections.Array<double>(data[3]);
			debugInfo["PreprocessedGyroY"] = new Godot.Collections.Array<double>(data[4]);
			debugInfo["PreprocessedGyroZ"] = new Godot.Collections.Array<double>(data[5]);
		}
		
		// Run ICA (using external C# Accord library) 
		data = SignalHelper.IndependentComponentAnalysis(data, sampleSize - LowPassRespirationFilter.Length);
		
		if (debug) {
			debugInfo["ICAOutput0"] = new Godot.Collections.Array<double>(data[0]);
			debugInfo["ICAOutput1"] = new Godot.Collections.Array<double>(data[1]);
			debugInfo["ICAOutput2"] = new Godot.Collections.Array<double>(data[2]);
			debugInfo["ICAOutput3"] = new Godot.Collections.Array<double>(data[3]);
			debugInfo["ICAOutput4"] = new Godot.Collections.Array<double>(data[4]);
			debugInfo["ICAOutput5"] = new Godot.Collections.Array<double>(data[5]);
		}
		
		// Run FFT (using external C# Accord library) to find the strongest signal within respiration rate ranges
		if (parallel) {
			Parallel.For(0, 6, delegate(int i) {
				data[i] = SignalHelper.FastFourierTransform(data[i], data[i].Length);
			});
		} else {
			for (int i = 0; i < 6; i++) {
				data[i] = SignalHelper.FastFourierTransform(data[i], data[i].Length);
			}
		}
		
		if (debug) {
			debugInfo["SelectedICAIndex"] = 0; 
			debugInfo["FFT"] = new Godot.Collections.Array<double>();
		} 
		
		double maxConfidence = 0.0;
		double maxConfidenceFrequency = 0.0;
		for (int i = 0; i < 6; i++) {
			int index = SignalHelper.ExtractRate(data[i], 8.0, 45.0);
			if (data[i][index] >= maxConfidence) {
				maxConfidence = data[i][index];
				maxConfidenceFrequency = 60.0 / data[i].Length * index;
				if (debug) {
					debugInfo["SelectedICAIndex"] = i; 
					debugInfo["FFT"] = new Godot.Collections.Array<double>(data[i]);
				} 
			}
		}
		return new Godot.Collections.Dictionary
		{
			{"rate", maxConfidenceFrequency * 60.0},
			{"magnitude", maxConfidence},
		};
	}
	
	private static void PreprocessSignal(double[] signal) {
		// Use a sliding window average to detrend the samples
		double[] detrendedSignal = SignalHelper.Detrend(signal, DetrendWindowSize);
		
		// Set mean and variance to 0 (z-scoring)
		SignalHelper.Normalize(detrendedSignal);
		
		//  [not in paper] Use a low pass filter to isolate signals < 1 Hz
		double[] filteredSignal = SignalHelper.ApplyFirFilter(detrendedSignal, LowPassRespirationFilter);
		filteredSignal.CopyTo(signal, 0);
	}
	
	private static void FrequencyDomain(double[] ica_signal) {
		double[] fft = SignalHelper.FastFourierTransform(ica_signal, ica_signal.Length);
		fft.CopyTo(ica_signal, 0);
	}
	
	public static int GetActualSampleSize(int outputSampleSize) {
		return outputSampleSize + LowPassRespirationFilter.Length;
	}
	
	public static readonly double[] LowPassRespirationFilter = {
	  -0.0045644380812448395,
	  -0.00042392147736656203,
	  -0.00039921600949020575,
	  -0.0003410784893278681,
	  -0.000246559170922285,
	  -0.00011254431170462093,
	  0.00006433750801042243,
	  0.0002870933492719704,
	  0.000558700907206804,
	  0.0008818359747837215,
	  0.0012587683624311434,
	  0.0016910582068537412,
	  0.0021856908690502572,
	  0.002726710900242062,
	  0.0033400842043236107,
	  0.0040111517348650795,
	  0.004739077342511737,
	  0.005524253099514465,
	  0.006366047547594455,
	  0.007263122811066336,
	  0.008212361291143212,
	  0.009210018111155885,
	  0.010251426837018239,
	  0.011331469979433535,
	  0.01244362364387908,
	  0.013584873647048157,
	  0.01474370531018102,
	  0.015917964027590856,
	  0.01709913746351596,
	  0.01827867257835206,
	  0.01944880098525925,
	  0.020601194890077695,
	  0.02172846305781673,
	  0.022822065011098805,
	  0.0238738289199096,
	  0.02487547673817073,
	  0.025819341620558257,
	  0.026697029918866366,
	  0.02750356254696736,
	  0.028230666791627817,
	  0.028873750993741547,
	  0.029427571925871056,
	  0.029887019803322225,
	  0.030248790890860813,
	  0.030508789150617766,
	  0.030665772386255268,
	  0.03071819701905401,
	  0.030665772386255268,
	  0.030508789150617766,
	  0.030248790890860813,
	  0.029887019803322225,
	  0.029427571925871056,
	  0.028873750993741547,
	  0.028230666791627817,
	  0.02750356254696736,
	  0.026697029918866366,
	  0.025819341620558257,
	  0.02487547673817073,
	  0.0238738289199096,
	  0.022822065011098805,
	  0.02172846305781673,
	  0.020601194890077695,
	  0.01944880098525925,
	  0.01827867257835206,
	  0.01709913746351596,
	  0.015917964027590856,
	  0.01474370531018102,
	  0.013584873647048157,
	  0.01244362364387908,
	  0.011331469979433535,
	  0.010251426837018239,
	  0.009210018111155885,
	  0.008212361291143212,
	  0.007263122811066336,
	  0.006366047547594455,
	  0.005524253099514465,
	  0.004739077342511737,
	  0.0040111517348650795,
	  0.0033400842043236107,
	  0.002726710900242062,
	  0.0021856908690502572,
	  0.0016910582068537412,
	  0.0012587683624311434,
	  0.0008818359747837215,
	  0.000558700907206804,
	  0.0002870933492719704,
	  0.00006433750801042243,
	  -0.00011254431170462093,
	  -0.000246559170922285,
	  -0.0003410784893278681,
	  -0.00039921600949020575,
	  -0.00042392147736656203,
	  -0.0045644380812448395
	};
}
