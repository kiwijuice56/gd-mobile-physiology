using Godot;
using System;

using Accord.Statistics;
using Accord.Statistics.Analysis;
using Accord.Statistics.Analysis.ContrastFunctions;
using Accord.Statistics.Models.Regression.Linear;

using Accord.Math;
using Accord.Math.Transforms;

// Implements helper methods for common signal processing algorithms

// Requires Accord.NET

[GlobalClass]
public partial class SignalHelper : RefCounted {
	public static readonly int DetrendWindowSize = 128; // For now, both HR and BR use the same size window
	public static readonly float SamplingFrequency = 60;
	
	// Returns the average of a signal
	public static double Average(double[] sample) {
		double average = 0.0;
		foreach (double val in sample) {
			average += val;
		}
		return average / sample.Length;
	}
	
	// Returns the stdev of a signal
	public static double StandardDeviation(double[] sample) {
		double average = Average(sample);
		double stdev = 0.0;
		foreach (double val in sample) {
			stdev += Math.Pow(val - average, 2);
		}
		return Math.Sqrt(stdev / sample.Length);
	}
	
	// Returns a smoothed signal with the given windowSize;
	// Warning: the signal will become slightly shifted left, so don't
	// use this operation where the precise domain values matter  
	public static double[] Smooth(double[] signal, int windowSize) {
		double[] output = new double[signal.Length];
		double sum = 0.0;
		for (int i = 0; i < signal.Length; i++) {
			sum += signal[i];
			
			output[i] = sum / windowSize;
			
			if (i - windowSize >= 0) {
				sum -= signal[i - windowSize];
			}
		}
		
		return output;
	}
	
	// Removes overall patterns in a signal, such as a subtle increase
	// or decrease over time. A good windowSize requires some experimentation,
	// but 1/8 - 1/16 of the input size seems to work well
	// Warning: the signal will become slightly shifted left, so don't
	// use this operation where the precise domain values matter
	public static double[] Detrend(double[] sample, int windowSize) {
		double totalAverage = sample[0];
		double average = totalAverage;
		double[] output = new double[sample.Length];
		for (int i = 0; i < sample.Length; i++) {
			if (i - windowSize < 0) {
				average -= totalAverage / windowSize;
			} else {
				average -= sample[i - windowSize] / windowSize;
			}
			average += sample[i] / windowSize;
			output[i] = sample[i] - average;
		}
		return output;
	}
	
	// Sets the mean and standard deviation of a signal to 0 and 1 respectively
	public static void Normalize(double[] sample) {
		double average = Average(sample);
		double stdev = StandardDeviation(sample);
		for (int i = 0; i < sample.Length; i++) {
			sample[i] -= average;
			
			// Prevents samples from blowing up or becoming NaN
			if (stdev > 0.01) {
				sample[i] /= stdev;
			}
		}
	} 
	
	// Returns a new signal with a Finite Impulse Response (FIR) filter applied to 
	// isolate frequency ranges in the signal
	public static double[] ApplyFirFilter(double[] sample, double[] filter) {
		double[] output = new double[sample.Length - filter.Length];
		for (int i = filter.Length; i < sample.Length; i++) {
			double weightedVal = 0.0;
			for (int k = 0; k < filter.Length; k++) {
				weightedVal += sample[i - k] * filter[k];
			}
			output[i - filter.Length] = weightedVal;
		}
		return output;
	}
	
	// Modify a time-series signal in-place to filter, detrend, and center it
	public static void PreprocessSignal(double[] signal, double[] firFilter) {
		// Use a sliding window average to detrend the samples
		double[] detrendedSignal = SignalHelper.Detrend(signal, DetrendWindowSize);
		
		// Set mean and variance to 0 (z-scoring)
		SignalHelper.Normalize(detrendedSignal);
		
		// Isolate signal using FIR filter
		double[] filteredSignal = SignalHelper.ApplyFirFilter(detrendedSignal, firFilter);
		filteredSignal.CopyTo(signal, 0);
	}
	
	// Returns the data matrix (nxm) transpoed to (mxn)
	// Needed to interface with Accord's ICA algorithm
	private static double[][] TransposeMatrix(double[][] data, int n, int m) {
		double[][] transposedData = new double[m][];
		for (int i = 0; i < m; i++) {
			transposedData[i] = new double[n];
		}
		
		for (int i = 0; i < m; i++) {
			for (int j = 0; j < n; j++) {
				transposedData[i][j] = data[j][i];
			}
		}
		
		return transposedData;
	}
	
	// Isolates correlated signals into clean (possible) components
	
	// `signalCount` should be the number of distinct signals (like the x, y, and z channels of the accelerometer),
	// while `sampleSize` is the length of each signal.
	
	// Returns `signalCount` signals of `sampleSize`
	public static double[][] IndependentComponentAnalysis(double[][] samples, int signalCount, int sampleSize) {
		// Accord.NET expects each column to be an input to ICA,
		// while the rest of this program expects each row
		// to be a signal
		double[][] transposedSamples = TransposeMatrix(samples, signalCount, sampleSize);
		
		IndependentComponentAnalysis ica = new IndependentComponentAnalysis();
		MultivariateLinearRegression demix = ica.Learn(transposedSamples);
		double[][] transposedResult = demix.Transform(transposedSamples);
		
		return TransposeMatrix(transposedResult, transposedSamples.Length, transposedSamples[0].Length);
	}
	
	// Returns the FFT of a signal's first `sampleSize` elements (usually just the length)
	public static double[] FastFourierTransform(double[] signal, int sampleSize) {
		double[] realComponent = new double[sampleSize];
		double[] complexComponent = new double[sampleSize];
		Array.Copy(signal, 0, realComponent, 0, sampleSize);
		
		FourierTransform2.FFT(realComponent, complexComponent, FourierTransform.Direction.Forward);
		
		for (int i = 0; i < sampleSize; i++) {
			realComponent[i] = Math.Sqrt(realComponent[i] * realComponent[i] + complexComponent[i] * complexComponent[i]);
		}
		
		return realComponent;
	}
	
	// Finds the 0-based index within the Fourier transform corresponding to the highest amplitude 
	// within the given frequency range (in beats per minute).
	public static int FindPeakInRange(double[] fft, double minBeatsPerMin, double maxBeatsPerMin) {
		double maxAmplitude = 0.0;
		int outputIndex = 0;
		for (int i = 0; i < fft.Length; i++) {
			double frequency = SamplingFrequency / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0 && fft[i] > maxAmplitude) {
				maxAmplitude = fft[i];
				outputIndex = i;
			}
		}
		return outputIndex;
	}
	
	// Creates a normalized FFT from an FFT in the range [minBeatsPerMin, maxBeatsPerMin];
	// Return has the same length as `fft`, but all values outisde range are 0
	public static double[] NormalizedFrequencyDomainInRange(double[] fft, double minBeatsPerMin, double maxBeatsPerMin) {
		double[] distribution = new double[fft.Length];
		
		double sum = 0;
		for (int i = 0; i < fft.Length; i++) {
			double frequency = SamplingFrequency / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0) {
				sum += fft[i];
			}
		}
		
		for (int i = 0; i < fft.Length; i++) {
			double frequency = SamplingFrequency / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0) {
				distribution[i] = (fft[i]) / sum;
			} else {
				distribution[i] = 0;
			}
		}
		
		return distribution;
	}
	
	// Creates a power spectrum from an FFT in range [minBeatsPerMin, maxPeatsPerMin];
	// Return has the same length as `fft` but all values outside range are 0
	public static double[] PowerSpectrumInRange(double[] fft, double minBeatsPerMin, double maxBeatsPerMin) {
		double[] powerSpectrum = new double[fft.Length];
		
		for (int i = 0; i < fft.Length; i++) {
			double frequency = SamplingFrequency / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0) {
				powerSpectrum[i] = fft[i] * fft[i];
			}
		}
		
		return powerSpectrum;
	}
	
	// Returns the spectral centroid of a power spectrum in range [minBeatsPerMin, maxPeatsPerMin]
	// https://openae.io/standards/features/latest/spectral-kurtosis/
	public static double SpectralCentroid(double[] powerSpectrum, double minBeatsPerMin, double maxBeatsPerMin) {
		double powerSpectrumSum = 0;
		double powerSpectrumSumWeighted = 0;
		for (int i = 0; i < powerSpectrum.Length; i++) {
			double frequency = SamplingFrequency / powerSpectrum.Length * i;
			double magnitude = powerSpectrum[i];
			powerSpectrumSum += magnitude;
			powerSpectrumSumWeighted += magnitude * frequency;
		}
		
		return powerSpectrumSumWeighted / powerSpectrumSum;
	}
	
	// Returns the spectral centroid of a FFT in range [minBeatsPerMin, maxPeatsPerMin]
	// https://openae.io/standards/features/latest/spectral-kurtosis/
	public static double SpectralKurtosis(double[] fft, double minBeatsPerMin, double maxBeatsPerMin) {
		double[] powerSpectrum = PowerSpectrumInRange(fft, minBeatsPerMin, maxBeatsPerMin);
		double centroid = SpectralCentroid(powerSpectrum, minBeatsPerMin, maxBeatsPerMin);
		
		double powerSpectrumSum = 0;
		double powerSpectrumSumWeighted2 = 0;
		double powerSpectrumSumWeighted4 = 0;
		
		for (int i = 0; i < powerSpectrum.Length; i++) {
			double frequency = 60.0 / fft.Length * i;
			double magnitude = powerSpectrum[i];
			powerSpectrumSum += magnitude;
			powerSpectrumSumWeighted2 += magnitude * Math.Pow(frequency - centroid, 2);
			powerSpectrumSumWeighted4 += magnitude * Math.Pow(frequency - centroid, 4);
		}
		
		// Subtract by 2 to normalize
		return (powerSpectrumSumWeighted4 / powerSpectrumSum) / Math.Pow(powerSpectrumSumWeighted2 / powerSpectrumSum, 2) - 2;
	}
	
	// From a matrix of ICA output signals, return the signal with the highest strength
	// component frequency in the range of [minBeatsPerMin, maxBeatsPerMin]
	//
	// Returns the strongest frequency of the selected signal (hZ), 
	// the relative strength of the peak frequency (0, 1), 
	// the spectral kurtosis of the clearest signal (in the frequency domain), 
	// the frequency distribution of the signal (normalized) [for debugging], 
	// and the index of the selected signal in the ICA matrix [for debugging]
	public static (double, double, double, double[], int) SelectHighestConfidenceSignal(double[][] signals, double minBeatsPerMin, double maxBeatsPerMin) {
		double maxStrength = 0.0;
		
		double bestSignalFrequency = 0.0;
		double[] bestSignalFft = [];
		double bestSignalKurtosis = 0;
		int bestSignalIndex = 0;
		
		for (int i = 0; i < signals.Length; i++) {
			// Convert signal to frequency domain, then normalize it in the BPM range given
			double[] fft = SignalHelper.FastFourierTransform(signals[i], signals[i].Length);
			double[] normalizedFft = SignalHelper.NormalizedFrequencyDomainInRange(fft, minBeatsPerMin, maxBeatsPerMin);
			
			// We want to measure kurtosis in the frequency domain (i.e. the strength of the peak)
			// Smoothing helps with this, although this signal might be shifted due to how the smoothing 
			// is implemented -- only use for kurtosis!
			double[] smoothedFft = SignalHelper.Smooth(normalizedFft, 6);
			double kurtosis = SpectralKurtosis(smoothedFft, minBeatsPerMin, maxBeatsPerMin);
			
			// This frequency has a strong peak, so it probably closer to a clean pulse
			if (kurtosis >= bestSignalKurtosis) {
				// Find the strongest frequency in this ICA signal
				int peakIndex = SignalHelper.FindPeakInRange(fft, minBeatsPerMin, maxBeatsPerMin);
				
				maxStrength = normalizedFft[peakIndex];
				
				bestSignalFrequency = SamplingFrequency / fft.Length * peakIndex;
				bestSignalFft = smoothedFft;
				
				bestSignalKurtosis = kurtosis;
				bestSignalIndex = i;
			}
		}
		
		return (bestSignalFrequency, maxStrength, bestSignalKurtosis, bestSignalFft, bestSignalIndex);
	}
	
	
	// Calculates heart/breathing rate (in the range [`minBeatsPerMin`, `maxBeatsPerMin`]) from the 
	// vibrations of a person holding a mobile device using the accelerometer and gyroscope, 
	// both sampled at 60 Hz.
	//
	// Accepts accelerometer and gyroscope samples and returns a dictionary with the entries:
	// - "rate": Estimated rate in beats per minute.
	// - "confidence": The confidence that the given rate is the actual rate. Generally, the lower the magnitude, the weaker the pulse waveform.
	// - "kurtosis": the "peak"-ness of the pulse waveform in the frequency domain. Generally, values >> 0 indicate a strong pulse waveform.
	//
	// Requires a FIR filter, used to preprocess the signal and remove background noise.
	// The length of `gyro` minus the length of `filter` must be a power of 2, likewise for `accel`. 
	// 
	// If `debugOutput` is true, the output dictionary will also be filled with the following items:
	// - Raw[Accel/Gyro][X/Y/Z]: Signals before any processing
	// - Preprocessed[Accel/Gyro][X/Y/Z]: Signals after basic processing
	// - ICAOutput[0/1/2/3/4/5]: Output of independent component analysis in random order
	// - SelectedICAIndex: The ICA signal that was used for determining rate
	// - ProbabilityDistribution: The confidence over the frequency domain
	// 
	// Algorithm modified from `BioPhone: Physiology Monitoring from Peripheral Smartphone Motions: Javier Hernandez, Daniel J. McDuff and Rosalind W. Picard.`
	public static Godot.Collections.Dictionary FindRate(Godot.Collections.Array<Godot.Vector3> accel, Godot.Collections.Array<Godot.Vector3> gyro, double minBeatsPerMin, double maxBeatsPerMin, double[] filter, bool debugOutput) {
		/////////////////////////
		//// INITIALIZATION  ////
		/////////////////////////
		
		Godot.Collections.Dictionary output = new Godot.Collections.Dictionary{}; 
		
		int sampleSize = accel.Count;
		if (accel.Count != gyro.Count) {
			throw new ArgumentException("Must have the same amount of gyroscope and accelerometer samples.");
		}
		
		// Load gyro/accel data into a 2D matrix
		double[][] signals = new double[6][];
		for (int i = 0; i < 6; i++) {
			signals[i] = new double[sampleSize];
		}
		
		for (int i = 0; i < sampleSize; i++) {
			signals[0][i] = accel[i].X;
			signals[1][i] = accel[i].Y;
			signals[2][i] = accel[i].Z;
			signals[3][i] = gyro[i].X;
			signals[4][i] = gyro[i].Y;
			signals[5][i] = gyro[i].Z;
		}
		
		// Check if gyroscope is completely 0 (can cause NaN propagation later);
		
		// Almost all devices with a gyroscope also have an accelerometer, but some devices with
		// an accelerometer and no gyroscope exist, hence why the algorithm should work with
		// a missing gyroscope. Godot/Unity return an empty Vector3 if the gyroscope is missing,
		// so we can check for a missing gyroscope by looking at the values of the array
		bool gyroscopeMissing = true;
		for (int i = 0; i < sampleSize; i++) {
			gyroscopeMissing = gyroscopeMissing && (signals[3][i] == 0 && signals[4][i] == 0 && signals[5][i] == 0);
		}
		
		if (debugOutput) {
			output["RawAccelX"] = new Godot.Collections.Array<double>(signals[0]);
			output["RawAccelY"] = new Godot.Collections.Array<double>(signals[1]);
			output["RawAccelZ"] = new Godot.Collections.Array<double>(signals[2]);
			output["RawGyroX"] = new Godot.Collections.Array<double>(signals[3]);
			output["RawGyroY"] = new Godot.Collections.Array<double>(signals[4]);
			output["RawGyroZ"] = new Godot.Collections.Array<double>(signals[5]);
		}
		
		////////////////////////
		//// PREPROCESSING  ////
		////////////////////////
		
		// Note: Signal will have extraneous samples at the end due to FIR filtering.
		// These are removed as the array is recreated in the ICA step
		for (int i = 0; i < 6; i++) {
			SignalHelper.PreprocessSignal(signals[i], filter);
		}
		
		if (debugOutput) {
			output["PreprocessedAccelX"] = new Godot.Collections.Array<double>(signals[0]);
			output["PreprocessedAccelY"] = new Godot.Collections.Array<double>(signals[1]);
			output["PreprocessedAccelZ"] = new Godot.Collections.Array<double>(signals[2]);
			output["PreprocessedGyroX"] = new Godot.Collections.Array<double>(signals[3]);
			output["PreprocessedGyroY"] = new Godot.Collections.Array<double>(signals[4]);
			output["PreprocessedGyroZ"] = new Godot.Collections.Array<double>(signals[5]);
		}
		
		
		/////////////////////////////////////////
		//// INDEPENDENT COMPONENT ANALYSIS  ////
		/////////////////////////////////////////
		
		// Run ICA (using external C# Accord library) 
		double[][] componentSignals = SignalHelper.IndependentComponentAnalysis(signals, gyroscopeMissing ? 3 : 6, sampleSize - filter.Length);
		var (frequency, strength, kurtosis, probabilityDistribution, index) = SignalHelper.SelectHighestConfidenceSignal(componentSignals, minBeatsPerMin, maxBeatsPerMin); 
		
		output["rate"] = 60 * frequency;
		output["confidence"] = strength;
		output["kurtosis"] = kurtosis;
		
		if (debugOutput) {
			output["ICAOutput0"] = new Godot.Collections.Array<double>(componentSignals[0]);
			output["ICAOutput1"] = new Godot.Collections.Array<double>(componentSignals[1]);
			output["ICAOutput2"] = new Godot.Collections.Array<double>(componentSignals[2]);
			if (!gyroscopeMissing) {
				output["ICAOutput3"] = new Godot.Collections.Array<double>(componentSignals[3]);
				output["ICAOutput4"] = new Godot.Collections.Array<double>(componentSignals[4]);
				output["ICAOutput5"] = new Godot.Collections.Array<double>(componentSignals[5]);
			}
			output["SelectedICAIndex"] = index;
			output["ProbabilityDistribution"] = probabilityDistribution;
		}
		
		return output;
	}
}
