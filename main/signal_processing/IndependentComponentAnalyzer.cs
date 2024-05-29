using Godot;
using System;
using Accord.Statistics.Analysis;
using Accord.Statistics.Analysis.ContrastFunctions;
using Accord.Statistics.Models.Regression.Linear;

public partial class IndependentComponentAnalyzer : Node {
	public Godot.Collections.Array analyze(
		Godot.Collections.Array g_x, 
		Godot.Collections.Array g_y, 
		Godot.Collections.Array g_z,
		int size) {
		
		double[][] data = new double[size][];
		
		for (int i = 0; i < size; i++) {
			data[i] = new double[3];
		}
		
		for (int i = 0; i < size; i++) {
			data[i][0] = (double) g_x[i];
			data[i][1] = (double) g_y[i];
			data[i][2] = (double) g_z[i];
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
}
