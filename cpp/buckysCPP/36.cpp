#inlcude <iostream>
using namespace std;

int main() {

  int array[3][1] = {{1},{1},{1}};
  int array2[1][3] = {{1,2,3}};

  cout << array*array2;
  cout << array2*array; 
}
