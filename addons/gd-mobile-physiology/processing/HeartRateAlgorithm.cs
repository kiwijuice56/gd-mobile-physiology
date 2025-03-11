using Godot;
using System;

using System.Threading.Tasks;

// Calculates heart rate from gyroscope and accelerometer samples

[GlobalClass]
public partial class HeartRateAlgorithm : GodotObject {
	private const int DetrendWindowSize = 128;
	
	// Accepts accelerometer and gyroscope samples and returns a dictionary with the entries:
	// - "rate": Estimated heart rate in beats per minute.
	// - "magnitude": The strength of the given rate in the frequency domain. This value varies based on sample size, but you can use this value to filter out noise/bad readings. Generally, the lower the magnitude, the weaker the signal.
	// Algorithm modified from `BioPhone: Physiology Monitoring from Peripheral Smartphone Motions: Javier Hernandez, Daniel J. McDuff and Rosalind W. Picard.`
	// If `debug` is true, `debugInfo` will be filled with the following items:
	// - Raw[Accel/Gyro][X/Y/Z]: Signals before any processing
	// - Preprocessed[Accel/Gyro][X/Y/Z]: Signals after basic processing
	// - ICAOutput[0/1/2/3/4/5]: Output of independent component analysis in random order
	// - SelectedICAIndex: The ICA signal that was used for determining breathing rate
	// - FFT: The Fourier transform for the selected ICA signal
	public static Godot.Collections.Dictionary Analyze(Godot.Collections.Array<Vector3> accel, Godot.Collections.Array<Vector3> gyro, bool parallel, Godot.Collections.Dictionary debugInfo, bool debug) {
		int sampleSize = accel.Count;
		
		// Load data into arrays
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
		
		if (debug) {
			debugInfo["RawAccelX"] = new Godot.Collections.Array<double>(data[0]);
			debugInfo["RawAccelY"] = new Godot.Collections.Array<double>(data[1]);
			debugInfo["RawAccelZ"] = new Godot.Collections.Array<double>(data[2]);
			debugInfo["RawGyroX"] = new Godot.Collections.Array<double>(data[3]);
			debugInfo["RawGyroY"] = new Godot.Collections.Array<double>(data[4]);
			debugInfo["RawGyroZ"] = new Godot.Collections.Array<double>(data[5]);
		}
		
		// Note: Signal will have extraneous samples at the end due to FIR filtering.
		// These are removed as the array is recreated in the FFT step
		if (parallel) {
			Parallel.For(0, 6, delegate(int i) {
				PreprocessSignal(data[i]);
			});
		} else {
			for (int i = 0; i < 6; i++) {
				PreprocessSignal(data[i]);
			}
		}
				
		if (debug) {
			debugInfo["PreprocessedAccelX"] = new Godot.Collections.Array<double>(data[0]);
			debugInfo["PreprocessedAccelY"] = new Godot.Collections.Array<double>(data[1]);
			debugInfo["PreprocessedAccelZ"] = new Godot.Collections.Array<double>(data[2]);
			debugInfo["PreprocessedGyroX"] = new Godot.Collections.Array<double>(data[3]);
			debugInfo["PreprocessedGyroY"] = new Godot.Collections.Array<double>(data[4]);
			debugInfo["PreprocessedGyroZ"] = new Godot.Collections.Array<double>(data[5]);
		}
		
		// Aggregate all signals into one
		for (int i = 0; i < sampleSize - BallistocardiographyFilter.Length; i++) {
			double sum = 0;
			for (int j = 0; j < 6; j++) {
				sum += data[j][i] * data[j][i];
			}
			data[0][i] = Math.Sqrt(sum); 
		}
		
		double[] combinedSignal = SignalHelper.ApplyFirFilter(data[0], BandpassHeartRateFilter);
		
		if (debug) {
			debugInfo["CombinedSignal"] = new Godot.Collections.Array<double>(combinedSignal);
		}
		
		// Run FFT (using external C# Accord library) 
		double[] fft = SignalHelper.FastFourierTransform(combinedSignal, sampleSize - (BandpassHeartRateFilter.Length + BallistocardiographyFilter.Length));
		double[] probabilityDistribution = SignalHelper.SoftMax(fft, 60, 120);
		int index = SignalHelper.ExtractRate(fft, 60, 120);
		
		double confidenceSum = 0;
		double frequencySum = 0;
		int included = 0;
		for (int i = index - 2; i <= index + 2; i++) {
			if (i < 0 || i >= probabilityDistribution.Length) {
				continue;
			}
			included++;
			frequencySum += 60.0 / fft.Length * i;
			confidenceSum += probabilityDistribution[i];
		}
		
		double frequencyAverage = frequencySum / included;
		
		if (debug) {
			debugInfo["FFT"] = new Godot.Collections.Array<double>(fft);
			debugInfo["ProbabilityDistribution"] = probabilityDistribution;
		} 
		
		return new Godot.Collections.Dictionary
		{
			{"rate", frequencyAverage * 60.0},
			{"confidence", confidenceSum},
		};
	}
	
	private static void PreprocessSignal(double[] signal) {
		// Use a sliding window average to detrend the samples
		double[] detrendedSignal = SignalHelper.Detrend(signal, DetrendWindowSize);
		
		// Set mean and variance to 0 (z-scoring)
		SignalHelper.Normalize(detrendedSignal);
		
		// Isolate signals realted to BCG movements
		double[] filteredSignal = SignalHelper.ApplyFirFilter(detrendedSignal, BallistocardiographyFilter);
		filteredSignal.CopyTo(signal, 0);
	}
	
	public static int GetActualSampleSize(int outputSampleSize) {
		return outputSampleSize + BallistocardiographyFilter.Length + BandpassHeartRateFilter.Length;
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
	
	public static readonly double[] BandpassHeartRateFilter = {
	-0.006650726180051685,
	-0.003716190467484666,
	-0.004510744296719715,
	-0.00521720254595281,
	-0.005771884734247281,
	-0.006112786912708464,
	-0.006184449660677189,
	-0.005942963681906568,
	-0.005360275248333453,
	-0.004428058953483523,
	-0.0031602339799788617,
	-0.0015940474963824292,
	0.0002107287467915236,
	0.0021734692777209135,
	0.004197688017291434,
	0.0061756483463492684,
	0.007995836395720823,
	0.009549851178976733,
	0.01074053545530899,
	0.011489199325603959,
	0.01174142027972564,
	0.011473125747180736,
	0.010692752587047466,
	0.009441977549539293,
	0.0077945154256935205,
	0.00585153654824873,
	0.003735317370191544,
	0.0015818363339545395,
	-0.00046994038796042227,
	-0.002287324763791782,
	-0.0037535626277634647,
	-0.0047775598563370445,
	-0.005299762805172446,
	-0.005298757975731796,
	-0.004792334537451158,
	-0.003837671386247419,
	-0.0025266790910885946,
	-0.0009798937415978208,
	0.0006624671338988642,
	0.0022512793269127696,
	0.0036402956565108613,
	0.004697891830396264,
	0.005316999183500442,
	0.005426507037606445,
	0.004991733402421099,
	0.004029801016319747,
	0.002591235987309778,
	0.0007775479171242652,
	-0.0012828286475674658,
	-0.0034384643990877815,
	-0.005521909205595639,
	-0.007369871972672792,
	-0.008835776558541876,
	-0.009798029019443134,
	-0.010172031790243112,
	-0.009920569639718763,
	-0.00905640679815879,
	-0.007640763405986468,
	-0.005779528973142642,
	-0.003617919295961601,
	-0.0013283812612890362,
	0.0009038796533649169,
	0.0028956939883382857,
	0.004480186863194159,
	0.005520248703527061,
	0.00592141510395165,
	0.005639998415945421,
	0.004688480667214156,
	0.00313545122457396,
	0.001099852542942314,
	-0.0012570368102269756,
	-0.0037426824914975265,
	-0.006149842569802828,
	-0.00827219661757323,
	-0.009921555154110523,
	-0.010945299736035528,
	-0.011236753007362833,
	-0.010747492413547393,
	-0.009489868186975697,
	-0.007539142375360321,
	-0.005027759240438287,
	-0.0021363019972450935,
	0.0009207559935499307,
	0.003912069846567289,
	0.0066079984344965285,
	0.008799276008116757,
	0.010313800571439756,
	0.011032049371452851,
	0.010896161892012486,
	0.009918851966815074,
	0.008179350176957709,
	0.005822521639818249,
	0.003044063772844189,
	0.00007769315809848107,
	-0.002822720293968579,
	-0.005405893672337686,
	-0.0074419349324242375,
	-0.008741496470716847,
	-0.009172744541069944,
	-0.008672042402580585,
	-0.007250219825001241,
	-0.004994440871821379,
	-0.002061185254621659,
	0.0013358917244631367,
	0.0049422408931557036,
	0.008481103753208867,
	0.011676150958180231,
	0.014275037216580341,
	0.016068582426412577,
	0.0169077871088391,
	0.016717145023417288,
	0.01550315505084635,
	0.013353213538087846,
	0.010428810653384834,
	0.006955708559967003,
	0.0032061319551583128,
	-0.0005253993218612463,
	-0.003942199489431793,
	-0.006769913145391314,
	-0.00878249243496293,
	-0.009813192366626065,
	-0.009777135637696837,
	-0.008672015800093415,
	-0.006583784745351314,
	-0.0036780312453136574,
	-0.00018890893091516532,
	0.0035988366618277564,
	0.007371812427375682,
	0.010812823867778779,
	0.013626250988567632,
	0.015561455601649942,
	0.01643343608164373,
	0.01613696107640229,
	0.014656772571101779,
	0.012067657175553791,
	0.008531306620985512,
	0.0042826327936871045,
	-0.0003867073981068337,
	-0.005150125958923728,
	-0.009672135627923144,
	-0.013633251345064615,
	-0.016755840767390392,
	-0.018826120288331825,
	-0.019709870146954914,
	-0.019362872440034807,
	-0.01783463774510807,
	-0.015264026247988826,
	-0.011870643370144133,
	-0.007939181851067817,
	-0.0037982084660973213,
	0.0002050684536772795,
	0.0037320313490713172,
	0.0064768442132456935,
	0.008187941379842578,
	0.008683717889847346,
	0.007867082851053945,
	0.0057360584968596025,
	0.002385055352958615,
	-0.0020074496458915127,
	-0.007196225346133665,
	-0.012878182901313881,
	-0.01870169306417043,
	-0.024308136815009557,
	-0.029370988652255958,
	-0.03354824056986322,
	-0.0365926700170824,
	-0.03829121291334846,
	-0.03851736883703231,
	-0.037217931460029274,
	-0.03441746798235499,
	-0.03021187532857289,
	-0.02475887204346364,
	-0.01826561026677426,
	-0.01097439448341237,
	-0.003147876491201682,
	0.004945587757483256,
	0.013044041865591804,
	0.020903566529820245,
	0.02830492388564697,
	0.035059278980770915,
	0.04100915697641635,
	0.046028972666249744,
	0.05002303185398491,
	0.05292158852892863,
	0.05467842197620301,
	0.055266968193100854,
	0.05467842197620301,
	0.05292158852892863,
	0.05002303185398491,
	0.046028972666249744,
	0.04100915697641635,
	0.035059278980770915,
	0.02830492388564697,
	0.020903566529820245,
	0.013044041865591804,
	0.004945587757483256,
	-0.003147876491201682,
	-0.01097439448341237,
	-0.01826561026677426,
	-0.02475887204346364,
	-0.03021187532857289,
	-0.03441746798235499,
	-0.037217931460029274,
	-0.03851736883703231,
	-0.03829121291334846,
	-0.0365926700170824,
	-0.03354824056986322,
	-0.029370988652255958,
	-0.024308136815009557,
	-0.01870169306417043,
	-0.012878182901313881,
	-0.007196225346133665,
	-0.0020074496458915127,
	0.002385055352958615,
	0.0057360584968596025,
	0.007867082851053945,
	0.008683717889847346,
	0.008187941379842578,
	0.0064768442132456935,
	0.0037320313490713172,
	0.0002050684536772795,
	-0.0037982084660973213,
	-0.007939181851067817,
	-0.011870643370144133,
	-0.015264026247988826,
	-0.01783463774510807,
	-0.019362872440034807,
	-0.019709870146954914,
	-0.018826120288331825,
	-0.016755840767390392,
	-0.013633251345064615,
	-0.009672135627923144,
	-0.005150125958923728,
	-0.0003867073981068337,
	0.0042826327936871045,
	0.008531306620985512,
	0.012067657175553791,
	0.014656772571101779,
	0.01613696107640229,
	0.01643343608164373,
	0.015561455601649942,
	0.013626250988567632,
	0.010812823867778779,
	0.007371812427375682,
	0.0035988366618277564,
	-0.00018890893091516532,
	-0.0036780312453136574,
	-0.006583784745351314,
	-0.008672015800093415,
	-0.009777135637696837,
	-0.009813192366626065,
	-0.00878249243496293,
	-0.006769913145391314,
	-0.003942199489431793,
	-0.0005253993218612463,
	0.0032061319551583128,
	0.006955708559967003,
	0.010428810653384834,
	0.013353213538087846,
	0.01550315505084635,
	0.016717145023417288,
	0.0169077871088391,
	0.016068582426412577,
	0.014275037216580341,
	0.011676150958180231,
	0.008481103753208867,
	0.0049422408931557036,
	0.0013358917244631367,
	-0.002061185254621659,
	-0.004994440871821379,
	-0.007250219825001241,
	-0.008672042402580585,
	-0.009172744541069944,
	-0.008741496470716847,
	-0.0074419349324242375,
	-0.005405893672337686,
	-0.002822720293968579,
	0.00007769315809848107,
	0.003044063772844189,
	0.005822521639818249,
	0.008179350176957709,
	0.009918851966815074,
	0.010896161892012486,
	0.011032049371452851,
	0.010313800571439756,
	0.008799276008116757,
	0.0066079984344965285,
	0.003912069846567289,
	0.0009207559935499307,
	-0.0021363019972450935,
	-0.005027759240438287,
	-0.007539142375360321,
	-0.009489868186975697,
	-0.010747492413547393,
	-0.011236753007362833,
	-0.010945299736035528,
	-0.009921555154110523,
	-0.00827219661757323,
	-0.006149842569802828,
	-0.0037426824914975265,
	-0.0012570368102269756,
	0.001099852542942314,
	0.00313545122457396,
	0.004688480667214156,
	0.005639998415945421,
	0.00592141510395165,
	0.005520248703527061,
	0.004480186863194159,
	0.0028956939883382857,
	0.0009038796533649169,
	-0.0013283812612890362,
	-0.003617919295961601,
	-0.005779528973142642,
	-0.007640763405986468,
	-0.00905640679815879,
	-0.009920569639718763,
	-0.010172031790243112,
	-0.009798029019443134,
	-0.008835776558541876,
	-0.007369871972672792,
	-0.005521909205595639,
	-0.0034384643990877815,
	-0.0012828286475674658,
	0.0007775479171242652,
	0.002591235987309778,
	0.004029801016319747,
	0.004991733402421099,
	0.005426507037606445,
	0.005316999183500442,
	0.004697891830396264,
	0.0036402956565108613,
	0.0022512793269127696,
	0.0006624671338988642,
	-0.0009798937415978208,
	-0.0025266790910885946,
	-0.003837671386247419,
	-0.004792334537451158,
	-0.005298757975731796,
	-0.005299762805172446,
	-0.0047775598563370445,
	-0.0037535626277634647,
	-0.002287324763791782,
	-0.00046994038796042227,
	0.0015818363339545395,
	0.003735317370191544,
	0.00585153654824873,
	0.0077945154256935205,
	0.009441977549539293,
	0.010692752587047466,
	0.011473125747180736,
	0.01174142027972564,
	0.011489199325603959,
	0.01074053545530899,
	0.009549851178976733,
	0.007995836395720823,
	0.0061756483463492684,
	0.004197688017291434,
	0.0021734692777209135,
	0.0002107287467915236,
	-0.0015940474963824292,
	-0.0031602339799788617,
	-0.004428058953483523,
	-0.005360275248333453,
	-0.005942963681906568,
	-0.006184449660677189,
	-0.006112786912708464,
	-0.005771884734247281,
	-0.00521720254595281,
	-0.004510744296719715,
	-0.003716190467484666,
	-0.006650726180051685
	};
}
