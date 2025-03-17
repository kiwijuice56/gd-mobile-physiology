using Godot;
using System;

using System.Threading.Tasks;

// Calculates heart rate from gyroscope and accelerometer samples

[GlobalClass]
public partial class HeartRateAlgorithm : GodotObject {
	private const int DetrendWindowSize = 128;
	
	// Accepts accelerometer and gyroscope samples and returns a dictionary with the entries:
	// - "rate": Estimated heart rate in beats per minute.
	// - "confidence": The confidence that the given rate is the actual heart rate. Generally, the lower the magnitude, the weaker the pulse waveform.
	// Algorithm modified from `BioPhone: Physiology Monitoring from Peripheral Smartphone Motions: Javier Hernandez, Daniel J. McDuff and Rosalind W. Picard.`
	// If `debugOutput` is true, the output dictionary will also be filled with the following items:
	// - Raw[Accel/Gyro][X/Y/Z]: Signals before any processing
	// - Preprocessed[Accel/Gyro][X/Y/Z]: Signals after basic processing
	// - ICAOutput[0/1/2/3/4/5]: Output of independent component analysis in random order
	// - SelectedICAIndex: The ICA signal that was used for determining heart rate
	// - ProbabilityDistribution: The confidence over the frequency domain
	public static Godot.Collections.Dictionary Analyze(Godot.Collections.Array<Vector3> accel, Godot.Collections.Array<Vector3> gyro, bool debugOutput) {
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
		
		// Check if gyroscope is completely 0 (can cause NaN propagation later)
		bool gyroInvalid = true;
		for (int i = 0; i < sampleSize; i++) {
			gyroInvalid = gyroInvalid && (signals[3][i] == 0 && signals[4][i] == 0 && signals[5][i] == 0);
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
			PreprocessSignal(signals[i]);
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
		double[][] componentSignals = SignalHelper.IndependentComponentAnalysis(signals, gyroInvalid ? 3 : 6, sampleSize - BallistocardiographyFilter.Length);
		var (frequency, confidence, probabilityDistribution, index) = SelectHighestConfidenceSignal(componentSignals, 60, 120);
		
		if (debugOutput) {
			output["ICAOutput0"] = new Godot.Collections.Array<double>(componentSignals[0]);
			output["ICAOutput1"] = new Godot.Collections.Array<double>(componentSignals[1]);
			output["ICAOutput2"] = new Godot.Collections.Array<double>(componentSignals[2]);
			output["ICAOutput3"] = new Godot.Collections.Array<double>(componentSignals[3]);
			output["ICAOutput4"] = new Godot.Collections.Array<double>(componentSignals[4]);
			output["ICAOutput5"] = new Godot.Collections.Array<double>(componentSignals[5]);
			output["SelectedICAIndex"] = index;
			output["ProbabilityDistribution"] = probabilityDistribution;
		}
		
		output["rate"] = 60 * frequency;
		output["confidence"] = confidence;
		
		return output;
	}
	
	// From a matrix of ICA output signals, return the signal with the highest confidence
	// component frequency in the range of [minBeatsPerMin, maxBeatsPerMin]
	//
	// Returns frequency (hZ), confidence (0, 1), the frequency probabiility distribution of the signal,
	// and the index of the selected signal in the matrix
	private static (double, double, double[], int) SelectHighestConfidenceSignal(double[][] signals, double minBeatsPerMin, double maxBeatsPerMin) {
		double maxConfidence = 0.0;
		double maxConfidenceFrequency = 0.0;
		double[] maxProbabilityDistribution = [];
		int icaIndex = 0;
		
		for (int i = 0; i < signals.Length; i++) {
			double[] fft = SignalHelper.FastFourierTransform(signals[i], signals[i].Length);
			double[] probabilityDistribution = SignalHelper.FftToProbabilityDistribution(fft, minBeatsPerMin, maxBeatsPerMin);
			
			int index = SignalHelper.ExtractRate(fft, minBeatsPerMin, maxBeatsPerMin);
			
			double confidence = probabilityDistribution[index];
			
			if (confidence >= maxConfidence) {
				maxConfidence = confidence;
				maxConfidenceFrequency = 60.0 / fft.Length * index;
				maxProbabilityDistribution = probabilityDistribution;
				icaIndex = i;
			}
		}
		
		return (maxConfidenceFrequency, maxConfidence, maxProbabilityDistribution, icaIndex);
	}
	
	// Modify a time-series signal in-place to filter, detrend, and center it
	private static void PreprocessSignal(double[] signal) {
		// Use a sliding window average to detrend the samples
		double[] detrendedSignal = SignalHelper.Detrend(signal, DetrendWindowSize);
		
		// Set mean and variance to 0 (z-scoring)
		SignalHelper.Normalize(detrendedSignal);
		
		// Isolate signals related to BCG movements
		double[] filteredSignal = SignalHelper.ApplyFirFilter(detrendedSignal, BallistocardiographyFilter);
		filteredSignal.CopyTo(signal, 0);
	}
	
	public static int GetActualSampleSize(int outputSampleSize) {
		return outputSampleSize + BallistocardiographyFilter.Length;
	}
	
	public static readonly double[] BallistocardiographyFilter = {
	  0.011242603325458032,
	  0.005157445306135791,
	  -0.020243957295231565,
	  -0.04978418439707147,
	  -0.05146734321980074,
	  -0.01933178738540511,
	  0.012640423434563376,
	  0.011369390979624056,
	  -0.011331746140377533,
	  -0.016553072765213617,
	  0.004257531843301175,
	  0.017474649529860043,
	  0.0011303410151159996,
	  -0.018414952362707314,
	  -0.007667784656641857,
	  0.017740965170233994,
	  0.014966746863089552,
	  -0.014919554924280648,
	  -0.022854933676013246,
	  0.009199513327732544,
	  0.030957851170445427,
	  0.0004909554223186425,
	  -0.038658009901164324,
	  -0.015813873351272083,
	  0.04542105330550204,
	  0.04102319656541392,
	  -0.05069028461842269,
	  -0.09190143428973485,
	  0.054052369911113604,
	  0.31347691405141814,
	  0.44480805498222264,
	  0.31347691405141814,
	  0.054052369911113604,
	  -0.09190143428973485,
	  -0.05069028461842269,
	  0.04102319656541392,
	  0.04542105330550204,
	  -0.015813873351272083,
	  -0.038658009901164324,
	  0.0004909554223186425,
	  0.030957851170445427,
	  0.009199513327732544,
	  -0.022854933676013246,
	  -0.014919554924280648,
	  0.014966746863089552,
	  0.017740965170233994,
	  -0.007667784656641857,
	  -0.018414952362707314,
	  0.0011303410151159996,
	  0.017474649529860043,
	  0.004257531843301175,
	  -0.016553072765213617,
	  -0.011331746140377533,
	  0.011369390979624056,
	  0.012640423434563376,
	  -0.01933178738540511,
	  -0.05146734321980074,
	  -0.04978418439707147,
	  -0.020243957295231565,
	  0.005157445306135791,
	  0.011242603325458032
	};
}
