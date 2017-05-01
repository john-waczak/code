import numpy as np
import matplotlib.pyplot as plt
import matplotlib.path as mpath
import matplotlib.lines as mlines
import matplotlib.patches as mpatches
from matplotlib.collections import PatchCollection


patches = []

#circle joint
fig, ax = plt.subplots()
circle1 = mpatches.Circle((1,1),radius=0.5)
circle1.set_color("black")
circle2 = mpatches.Circle((1,1), radius = 0.05)
circle2.set_color("white")

patches.append(circle1)
patches.append(circle2) 

collection = PatchCollection(patches)
ax.add_collection(collection)



plt.axis('equal')
plt.show()