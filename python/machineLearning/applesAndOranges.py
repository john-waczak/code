from sklearn import tree

# write a function to classify piece of fruit
# based on weight and texture features


# smooth = 1, bumpy = 0
features = [[140, 1], [130, 1], [150, 0], [170, 0]]

# apple = 0, 1 = orange
labels = [0, 0, 1, 1]
clf = tree.DecisionTreeClassifier()
clf = clf.fit(features, labels)
print(clf.predict([[160, 0]]))
