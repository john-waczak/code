import numpy as np
import sys


cmdArgs = sys.argv[1:]
file = cmdArgs[0]
values = np.loadtxt(file, delimiter=',', comments='#', dtype='string')


print values.shape
print values.ndim



#~ keys = ['ls', 'lt', 'k_b', 'k_ub', 'T', 'cb', 'cm', 'ct']
runs = []


if values.ndim != 1:
	for i in range(0, values.shape[0]):
		runs.append({"ls":float(values[i,0]), "lt":float(values[i,1]), "k_b":float(values[i,2]), "k_ub":float(values[i,3]),
	"T":float(values[i,4]), "cb":float(values[i,5]), "cm":float(values[i,6]), "ct":float(values[i,7])})
else:
	runs.append({"ls":float(values[0]), "lt":float(values[1]), "k_b":float(values[2]), "k_ub":float(values[3]),
	"T":float(values[4]), "cb":float(values[5]), "cm":float(values[6]), "ct":float(values[7])})

print "\n"