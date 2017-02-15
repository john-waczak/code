from __future__ import division 
import numpy as np 
import matplotlib.pyplot as plt

mu = 1      #mass per length
T  = 100      # tension 

v = np.sqrt(T/mu) 
l = 1       # length of string      [m]
Vy = 15     # vertical string speed [m/s]
dx = 0.01    #       [m]
dt = 0.001  #       [s]
n = int(l/dx)    # number of points on string
f = 10000     #frames

x = np.linspace(0, l, n)  
Y_initial_shape = np.sin(np.pi*x/l)
#Y_initial_shape = np.sin(x*np.pi/l) 

def y_next(y_previous, y_current): 

    y_next = np.zeros(n)  
    
    for i in range(1, n-1): 
        y_next[i] = 2*y_current[i]-y_previous[i]-v**2*(dt/dx)**2*(2*y_current[i]-y_current[i-1]-y_current[i+1])
    return y_next



# now fill an array with the string position for each time step

Y = []
Y.append(Y_initial_shape)
Y.append((1-Vy*dt)*Y_initial_shape)
for i in range(2,f):
    y_previous = Y[i-2]
    y_current = Y[i-1]
    Y.append(y_next(y_previous, y_current))


#~ plt.plot(x, Y[0], x, Y[1], x, Y[2], x, Y[3], x, Y[4], x, Y[5], x, Y[6]) 
#~ plt.plot(x, Y[7], x, Y[8], x, Y[9], x, Y[10])
#~ plt.plot(x,Y[99], x, Y[300], x, Y[800], x, Y[f-1], 'r')
#~ plt.show()


plt.ion() # this turns on interaction---needed for animation
t = 0
for i in range(0,f):  # run for one period
    plt.cla() # erase the existing plot to start over
    plt.plot(x, Y[i]) # plot the data
    plt.xlabel('x') # label the axes
    plt.ylabel('$\psi(x)$')
    plt.xlim(0,l)
    plt.ylim(-1.25,1.25)
    plt.annotate("t = "+ str(t) + "s" , xy=(0.2, 1.0))
    plt.draw() 
    t+=dt                        # redraw the canvas



