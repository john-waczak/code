import cv2


class Facedetector(object):
    """ Class to detect faces in an image

    Detect faces using a Haar Cascade via the OpenCV library.
    """

    def __init__(self, xml_path):
        self.classifier = cv2.CascadeClassifier(xml_path)

    def detect(self, image, biggest_only = True):
        """Detect faces in an image"""

        is_color = (len(image) == 3)  # if image has 3 color components

        if is_color:
            image_gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        else:
            image_gray = image

        scale_factor = 1.2
        min_neighbors = 5
        min_size = (20,20)

        flags = cv2.CASCADE_FIND_BIGGEST_OBJECT | \
            cv2.CASCADE_DO_ROUGH_SEARCH if biggest_only else \
            cv2.CASCADE_SCALE_IMAGE

        face_coord = self.classifier.detectMultiScale(
            image_gray,
            scaleFactor = scale_factor,
            minNeighbors = min_neighbors,
            minSize = min_size,
            flags = flags
        )

        return face_coord

