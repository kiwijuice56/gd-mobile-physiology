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
			sample[i] /= stdev;
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
	
	public static double[][] TransposeMatrix(double[][] data) {
		double[][] transposedData = new double[data[0].Length][];
		for (int i = 0; i < data[0].Length; i++) {
			transposedData[i] = new double[data.Length];
		}
		
		for (int i = 0; i < data[0].Length; i++) {
			for (int j = 0; j < data.Length; j++) {
				transposedData[i][j] = data[j][i];
			}
		}
		
		return transposedData;
	}
	
	// Isolates correlated signals into clean possible components
	public static double[][] IndependentComponentAnalysis(double[][] samples) {
		// Accord.NET expects each column to be an input to ICA,
		// while the rest of this program expects each row
		// to be a signal
		double[][] transposedSamples = TransposeMatrix(samples);
		
		IndependentComponentAnalysis ica = new IndependentComponentAnalysis();
		MultivariateLinearRegression demix = ica.Learn(transposedSamples);
		double[][] transposedResult = demix.Transform(transposedSamples);
		
		return TransposeMatrix(transposedResult);
	}
	
	public static double[] FastFourierTransform(double[] signal) {
		double[] realComponent = new double[signal.Length];
		double[] complexComponent = new double[signal.Length];
		signal.CopyTo(realComponent, 0);
		
		FourierTransform2.FFT(realComponent, complexComponent, FourierTransform.Direction.Forward);
		
		for (int i = 0; i < signal.Length; i++) {
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
}
