using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;


namespace DoubleDashPlotter
{
    public partial class CaptureWindow : Window
    {

        public Bitmap capturedBitmapFull;
        public Bitmap capturedBitmap;

        // Golden rule aspect ratio, DO NOT CHANGE if there is no reason for it (Changing will break half of the code)
        private readonly double aspectRatio = 114.0 / 66.0;

        // Declare coordinate fileds to store CaptureWindow position
        public double windowLeft = double.NaN;
        public double windowTop = double.NaN;
        public double windowWidth = double.NaN;
        public double windowHeight = double.NaN;

        public CaptureWindow()
        {
            InitializeComponent();

            // Atttach window movement event handler to update the values of the window's coordinates
            this.LocationChanged += Window_Loaded;
            UpdateWindowCoordinates();


            // capturedBitmapFull = CaptureScreen();

            windowHeight = double.NaN;

            double topLeftX = this.Left;
            double topLeftY = this.Top;

            double bottomRightX = this.Left + this.Width;
            double bottomRightY = this.Top + this.Height;

        }

        // Core method that can be called directly
        private void UpdateWindowCoordinates()
        {
            //Lock vars from updating if the value is NaN
            windowLeft = !double.IsNaN(this.Left) ? this.Left : windowLeft;
            windowTop = !double.IsNaN(this.Top) ? this.Top : windowTop;
            windowWidth = !double.IsNaN(this.Width) ? this.Width : windowWidth;
            windowHeight = !double.IsNaN(this.Height) ? this.Height : windowHeight;


            // Calculate the top-left coordinates
            double topLeftX = windowLeft;
            double topLeftY = windowTop;

            // Calculate the bottom-right coordinates
            double bottomRightX = windowLeft + windowWidth;
            double bottomRightY = windowTop + windowHeight;

            // Output the coordinates to the console
            Console.Write("topLeftX is: " + topLeftX);
            Console.Write("topLeftY is: " + topLeftY);
            Console.Write("bottomRightX is: " + bottomRightX);
            Console.Write("bottomRightY is: " + bottomRightY);
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            windowLeft += 10;

            UpdateWindowCoordinates();
        }

        // PInvoke to access native Win32 APIs for screen capture
        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        const int SRCCOPY = 0x00CC0020;

        // Make sure CaptureScreenVarPush is async
        public async Task CaptureScreenVarPush()
        {
            await CropBitmapBasedOnWindowCoords(); // Await here too
            Console.WriteLine("DEBUG VARPUSH!!!");
        }


        // Event handler to maintain the aspect ratio
        private void CaptureWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Get the current width and height of the window
            double newWidth = e.NewSize.Width;
            double newHeight = e.NewSize.Height;

            // Calculate the target height based on the aspect ratio
            double targetHeight = newWidth / aspectRatio;

            // Calculate the target width based on the aspect ratio
            double targetWidth = newHeight * aspectRatio;

            // Determine whether the width or the height is being resized more
            if (newHeight != e.PreviousSize.Height) // Height is changing
            {
                // Adjust the width to match the aspect ratio
                this.Width = targetWidth;
            }
            else if (newWidth != e.PreviousSize.Width) // Width is changing
            {
                // Adjust the height to match the aspect ratio
                this.Height = targetHeight;
            }
        }

        public static Bitmap CaptureScreen()
        {
            // Get the desktop window handle and device context
            IntPtr desktopWindow = GetDesktopWindow();
            IntPtr desktopDC = GetWindowDC(desktopWindow);

            // Get the size of the primary screen
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            // Create a memory device context compatible with the desktop device context
            IntPtr memoryDC = CreateCompatibleDC(desktopDC);

            // Create a compatible bitmap to store the screenshot
            IntPtr bitmap = CreateCompatibleBitmap(desktopDC, screenWidth, screenHeight);

            // Select the new bitmap into the memory device context
            IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

            // Copy the screen content into the memory device context (bitmap)
            BitBlt(memoryDC, 0, 0, screenWidth, screenHeight, desktopDC, 0, 0, SRCCOPY);

            // Create a .NET Bitmap from the captured screenshot
            Bitmap screenshot = Bitmap.FromHbitmap(bitmap);

            // Clean up resources
            SelectObject(memoryDC, oldBitmap);
            DeleteObject(bitmap);
            DeleteDC(memoryDC);
            ReleaseDC(desktopWindow, desktopDC);

            return screenshot;
        }

        public static BitmapSource CaptureScreenAsBitmapSource()
        {
            Bitmap bitmap = CaptureScreen();

            // Convert the Bitmap to BitmapSource (which WPF uses)
            IntPtr hBitmap = bitmap.GetHbitmap();

            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // Clean up the HBitmap
            DeleteObject(hBitmap);

            return bitmapSource;
        }

        /*
        public async void CropBitmapBasedOnWindowCoords() {

            Console.WriteLine("DEBUG CROP!!!");

            // Calculate the top-left coordinates
            double topLeftX = windowLeft;
            double topLeftY = windowTop;

            // Calculate the bottom-right coordinates
            double bottomRightX = windowLeft + windowWidth;
            double bottomRightY = windowTop + windowHeight;

            CaptureScreen();
            await Task.Run(async () =>
            {
                // Wait until the values are no longer NaN
                while (double.IsNaN(windowLeft) || double.IsNaN(windowHeight) || double.IsNaN(windowTop) || double.IsNaN(windowWidth))
                {
                    // Sleep for a short period before checking again
                    await Task.Delay(100); // Check every 100ms (can adjust as needed)
                }

                // Once the values are valid, perform the crop operation
                capturedBitmap = CropBitmap(capturedBitmapFull, windowLeft, windowTop, windowLeft + windowWidth, windowTop + windowHeight);
                Console.WriteLine("DEBUG CROP ASYNC!!!");
            });
        }
        */

        public async Task CropBitmapBasedOnWindowCoords()
        {
            Console.WriteLine("DEBUG CROP!!!");

            // Wait until the values are valid
            while (double.IsNaN(windowLeft) || double.IsNaN(windowHeight) || double.IsNaN(windowTop) || double.IsNaN(windowWidth))
            {
                await Task.Delay(100); // Check every 100ms
            }

            // Calculate the top-left and bottom-right coordinates
            double topLeftX = windowLeft;
            double topLeftY = windowTop;
            double bottomRightX = windowLeft + windowWidth;
            double bottomRightY = windowTop + windowHeight;

            capturedBitmapFull = CaptureScreen(); // Ensure this sets the capturedBitmapFull correctly

            // Perform the crop operation
            capturedBitmap = CropBitmap(capturedBitmapFull, topLeftX, topLeftY, bottomRightX, bottomRightY);
            Console.WriteLine("DEBUG CROP ASYNC!!!");
        }

        public static Bitmap CropBitmap(Bitmap source, double topLeftX, double topLeftY, double bottomRightX, double bottomRightY)
        {
            // Convert double to int (round or truncate as needed for pixel values)
            int x1 = (int)Math.Round(topLeftX);
            int y1 = (int)Math.Round(topLeftY);
            int x2 = (int)Math.Round(bottomRightX);
            int y2 = (int)Math.Round(bottomRightY);

            // Calculate the width and height of the cropping rectangle
            int width = x2 - x1;
            int height = y2 - y1;

            // Ensure the rectangle is within the bounds of the bitmap
            if (x1 < 0 || y1 < 0 || x2 > source.Width || y2 > source.Height)
            {
                throw new ArgumentOutOfRangeException("The cropping rectangle is outside the bounds of the bitmap.");
            }

            // Create the cropping rectangle using the integer values
            Rectangle cropRect = new Rectangle(x1, y1, width, height);

            // Create a new bitmap to hold the cropped area
            Bitmap croppedBitmap = new Bitmap(cropRect.Width, cropRect.Height);

            // Use a Graphics object to draw the cropped area from the source bitmap into the new bitmap
            using (Graphics g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(source, new Rectangle(0, 0, croppedBitmap.Width, croppedBitmap.Height), cropRect, GraphicsUnit.Pixel);
            }

            return croppedBitmap;
        }

        // This method allows the window to be dragged
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }


    }
}
