#include <iostream>
#include <string>

using namespace std;

class JohnsClass{

private:
  string name ;

public:
  void setName(string x){
    name = x; 
  }

  string getName(){
    return name;
  }

};


int main() {

  JohnsClass jc;
  jc.setName("John Waczak");
  cout << jc.getName();
  
  return 0; 
}
