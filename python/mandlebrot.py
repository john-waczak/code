from PIL import Image

max_iteration = 1000
x_center = -1.0
y_center = 0.0
size = 300

im = Image.new("RGB", (size,size))
for i in xrange(size):
    print i 
    for j in xrange(size):
        x,y = ( x_center + 4.0*float(i-size/2)/size,
                y_center + 4.0*float(j-size/2)/size
                )
        a,b = (0,0)
        iteration = 0

        while (a**2 + b**2 <= 4.0 and iteration < max_iteration):
            a,b = a**2 - b**2 + x, 2*a*b + y
            iteration += 1

        if iteration == max_iteration:
            color_value = 255
        else:
            color_value = iteration*10 % 255
        im.putpixel( (i,j), (color_value, color_value, color_value))
im.save("mandelbrot.png", "PNG") 
    
