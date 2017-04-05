import numpy as np
import matplotlib.pyplot as plt
import matplotlib.image as image


path1 = "microtubule_pic.PNG "
path2 = "dynein_pic.jpg"

fig = plt.figure()
microtubule = image.imread(path1)
dynein = image.imread(path2)

#put image on plot -- not sure what aspect and zorder do but extent positions and crops the image (x,x,y,y)
plt.imshow(microtubule, aspect='auto', extent=(-0.01, 1.03, -.1, .1), zorder=-1)
plt.imshow(dynein, aspect = 'auto', extent=(0.4,0.6,0,0.9), zorder=-1)



x = np.linspace(0,1,500)
y = np.sin(2*np.pi*x)
plt.plot(x,y)
plt.xlim(0,1)
plt.ylim(-1,1)

plt.show()
