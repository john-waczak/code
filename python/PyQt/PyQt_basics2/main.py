from PyQt4 import QtGui
from PyQt4 import QThread
import sys
import design
import subreddit as sr


# class ThreadingTutorial(QtGui.QMainWindow, design.Ui_MainWindow):

#     def __init__(self):
#         super(self.__class__, self).__init__()
#         self.setupUi(self)
#         self.btn_start.clicked.connect(self.start_getting_top_posts)

#     def start_getting_top_posts(self):
#         subreddit_list = str(self.edit_subreddits.text()).split(',')
#         print subreddit_list
#         if subreddit_list == ['']:
#             QtGui.QMessageBox.critical(self, "No subreddits",
#                                        "You didn't enter any subreddits.",
#                                        QtGui.QMessageBox.Ok)
#             return
#         self.progressBar.setMaximum(len(subreddit_list))
#         self.progressBar.setValue(0)
#         for top_post in sr.get_top_from_subreddits(subreddit_list):
#             self.list_submissions.addItem(top_post)
#             self.progressBar.setValue(self.progressBar.value()+1)

# def main():
#     app = QtGui.QApplication(sys.argv)
#     form = ThreadingTutorial()
#     form.show()
#     app.exec_()

class getPostsThread(QThread):

    def __init__(self, subreddits):
        QThread.__init__(self)
        self.subreddits = subreddits

    def __del__(self):
        self.wait()

    def run(self):
        pass 

if __name__ == '__main__':
    main()
