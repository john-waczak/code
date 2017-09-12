#include <iostream>


using namespace std;

bool ispalindrome(string word); 

int main() {
  cout<<"Enter a word\n"<<endl;
  string word;
  cin>>word;
  bool isP = ispalindrome(word);
  if (isP == true){
    cout<<"PALINDROME"<<endl;
  }
  else{
    cout<<"NOT PALINDROME"<<endl;
  }
  
  
  
} 


bool ispalindrome(string word){

  int length = word.length();
  if (length<=1){
    return true;
  }
  if(word[0] != word[length-1]){
    return false;
  }
  else{
    //string newWord = "";
    // create new string from old by getting
    // rid of first and last letter
    /*
    for (int i=1; i<length-1;i++){
      newWord = newWord+word[i];
    */
    string newWord = word.substr(1, length-2); 
    
    return ispalindrome(newWord); 
  }
}
