using Godot;
using System;

using Accord.Statistics.Analysis;
using Accord.Statistics.Analysis.ContrastFunctions;
using Accord.Statistics.Models.Regression.Linear;

using Accord.Math;
using Accord.Math.Transforms;

// Implements helper methods for common signal processing algorithms

// Requires Accord.NET

[GlobalClass]
public partial class SignalHelper : RefCounted {
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
	public static int ExtractRate(double[] fft, double minBeatsPerMin, double maxBeatsPerMin) {
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
	
	public static double[] SoftMax(double[] fft, double minBeatsPerMin, double maxBeatsPerMin) {
		double[] softMax = new double[fft.Length];
		
		double expSum = 0;
		for (int i = 0; i < fft.Length; i++) {
			double frequency = 60.0 / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0) {
				expSum += fft[i] * fft[i];
			}
		}
		
		for (int i = 0; i < fft.Length; i++) {
			double frequency = 60.0 / fft.Length * i;
			if (minBeatsPerMin / 60.0 < frequency && frequency < maxBeatsPerMin / 60.0) {
				softMax[i] = (fft[i] * fft[i]) / expSum;
			} else {
				softMax[i] = 0;
			}
		}
		
		return softMax;
	}
	
	public static double[] NormalizeMagnitude(double[] signal) {
		double[] normalizedSignal = new double[signal.Length];
		
		double sum = 0;
		for (int i = 0; i < signal.Length; i++) {
			sum += signal[i];
		}
		
		for (int i = 0; i < signal.Length; i++) {
			normalizedSignal[i] = signal[i] / sum;
		}
		
		return normalizedSignal;
	}
	
	public static double[] MovingAverage(double[] signal, int windowSize) {
		double[] averagedSignal = new double[signal.Length];
		
		double sum = 0;
		for (int i = 0; i < signal.Length; i++) {
			sum += signal[i];
			
			averagedSignal[i] = sum / windowSize;
			
			if (i - windowSize >= 0) {
				sum -= signal[i - windowSize];
			}
		}
		
		return averagedSignal;
	}
}
