using Godot;
using System;

using System.Threading.Tasks;

// Calculates wiggle index gyroscope and accelerometer samples

[GlobalClass]
public partial class WiggleIndexAlgorithm : GodotObject {
	
	const int smoothWindowSize = 16;
	
	public static Godot.Collections.Dictionary Analyze(Godot.Collections.Array<Vector3> accel, bool debugOutput) {
		Godot.Collections.Dictionary output = new Godot.Collections.Dictionary{}; 
		
		// Load gyro/accel data into a 2D matrix
		int sampleSize = accel.Count;
		int signalCount = 3;
		double[][] signals = new double[3][];
		for (int i = 0; i < signalCount; i++) {
			signals[i] = new double[sampleSize];
		}
		
		for (int i = 0; i < sampleSize; i++) {
			signals[0][i] = accel[i].X;
			signals[1][i] = accel[i].Y;
			signals[2][i] = accel[i].Z;
		}
		
		if (debugOutput) {
			output["RawAccelX"] = new Godot.Collections.Array<double>(signals[0]);
			output["RawAccelY"] = new Godot.Collections.Array<double>(signals[1]);
			output["RawAccelZ"] = new Godot.Collections.Array<double>(signals[2]);
		}
		
		// Smooth all signals
		double[][] smoothedSignals = new double[signalCount][];
		for (int i = 0; i < signalCount; i++) {
			smoothedSignals[i] = SignalHelper.Smooth(signals[i], smoothWindowSize);
		}
		
		if (debugOutput) {
			output["SmoothedX"] = new Godot.Collections.Array<double>(smoothedSignals[0]).Slice(smoothWindowSize);
			output["SmoothedY"] = new Godot.Collections.Array<double>(smoothedSignals[1]).Slice(smoothWindowSize);
			output["SmoothedZ"] = new Godot.Collections.Array<double>(smoothedSignals[2]).Slice(smoothWindowSize);
		}
		
		// Find first-order derivative
		double[][] derivatives = new double[signalCount][];
		for (int i = 0; i < signalCount; i++) {
			derivatives[i] = SignalHelper.Derivative(smoothedSignals[i], smoothWindowSize);
		}
		
		if (debugOutput) {
			output["DerivativeAccelX"] = new Godot.Collections.Array<double>(derivatives[0]);
			output["DerivativeAccelY"] = new Godot.Collections.Array<double>(derivatives[1]);
			output["DerivativeAccelZ"] = new Godot.Collections.Array<double>(derivatives[2]);
		}
		
		
		double[] derivativeMeans = new double[signalCount];
		for (int j = 0; j < derivatives[0].Length; j++) {
			for (int i = 0; i < signalCount; i++) {
				derivativeMeans[i] += Math.Abs(derivatives[i][j]);
			}
		}
		for (int i = 0; i < signalCount; i++) {
			derivativeMeans[i] /= derivatives[0].Length;
		}
		
		double[] standardDeviations = new double[signalCount];
		for (int j = 0; j < derivatives[0].Length; j++) {
			for (int i = 0; i < signalCount; i++) {
				standardDeviations[i] += Math.Pow(Math.Abs(derivatives[i][j]) - derivativeMeans[i], 2);
			}
		}
		double standardDeviationSum = 0;
		for (int i = 0; i < signalCount; i++) {
			standardDeviations[i] /= Math.Sqrt(standardDeviations[i] / derivatives[0].Length);
			standardDeviationSum += standardDeviations[i];
		}
		
		double wiggleSum = 0;
		double[] totalWiggle = new double[derivatives[0].Length];
		for (int j = 0; j < derivatives[0].Length; j++) {
			double wiggleInInstant = 0.0;
			for (int i = 0; i < signalCount; i++) {
				wiggleInInstant += Math.Abs(derivatives[i][j]);
			}
			totalWiggle[j] = wiggleInInstant;
			wiggleSum += wiggleInInstant;
		}
		double meanWigglePerInstant = wiggleSum / derivatives[0].Length;
		
		if (debugOutput) {
			output["WiggleTotal"] = new Godot.Collections.Array<double>(totalWiggle);
		}
		
		output["wiggle"] = 100 * meanWigglePerInstant;
		output["stdev"] = standardDeviationSum;
		
		return output;
	}
	
}
