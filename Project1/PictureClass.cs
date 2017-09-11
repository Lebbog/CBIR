//---------------------------------------------------------------------------
// PictureClass.cs
// Geoffrey Lebbos CSS 490 B
// 4/02/2017
// Last Modified: 4/11/2017
//---------------------------------------------------------------------------
//This class is used to hold information about every picture in the database
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Project1
{
    class PictureClass
    {
        //set up important information about picture
        public string Path;
        public double method1;
        public double method2;
        //when object create set values to the ones passed in
        public PictureClass(string path, double method1Value, double method2Value)
        {
            this.Path = path;
            this.method1 = method1Value;
            this.method2 = method2Value;
        }
        //return value for method 1
        public double getMethod1()
        {
            return method1;
        }
        //return value for method 2
        public double getMethod2()
        {
            return method2;
        }
        //return path of picture
        public string getPath()
        {
            return Path;
        }
    }
}
