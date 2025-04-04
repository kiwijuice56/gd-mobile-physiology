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
	public const int DetrendWindowSize = 128; // For now, both HR and BR use the same size window
	
	public static double Average(double[] sample) {
		double average = 0.0;
		foreach (double val in sample) {
			average += val;
		}
		return average / sample.Length;
	}
	
	public static double StandardDeviation(double[] sample) {
		double average = Average(sample);
		double stdev = 0.0;
		foreach (double val in sample) {
			stdev += Math.Pow(val - average, 2);
		}
		return Math.Sqrt(stdev / sample.Length);
	}
	
	// Removes overall patterns in a signal, such as subtly increasing
	// or decreasing over time. A good windowSize requires some experimentation,
	// but 1/8 - 1/16 of the sample size is a good rule of thumb
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
	
	// Applies a Finite Impulse Response (FIR) filter to 
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
	
	public static double[][] TransposeMatrix(double[][] data, int n, int m) {
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
	
	// Isolates correlated signals into clean possible components
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
	
	// Finds the index within the Fourier transform corresponding to the highest amplitude 
	// within the given frequency range (in beats per minute).
	public static int FindPeakInRange(double[] fft, double minBeatsPerMin, double maxBeatsPerMin) {
		double maxAmplitude = 0.0;
		int outputIndex = 0;
		for (int i = 0; i < fft.Length; i++) {
			double frequency = 60.0 / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0 && fft[i] > maxAmplitude) {
				maxAmplitude = fft[i];
				outputIndex = i;
			}
		}
		return outputIndex;
	}
	
	// Given a probability distribution and index of the result (HR, BR) in that probability distribution,
	// return the confidence score. For now, this sums nearby probabilities to account for 
	// fuzzy peaks (in the range windowSize). 
	public static double GetConfidenceOfPrediction(double[] probabilityDistribution, int peakIndex, int windowSize) {
		double confidenceSum = 0.0;
		for (int i = peakIndex - windowSize; i <= peakIndex + windowSize; i++) {
			if (i < 0 || i >= probabilityDistribution.Length) {
				continue;
			}
			confidenceSum += probabilityDistribution[i];
		}
		return confidenceSum;
	}
	
	// Creates a probability distribution from an FFT in the range [minBeatsPerMin, maxBeatsPerMin];
	// All other values are 0
	public static double[] FftToProbabilityDistribution(double[] fft, double minBeatsPerMin, double maxBeatsPerMin) {
		double[] distribution = new double[fft.Length];
		
		double squareSum = 0;
		for (int i = 0; i < fft.Length; i++) {
			double frequency = 60.0 / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0) {
				squareSum += fft[i];
			}
		}
		
		for (int i = 0; i < fft.Length; i++) {
			double frequency = 60.0 / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0) {
				distribution[i] = (fft[i]) / squareSum;
			} else {
				distribution[i] = 0;
			}
		}
		
		return distribution;
	}
	
	public static double[] PowerSpectrumInRange(double[] fft,  double minBeatsPerMin, double maxBeatsPerMin) {
		double[] powerSpectrum = new double[fft.Length];
		
		for (int i = 0; i < fft.Length; i++) {
			double frequency = 60.0 / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0) {
				powerSpectrum[i] = fft[i] * fft[i];
			}
		}
		
		return powerSpectrum;
	}
	
	// https://openae.io/standards/features/latest/spectral-kurtosis/
	public static double SpectralCentroid(double[] powerSpectrum, double minBeatsPerMin, double maxBeatsPerMin) {
		double powerSpectrumSum = 0;
		double powerSpectrumSumWeighted = 0;
		for (int i = 0; i < powerSpectrum.Length; i++) {
			double frequency = 60.0 / powerSpectrum.Length * i;
			double magnitude = powerSpectrum[i];
			powerSpectrumSum += magnitude;
			powerSpectrumSumWeighted += magnitude * frequency;
		}
		
		return powerSpectrumSumWeighted / powerSpectrumSum;
	}
	
	// https://openae.io/standards/features/latest/spectral-kurtosis/
	public static double SpectralKurtosis(double[] fft, double minBeatsPerMin, double maxBeatsPerMin) {
		double[] powerSpectrum = PowerSpectrumInRange(fft, minBeatsPerMin, maxBeatsPerMin);
		double centroid = SpectralCentroid(powerSpectrum, minBeatsPerMin, maxBeatsPerMin);
		
		GD.Print(centroid);
		
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
		
		return (powerSpectrumSumWeighted4 / powerSpectrumSum) / Math.Pow(powerSpectrumSumWeighted2 / powerSpectrumSum, 2);
	}
	
	// From a matrix of ICA output signals, return the signal with the highest confidence
	// component frequency in the range of [minBeatsPerMin, maxBeatsPerMin]
	//
	// Returns frequency (hZ), confidence (0, 1), the frequency probabiility distribution of the signal,
	// and the index of the selected signal in the ICA matrix (for debugging purposes)
	public static (double, double, double[], int, double) SelectHighestConfidenceSignal(double[][] signals, double minBeatsPerMin, double maxBeatsPerMin) {
		double maxConfidence = 0.0;
		double maxConfidenceFrequency = 0.0;
		double[] maxProbabilityDistribution = [];
		double maxKurtosis = 0;
		int icaIndex = 0;
		
		for (int i = 0; i < signals.Length; i++) {
			// Convert signal to frequency domain
			double[] fft = SignalHelper.FastFourierTransform(signals[i], signals[i].Length);
			double[] probabilityDistribution = SignalHelper.FftToProbabilityDistribution(fft, minBeatsPerMin, maxBeatsPerMin);
			
			// Find the strongest HR frequency in this ICA signal
			int peakIndex = SignalHelper.FindPeakInRange(fft, minBeatsPerMin, maxBeatsPerMin);
			double confidence = GetConfidenceOfPrediction(probabilityDistribution, peakIndex, 6);
			
			if (confidence >= maxConfidence) {
				maxConfidence = confidence;
				maxConfidenceFrequency = 60.0 / fft.Length * peakIndex;
				maxProbabilityDistribution = probabilityDistribution;
				maxKurtosis = SpectralKurtosis(fft, minBeatsPerMin, maxBeatsPerMin);
				icaIndex = i;
			}
		}
		
		return (maxConfidenceFrequency, maxConfidence, maxProbabilityDistribution, icaIndex, maxKurtosis);
	}
}
