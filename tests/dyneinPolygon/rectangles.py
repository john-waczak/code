import matplotlib.pyplot as plt
import matplotlib.patches as patches
import numpy as np




fig = plt.figure()
ax = fig.add_subplot(111, aspect='equal')






# def Rectangle(x, y, width, height, c, a, ax):
#     ax.add_patch(
#         patches.Rectangle(
#             (x,y),
#             width,
#             height,
#             color=c,
#             alpha=a
#         )
#     )


# def Polygon(xy, c, a, ax):
#     ax.add_patch(
#         patches.Polygon(
#             xy,
#             color = c,
#             alpha = a 
#         )
#     )



def Lower(xL, yL, xU, yU, c, a, ax):
    length = np.sqrt((xU-xL)**2+(yU-yL)**2)
    r1 = 0.05*length
    r2 = 0.1*length

    # binding domain 
    ax.add_patch(
        patches.Circle(
            (xL, yL),
            radius = r1,
            color = c,
            alpha = a
        )
    )

    #leg 
    ax.add_patch(
        patches.Polygon(
            [[xL,yL],[xU,yU]],
            color = c ,
            alpha = a,
            lw = 0.65*length
        )
    )

    #motor domain 
    ax.add_patch(
        patches.Circle(
            (xU, yU),
            radius = r2,
            color = c,
            alpha = a
        )
    )
    

#Lower(0,0, 5,5, 'blue', 1, ax)
file = np.loadtxt('circle_coords.txt')
print file


plt.xlim(-10,10)
plt.ylim(-10,10)
plt.grid(True) 
plt.show() 
