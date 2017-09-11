//---------------------------------------------------------------------------
// ResultofSearch.cs
// Geoffrey Lebbos CSS 490 B
// 4/09/2017
// Last Modified: 4/29/2017
//---------------------------------------------------------------------------
//------------------------IMPLEMENTATION DETAILS-----------------------------
//Images loaded into this program are expected to be .jpg images
//Directory containing images is expected to contain 100 images
//Images will be displayed 20 per page, for a total of 5 pages
/*The very first search done with this program will likely take 20-30 seconds
to process, every search after that should be instant UNLESS the text files 
are deleted*/
//---------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;

namespace Project1
{
    //---------------------------------------------------------------------------
    //ResultofSearch class reponsible for processing user input and displaying
    //the correct output to the user
    public partial class ResultofSearch : Form
    {
        //set up global varabiles for class
        static public bool useMethod1 = true;
        List<PictureClass> list;
        List<PictureClass> tempList;
        ArrayList currentPicturesList = new ArrayList(20);
        Form1 originalForm;

        bool[] relevanceCheck;
        bool[] relevanceStore; //For storing relevant images
        int structIndex = 0;
        int stdIndex = 0;
        int page;
        int totalPages;
        string imageFoldPath;
        string clickedPicture = "default\\1.jpg";
        string queryName;
        double currentWeight = 89; //Used for intensity+color method
        double[,] intColor;
        double[,] normalizedFeatures; // For normalized matrix
        struct avgsd
        {
             public double average;
             public double std;
        }
        avgsd[] columnVals;   //To hold standard deviation and average of normalized
        avgsd[] standardDev;  //To hold standard deviation and average of sub-matrix

        double[,] updatedFeatures;  //For updated features

        //---------------------------------------------------------------------------
        //Constructor
        //initialize both arrayLists
        public ResultofSearch(Form1 form)
        {
            //initialize information when form is created
            originalForm = form;
            InitializeComponent();
            tempList = new List<PictureClass>();
            list = new List<PictureClass>();
            //set up list to hold 20 pictures
            for (int i = 0; i < 20; i++)
            {
                currentPicturesList.Add(0);
            }
            intColor = new double[100, 89]; 
            normalizedFeatures = new double[100, 89];
            columnVals = new avgsd[89];
            standardDev = new avgsd[89];
            relevanceCheck = new bool[100];
            relevanceStore = new bool[100];
            updatedFeatures = new double[100,89];
            for (int i = 0; i < updatedFeatures.GetLength(0); i++)
            {
                for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                {
                    // Default value of 4000
                    updatedFeatures[i, j] = 4000; 
                }
            }
        }
        //---------------------------------------------------------------------------
        //function to search through database based on the Query Picture sent in
        public void doSearch(string QueryPicture, string folderPath, bool intensityMethod, bool colorMethod, bool intensityColorMethod)
        {
            if (QueryPicture == null)
                return;
            queryName = QueryPicture;
            imageFoldPath = folderPath;
            Bitmap myImg = new Bitmap(QueryPicture);

            //Grab the names of all the pictures and store in string array
            string[] images = Directory.GetFiles(folderPath);
            int counter = 1;
            while (counter != 101)
            {
                foreach (string image in images)
                {
                    // Look for the next image in folder based on number
                    string tempName = folderPath + "\\" + Convert.ToString(counter) + ".jpg";
                    //compare image name and tempname
                    if (image == tempName)  
                    {
                        list.Add(new PictureClass(image, 0, 0));
                        counter++;
                    }
                }
            }
            if (!File.Exists("intensity.txt") || !File.Exists("color.txt"))
            {
                int counter2 = 1;
                while (counter2 != 101)
                {
                    foreach (string image2 in images)
                    {
                        string tempName2 = folderPath + "\\" + Convert.ToString(counter2) + ".jpg";
                        if (image2 == tempName2)
                        {
                            counter2++;
                            intensity(image2);
                            colorCode(image2);
                        }
                    }
                }
            }
            //Calculate number of pages based on list count
            double numberPerPage = (list.Count / 20.0);
            //Round up just incase number isn't even
            totalPages = (int)Math.Ceiling(numberPerPage);
            pageLabel.Text = "Page 1 /Out of " + totalPages;
            page = 0;

            //grab dimensions of queryPicture and store 
            int qWidth = myImg.Width;
            int qHeight = myImg.Height;
            Image imageQ = myImg;
            Size newS = new Size(qWidth, qHeight);
            queryPicture.Size = newS;
            queryPicture.Image = imageQ;

            intensityColor();
            normalize();

             //Perform calculation based on what the user clicked
            if (intensityMethod)
            {
                sortIntensity(QueryPicture);
            }
            else if (colorMethod)
            {
                sortColorCode(QueryPicture);
            }
            else
            {
                sortIntensityColor(QueryPicture);
            }
        }
        //---------------------------------------------------------------------------
        //addToFile writes lines to the text file specified by the second parameter
        public void addToFile(string line, string file)
        {
            FileStream fileWriter = new FileStream(file, FileMode.Append);
            StreamWriter tw = new StreamWriter(fileWriter);
            tw.WriteLine(line);
            tw.Close();
            fileWriter.Close();

        }
        //---------------------------------------------------------------------------
        // intensity calculates the RGB values of images and stores them in bins
        // Calculation is done by 0.299R+0.587G+0.114B
        public void intensity(string pictureName)
        {
            //To store RGB values in 25 bins
            int[] intensityValues = new int[25];

            Bitmap myImg = new Bitmap(pictureName);
            //Acquire dimensions of image
            int width = myImg.Width;
            int height = myImg.Height;
            int res = width * height;
            Color pixCol;
            int r, g, b;
            double intensity;
            //Acquire color value for each pixel in image
            for (int i = 0; i < width; i++)
            {
                for(int j = 0; j < height; j++)
                {
                    
                   pixCol = myImg.GetPixel(i, j);
                    r = pixCol.R;
                    g = pixCol.G;
                    b = pixCol.B;
                   
                    //Calculate intensity based on equation
                    intensity = (0.299 * r) + (0.587 * g) + (0.114 * b);
 
                    //Special case for anything above 250
                    if (intensity < 250)
                    {
                        intensityValues[(int)intensity / 10]++;
                    }
                    else
                    {
                        intensityValues[24]++;
                    }
                }
            }
            //Capture the intensity values of 1 image into a string
            string intensityLine = "";
            for(int i = 0; i < intensityValues.Length; i++)
            {
                intensityLine += intensityValues[i].ToString();
                intensityLine += ",";
            }
            //Add the resolution at the end for reference
            intensityLine += res.ToString();
            //Pass to function which adds information as a line in text file
            addToFile(intensityLine, "intensity.txt");
        }
        //---------------------------------------------------------------------------
        //ColorCode function that calculates colors of pictures
        public void colorCode(string pictureName)
        {
            //Array used to store color information
            int[] colorValues = new int[64]; 
            Bitmap myImg = new Bitmap(pictureName);
            //Acquire dimensions of picture
            int width = myImg.Width;
            int height = myImg.Height;
            int res = width * height;
            Color pixCol;
            //To store binary representation
            string rCode, gCode, bCode, colorCodeString;
            //Combines two leading bits of each color code
            int colorCodeNum; 
           
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    pixCol = myImg.GetPixel(i, j);

                    //convert to binary, pad to make 8-bit
                    rCode = Convert.ToString(pixCol.R, 2).PadLeft(8, '0');      
                    gCode = Convert.ToString(pixCol.G, 2).PadLeft(8, '0'); 
                    bCode = Convert.ToString(pixCol.B, 2).PadLeft(8, '0');

                    //combine two leading bits into 1 string
                    colorCodeString = rCode.Substring(0, 2) + gCode.Substring(0, 2) + bCode.Substring(0, 2); 
                   
                    //Convert binary string into decimal
                    colorCodeNum = Convert.ToInt32(colorCodeString, 2);

                    //increment bin
                    colorValues[colorCodeNum]++;
                }
            }
            string colorLine = "";
            //Capture the color values for a picture into string
            for (int i = 0; i < colorValues.Length; i++)
            {
                colorLine += colorValues[i].ToString();
                colorLine += ",";
            }
            //Add the resolution at the end for reference
            colorLine += res.ToString();
            //Pass to function which writes information into a text file
            addToFile(colorLine, "color.txt");
        }

        //---------------------------------------------------------------------------
        /*IntensityColor is used to fill a 2d array with
       intensity and color text file values*/
        private void intensityColor()
        {
            //string combinedValues = "";
            string[] intensityValues;
            string[] colorValues;

            string intensityLine = "";
            string colorLine = "";
             System.IO.StreamReader colorFile =
            new System.IO.StreamReader("color.txt");
              System.IO.StreamReader intensityFile =
              new System.IO.StreamReader("intensity.txt");           
            for(int i = 0; i < intColor.GetLength(0); i++)
            {
                //get file lines here
                //combinedValues = intensityFile.ReadLine() + "," + colorFile.ReadLine();
                //values = combinedValues.Split(',');
                intensityLine = intensityFile.ReadLine();
                colorLine = colorFile.ReadLine();

                intensityValues = intensityLine.Split(',');
                colorValues = colorLine.Split(',');

                for (int j = 0; j <intColor.GetLength(1); j++)
                {
                    if(j < intensityValues.Length - 1)
                    {
                        //Indexes 0-24 of intColor are intensity values
                        intColor[i,j] = (double.Parse(intensityValues[j]) / (double.Parse(intensityValues[intensityValues.Length - 1])));
                    }                   
                    else
                    {
                        //Indexes 25-88 of intColor are color values
                        intColor[i, j] = (double.Parse(colorValues[j - 25]) / (double.Parse(colorValues[colorValues.Length - 1])));
                    }
                }
            }
              colorFile.Close();
              intensityFile.Close();
        }
        //---------------------------------------------------------------------------
        //Creates a new feature matrix with normalized values
        //
        private void normalize()
        {
            List<double> columnList = new List<double>();
            double standardDev = 0.0;
         
            //Loop through all 89 columns
            for (int i = 0; i < intColor.GetLength(1); i++) 
            {
                for (int j = 0; j < intColor.GetLength(0); j++)
                {
                    columnList.Add(intColor[j, i]);                 
                }
                //Get standard dev of list
                standardDev = CalculateStdDev(columnList); 
                //Assign standard dev
                columnVals[structIndex].std = standardDev; 
                structIndex++; 
                columnList.Clear();              
            }
            //--After columnVals is filled, fill normalizedFeatures--

            //Loop until all 89 columns
            for (int i = 0; i < intColor.GetLength(1); i++) 
            {
                for (int j = 0; j < intColor.GetLength(0); j++)
                {
                    //Don't divide by 0
                    if (columnVals[i].std != 0) 
                    {
                        normalizedFeatures[j, i] = ((intColor[j, i] - columnVals[i].average) / columnVals[i].std);
                    }
                }
            }
            }
        //---------------------------------------------------------------------------
        //For calculation standard deviation of a list of values
        private double CalculateStdDev(IEnumerable<double> values)
        {
            double avg = values.Average();
            columnVals[structIndex].average = avg;
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }
        //---------------------------------------------------------------------------
        //For calculation standard deviation of a list of values
        private double CalculateStdDev2(IEnumerable<double> values)
        {
            double avg = values.Average();
            standardDev[stdIndex].average = avg;
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }
        //---------------------------------------------------------------------------
        //SortIntensityColor is used to display the output for intensity+color method
        //Parameter is the current query picture
        private void sortIntensityColor(string pic)
        {   
            int pictureNum = getPictureNum(pic);
            //Clear the results of the last search
            tempList.Clear();

            /*Calculate distance of queryPic and all pictures using manhattan distance 
            with weight*/

            double[] manHatDist = new double[list.Count]; //To store values
            double manHatttanDistance;


            //Need to figure out if user has selected images or not and deal with it accordingly.
            bool userRF = false;
            for(int i = 0; i < relevanceCheck.Length; i++)
            {
                if (relevanceCheck[i] == true)
                {
                    userRF = true;
                    break;
                 }
            }

            //User has given feedback
            if(userRF)
            {
                for(int i = 0; i < relevanceCheck.Length; i++)
                {
                    if (relevanceStore[i] != true)
                    {
                        relevanceStore[i] = relevanceCheck[i];
                    }
                }
                for(int i = 0; i < relevanceCheck.Length; i++)
                {
                    //if selected as relevant or is query image
                    if (relevanceCheck[i] == true || i == pictureNum - 1) 
                    {
                        for (int j = 0; j < normalizedFeatures.GetLength(1); j++)
                        {
                            updatedFeatures[i, j] = normalizedFeatures[i, j];
                        }
                    }
                }
                List<double> columnList = new List<double>();
               
                //NOW SUBMATRIX CONTAINS RELEVANT IMAGE FEATURES
                for (int i = 0; i < updatedFeatures.GetLength(1); i++)
                {
                   for (int j = 0; j < updatedFeatures.GetLength(0); j++)
                    {
                         if (updatedFeatures[j, i] != 4000)
                         {
                                columnList.Add(updatedFeatures[j, i]);
                         }
                    }      
                    standardDev[stdIndex].std = CalculateStdDev2(columnList);
                    stdIndex++;
                    columnList.Clear();
                }
                //At this point we have standard devs for features inside standardDev[]
                //Time to calculate updated weight
                double[] updatedWeight = new double[89];
                for (int i = 0; i < updatedWeight.Length; i++)
                {
                    if (standardDev[i].std != 0)
                    {
                        updatedWeight[i] = 1.0 / standardDev[i].std; //Divide std by 1
                    }
                    else if (standardDev[i].average != 0)
                    {
                        double smallest = 5000.0;
                        //Loop until I find smallest
                        for (int s = 0; s < standardDev.Length; s++)
                        {
                            if (standardDev[s].std < smallest && standardDev[s].std != 0)
                            {
                                smallest = standardDev[s].std;
                            }
                        }
                         smallest *= 0.5;
                         updatedWeight[i] = 1.0 / smallest;
                    }
                    //Average and SD are 0, updated weight becomes 0
                    else
                    {
                        updatedWeight[i] = 0;
                    }
                }
                //Now I have updated weights. stored in updatedWeight []
                //Need to calculate normalized weight.
                double updatedSum = 0.0;
                for (int i = 0; i < updatedWeight.Length; i++)
                {
                    updatedSum += updatedWeight[i];
                }
                //Need to normalize weights now that I have sum
                double[] normalizedWeights = new double[89];
                for (int i = 0; i < normalizedWeights.Length; i++)
                {
                    normalizedWeights[i] = (updatedWeight[i] / updatedSum);
                }
                for (int i = 0; i < normalizedFeatures.GetLength(0); i++)
                {
                    manHatttanDistance = 0.0;
                    for (int j = 0; j < normalizedFeatures.GetLength(1); j++)
                    {
                        //Divide by weight
                        if (normalizedWeights[j] != 0) //Don't divide by 0
                        {
                            manHatttanDistance += (( normalizedWeights[j]) * (Math.Abs(normalizedFeatures[pictureNum - 1, j] - normalizedFeatures[i, j])));
                        }
                    }
                    manHatDist[i] = manHatttanDistance;
                }
            }
            else //user has not selected relevance feedback, divide by 1/89
            {
                for (int i = 0; i < normalizedFeatures.GetLength(0); i++)
                {
                    manHatttanDistance = 0.0;
                    for (int j = 0; j < normalizedFeatures.GetLength(1); j++)
                    {
                        //Divide by weight
                        manHatttanDistance += ((1.0 / currentWeight) * (Math.Abs(normalizedFeatures[pictureNum - 1, j] - normalizedFeatures[i, j])));
                    }
                    manHatDist[i] = manHatttanDistance;
                }
            }
                       
            
            bool[] picVisited = new bool[list.Count]; //for sorting
            int count = 0, visited = 0;

            while (count != list.Count)
            {
                double smallest = double.MaxValue;
                for (int j = 0; j < list.Count; j++)
                {
                    //To calculate which distance is smaller
                    if (smallest > manHatDist[j] && picVisited[j] == false)
                    {
                        smallest = manHatDist[j];
                        visited = j;
                    }
                }
                picVisited[visited] = true;
                string image = imageFoldPath + "\\" + Convert.ToString(visited + 1) + ".jpg";
                tempList.Add(new PictureClass(image, 0, 0));
                count++;
            }
            //Copy contents of tempList into list for display
            for (int i = 0; i < tempList.Count; i++)
            {
                list[i] = tempList[i];
            }
            
            //Clear out standardDev, set stdIndex = 0;
            stdIndex = 0;
            for(int i = 0; i < standardDev.Length; i ++)
            {
                standardDev[i].average = 0.0;
                standardDev[i].std = 0.0;
            }
            displayResults();
           
        }
        private int getPictureNum(string pic)
        {
            string curPic = "";
            if (clickedPicture == "default\\1.jpg")
            {
                curPic = pic;
            }
            else
            {
                curPic = clickedPicture;
            }
            //to store temporary variable
            string temp = "";
            string picNumber = "";
            int pictureNum = 0;
            //Find the number of the picture
            curPic = curPic.Remove(curPic.Length - 4);
            char num;
            for (int i = curPic.Length - 1; i > 0; i--)
            {
                num = curPic[i];
                if (Char.IsDigit(num))
                {
                    //store number as string, keep adding
                    temp += num;
                }
                else
                {
                    break;
                }
            }
            //Loop through string array to get the image number
            for (int i = temp.Length - 1; i >= 0; i--)
            {
                picNumber += temp[i];
            }
            //Convert image number into int
            pictureNum = Convert.ToInt32(picNumber);
            return pictureNum;
        }
        //---------------------------------------------------------------------------
        //To get the numbers of pictures in currentPicturesList
        private int getPictureNum2(string pic)
        {
            string curPic = pic;
          
            //to store temporary variable
            string temp = "";
            string picNumber = "";
            int pictureNum = 0;
            //Find the number of the picture
            curPic = curPic.Remove(curPic.Length - 4);
            char num;
            for (int i = curPic.Length - 1; i > 0; i--)
            {
                num = curPic[i];
                if (Char.IsDigit(num))
                {
                    //store number as string, keep adding
                    temp += num;
                }
                else
                {
                    break;
                }
            }
            //Loop through string array to get the image number
            for (int i = temp.Length - 1; i >= 0; i--)
            {
                picNumber += temp[i];
            }
            //Convert image number into int
            pictureNum = Convert.ToInt32(picNumber);
            return pictureNum;
        }
        //---------------------------------------------------------------------------
        //SortColorCode is used to display the output for color method
        //Parameter is the current query picture
        private void sortColorCode(string pic)
        {
        
            int pictureNum = getPictureNum(pic);

            //Clear the results of the last search
            tempList.Clear();

            //To find and calculate distances between queryPicture and other images
            double[] colorHistogram = new double[64];
            double[] tempColor = new double[64];
            double[] manHatDist = new double[list.Count];
            bool[] picVisited = new bool[list.Count];

            //To find queryPicture in textfile based on number
            string lineToSkip = "";
            string lineToSplit = "";
            int count = 0;          
            System.IO.StreamReader file =
              new System.IO.StreamReader("color.txt");
            //To find the line corresponding to queryPicture
            for (int i = 0; i <= list.Count; i++)
            {

                if (i == pictureNum)
                {
                    lineToSplit = lineToSkip;
                    break;
                }
                lineToSkip = file.ReadLine(); 
            }

            //Split line based on comma placement
            string[] values = lineToSplit.Split(',');

            //Divide by resolution to get value required for denominater
            for (int i = 0; i < colorHistogram.Length; i++)
            {              
                colorHistogram[i] = (double.Parse(values[i]) / double.Parse(values[colorHistogram.Length]));
            }
            file.Close();
            System.IO.StreamReader file1 =
              new System.IO.StreamReader("color.txt");

            // To store manhattan distance
            double manHatttanDistance;

            //To read each value from text file
            for (int i = 0; i < list.Count; i++)
            {
                lineToSkip = file1.ReadLine();
                manHatttanDistance = 0;
                string[] tempValues = lineToSkip.Split(',');

                for (int j = 0; j < colorHistogram.Length; j++)
                {
                    //perform part1 of manhattan distance calculation
                    tempColor[j] = (double.Parse(tempValues[j]) / double.Parse(tempValues[64]));
                }
                for (int j = 0; j < 64; j++)
                {
                    // Perform part two of manhattan distance calculation
                    manHatttanDistance += Math.Abs(colorHistogram[j] - tempColor[j]);
                }
                //Store manhattan distance of image 
                manHatDist[i] = manHatttanDistance; 
            }
            file1.Close();
            int visited = 0;
            // Iterate through list and sort
            while (count != list.Count)
            {
                double smallest = 10000;
                for (int j = 0; j < list.Count; j++)
                {
                    //To calculate which distance is smaller
                    if (smallest > manHatDist[j] && picVisited[j] == false)
                    {
                        smallest = manHatDist[j];
                        visited = j;
                    }
                }
                picVisited[visited] = true;
                string image = imageFoldPath + "\\" + Convert.ToString(visited + 1) + ".jpg";
                tempList.Add(new PictureClass(image, 0, 0));
                count++;
            }
            //Copy contents of tempList into list for display
            for (int i = 0; i < tempList.Count; i++)
            {
                list[i] = tempList[i];
            }
            displayResults();
        }
        //---------------------------------------------------------------------------
        //sortIntensity is used to display the output for intensity method
        //Parameter is the current query picture
        private void sortIntensity(string pic)
        {
           
            int picture = getPictureNum(pic);

            tempList.Clear();

            //To find and calculate distances between queryPicture and other images        
            double[] intensityArray = new double[25];      
            double[] tempIntensity = new double[25];        
            double[] manhattanDistance = new double[list.Count]; 
            bool[] visitedPictures = new bool[list.Count]; 
            string lineToSkip = "";
            string lineToSplit = "";
            int count = 0;          
            System.IO.StreamReader file =
                 new System.IO.StreamReader("intensity.txt");
            //To find queryPicture in textfile based on number
            for (int i = 0; i <= list.Count; i++)
            {

                if (i == picture)
                {
                    lineToSplit = lineToSkip;
                    break;
                }
                lineToSkip = file.ReadLine(); 
            }
            string[] values = lineToSplit.Split(',');
            //For denominator part of manhattan distance
            for (int i = 0; i < 25; i++)
            {
               
                intensityArray[i] = (double.Parse(values[i]) / double.Parse(values[25]));
            }
            file.Close();
       
            System.IO.StreamReader file1 =
               new System.IO.StreamReader("intensity.txt");
            double manDist;

            for (int i = 0; i < list.Count; i++)
            {
                lineToSkip = file1.ReadLine();
                manDist = 0;
                //Split line based on comma placement
                string[] tempValues = lineToSkip.Split(',');

                for (int j = 0; j < 25; j++)
                {
                  
                    tempIntensity[j] = (double.Parse(tempValues[j]) / double.Parse(tempValues[25]));
                }
                for (int j = 0; j < 25; j++)
                {
                    // manhattan distance 
                    manDist += Math.Abs(intensityArray[j] - tempIntensity[j]);
                }
                manhattanDistance[i] = manDist; 
            }
            file1.Close();
            int visited = 0;
            // Iterate through list
            while (count != list.Count)
            {
                double smallest = 10000;

                for (int j = 0; j < list.Count; j++)
                {
                    //sort and place based on distance
                    if (smallest > manhattanDistance[j] && visitedPictures[j] == false)
                    {
                        smallest = manhattanDistance[j];
                        visited = j;
                    }
                }

                visitedPictures[visited] = true;
                string image = imageFoldPath + "\\" + Convert.ToString(visited + 1) + ".jpg";
                tempList.Add(new PictureClass(image, 0, 0));
                count++;
            }

            //Copy contents of tempList into list for display
            for (int i = 0; i < tempList.Count; i++)
            {
                list[i] = tempList[i];
            }

            displayResults();
        }
        //---------------------------------------------------------------------------
        //this function is used to display the results of the search to the screen
        public void displayResults()
        {
            //offset used to scan through the list and get the right pictures based on the current page we are on 
            int offSet = page * 20;
            //each block does same thing will only comment one of them!!!!!!!!!!!!!!!!!!!!
            //check to make sure we are still with in the array and picture still exists
            if ((0 + offSet) < list.Count && File.Exists(((PictureClass)list[0 + offSet]).getPath()))
            {
                //add the path to the current page listing
                currentPicturesList[0] = ((PictureClass)list[0 + offSet]).getPath();
                //get the image from the memory and create thumbnail of it
                Bitmap image1 = (Bitmap)Bitmap.FromFile(((PictureClass)list[0 + offSet]).getPath());
                Image newImag1 = image1.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                //set thumbnail as the image of the picturebox starting from one and moving across then down
                pictureBox1.Image = newImag1;

                string picNumber = currentPicturesList[0].ToString();
                int picIndex = getPictureNum2(picNumber);
                if(relevanceStore[picIndex - 1 ] == true)
                {
                    checkBox2.Checked = true;
                }
                else
                {
                    checkBox2.Checked = false;
                }
            }
            else
            {
                //if we are past the array or image does not exist them set image in picturebox to null and path to null
                currentPicturesList[0] = null;
                pictureBox1.Image = null;
            }
            //does same thing as first one but for second picture box
            if ((1 + offSet) < list.Count && File.Exists(((PictureClass)list[1 + offSet]).getPath()))
            {
                currentPicturesList[1] = ((PictureClass)list[1 + offSet]).getPath();
                Bitmap image2 = (Bitmap)Bitmap.FromFile(((PictureClass)list[1 + offSet]).getPath());
                Image newImag2 = image2.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox2.Image = newImag2;


                string picNumber = currentPicturesList[1].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox3.Checked = true;
                }
                else
                {
                    checkBox3.Checked = false;
                }
            }
            else
            {
                currentPicturesList[1] = null;
                pictureBox2.Image = null;
            }
            if ((2 + offSet) < list.Count && File.Exists(((PictureClass)list[2 + offSet]).getPath()))
            {
                currentPicturesList[2] = ((PictureClass)list[2 + offSet]).getPath();
                Bitmap image3 = (Bitmap)Bitmap.FromFile(((PictureClass)list[2 + offSet]).getPath());
                Image newImag3 = image3.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox3.Image = newImag3;

                string picNumber = currentPicturesList[2].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox4.Checked = true;
                }
                else
                {
                    checkBox4.Checked = false;
                }
            }
            else
            {
                currentPicturesList[2] = null;
                pictureBox3.Image = null;

            }
            if ((3 + offSet) < list.Count && File.Exists(((PictureClass)list[3 + offSet]).getPath()))
            {
                currentPicturesList[3] = ((PictureClass)list[3 + offSet]).getPath();
                Bitmap image4 = (Bitmap)Bitmap.FromFile(((PictureClass)list[3 + offSet]).getPath());
                Image newImag4 = image4.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox4.Image = newImag4;
                string picNumber = currentPicturesList[3].ToString();
                int picIndex = getPictureNum2(picNumber);

                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox5.Checked = true;
                }
                else
                {
                    checkBox5.Checked = false;
                }
            }
            else
            {
                currentPicturesList[4] = null;
                pictureBox4.Image = null;

            }
            if ((4 + offSet) < list.Count && File.Exists(((PictureClass)list[4 + offSet]).getPath()))
            {
                currentPicturesList[4] = ((PictureClass)list[4 + offSet]).getPath();
                Bitmap image5 = (Bitmap)Bitmap.FromFile(((PictureClass)list[4 + offSet]).getPath());
                Image newImag5 = image5.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox5.Image = newImag5;

                string picNumber = currentPicturesList[4].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox6.Checked = true;
                }
                else
                {
                    checkBox6.Checked = false;
                }

            }
            else
            {
                currentPicturesList[4] = null;
                pictureBox5.Image = null;
            }

            //row #2

            if ((5 + offSet) < list.Count && File.Exists(((PictureClass)list[5 + offSet]).getPath()))
            {
                currentPicturesList[5] = ((PictureClass)list[5 + offSet]).getPath();
                Bitmap image6 = (Bitmap)Bitmap.FromFile(((PictureClass)list[5 + offSet]).getPath());
                Image newImag6 = image6.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox6.Image = newImag6;

                string picNumber = currentPicturesList[5 ].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox7.Checked = true;
                }
                else
                {
                    checkBox7.Checked = false;
                }

            }
            else
            {
                currentPicturesList[5] = null;
                pictureBox6.Image = null;
            }
            if ((6 + offSet) < list.Count && File.Exists(((PictureClass)list[6 + offSet]).getPath()))
            {
                currentPicturesList[6] = ((PictureClass)list[6 + offSet]).getPath();
                Bitmap image7 = (Bitmap)Bitmap.FromFile(((PictureClass)list[6 + offSet]).getPath());
                Image newImag7 = image7.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox7.Image = newImag7;

                string picNumber = currentPicturesList[6 ].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox8.Checked = true;
                }
                else
                {
                    checkBox8.Checked = false;
                }
            }
            else
            {
                currentPicturesList[6] = null;
                pictureBox7.Image = null;
            }
            if ((7 + offSet) < list.Count && File.Exists(((PictureClass)list[7 + offSet]).getPath()))
            {
                currentPicturesList[7] = ((PictureClass)list[7 + offSet]).getPath();
                Bitmap image8 = (Bitmap)Bitmap.FromFile(((PictureClass)list[7 + offSet]).getPath());
                Image newImag8 = image8.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox8.Image = newImag8;

                string picNumber = currentPicturesList[7 ].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox9.Checked = true;
                }
                else
                {
                    checkBox9.Checked = false;
                }

            }
            else
            {
                currentPicturesList[7] = null;
                pictureBox8.Image = null;
            }
            if ((8 + offSet) < list.Count && File.Exists(((PictureClass)list[8 + offSet]).getPath()))
            {
                currentPicturesList[8] = ((PictureClass)list[8 + offSet]).getPath();
                Bitmap image9 = (Bitmap)Bitmap.FromFile(((PictureClass)list[8 + offSet]).getPath());
                Image newImag9 = image9.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox9.Image = newImag9;

                string picNumber = currentPicturesList[8 ].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox10.Checked = true;
                }
                else
                {
                    checkBox10.Checked = false;
                }

            }
            else
            {
                currentPicturesList[8] = null;
                pictureBox9.Image = null;

            }
            if ((9 + offSet) < list.Count && File.Exists(((PictureClass)list[9 + offSet]).getPath()))
            {
                currentPicturesList[9] = ((PictureClass)list[9 + offSet]).getPath();
                Bitmap image10 = (Bitmap)Bitmap.FromFile(((PictureClass)list[9 + offSet]).getPath());
                Image newImag10 = image10.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox10.Image = newImag10;

                string picNumber = currentPicturesList[9 ].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox11.Checked = true;
                }
                else
                {
                    checkBox11.Checked = false;
                }

            }
            else
            {
                currentPicturesList[9] = null;
                pictureBox10.Image = null;

            }

            //row 3


            if ((10 + offSet) < list.Count && File.Exists(((PictureClass)list[10 + offSet]).getPath()))
            {
                currentPicturesList[10] = ((PictureClass)list[10 + offSet]).getPath();
                Bitmap image11 = (Bitmap)Bitmap.FromFile(((PictureClass)list[10 + offSet]).getPath());
                Image newImag11 = image11.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox11.Image = newImag11;

                string picNumber = currentPicturesList[10 ].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox12.Checked = true;
                }
                else
                {
                    checkBox12.Checked = false;
                }
            }
            else
            {
                currentPicturesList[10] = null;
                pictureBox11.Image = null;

            }
            if ((11 + offSet) < list.Count && File.Exists(((PictureClass)list[11 + offSet]).getPath()))
            {
                currentPicturesList[11] = ((PictureClass)list[11 + offSet]).getPath();
                Bitmap image12 = (Bitmap)Bitmap.FromFile(((PictureClass)list[11 + offSet]).getPath());
                Image newImag12 = image12.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox12.Image = newImag12;
                string picNumber = currentPicturesList[11 ].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox13.Checked = true;
                }
                else
                {
                    checkBox13.Checked = false;
                }
            }
            else
            {
                currentPicturesList[11] = null;
                pictureBox12.Image = null;

            }
            if ((12 + offSet) < list.Count && File.Exists(((PictureClass)list[12 + offSet]).getPath()))
            {
                currentPicturesList[12] = ((PictureClass)list[12 + offSet]).getPath();
                Bitmap image13 = (Bitmap)Bitmap.FromFile(((PictureClass)list[12 + offSet]).getPath());
                Image newImag13 = image13.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox13.Image = newImag13;

                string picNumber = currentPicturesList[12].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox14.Checked = true;
                }
                else
                {
                    checkBox14.Checked = false;
                }
            }
            else
            {
                currentPicturesList[12] = null;
                pictureBox13.Image = null;

            }
            if ((13 + offSet) < list.Count && File.Exists(((PictureClass)list[13 + offSet]).getPath()))
            {
                currentPicturesList[13] = ((PictureClass)list[13 + offSet]).getPath();
                Bitmap image14 = (Bitmap)Bitmap.FromFile(((PictureClass)list[13 + offSet]).getPath());
                Image newImag14 = image14.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox14.Image = newImag14;

                string picNumber = currentPicturesList[13 ].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox15.Checked = true;
                }
                else
                {
                    checkBox15.Checked = false;
                }
                
            }
            else
            {
                currentPicturesList[13] = null;
                pictureBox14.Image = null;

            }
            if ((14 + offSet) < list.Count && File.Exists(((PictureClass)list[14 + offSet]).getPath()))
            {
                currentPicturesList[14] = ((PictureClass)list[14 + offSet]).getPath();
                Bitmap image15 = (Bitmap)Bitmap.FromFile(((PictureClass)list[14 + offSet]).getPath());
                Image newImag15 = image15.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox15.Image = newImag15;

                string picNumber = currentPicturesList[14].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox16.Checked = true;
                }
                else
                {
                    checkBox16.Checked = false;
                }
            }
            else
            {
                currentPicturesList[14] = null;
                pictureBox15.Image = null;
            }

            //row 4


            if ((15 + offSet) < list.Count && File.Exists(((PictureClass)list[15 + offSet]).getPath()))
            {
                currentPicturesList[15] = ((PictureClass)list[15 + offSet]).getPath();
                Bitmap image16 = (Bitmap)Bitmap.FromFile(((PictureClass)list[15 + offSet]).getPath());
                Image newImag16 = image16.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox16.Image = newImag16;

                string picNumber = currentPicturesList[15 ].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox17.Checked = true;
                }
                else
                {
                    checkBox17.Checked = false;
                }

            }
            else
            {
                currentPicturesList[15] = null;
                pictureBox16.Image = null;
 
            }
            if ((16 + offSet) < list.Count && File.Exists(((PictureClass)list[16 + offSet]).getPath()))
            {
                currentPicturesList[16] = ((PictureClass)list[16 + offSet]).getPath();
                Bitmap image17 = (Bitmap)Bitmap.FromFile(((PictureClass)list[16 + offSet]).getPath());
                Image newImag17 = image17.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox17.Image = newImag17;

                string picNumber = currentPicturesList[16].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox18.Checked = true;
                }
                else
                {
                    checkBox18.Checked = false;
                }

            }
            else
            {
                currentPicturesList[16] = null;
                pictureBox17.Image = null;

            }
            if ((17 + offSet) < list.Count && File.Exists(((PictureClass)list[17 + offSet]).getPath()))
            {
                currentPicturesList[17] = ((PictureClass)list[17 + offSet]).getPath();
                Bitmap image18 = (Bitmap)Bitmap.FromFile(((PictureClass)list[17 + offSet]).getPath());
                Image newImag18 = image18.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox18.Image = newImag18;

                string picNumber = currentPicturesList[17].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox19.Checked = true;
                }
                else
                {
                    checkBox19.Checked = false;
                }

            }
            else
            {
                currentPicturesList[17] = null;
                pictureBox18.Image = null;

            }
            if ((18 + offSet) < list.Count && File.Exists(((PictureClass)list[18 + offSet]).getPath()))
            {
                currentPicturesList[18] = ((PictureClass)list[18 + offSet]).getPath();
                Bitmap image19 = (Bitmap)Bitmap.FromFile(((PictureClass)list[18 + offSet]).getPath());
                Image newImag19 = image19.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox19.Image = newImag19;

                string picNumber = currentPicturesList[18].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox20.Checked = true;
                }
                else
                {
                    checkBox20.Checked = false;
                }

            }
            else
            {
                currentPicturesList[18] = null;
                pictureBox19.Image = null;
                
            }

            if ((19 + offSet) < list.Count && File.Exists(((PictureClass)list[19 + offSet]).getPath()))
            {
                currentPicturesList[19] = ((PictureClass)list[19  + offSet]).getPath();
                Bitmap image20 = (Bitmap)Bitmap.FromFile(((PictureClass)list[19 + offSet]).getPath());
                Image newImag20 = image20.GetThumbnailImage(150, 150, new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback), IntPtr.Zero);
                pictureBox20.Image = newImag20;

                string picNumber = currentPicturesList[19].ToString();
                int picIndex = getPictureNum2(picNumber);
                if (relevanceStore[picIndex - 1] == true)
                {
                    checkBox21.Checked = true;
                }
                else
                {
                    checkBox21.Checked = false;
                }

            }
            else
            {
                currentPicturesList[19] = null;
                pictureBox20.Image = null;

            }
        }
        //---------------------------------------------------------------------------
        private void Next_Click(object sender, EventArgs e)
        {

            panel1.VerticalScroll.Value = 0;
            panel1.VerticalScroll.Value = 0;
            panel1.HorizontalScroll.Value = 0; //assign the position value    
            panel1.HorizontalScroll.Value = 0;


            if (page < (totalPages - 1))
            {
                page += 1;
                pageLabel.Text = "Page "+ (page+1) + "/Out of " + totalPages;
                displayResults();
            }
            else
            {
                page = 0;
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                displayResults();


            }
        }
        //---------------------------------------------------------------------------
        //reset window if previous button is clicked
        private void Previous_Click(object sender, EventArgs e)
        {
            panel1.VerticalScroll.Value = 0;
            panel1.VerticalScroll.Value = 0;
            panel1.HorizontalScroll.Value = 0; //assign the position value    
            panel1.HorizontalScroll.Value = 0;


            if (page >  0)
            {
                page -= 1;
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                displayResults();
            }
            else
            {
                page = (totalPages - 1);
                pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
                displayResults();

            }
        }
        //---------------------------------------------------------------------------
        //clear info if application closed
        private void ResultofSearch_FormClosing(object sender, FormClosingEventArgs e)
        {
            originalForm.Close();

        }
        //---------------------------------------------------------------------------
        //Function to search by intensity for queryPicture
        private void button1_Click(object sender, EventArgs e)
        {
            page = 0;
            sortIntensity(queryName);
            pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
            displayResults();
        }
        //---------------------------------------------------------------------------
        //Function to search by color for queryPicture
        private void button2_Click(object sender, EventArgs e)
        {
            page = 0;
            sortColorCode(queryName);
            pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
            displayResults();
        }
        //---------------------------------------------------------------------------
        //Function to search by Color and Intensity for queryPicture
        private void button3_Click(object sender, EventArgs e)
        {

            page = 0;
            sortIntensityColor(queryName);
            pageLabel.Text = "Page " + (page + 1) + "/Out of " + totalPages;
            displayResults();

            //Here I should reset the check boxes back to false...
        }
        private void uncheckRelevancy()
        {
            checkBox2.Checked = false;
            checkBox3.Checked = false;
            checkBox4.Checked = false;
            checkBox5.Checked = false;
            checkBox6.Checked = false;
            checkBox7.Checked = false;
            checkBox8.Checked = false;
            checkBox9.Checked = false;
            checkBox10.Checked = false;
            checkBox11.Checked = false;
            checkBox12.Checked = false;
            checkBox13.Checked = false;
            checkBox14.Checked = false;
            checkBox15.Checked = false;
            checkBox16.Checked = false;
            checkBox17.Checked = false;
            checkBox18.Checked = false;
            checkBox19.Checked = false;
            checkBox20.Checked = false;
            checkBox21.Checked = false;
        }

        //////////////////////////////////////////////////////////////////////////
        //method needed to create thumbnails
        public bool ThumbnailCallback()
        {
            return true;
        }
        //The following section are the methods needed to allow application to diplsay clicked images as queryPicture
        //Each checks to see if path has been set then it passes path to the form used to open image
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[0] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[0].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[0].ToString();

                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
                for(int i = 0; i< standardDev.Length; i++)
                {
                    standardDev[i].average = 0.0;
                    standardDev[i].std = 0.0;
                }

            }

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[1] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[1].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[1].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[2] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[2].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[2].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[3] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[3].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[3].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[4] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[4].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[4].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[5] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[5].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[5].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[6] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[6].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[6].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[7] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[7].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[7].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[8] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[8].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[8].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[9] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[9].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[9].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[10] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[10].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[10].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[11] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[11].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[11].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000;
                    }
                }
            }
        }

        private void pictureBox13_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[12] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[12].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[12].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox14_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[13] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[13].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[13].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox15_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[14] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[14].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[14].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox16_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[15] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[15].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[15].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox17_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[16] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[16].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[16].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[17] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[17].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[17].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox19_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[18] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[18].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[18].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }

        private void pictureBox20_Click(object sender, EventArgs e)
        {
            if (currentPicturesList[19] != null)
            {
                Bitmap myImage = new Bitmap(currentPicturesList[19].ToString());
                int width = myImage.Width;
                int height = myImage.Height;
                Image image = myImage;
                queryPicture.Size = myImage.Size;
                queryPicture.Image = myImage;
                clickedPicture = currentPicturesList[19].ToString();
                //To clear relevant images
                uncheckRelevancy();
                for (int i = 0; i < relevanceCheck.Length; i++)
                {
                    relevanceCheck[i] = false;
                    relevanceStore[i] = false;
                }
                for (int i = 0; i < updatedFeatures.GetLength(0); i++)
                {
                    for (int j = 0; j < updatedFeatures.GetLength(1); j++)
                    {
                        updatedFeatures[i, j] = 4000; 
                    }
                }
            }
        }


        //---------------------------------------------------------------------------
        //To show check boxes for relevancy search option
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
             if(checkBox1.Checked == true)
            {
                checkBox2.Visible = true;
                checkBox3.Visible = true;
                checkBox4.Visible = true;
                checkBox5.Visible = true;
                checkBox6.Visible = true;
                checkBox7.Visible = true;
                checkBox8.Visible = true;
                checkBox9.Visible = true;
                checkBox10.Visible = true;
                checkBox11.Visible = true;
                checkBox12.Visible = true;
                checkBox13.Visible = true;
                checkBox14.Visible = true;
                checkBox15.Visible = true;
                checkBox16.Visible = true;
                checkBox17.Visible = true;
                checkBox18.Visible = true;
                checkBox19.Visible = true;
                checkBox20.Visible = true;
                checkBox21.Visible = true;
                //button1.Enabled = false;
                //button2.Enabled = false;

            }
            if (checkBox1.Checked == false)
            {
                checkBox2.Visible =false;
                checkBox3.Visible =false;
                checkBox4.Visible =false;
                checkBox5.Visible =false;
                checkBox6.Visible =false;
                checkBox7.Visible =false;
                checkBox8.Visible =false;
                checkBox9.Visible = false;
                checkBox10.Visible = false;
                checkBox11.Visible = false;
                checkBox12.Visible = false;
                checkBox13.Visible = false;
                checkBox14.Visible = false;
                checkBox15.Visible = false;
                checkBox16.Visible = false;
                checkBox17.Visible = false;
                checkBox18.Visible = false;
                checkBox19.Visible = false;
                checkBox20.Visible = false;
                checkBox21.Visible = false;
                //button1.Enabled = true;
                //button2.Enabled = true;
            }
        }

        //---------------------------------------------------------------------------
        //To figure out which pictures were deemed relevant
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[0].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox2.Checked == true)
            {             
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[1].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox3.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[2].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox4.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[3].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox5.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[4].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox6.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[5].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox7.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[6].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox8.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[7].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox9.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[8].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox10.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[9].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox11.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[10].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox12.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[11].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox13.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox14_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[12].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox14.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox15_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[13].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox15.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox16_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[14].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox16.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox17_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[15].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox17.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox18_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[16].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox18.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox19_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[17].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox19.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }

        }

        private void checkBox20_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[18].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox20.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

        private void checkBox21_CheckedChanged(object sender, EventArgs e)
        {
            string picNumber = currentPicturesList[19].ToString();
            int picIndex = getPictureNum2(picNumber);
            if (checkBox21.Checked == true)
            {
                relevanceCheck[picIndex - 1] = true;
            }
            else
            {
                relevanceCheck[picIndex - 1] = false;
            }
        }

    }
}
