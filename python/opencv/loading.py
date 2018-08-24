import cv2
import numpy as np
import matplotlib.pyplot as plt

cap = cv2.VideoCapture(0)  # use the first webcam in the system

# for saving the video 
fourcc = cv2.VideoWriter_fourcc(*'XVID')
out = cv2.VideoWriter('output.avi', fourcc, 20.0, (640, 480))

while(True):
    ret, frame = cap.read()  # ret is true/false 
    cv2.imshow('frame', frame)

    # convert to gray 
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    cv2.imshow('gray', gray) 

    out.write(frame)

    if cv2.waitKey(1) & 0xFF == ord('q'): 
        break


cap.release()  # release the camaera (like closing a file)
out.release()
cv2.destroyAllWindows()


