#import numpy as np
#import matplotlib.pyplot as plt 
#import matplotlib.transforms as tr
#import matplotlib.image as image
#import scipy as sci 

#path1 = "microtubule_pic.PNG "
#path2 = "dynein_pic.jpg"

#new idea http://matplotlib.org/examples/api/demo_affine_image.html

#microtubule = plt.imread(path1)
#im = plt.imshow(microtubule, aspect='auto', extent=(-0.01, 1.03, -.1, .1), zorder=-1)
#plt.imshow(dynein, aspect = 'auto', extent=(0.4,0.6,0,0.9), zorder=-1)
#rotated_img = sci.ndimage.rotate(dynein, 60) 
#plt.imshow(rotated_img, aspect='equal', zorder=-1) 

#x = np.linspace(0,1,500)
#y = np.sin(2*np.pi*x)
#plt.plot(x,y)
 
"""
============================
Affine transform of an image
============================

For the backends that support draw_image with optional affine
transform (e.g., agg, ps backend), the image of the output should
have its boundary match the dashed yellow rectangle.
"""

import numpy as np
import matplotlib.mlab as mlab
import matplotlib.pyplot as plt
import matplotlib.transforms as mtransforms


def get_image():
    delta = 0.25
    x = y = np.arange(-3.0, 3.0, delta)
    X, Y = np.meshgrid(x, y)
    Z1 = mlab.bivariate_normal(X, Y, 1.0, 1.0, 0.0, 0.0)
    Z2 = mlab.bivariate_normal(X, Y, 1.5, 0.5, 1, 1)
    Z = Z2 - Z1  # difference of Gaussians
    return Z


def do_plot(ax, Z, transform):
    im = ax.imshow(Z, interpolation='none',
                   origin='lower',
                   extent=[-2, 4, -3, 2], clip_on=True)

    trans_data = transform + ax.transData
    im.set_transform(trans_data)

    # display intended extent of the image
    x1, x2, y1, y2 = im.get_extent()
    ax.plot([x1, x2, x2, x1, x1], [y1, y1, y2, y2, y1], "y--",
            transform=trans_data)
    ax.set_xlim(-5, 5)
    ax.set_ylim(-4, 4)


# prepare image and figure
fig, ((ax1, ax2), (ax3, ax4)) = plt.subplots(2, 2)
Z = get_image()

# image rotation
do_plot(ax1, Z, mtransforms.Affine2D().rotate_deg(30))

ax2.imshow(Z) 

# scale and reflection
do_plot(ax3, Z, mtransforms.Affine2D().scale(-1, .5))

# everything and a translation
do_plot(ax4, Z, mtransforms.Affine2D().
        rotate_deg(30).skew_deg(30, 15).scale(-1, .5).translate(.5, -1))

plt.show()
