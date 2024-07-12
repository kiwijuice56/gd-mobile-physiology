using Godot;
using System;

[GlobalClass]
public partial class BreathingRateAlgorithm : GodotObject {
	private const int DetrendWindowSize = 128;
	
	public static double Analyze(Godot.Collections.Array<Vector3> accel, Godot.Collections.Array<Vector3> gyro, Godot.Collections.Dictionary debugInfo = null) {
		int sampleSize = accel.Count;
		
		// Step 1) Load data into a more compact format
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
		
		if (debugInfo != null) {
			debugInfo["RawAccelX"] = new Godot.Collections.Array<double>(data[0]);
			debugInfo["RawAccelY"] = new Godot.Collections.Array<double>(data[1]);
			debugInfo["RawAccelZ"] = new Godot.Collections.Array<double>(data[2]);
			debugInfo["RawGyroX"] = new Godot.Collections.Array<double>(data[3]);
			debugInfo["RawGyroY"] = new Godot.Collections.Array<double>(data[4]);
			debugInfo["RawGyroZ"] = new Godot.Collections.Array<double>(data[5]);
		}
		
		// Step 2) 
		// Use a sliding window average to detrend the samples
		for (int i = 0; i < 6; i++) {
			data[i] = SignalHelper.Detrend(data[i], DetrendWindowSize);
		}
		
		// Step 3) 
		// Set mean and variance to 0 (z-scoring)
		for (int i = 0; i < 6; i++) {
			SignalHelper.Normalize(data[i]);
		}
		
		// Step 4) 
		//  [not in paper] Use a low pass filter to isolate signals < 1 Hz
		for (int i = 0; i < 6; i++) {
			data[i] = SignalHelper.ApplyFirFilter(data[i], LowPassRespirationFilter);
		}
		
		if (debugInfo != null) {
			debugInfo["PreprocessedAccelX"] = new Godot.Collections.Array<double>(data[0]);
			debugInfo["PreprocessedAccelY"] = new Godot.Collections.Array<double>(data[1]);
			debugInfo["PreprocessedAccelZ"] = new Godot.Collections.Array<double>(data[2]);
			debugInfo["PreprocessedGyroX"] = new Godot.Collections.Array<double>(data[3]);
			debugInfo["PreprocessedGyroY"] = new Godot.Collections.Array<double>(data[4]);
			debugInfo["PreprocessedGyroZ"] = new Godot.Collections.Array<double>(data[5]);
		}
		
		
		// Step 5)
		// Run ICA (using external C# Accord library) 
		data = SignalHelper.IndependentComponentAnalysis(data);
		
		if (debugInfo != null) {
			debugInfo["SelectedICAIndex"] = 0; 
			debugInfo["FFT"] = new Godot.Collections.Array<double>();
		} 
		
		// Step 6)
		// Use FFT (using external C# Accord library) to find the strongest signal within respiration rate ranges
		double maxConfidence = 0.0;
		double maxConfidenceFrequency = 0.0;
		for (int i = 0; i < 6; i++) {
			data[i] = SignalHelper.ApplyFirFilter(data[i], LowPassRespirationFilter);
			
			double[] fft = SignalHelper.FastFourierTransform(data[i]);
			int index = SignalHelper.ExtractRate(fft, 8.0, 45.0);
			if (fft[index] >= maxConfidence) {
				maxConfidence = fft[index];
				maxConfidenceFrequency = 60.0 / fft.Length * index;
				if (debugInfo != null) {
					debugInfo["SelectedICAIndex"] = i; 
					debugInfo["FFT"] = new Godot.Collections.Array<double>(fft);
				} 
			}
		}
		
		if (debugInfo != null) {
			debugInfo["ICAOutput0"] = new Godot.Collections.Array<double>(data[0]);
			debugInfo["ICAOutput1"] = new Godot.Collections.Array<double>(data[1]);
			debugInfo["ICAOutput2"] = new Godot.Collections.Array<double>(data[2]);
			debugInfo["ICAOutput3"] = new Godot.Collections.Array<double>(data[3]);
			debugInfo["ICAOutput4"] = new Godot.Collections.Array<double>(data[4]);
			debugInfo["ICAOutput5"] = new Godot.Collections.Array<double>(data[5]);
		}
		
		return maxConfidenceFrequency * 60.0;
	}
	
	private static readonly double[] LowPassRespirationFilter = {
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
