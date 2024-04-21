class_name Filter
extends Node

static var BANDPASS_FILTER_BCG: PackedFloat64Array = PackedFloat64Array([
	-0.00963913569775546,
	-0.006655844723734262,
	0.012609324719232156,
	0.034207073349388446,
	0.02251984555477809,
	-0.024746833895728435,
	-0.05384119118964488,
	-0.02517892701931395,
	0.020971545340088847,
	0.022597255832372435,
	-0.001840895318626873,
	0.018714332862116934,
	0.07501401970779477,
	0.057535379924847106,
	-0.07804538913054124,
	-0.19216911131161016,
	-0.11083852272215548,
	0.11964117061616084,
	0.24618079747366073,
	0.11964117061616084,
	-0.11083852272215548,
	-0.19216911131161016,
	-0.07804538913054124,
	0.057535379924847106,
	0.07501401970779477,
	0.018714332862116934,
	-0.001840895318626873,
	0.022597255832372435,
	0.020971545340088847,
	-0.02517892701931395,
	-0.05384119118964488,
	-0.024746833895728435,
	0.02251984555477809,
	0.034207073349388446,
	0.012609324719232156,
	-0.006655844723734262,
	-0.00963913569775546
])

static var BANDPASS_FILTER_HR: PackedFloat64Array = PackedFloat64Array([
0.10499617978880085,
-0.03313082124432523,
-0.029762203105922317,
-0.027558465361465587,
-0.026172373921831606,
-0.025287058250264914,
-0.024612164542347787,
-0.023943270118214923,
-0.023110296711748086,
-0.021929525322440014,
-0.020412598775092473,
-0.018529647745837326,
-0.01633135460025227,
-0.01389276814518074,
-0.011344496246856071,
-0.008831322192359487,
-0.006544778949684921,
-0.004676880824319051,
-0.003378574423382352,
-0.0027733867518791432,
-0.002981880054930019,
-0.004053547284917051,
-0.005984590321998136,
-0.008721699716083697,
-0.012116133510122373,
-0.01599872374226732,
-0.02014060257371049,
-0.02441520897554588,
-0.028400175345440662,
-0.031905989833849145,
-0.03456029935353561,
-0.036010491940445255,
-0.036127625969924644,
-0.03491806132715043,
-0.03221976462098455,
-0.027555331024350094,
-0.02167259982073734,
-0.014232785982635183,
-0.005667744318139297,
0.003776871598093129,
0.013764018255623066,
0.02393638145098505,
0.0338619682086885,
0.0430974490957709,
0.05127819675921775,
0.05803472721525856,
0.06308941321862684,
0.06622315898126963,
0.06728992987458056,
0.06622315898126963,
0.06308941321862684,
0.05803472721525856,
0.05127819675921775,
0.0430974490957709,
0.0338619682086885,
0.02393638145098505,
0.013764018255623066,
0.003776871598093129,
-0.005667744318139297,
-0.014232785982635183,
-0.02167259982073734,
-0.027555331024350094,
-0.03221976462098455,
-0.03491806132715043,
-0.036127625969924644,
-0.036010491940445255,
-0.03456029935353561,
-0.031905989833849145,
-0.028400175345440662,
-0.02441520897554588,
-0.02014060257371049,
-0.01599872374226732,
-0.012116133510122373,
-0.008721699716083697,
-0.005984590321998136,
-0.004053547284917051,
-0.002981880054930019,
-0.0027733867518791432,
-0.003378574423382352,
-0.004676880824319051,
-0.006544778949684921,
-0.008831322192359487,
-0.011344496246856071,
-0.01389276814518074,
-0.01633135460025227,
-0.018529647745837326,
-0.020412598775092473,
-0.021929525322440014,
-0.023110296711748086,
-0.023943270118214923,
-0.024612164542347787,
-0.025287058250264914,
-0.026172373921831606,
-0.027558465361465587,
-0.029762203105922317,
-0.03313082124432523,
0.10499617978880085
])
