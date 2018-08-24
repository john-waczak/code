import os
from glob import glob
import numpy as np
from PIL import Image
import cv2
import pickle

face_cascade = cv2.CascadeClassifier('./haarcascade_frontalface_default.xml')
recognizer = cv2.face.LBPHFaceRecognizer_create()

files= glob("./John/*.*")
pics = []
current_id = 0
label_ids = {} 


y_labels = []
x_train = [] 

for file in files:
    if file.endswith("png") or file.endswith("jpg"):
        pics.append(file)
        label = os.path.basename(os.path.dirname(file)).replace(' ', '-').lower()
        pil_image = Image.open(file).convert("L")  # convert to greyscale
        img = np.array(pil_image, "uint8")
        faces = face_cascade.detectMultiScale(img)

        if label not in label_ids:
            label_ids[label] = current_id
            current_id += 1
        id_ = label_ids[label]

        for (x,y,w,h) in faces:
            roi = img[y:y+h, x:x+w]
            x_train.append(roi)
            y_labels.append(id_) 

with open("labels.pickle", 'wb') as f:
    pickle.dump(label_ids, f)

recognizer.train(x_train, np.array(y_labels))
recognizer.save("trainner.yml") 

