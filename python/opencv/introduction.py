import cv2
import numpy as np
import matplotlib.pyplot as plt

# read in the image
img = cv2.imread('lightsaber.png', cv2.IMREAD_GRAYSCALE)  # 0
# img = cv2.imread_color - 1
# img = cv2.imread_unchanged - (-1)


# plotting with matplotlib and opencv 

# plt.figure()
# plt.imshow(img, cmap=plt.cm.Greys_r)
# plt.show()

# cv2.imshow('image', img)
# cv2.waitKey(0)
# cv2.destroyAllWindows

# save an image
cv2.imwrite('lightsaber_grey.png', img)
