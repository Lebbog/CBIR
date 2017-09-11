//This class is used to display images in full size 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Project1
{
    public partial class DisplayPicture : Form
    {
        ResultofSearch originalForm;
        public DisplayPicture(ResultofSearch form)
        {
            originalForm = form;
            InitializeComponent();
        }
        public void displayPicture(string file, string name)
        {
            //set up an image objec and set it to the iamge of the picture box
            //set the label to the file name
            Bitmap myImg = (Bitmap)Bitmap.FromFile(file);
            label2.Text = name;
            label2.Refresh();
            int hor = myImg.Height;
            int width = myImg.Width;
            Size newS = new Size(width, hor);
            pictureBox1.Size = newS;
            pictureBox1.Image = myImg;
        }
        //when form closed clean up and hide this form
        private void DisplayPicture_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
        }

    }
}
