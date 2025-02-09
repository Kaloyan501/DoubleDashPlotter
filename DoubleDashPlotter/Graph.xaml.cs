using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ScottPlot;

namespace DoubleDashPlotter
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph : Window
    {

        static double[] SpeedData_Public;
        static List<int> TimeData_Public = new List<int> { };

        public Graph()
        {
            InitializeComponent();

            double[] dataX = { 1, 2, 3, 4, 5 };
            double[] dataY = { 1, 4, 9, 16, 25 };

            // Set up the plot background color to #1a1a1d (dark color)

            var DataPlotStyle = new PlotStyle()
            {
                DataBackgroundColor = ScottPlot.Color.FromHex("#1a1a1d"),
                //AxisColor = ScottPlot.Color.FromHex("#ffffff"),
                GridMajorLineColor = ScottPlot.Color.FromHex("#a6a6a6"),
                LegendBackgroundColor = ScottPlot.Color.FromHex("1a1a1ad")
            };

            //DataPlotStyle.Apply(DataPlot.Plot);

            //DataPlot.Plot.YLabel("Speed (KM/h)");
            //DataPlot.Plot.XLabel("Seconds");
            //DataPlot.Plot.Add.Scatter(dataX, dataY);
            //DataPlot.Refresh();

        }

        public static double[] GetSpeedData(string databaseFilePath)
        {
            // Check if the file exists
            if (!System.IO.File.Exists(databaseFilePath))
            {
                throw new ArgumentException("Database file not found.", nameof(databaseFilePath));
            }

            // List to store speed values
            List<double> speedValues = new List<double>();

            // Connection string
            string connectionString = $"Data Source={databaseFilePath};Version=3;";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to fetch all data from SpeedData table
                    string query = "SELECT Speed FROM SpeedData";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        // Read each row
                        while (reader.Read())
                        {
                            // Get the Speed value and add it to the list
                            if (reader["Speed"] != DBNull.Value)
                            {
                                speedValues.Add(Convert.ToDouble(reader["Speed"]));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }

            // Convert the list to an array and return it
            return speedValues.ToArray();
        }

        private static double[] SpeedDbToArray(string dbPath)
        {
            List<double> SpeedList = new List<double>();

            if (!System.IO.File.Exists(dbPath))
            {
                Console.WriteLine("Db does not exist at the path specified, returning null.");
                return null;
            }

            try
            {
                // Specify the version explicitly and construct the connection string correctly
                string connectionString = $"Data Source={dbPath};Version=3;";

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // SQL query to get the maximum ID from SpeedData
                    string query = "SELECT MAX(ID) FROM SpeedData";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();

                        if (result != DBNull.Value)
                        {
                            int lastID = Convert.ToInt32(result);
                            for (int i = 1; i <= lastID; i++)
                            {
                                string querySQL = "SELECT Speed FROM SpeedData WHERE ID = @ID";

                                using (SQLiteCommand commandSQL = new SQLiteCommand(querySQL, connection))
                                {
                                    // Add the ID as a parameter to the query to avoid SQL injection
                                    commandSQL.Parameters.AddWithValue("@ID", i);

                                    object resultSQL = commandSQL.ExecuteScalar();

                                    if (resultSQL != DBNull.Value)
                                    {
                                        double speed = Convert.ToDouble(resultSQL);
                                        SpeedList.Add(speed);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"No speed data found for ID: {i}");
                                        return null;
                                    }
                                }
                            }

                            return SpeedList.ToArray();
                        }
                        else
                        {
                            Console.WriteLine("The table SpeedData is empty. Returning null");
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}. Returning null.");
                return null;
            }
        }


        public static string ShowFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a SQLite Database File",
                Filter = "SQLite Database Files (*.db)|*.db",
                Multiselect = false
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true) // True indicates the user clicked OK
            {
                string selectedFilePath = openFileDialog.FileName;
                Console.WriteLine($"Selected file: {selectedFilePath}");
                return selectedFilePath;
            }

            return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string FilePath = ShowFileDialog();
            SpeedData_Public = SpeedDbToArray(FilePath);
            TimeData_Public = new List<int> { };

            try
            {
                for (int i = 0; i < SpeedData_Public.Length; i++)
                {
                    Console.WriteLine("Spewing out Timedata as it's generated");
                    TimeData_Public.Add((i + 1) * 5);
                    Console.WriteLine(i);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred while trying to loop trough data in db: " + ex);


                if (ex is NullReferenceException)
                {
                    MessageBox.Show("Error reading db: " + ex + " Your DB is probably empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                }
                else
                {
                    MessageBox.Show("Error reading db: " + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                return;
            }



            //DEBUG

            Console.WriteLine("SpeedData lenght is: " + SpeedData_Public.Length);
            Console.WriteLine("Timedata lenght is: " + TimeData_Public.ToArray().Length);
            /*
            for (int i = 0; i < SpeedData_Public.Length; i++)
            {
                Console.WriteLine("Spewing out SpeedData");
                Console.WriteLine(SpeedData_Public[i]);
            }

            for (int i = 0; i < TimeData_Public.ToArray().Length; i++)
            {
                Console.WriteLine("Spewing out TimeData");
                Console.WriteLine(TimeData_Public[i]);
            }

            // END OF DEBUG

            //DEBUG

            //int a = 0;
            //Console.WriteLine("Logging SpeedData_Public");
            //while (a != SpeedData_Public.Length) {
            //    Console.WriteLine(SpeedData_Public[a]);
            //}

            //END OF DEBUG
            */

            DataPlot.Plot.YLabel("Speed (KM/h)");
            DataPlot.Plot.XLabel("Seconds");
            DataPlot.Plot.Add.Scatter(TimeData_Public.ToArray(), SpeedData_Public);
            DataPlot.Refresh();

            //lowestspeed.Text = findlowestspeed();
            //highestspeed.Text = findhigheststreet();
            //averagespeed.Text = findaveragespeed();
            //averageaccel.Text = findaverageacceleration();

        }

        async private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {

            var lowestspeed_progress_var = new Progress<double>(value =>
            {
                // Update the progress bar based on the progress
                lowestspeed_progress.Value = value;
            });

            var highestspeed_progress_var = new Progress<double>(value =>
            {
                highestspeed_progress.Value = value;
            });

            var averagespeed_progress_var = new Progress<double>(value =>
            {
                averatespeed_progress.Value = value;
            });

            var averateacel_progress_var = new Progress<double>(value =>
            {
                averateacel_progress.Value = value;
            });
            
            

            lowestspeed.Text = await FindLowestSpeedAsync(lowestspeed_progress_var);
            highestspeed.Text = await FindHighestSpeedAsync(highestspeed_progress_var);
            averagespeed.Text = await FindAverageSpeedAsync(averagespeed_progress_var);
            averageaccel.Text = await FindAverageAccelerationAsync(averateacel_progress_var);
        }

        private void lowestspeed_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        

        public async Task<string> FindLowestSpeedAsync(IProgress<double> progress)
        {
            return await Task.Run(() =>
            {
                double currentLowestSpeed = double.MaxValue;
                for (int i = 0; i < SpeedData_Public.Length; i++)
                {
                    if (SpeedData_Public[i] < currentLowestSpeed)
                    {
                        currentLowestSpeed = SpeedData_Public[i];
                    }

                    // Report progress
                    progress?.Report((i + 1) / (double)SpeedData_Public.Length * 100);
                }

                return $"Lowest Speed: {currentLowestSpeed} km/h";
            });
        }

        public async Task<string> FindHighestSpeedAsync(IProgress<double> progress)
        {
            return await Task.Run(() =>
            {
                double currentHighestSpeed = double.MinValue;
                for (int i = 0; i < SpeedData_Public.Length; i++)
                {
                    if (SpeedData_Public[i] > currentHighestSpeed)
                    {
                        currentHighestSpeed = SpeedData_Public[i];
                    }

                    // Report progress
                    progress?.Report((i + 1) / (double)SpeedData_Public.Length * 100);
                }

                return $"Highest Speed: {currentHighestSpeed} km/h";
            });
        }

        public async Task<string> FindAverageSpeedAsync(IProgress<double> progress)
        {
            return await Task.Run(() =>
            {
                double totalSpeed = 0;
                for (int i = 0; i < SpeedData_Public.Length; i++)
                {
                    totalSpeed += SpeedData_Public[i];

                    // Report progress
                    progress?.Report((i + 1) / (double)SpeedData_Public.Length * 100);
                }

                double averageSpeed = totalSpeed / SpeedData_Public.Length;
                return $"Average Speed: {averageSpeed:F2} km/h";
            });
        }

        public async Task<string> FindAverageSpeedAsyncTimeFrame(int startTimeSeconds, int endTimeSeconds)
        {
            // Validate input times.
            if (startTimeSeconds < 0 || endTimeSeconds < startTimeSeconds)
                throw new ArgumentException("Invalid time range specified.");

            return await Task.Run(() =>
            {
                // Convert the start and end times (in seconds) to indices.
                int startIndex = startTimeSeconds / 5;
                int endIndex = endTimeSeconds / 5;

                // Ensure the start index is within the bounds of SpeedData_Public.
                if (startIndex >= SpeedData_Public.Length)
                    throw new ArgumentOutOfRangeException(nameof(startTimeSeconds), "Start time exceeds available data range.");

                // Adjust the end index if it exceeds the data length.
                if (endIndex >= SpeedData_Public.Length)
                    endIndex = SpeedData_Public.Length - 1;

                double totalSpeed = 0;
                int count = 0;
                int totalPoints = endIndex - startIndex + 1;

                // Iterate only over the specified time window.
                for (int i = startIndex; i <= endIndex; i++)
                {
                    totalSpeed += SpeedData_Public[i];
                    count++;
                }

                double averageSpeed = totalSpeed / count;
                return $"Average Speed: {averageSpeed:F2} km/h";
            });
        }


        public async Task<string> FindAverageAccelerationAsync(IProgress<double> progress)
        {
            if (SpeedData_Public.Length < 2)
            {
                throw new ArgumentException("At least two data points are needed to calculate acceleration.");
            }

            return await Task.Run(() =>
            {
                double totalAcceleration = 0;
                int intervalCount = SpeedData_Public.Length - 1;

                for (int i = 0; i < intervalCount; i++)
                {
                    double speed1 = SpeedData_Public[i] * (1000.0 / 3600.0); // Convert km/h to m/s
                    double speed2 = SpeedData_Public[i + 1] * (1000.0 / 3600.0); // Convert km/h to m/s

                    double timeInterval = 5.0; // Assume 5 seconds between data points
                    double acceleration = (speed2 - speed1) / timeInterval; // Compute acceleration (m/s²)

                    totalAcceleration += acceleration;

                    // Report progress
                    progress?.Report(((i + 1) / (double)intervalCount) * 100);
                }

                double averageAcceleration = totalAcceleration / intervalCount;

                return $"The average acceleration is: {averageAcceleration:F5} m/s²"; // 5 decimal places
            });
        }

        public async Task<string> FindAccelerationBetweenTimesAsync(int startTime, int endTime)
        {
            if (TimeData_Public == null || SpeedData_Public == null || TimeData_Public.Count != SpeedData_Public.Length)
            {
                throw new InvalidOperationException("Time and speed data must be valid and of equal length.");
            }

            if (startTime >= endTime)
            {
                throw new ArgumentException("Start time must be less than end time.");
            }

            return await Task.Run(() =>
            {
                // Find the closest indices in TimeData_Public
                int startIndex = TimeData_Public.FindIndex(t => t >= startTime);
                int endIndex = TimeData_Public.FindIndex(t => t >= endTime);

                // Validate indices
                if (startIndex == -1 || endIndex == -1)
                {
                    throw new ArgumentException("Start or end time is out of range.");
                }

                // Get corresponding speeds
                double initialSpeed = SpeedData_Public[startIndex] * (1000.0 / 3600.0); // Convert km/h to m/s
                double finalSpeed = SpeedData_Public[endIndex] * (1000.0 / 3600.0); // Convert km/h to m/s

                // Get actual time values
                double actualStartTime = TimeData_Public[startIndex];
                double actualEndTime = TimeData_Public[endIndex];

                // Compute acceleration
                double deltaTime = actualEndTime - actualStartTime;
                double acceleration = (finalSpeed - initialSpeed) / deltaTime;

                // Report progress (simple 100% since it's instant)

                return $"Average acceleration for selected time frame is: {acceleration:F5} m/s²";
            });
        }


        private void StartingTimeHint_TextChanged(object sender, RoutedEventArgs e)
        {
            StartingTimeHint.Visibility = Visibility.Visible;
            if (StartingTime.Text.Length > 0)
            {
                StartingTimeHint.Visibility = Visibility.Hidden;
            }
        }

        private void EndTimeHint_TextChanged(object sender, RoutedEventArgs e)
        {
            EndTimeHint.Visibility = Visibility.Visible;
            if (EndTime.Text.Length > 0)
            {
                EndTimeHint.Visibility = Visibility.Hidden;
            }
        }

        async private void CalculateButton_TimeFrame_Click(object sender, RoutedEventArgs e)
        {
            TimeFrameCalculatedResult.Text = await FindAccelerationBetweenTimesAsync(int.Parse(StartingTime.Text), int.Parse(EndTime.Text));
            SpeedTimeFrameCalculatedResult.Text = await FindAverageSpeedAsyncTimeFrame(int.Parse(StartingTime.Text), int.Parse(EndTime.Text));


        }

        private void TimeFrameCalculatedResult_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        /*
        public async Task<string> FindAverageAccelerationAsync(IProgress<double> progress)
        {
            if (SpeedData_Public.Length < 2)
            {
                throw new ArgumentException("At least two data points are needed to calculate acceleration.");
            }

            return await Task.Run(() =>
            {
                double initialSpeed = SpeedData_Public[0] * (1000.0 / 3600.0); // Convert km/h to m/s
                double finalSpeed = SpeedData_Public[SpeedData_Public.Length - 1] * (1000.0 / 3600.0); // Convert km/h to m/s

                double totalTime = (SpeedData_Public.Length - 1) * 5; // Assume 5 seconds between each point
                double averageAcceleration = (finalSpeed - initialSpeed) / totalTime;

                // Report progress as a single step (100% complete)
                progress?.Report(100);

                return $"The average acceleration is: {averageAcceleration:F2} m/s²";
            });
        }
        */
        /*
        private string findlowestspeed()
        {

            int i = 0;
            double CurrentLowestSpeed = -9999;
            while (i != SpeedData_Public.Length)
            {
                if (CurrentLowestSpeed == -9999)
                {
                    CurrentLowestSpeed = SpeedData_Public[i];
                }

                if (SpeedData_Public[i] < CurrentLowestSpeed)
                {
                    CurrentLowestSpeed = SpeedData_Public[i];
                }
            }

            return $"Lowest Speed: {CurrentLowestSpeed} km/h";

        }

        private string findhigheststreet()
        {
            int i = 0;
            double CurrentHighestSpeed = -9999;
            while (i != SpeedData_Public.Length)
            {
                if (CurrentHighestSpeed == -9999)
                {
                    CurrentHighestSpeed = SpeedData_Public[i];
                }

                if (SpeedData_Public[i] > CurrentHighestSpeed)
                {
                    CurrentHighestSpeed = SpeedData_Public[i];
                }
            }

            return $"Highest Speed: {CurrentHighestSpeed} km/h";
        }

        private string findaveragespeed()
        {
            int i = 0;
            double TotalSpeed = 0;
            int dataPointCount = SpeedData_Public.Length;

            foreach (double speed in SpeedData_Public)
            {
                TotalSpeed += speed;
            }

            return $"Average Speed: {(TotalSpeed / dataPointCount).ToString()} km/h";
        }

        private string findaverageacceleration()
        {
            if (SpeedData_Public.Length < 2)
            {
                throw new ArgumentException("At least two data points are needed to calculate acceleration.");
            }

            double initalSpeed = SpeedData_Public[0] * (1000.0 / 3600.0);
            double finalSpeed = SpeedData_Public[SpeedData_Public.Length - 1] * (1000.0 / 3600.0);

            double totalTime = (SpeedData_Public.Length - 1) * 5;

            double averageAcceleration = (finalSpeed - initalSpeed) / totalTime;

            return $"The average acceleration is: {averageAcceleration:F2} m/s²";
        }

        */

    }
}

