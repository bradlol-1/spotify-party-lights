using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApp1
{

    public partial class Settings : Window
    {
        //sets the minimum popularity level to 0, so any song can be played
        public int poplevel = 0;
        public Settings()
        {
            //hides the window for later use
            InitializeComponent();
            poptext.Visibility = Visibility.Hidden;
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //when the minimum popularity box is checked, the user can set an interger from 0-100
            poptext.Visibility = Visibility.Visible;
        }
        private void popbox_Unchecked(object sender, RoutedEventArgs e)
        {
            //hides the popularity setting and resets the minumum popularity to 0
            poptext.Visibility = Visibility.Hidden;
            poptext.Text = null;
            poplevel = 0;
        }
        private void poptext_TextChanged(object sender, TextChangedEventArgs e)
        {
            int res;
            //if the string cannot be set to an interger it resets it
            if(!int.TryParse(poptext.Text, out res))
            {
                poptext.Text = "";
            }
            //if the string is outside the bounds of a possible popularity level, it resets it
            else if(int.Parse(poptext.Text) < 0 || int.Parse(poptext.Text) > 100)
            {
                poptext.Text = "";
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //sets the minimum popularity to the desired interger
            poplevel = int.Parse(poptext.Text);
        }
    }
}
