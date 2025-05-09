using Godot;
using System;

using System.Threading.Tasks;

// Calculates wiggle index gyroscope and accelerometer samples

[GlobalClass]
public partial class WiggleIndexAlgorithm : GodotObject {
	
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
		
		// Get first order derivatives
		double[][] derivatives = new double[6][];
		for (int i = 0; i < signalCount; i++) {
			derivatives[i] = SignalHelper.Derivative(signals[i]);
		}
		
		if (debugOutput) {
			output["DerivativeAccelX"] = new Godot.Collections.Array<double>(derivatives[0]);
			output["DerivativeAccelY"] = new Godot.Collections.Array<double>(derivatives[1]);
			output["DerivativeAccelZ"] = new Godot.Collections.Array<double>(derivatives[2]);
		}
		
		double wiggleSum = 0;
		
		double[] totalWiggle = new double[derivatives[0].Length];
		for (int j = 0; j < derivatives[0].Length; j++) {
			double timeWiggle = 0.0;
			for (int i = 0; i < signalCount; i++) {
				timeWiggle += Math.Abs(derivatives[i][j]);
			}
			totalWiggle[j] = timeWiggle;
			wiggleSum += timeWiggle;
		}
		wiggleSum /= derivatives[0].Length;
		
		if (debugOutput) {
			output["WiggleTotal"] = new Godot.Collections.Array<double>(totalWiggle);
		}
		
		output["wiggle"] = wiggleSum;
		
		return output;
	}
	
}
