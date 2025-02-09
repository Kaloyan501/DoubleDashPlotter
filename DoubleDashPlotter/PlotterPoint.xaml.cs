using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Data.SQLite;

namespace DoubleDashPlotter
{
    /// <summary>
    /// Interaction logic for PlotterPoint.xaml
    /// </summary>
    public partial class PlotterPoint : Window
    {



        List<System.Drawing.Point> points = new List<System.Drawing.Point>()
        {
            //Right number (Least significant)

            new System.Drawing.Point(86, 3), //Top Segment A1 
            new System.Drawing.Point(86, 62), //Bottom Segment B1 
            new System.Drawing.Point(110, 22), // Top Right Segment C1
            new System.Drawing.Point(64, 22), // Top Left Segment D1
            new System.Drawing.Point(109, 43), // Bottom Right Segment E1 
            new System.Drawing.Point(64, 45), // Bottom Left Segment F1
            new System.Drawing.Point(86, 40), // Center Segment G1

            // Left Number (Most significant)

            new System.Drawing.Point(26, 3), //Top Segment A2
            new System.Drawing.Point(26, 62), //Bottom Segment B2
            new System.Drawing.Point(49, 22), //Top Right Segment C2
            new System.Drawing.Point(3, 22), //Top Left Segment D2
            new System.Drawing.Point(49, 44), //Bottom Right Segment E2
            new System.Drawing.Point(4, 45), //Bottom Left Segment F2
            new System.Drawing.Point(26, 40) // Center Segment G2
        };

        private DispatcherTimer recordSpeed;

        CaptureWindow captureWindow;

        bool Lock_SpeedCaptureState = false;

        private int _number = 1;        // Number part of ID (1, 2, ...)
        private char _letter = 'A';     // Letter part of ID (A, B, ..., G)
        bool InitialPass = false;
        bool InitialPassTwos = false;
        bool SwitchToTwos = false;
        private bool isDragging = false;
        private System.Windows.Point mouseOffset; // Track mouse position relative to the dot
        private Ellipse selectedDot = null;
        int SpeedGate = 80;

        private bool AreProbesShown = false;


        public PlotterPoint(bool launchDolphin)
        {
            InitializeComponent();

            Lock_SpeedCaptureState = false;

            captureWindow = new CaptureWindow();
            captureWindow.Show();

            this.Closed += IsClosed;

            //Bitmap bitmap = new Bitmap(@"C:\Users\Kaloyan Donev\Desktop\MKSpeedPlotter\OCR_TEST\SpeedOHightRes.png");

            //MessageBox.Show(FindNumberFromBitmap(ConvertYellowToBlackWithWhiteBackground(bitmap)).ToString());

            recordSpeed = new DispatcherTimer();
            recordSpeed.Interval = TimeSpan.FromSeconds(1);
            recordSpeed.Tick += GetSpeed;

            MainWindow mainWindow = new MainWindow();

            string fileName = MainWindow.FileName;

            MessageBox.Show(fileName);

            if (launchDolphin)
            {
                OpenDolphinWithParameter(fileName);
            }





        }

        public List<System.Drawing.Point> DumpDotCenterCoordinates()
        {
            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            string[] orderedTags = { "A1", "B1", "C1", "D1", "E1", "F1", "G1", "A2", "B2", "C2", "D2", "E2", "F2", "G2" };
            

            foreach (var tag in orderedTags)
            {
                foreach (UIElement child in DotCanvas.Children)
                {
                    if (child is Ellipse dot && dot.Tag?.ToString() == tag)
                    {
                        // Get the position of the dot on the canvas
                        double x = Canvas.GetLeft(dot);
                        double y = Canvas.GetTop(dot);

                        // Calculate the center coordinates and correct for the 2-pixel offset
                        double centerX = x + dot.Width / 2; // Subtract 2 to adjust for the dot size REMOVED FOR BUG - 2
                        double centerY = y + dot.Height / 2; // Subtract 2 to adjust for the dot size REMOVED FOR BUG - 2 

                        points.Add(new System.Drawing.Point((int)centerX, (int)centerY));

                        // Debugging output for confirmation
                        Console.WriteLine($"Found dot with tag {tag} at center ({(int)centerX}, {(int)centerY})");

                        // Move to the next tag after finding the correct dot
                        break;
                    }
                }
            }

            return points;
        }

        private void ColorDot(string name, System.Windows.Media.Brush color)
        {
            // Find the dot on the canvas with the specified Tag
            foreach (var child in DotCanvas.Children)
            {
                if (child is Ellipse dot && dot.Tag as string == name)
                {
                    // Change the fill color of the dot
                    dot.Fill = color;
                    Console.WriteLine($"Changed color of dot with Tag: {name}");
                    return; // Exit after finding and coloring the first match
                }
            }

            Console.WriteLine($"Dot with Tag: {name} not found.");
        }


        private void PlaceDot(double x, double y)
        {

            if (InitialPass == false)
            {
                InitialPass = true;
                _letter = 'A';
                _number = 1;
            }
            else
            {
                if (IncrementLetter(_letter) != 'H')
                {
                    _letter = IncrementLetter(_letter);
                }
                else
                {
                    if (SwitchToTwos == false)
                    {
                        SwitchToTwos = true;
                        _letter = 'A';
                    }

                    if (InitialPassTwos == false)
                    {
                        InitialPassTwos = true;
                        _letter = 'A';
                        _number = 2;
                    }
                    else
                    {
                        _letter = IncrementLetter(_letter);
                        _number = 2;
                    }
                }
            }


            Ellipse dot = new Ellipse
            {
                Fill = System.Windows.Media.Brushes.Red,
                Width = 4,
                Height = 4,
                Tag = _letter.ToString() + _number.ToString() // Corrected tag creation
            };


            Console.WriteLine(String.Concat(_letter.ToString(), _number));

            // Set the position of the dot on the canvas
            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);

            // Attach mouse event handlers for dragging
            dot.MouseDown += Dot_MouseDown;
            dot.MouseMove += Dot_MouseMove;
            dot.MouseUp += Dot_MouseUp;

            Console.WriteLine("PlaceDot called with coordinates: (" + x + ", " + y + ")");
            Console.WriteLine($"Creating dot with Tag: {_letter + _number}");

            // Add the dot to the canvas
            DotCanvas.Children.Add(dot);
            Console.WriteLine("Dot added. Total dots in canvas: " + DotCanvas.Children.Count);
        }

        // Mouse Down Event Handler - Initialize dragging
        private void Dot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse dot && e.LeftButton == MouseButtonState.Pressed)
            {
                isDragging = true;
                selectedDot = dot;

                // Capture the mouse position relative to the dot
                mouseOffset = e.GetPosition(dot);

                // Capture the mouse to ensure it keeps receiving events even when moved outside the dot
                dot.CaptureMouse();
            }
        }

        // Mouse Move Event Handler - Update dot position
        private void Dot_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedDot != null)
            {
                // Get the current mouse position relative to the canvas
                System.Windows.Point mousePosition = e.GetPosition(DotCanvas);

                // Calculate new position for the dot based on the initial offset
                double newX = mousePosition.X - mouseOffset.X;
                double newY = mousePosition.Y - mouseOffset.Y;

                // Update the dot position on the canvas
                Canvas.SetLeft(selectedDot, newX);
                Canvas.SetTop(selectedDot, newY);
            }
        }

        // Mouse Up Event Handler - End dragging
        private void Dot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;

                // Release mouse capture
                selectedDot?.ReleaseMouseCapture();
                selectedDot = null;
            }
        }

        static (double, double) ConvertPointToDoubles(System.Drawing.Point point)
        {
            double x = (double)point.X;
            double y = (double)point.Y;
            return (x, y);
        }

        public static char IncrementLetter(char input)
        {
            if (char.IsLetter(input))
            {
                // Increment the character
                if (input == 'z')
                {
                    return 'a'; // Wrap around for lowercase 'z'
                }
                else if (input == 'Z')
                {
                    return 'A'; // Wrap around for uppercase 'Z'
                }
                else
                {
                    return (char)(input + 1); // Increment the character
                }
            }
            else
            {
                // If it's not a letter, return the input character unchanged
                return input;
            }
        }


        private void OpenDolphinWithParameter(string parameter)
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            MessageBox.Show(appDirectory);

            string exePath = System.IO.Path.Combine(appDirectory, "Depends", "Dolphin", "Dolphin-x64", "Dolphin.exe");
            MessageBox.Show(exePath);

            // Enclose the parameter in double quotes to handle spaces
            string quotedParameter = $"\"{parameter}\"";

            ProcessStartInfo startDolphin = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = quotedParameter,
                UseShellExecute = false,
                WorkingDirectory = System.IO.Path.GetDirectoryName(exePath)
            };

            try
            {
                Process.Start(startDolphin);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void IsClosed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        public async void GetSpeed(object sender, EventArgs e)
        {
            //CaptureWindow captureWindow = new CaptureWindow();

            await captureWindow.CaptureScreenVarPush(); // Make sure this method is async and returns a Task

            // After capturing, ensure that capturedBitmap is set
            if (captureWindow.capturedBitmap != null)
            {
                SpeedCanvas.Source = ConvertBitmapToImageSource(ConvertYellowToBlackWithWhiteBackground(captureWindow.capturedBitmap));
                int convertedBitmap = FindNumberFromBitmap(ConvertYellowToBlackWithWhiteBackground(captureWindow.capturedBitmap));
                Speed_Label.Content = convertedBitmap;
                if (convertedBitmap >= 0 && convertedBitmap < SpeedGate)
                {
                    InsertSpeed(convertedBitmap);
                }
            }
            else
            {
                Console.WriteLine("Error: capturedBitmap is null.");
            }

            // Output the coordinates to the console
            Console.WriteLine("topLeftX is: " + captureWindow.windowLeft);
        }

        // Function to insert a Speed value into the database
        static void InsertSpeed(int speed)
        {

            int speedkmh = Convert.ToInt32(speed * 1.60934);

            // Specify the path for the SQLite database
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current.db"); // Replace with your desired path

            using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
            {
                connection.Open();

                // Create table query (if the table doesn't exist)
                string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS SpeedData (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Speed REAL,
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
            );";

                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Insert the speed value
                string insertQuery = "INSERT INTO SpeedData (Speed) VALUES (@Speed)";
                using (var insertCommand = new SQLiteCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@Speed", speed);
                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        public Bitmap GrayScaleFilter(Bitmap image)
        {
            Bitmap grayScale = new Bitmap(image.Width, image.Height);

            for (Int32 y = 0; y < grayScale.Height; y++)
                for (Int32 x = 0; x < grayScale.Width; x++)
                {
                    System.Drawing.Color c = image.GetPixel(x, y);

                    Int32 gs = (Int32)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);

                    grayScale.SetPixel(x, y, System.Drawing.Color.FromArgb(gs, gs, gs));
                }
            return grayScale;
        }

        public static Bitmap ConvertYellowToBlackWithWhiteBackground(Bitmap originalImage, int backgroundThreshold = 240)
        {
            // Create a new bitmap for the output
            Bitmap outputBitmap = new Bitmap(originalImage.Width, originalImage.Height);

            for (int y = 0; y < originalImage.Height; y++)
            {
                for (int x = 0; x < originalImage.Width; x++)
                {
                    System.Drawing.Color originalColor = originalImage.GetPixel(x, y);

                    // Calculate the grayscale value to determine background
                    int grayValue = (int)(originalColor.R * 0.3 + originalColor.G * 0.59 + originalColor.B * 0.11);

                    // Check for background pixels (light)
                    if (grayValue > backgroundThreshold)
                    {
                        // Set background to white
                        outputBitmap.SetPixel(x, y, System.Drawing.Color.White);
                    }
                    else
                    {
                        // Check for yellowish colors
                        if (IsYellowish(originalColor))
                        {
                            // Change yellowish colors to black
                            outputBitmap.SetPixel(x, y, System.Drawing.Color.White);
                        }
                        else
                        {
                            // Retain the original color (which should be black borders)
                            outputBitmap.SetPixel(x, y, System.Drawing.Color.Black);
                        }
                    }
                }
            }

            // Return the modified bitmap
            return outputBitmap;
        }

        private static bool IsYellowish(System.Drawing.Color color)
        {
            // Define the lower and upper bounds for yellowish colors using FromArgb
            System.Drawing.Color lowerBound = System.Drawing.Color.FromArgb(60, 60, 0); // Lower bound RGB
            System.Drawing.Color upperBound = System.Drawing.Color.FromArgb(255, 255, 220); // Upper bound RGB

            // Check if the color is within the bounds
            bool isWithinRed = color.R >= lowerBound.R && color.R <= upperBound.R;
            bool isWithinGreen = color.G >= lowerBound.G && color.G <= upperBound.G;
            bool isWithinBlue = color.B >= lowerBound.B && color.B <= upperBound.B;

            // Return true if the color is within the yellowish bounds
            return isWithinRed && isWithinGreen && isWithinBlue;
        }

        public byte[] BitmapToByteArray(Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Save the bitmap to the memory stream in a specific image format
                bitmap.Save(memoryStream, ImageFormat.Png); // Or use other formats like ImageFormat.Jpeg, ImageFormat.Bmp, etc.
                return memoryStream.ToArray();
            }
        }


        private static bool IsPixelBlackThicken(Bitmap image, int x, int y)
        {
            return image.GetPixel(x, y).ToArgb() == System.Drawing.Color.Black.ToArgb();
        }

        private static void SetBlack(byte[] outputBytes, int x, int y, int stride)
        {
            // Calculate the index of the byte that contains the pixel (1-bit pixels are packed into bytes)
            int byteIndex = (y * stride) + (x / 8);
            int bitIndex = 7 - (x % 8); // We need to set the corresponding bit in the byte

            // Set the bit for black (0)
            outputBytes[byteIndex] &= (byte)(~(1 << bitIndex)); // Clear the bit to 0 for black
        }

        private static bool IsWithinBounds(int x, int y, Bitmap image)
        {
            return x >= 0 && x < image.Width && y >= 0 && y < image.Height;
        }

        public ImageSource ByteArrayToImageSource(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            using (MemoryStream memoryStream = new MemoryStream(imageData))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Freeze to make it cross-thread accessible

                return bitmapImage;
            }
        }

        public static bool IsPixelBlack(Bitmap bitmap, System.Drawing.Point point, int buffer = 17)
        {
            // Get the pixel color at the specified coordinates from the Point object
            System.Drawing.Color pixelColor = bitmap.GetPixel(point.X, point.Y);

            // Check if the R, G, and B values are within the buffer from black (0, 0, 0)
            if (pixelColor.R <= buffer && pixelColor.G <= buffer && pixelColor.B <= buffer)
            {
                return true; // Pixel is close enough to black
            }
            else
            {
                return false; // Pixel is not close to black
            }
        }

        public int FindNumberFromBitmap(Bitmap bitmap)
        {
            int LeastSig = 0;
            int MostSig = 0;
            //Code to find least significant number
            bool A1 = false;
            bool B1 = false;
            bool C1 = false;
            bool D1 = false;
            bool E1 = false;
            bool F1 = false;
            bool G1 = false;
            bool A2 = false;
            bool B2 = false;
            bool C2 = false;
            bool D2 = false;
            bool E2 = false;
            bool F2 = false;
            bool G2 = false;

            ColorDot("A1", System.Windows.Media.Brushes.Red);
            ColorDot("B1", System.Windows.Media.Brushes.Red);
            ColorDot("C1", System.Windows.Media.Brushes.Red);
            ColorDot("D1", System.Windows.Media.Brushes.Red);
            ColorDot("E1", System.Windows.Media.Brushes.Red);
            ColorDot("F1", System.Windows.Media.Brushes.Red);
            ColorDot("G1", System.Windows.Media.Brushes.Red);
            ColorDot("A2", System.Windows.Media.Brushes.Red);
            ColorDot("B2", System.Windows.Media.Brushes.Red);
            ColorDot("C2", System.Windows.Media.Brushes.Red);
            ColorDot("D2", System.Windows.Media.Brushes.Red);
            ColorDot("E2", System.Windows.Media.Brushes.Red);
            ColorDot("F2", System.Windows.Media.Brushes.Red);
            ColorDot("G2", System.Windows.Media.Brushes.Red);
            for (int i = 0; i < points.Count / 2; i++)
            {
                if (i == 0 && IsPixelBlack(bitmap, points[i]))
                {
                    A1 = true;
                    ColorDot("A1", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 1 && IsPixelBlack(bitmap, points[i]))
                {
                    B1 = true;
                    ColorDot("B1", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 2 && IsPixelBlack(bitmap, points[i]))
                {
                    C1 = true;
                    ColorDot("C1", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 3 && IsPixelBlack(bitmap, points[i]))
                {
                    D1 = true;
                    ColorDot("D1", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 4 && IsPixelBlack(bitmap, points[i]))
                {
                    E1 = true;
                    ColorDot("E1", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 5 && IsPixelBlack(bitmap, points[i]))
                {
                    F1 = true;
                    ColorDot("F1", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 6 && IsPixelBlack(bitmap, points[i]))
                {
                    G1 = true;
                    ColorDot("G1", System.Windows.Media.Brushes.Blue);
                }

            }

            if (A1 && B1 && C1 && D1 && E1 && F1 && !G1) // 0
            {
                LeastSig = 0;
            }
            else if (!A1 && !B1 && C1 && !D1 && E1 && !F1 && !G1) // 1
            {
                LeastSig = 1;
            }
            else if (A1 && B1 && C1 && !D1 && !E1 && F1 && !G1)
            { // 2
                LeastSig = 2;
            }
            else if (A1 && B1 && C1 && !D1 && E1 && !F1 && !G1) // 3
            {
                LeastSig = 3;
            }
            else if (!A1 && !B1 && C1 && D1 && E1 && !F1 && !G1) // 4
            {
                LeastSig = 4;
            }
            else if (A1 && B1 && !C1 && D1 && E1 && !F1 && !G1) // 5
            {
                LeastSig = 5;
            }
            else if (A1 && B1 && !C1 && D1 && E1 && F1 && !G1) // 6
            {
                LeastSig = 6;
            }
            else if (A1 && !B1 && C1 && D1 && E1 && !F1 && !G1) // 7
            {
                LeastSig = 7;
            }
            else if (A1 && B1 && C1 && D1 && E1 && F1 && G1) // 8 (all segments active)
            {
                LeastSig = 8;
            }
            else if (A1 && B1 && C1 && D1 && E1 && !F1 && !G1) // 9
            {
                LeastSig = 9;
            }
            else
            {
                LeastSig = -999999999; // Error handling
                Console.WriteLine("[WARNING: No valid least significant number found]");
            }


            // Code to find the most significant number

            for (int i = 7; i < points.Count; i++)
            {
                if (i == 7 && IsPixelBlack(bitmap, points[i]))
                {
                    A2 = true;
                    ColorDot("A2", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 8 && IsPixelBlack(bitmap, points[i]))
                {
                    B2 = true;
                    ColorDot("B2", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 9 && IsPixelBlack(bitmap, points[i]))
                {
                    C2 = true;
                    ColorDot("C2", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 10 && IsPixelBlack(bitmap, points[i]))
                {
                    D2 = true;
                    ColorDot("D2", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 11 && IsPixelBlack(bitmap, points[i]))
                {
                    E2 = true;
                    ColorDot("E2", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 12 && IsPixelBlack(bitmap, points[i]))
                {
                    F2 = true;
                    ColorDot("F2", System.Windows.Media.Brushes.Blue);
                }
                else if (i == 13 && IsPixelBlack(bitmap, points[i]))
                {
                    G2 = true;
                    ColorDot("G2", System.Windows.Media.Brushes.Blue);
                }
            }

            if (A2 && B2 && C2 && D2 && E2 && F2 && !G2) // 0
            {
                MostSig = 0;
            }
            else if (!A2 && !B2 && C2 && !D2 && E2 && !F2 && !G2) // 1
            {
                MostSig = 1;
            }
            else if (A2 && B2 && C2 && !D2 && !E2 && F2 && !G2)
            { // 2
                MostSig = 2;
            }
            else if (A2 && B2 && C2 && !D2 && E2 && !F2 && !G2) // 3
            {
                MostSig = 3;
            }
            else if (!A2 && !B2 && C2 && D2 && E2 && !F2 && !G2) // 4
            {
                MostSig = 4;
            }
            else if (A2 && B2 && !C2 && D2 && E2 && !F2 && !G2) // 5
            {
                MostSig = 5;
            }
            else if (A2 && B2 && !C2 && D2 && E2 && F2 && !G2) // 6
            {
                MostSig = 6;
            }
            else if (A2 && !B2 && C2 && D2 && E2 && !F2 && !G2) // 7
            {
                MostSig = 7;
            }
            else if (A2 && B2 && C2 && D2 && E2 && F2 && G2) // 8 (all segments active)
            {
                MostSig = 8;
            }
            else if (A2 && B2 && C2 && D2 && E2 && !F2 && !G2) // 9
            {
                MostSig = 9;
            }
            else if (!A2 && !B2 && !C2 && !D2 && !E2 && !F2 && !G2)
            {
                MostSig = 0;
            }
            else
            {
                MostSig = -999999999; // Error handling
                Console.WriteLine("[WARNING: No valid most significant number found]");
            }

            // Debug output to check values
            Console.WriteLine($"A2: {A2}, B2: {B2}, C2: {C2}, D2: {D2}, E2: {E2}, F2: {F2}, G2: {G2}");



            // Print all A1 to G1
            Console.WriteLine($"A1: {A1}");
            Console.WriteLine($"B1: {B1}");
            Console.WriteLine($"C1: {C1}");
            Console.WriteLine($"D1: {D1}");
            Console.WriteLine($"E1: {E1}");
            Console.WriteLine($"F1: {F1}");
            Console.WriteLine($"G1: {G1}");

            // Print all A2 to G2
            Console.WriteLine($"A2: {A2}");
            Console.WriteLine($"B2: {B2}");
            Console.WriteLine($"C2: {C2}");
            Console.WriteLine($"D2: {D2}");
            Console.WriteLine($"E2: {E2}");
            Console.WriteLine($"F2: {F2}");
            Console.WriteLine($"G2: {G2}");

            Console.WriteLine(MostSig);
            Console.WriteLine(LeastSig);

            return MostSig * 10 + LeastSig;
        }


        public static ImageSource ConvertBitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Save the bitmap to a MemoryStream
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                // Create a new BitmapImage from the stream
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Ensures the image can be reused after the stream is closed
                bitmapImage.EndInit();

                //bitmap.Dispose();

                return bitmapImage;
            }
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {

            // Specify the path to your image file
            string imagePath = @"C:\Users\Kaloyan Donev\Desktop\MKSpeedPlotter\OCR_TEST\SpeedOHightRes.png"; // Replace with your actual image path

            Bitmap bitmap = new Bitmap(imagePath);

            SpeedCanvas.Source = ConvertBitmapToImageSource(ConvertYellowToBlackWithWhiteBackground((bitmap)));
        }

        private void SpeedCapture_Click(object sender, RoutedEventArgs e)
        {
            if (Lock_SpeedCaptureState == false)
            {
                recordSpeed.Start();
                Lock_SpeedCaptureState = true;
                SpeedCapture.Content = "Stop Speed Capture";

                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current.db");

                Console.WriteLine("Path to db is: " + System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current.db"));

                if (!File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "current.db")))
                {

                    File.Create(dbPath).Dispose();

                }
                    // Create a new database connection (this will create the database if it doesn't exist)
                    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
                    {
                        // Open the connection to the database
                        connection.Open();

                        // Define the SQL command to create the table
                        string createTableCommand = @"
                CREATE TABLE IF NOT EXISTS SpeedData (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Speed INTEGER
                );";

                        // Create and execute the command
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = createTableCommand;
                            command.ExecuteNonQuery(); // Executes the SQL command
                        }

                        Console.WriteLine("Database and table created successfully.");
                    }
                }
            
            else if (Lock_SpeedCaptureState == true)
            {
                recordSpeed.Stop();
                Lock_SpeedCaptureState = false;
                SpeedCapture.Content = "Start Speed Capture";

                System.IO.File.Move(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "current.db"), System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory) + $"{DateTime.Now:HH}-{DateTime.Now:mm}-{DateTime.Now:ss}-{DateTime.Now:dd}-{DateTime.Now:MM}-{DateTime.Now:yyyy}.db");

            }
            else
            {
                MessageBox.Show("Lock_SpeedCaptureState is in an unpredictable state (neither false nor true). The app is now going to close.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (AreProbesShown == false)
            {
                AreProbesShown = true;
                Show_Probes.Content = "Hide Detection Probes";
                for (int i = 0; i < points.Count; i++)
                {
                    (double x, double y) = ConvertPointToDoubles(points[i]);
                    PlaceDot(x, y);
                }
            }
            else if (AreProbesShown == true)
            {
                AreProbesShown = false;
                DotCanvas.Children.Clear();
                Show_Probes.Content = "Show Detection Probes";
                _letter = 'A';
                _number = 1;
                InitialPass = false;
                InitialPassTwos = false;
                SwitchToTwos = false;
            }
            else
            {
                MessageBox.Show("AreProbesShown is in an unpredictable state (neither false nor true). The app is now going to close.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void Dump_Probes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Dump_Probes clicked.");
            points = DumpDotCenterCoordinates();

            Console.WriteLine("Points count: " + points.Count);
            if (points == null || points.Count == 0)
            {
                MessageBox.Show("No points calculated. Check DotCanvas and tags.");
                return;
            }

            string pointsData = string.Join("\n", points.Select(p => p.ToString()));
            MessageBox.Show("Calculated Dump Coordinates:\n" + pointsData);
        }

        public static int ConvertStringToInt(string str)
        {
            // Check if the string is not null and contains only digits
            if (string.IsNullOrEmpty(str) || !long.TryParse(str, out _))
            {
                throw new ArgumentException("Input string is not a valid number.");
            }

            // Convert the string to an integer
            return int.Parse(str);
        }

        private void Analize_Click_Func(object sender, RoutedEventArgs e)
        {
            Graph graph = new Graph();
            graph.Show();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sometimes incorrect data gets detected by the data points (Example: From 34 the speed jumps to 80). To remove some of these errors, you can set a gate for the max possible speed your cart can achieve. By default it is 80. The lower cap is always set to equal or above 0 because the speed of the kart can't be below 0", "Question", MessageBoxButton.OK, MessageBoxImage.Question);
        }

        private void Set_SpeedCap_Click(object sender, RoutedEventArgs e)
        {
            SpeedGate = ConvertStringToInt(TextBox_SpeedGate.Text);
            Console.WriteLine("Set speed gate to " + SpeedGate);
        }
    }
}
