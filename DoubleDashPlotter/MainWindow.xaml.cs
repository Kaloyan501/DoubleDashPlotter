using System;
using System.Windows;

namespace DoubleDashPlotter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public static string FileName { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Document"; // Default file name
            dialog.DefaultExt = ".iso"; // Default file extension
            dialog.Filter = "Iso File (.iso)|*.iso"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                FileName = filename;
                MessageBox.Show(filename);

                DependencyDownload dependencyDownload = new DependencyDownload();

                try
                {
                    dependencyDownload.Show();
                }
                catch (InvalidOperationException) { 
                    //Do absolutley nothing.
                }

                    
                
                
                this.Close();
            }
        }

        private void Button_Click_Already_Running(object sender, RoutedEventArgs e)
        {
            PlotterPoint plotterPoint = new PlotterPoint(false);
            plotterPoint.Show();
            this.Close();
        }
    }
}
