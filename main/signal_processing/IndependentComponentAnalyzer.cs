using Godot;
using System;

using Accord.Statistics.Analysis;
using Accord.Statistics.Analysis.ContrastFunctions;
using Accord.Statistics.Models.Regression.Linear;

using Accord.Math;
using Accord.Math.Transforms;


public partial class IndependentComponentAnalyzer : Node {
	public Godot.Collections.Array analyze(
		Godot.Collections.Array a_x, 
		Godot.Collections.Array a_y, 
		Godot.Collections.Array a_z,
		Godot.Collections.Array g_x, 
		Godot.Collections.Array g_y, 
		Godot.Collections.Array g_z,
		int size) {
		
		double[][] data = new double[size][];
		
		for (int i = 0; i < size; i++) {
			data[i] = new double[6];
		}
		
		for (int i = 0; i < size; i++) {
			data[i][0] = (double) a_x[i];
			data[i][1] = (double) a_y[i];
			data[i][2] = (double) a_z[i];
			data[i][3] = (double) g_x[i];
			data[i][4] = (double) g_y[i];
			data[i][5] = (double) g_z[i];
		}
		
		IndependentComponentAnalysis ica = new IndependentComponentAnalysis();
		MultivariateLinearRegression demix = ica.Learn(data);
		
		double[][] result = demix.Transform(data);
		
		Godot.Collections.Array godot_result = new Godot.Collections.Array();
		for (int i = 0; i < result.Length; i++) {
			Godot.Collections.Array godot_signal =  new Godot.Collections.Array();
			for (int j = 0; j < result[i].Length; j++) {
				godot_signal.Add(result[i][j]);
			}
			godot_result.Add(godot_signal);
		}
		
		return godot_result;
	}
	
	public Godot.Collections.Array fft(
		Godot.Collections.Array real_data, 
		int size) {
		
		double[] real_input = new double[size];
		double[] complex_input = new double[size];
		
		for (int i = 0; i < size; i++) {
			real_input[i] = (double) real_data[i];
			complex_input[i] = 0.0;
		}
		
		FourierTransform2.FFT(real_input, complex_input, FourierTransform.Direction.Forward);
		
		Godot.Collections.Array godot_result = new Godot.Collections.Array();
		for (int i = 0; i < size; i++) {
			godot_result.Add(Math.Sqrt(real_input[i] * real_input[i] + complex_input[i] * complex_input[i]));
		}
		
		return godot_result;
	}
}
