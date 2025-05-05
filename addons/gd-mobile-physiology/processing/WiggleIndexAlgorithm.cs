using Godot;
using System;

using System.Threading.Tasks;

// Calculates wiggle index gyroscope and accelerometer samples

[GlobalClass]
public partial class WiggleIndexAlgorithm : GodotObject {
	
	public static Godot.Collections.Dictionary Analyze(Godot.Collections.Array<Vector3> accel, Godot.Collections.Array<Vector3> gyro, bool debugOutput) {
		Godot.Collections.Dictionary output = new Godot.Collections.Dictionary{}; 
		
		// Load gyro/accel data into a 2D matrix
		int sampleSize = accel.Count;
		double[][] signals = SignalHelper.GodotVectorsToMatrix(accel, gyro);
		
		bool gyroscopeMissing = true;
		for (int i = 0; i < sampleSize; i++) {
			gyroscopeMissing = gyroscopeMissing && (signals[3][i] == 0 && signals[4][i] == 0 && signals[5][i] == 0);
		}
		
		int signalCount = gyroscopeMissing ? 3 : 6;
		
		if (debugOutput) {
			output["RawAccelX"] = new Godot.Collections.Array<double>(signals[0]);
			output["RawAccelY"] = new Godot.Collections.Array<double>(signals[1]);
			output["RawAccelZ"] = new Godot.Collections.Array<double>(signals[2]);
			output["RawGyroX"] = new Godot.Collections.Array<double>(signals[3]);
			output["RawGyroY"] = new Godot.Collections.Array<double>(signals[4]);
			output["RawGyroZ"] = new Godot.Collections.Array<double>(signals[5]);
		}
		
		// Get first order derivatives
		double[][] derivatives = new double[6][];
		for (int i = 0; i < 6; i++) {
			derivatives[i] = SignalHelper.Derivative(signals[i]);
		}
		
		if (debugOutput) {
			output["DerivativeAccelX"] = new Godot.Collections.Array<double>(derivatives[0]);
			output["DerivativeAccelY"] = new Godot.Collections.Array<double>(derivatives[1]);
			output["DerivativeAccelZ"] = new Godot.Collections.Array<double>(derivatives[2]);
			output["DerivativeGyroX"] = new Godot.Collections.Array<double>(derivatives[3]);
			output["DerivativeGyroY"] = new Godot.Collections.Array<double>(derivatives[4]);
			output["DerivativeGyroZ"] = new Godot.Collections.Array<double>(derivatives[5]);
		}
		
		double wiggleSum = 0;
		
		double[] totalWiggle = new double[derivatives[0].Length];
		for (int j = 0; j < derivatives[0].Length; j++) {
			double timeWiggle = 0.0;
			for (int i = 0; i < signalCount; i++) {
				timeWiggle += Math.Abs(derivatives[i][j]);
			}
			totalWiggle[j] = timeWiggle / signalCount;
			wiggleSum += totalWiggle[j];
		}
		wiggleSum /= derivatives[0].Length;
		
		if (debugOutput) {
			output["WiggleTotal"] = new Godot.Collections.Array<double>(totalWiggle);
		}
		
		output["wiggle"] = wiggleSum;
		
		return output;
	}
	
}
