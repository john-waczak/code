#!/usr/bin/env python
from Tkinter import *
import numpy as np

root = Tk()
top = Frame(root)
top.pack(side='top')

hwtext = Label(top, text='Hello world! the sine of')
hwtext.pack(side='left')

r = StringVar()
r.set('1.2')
r_entry = Entry(top, width=6, textvariable=r)
r_entry.pack(side='left')

s = StringVar()


def comp_s():
    global s
    s.set('%g' %np.sin(float(r.get())))

    
compute = Button(top, text=' equals ', command=comp_s)
compute.pack(side='left')

s_label = Label(top, textvariable=s, width=18)
s_label.pack(side='left')

root.mainloop()
