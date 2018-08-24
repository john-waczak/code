import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
from inspect import signature 



def f(x, y):
    return x*y

sig = signature(f)
print(len(sig.parameters))


def graph(f, xlims, ylims, resolution):
    sig = signature(f)
    assert(len(sig.parameters) is 2)
    x = np.linspace(xlims[0], xlims[1], resolution)
    y = np.linspace(ylims[0], ylims[1], resolution)
    X,Y = np.meshgrid(x,y)

    fig = plt.figure()
    ax = fig.gca(projection='3d')
    ax.plot_surface(X, Y, f(X, Y))
    return ax


ax = graph(f, [-2, 2], [-2, 2], 50)
plt.show() 
