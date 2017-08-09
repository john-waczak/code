#include <iostream>
#include <string>

using namespace std;

class JohnsClass{
public:
  JohnsClass(string z){
    setName(z);
  }
  void setName(string x){
    name = x;
  }
  string getName(){
    return name;
  }

private:
  string name;

};


int main() {

  JohnsClass jc("John");
  cout << jc.getName() << endl; 
  
  return 0; 
}
