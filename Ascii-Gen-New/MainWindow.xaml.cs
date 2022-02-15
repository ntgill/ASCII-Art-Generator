using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Ascii_Gen_New {
    public partial class MainWindow : Window {
        // FIELDS
        string path = "";
        bool multithread = true;
        int numberOfThreads = 6;
        int colorRange = 1;
        bool invert = false;
        bool useKernels = true;
        int kernelSize = 4;
        System.Windows.Media.Brush ogColor;
        string currentTime = "00:00:00";
        DispatcherTimer tmr = new DispatcherTimer();
        Stopwatch sw = new Stopwatch();
        


        // CONSTRUCTOR
        public MainWindow() {
            InitializeComponent();

            // save button color
            ogColor = btnGenerate.Background;

            // initialize timer
            tmr.Tick += new EventHandler(Timer_Tick);
            tmr.Interval = new TimeSpan(0, 0, 0, 0, 1);

            // initialize combo box in settings
            cmbRange.ItemsSource = new string[] { "1", "2", "3", "4", "5" };
        }


        #region METHODS

        #region WORKER METHODS

        private BackgroundWorker worker_Initialize() {
            // background worker initialization
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            return worker;
        }

        private void worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e) {

            if (e.Result == null) {
                throw new Exception("Result is null");
            }//end if

            txtAscii.Text = e.Result.ToString();
            StopClock();
            btnGenerate.Background = ogColor;

            //Keep same proportions if different kernel size
            if (kernelSize != 4) {
                txtAscii.FontSize = kernelSize * 1.5;
            } else {
                txtAscii.FontSize = 6;
            }//end if

        }//end method

        private void worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            // get width of the grid column for the text box
            double actualWidth = txtBoxColumn.ActualWidth;

            // bitmap of selected image
            Bitmap bmp = new Bitmap(path);

            if ((sender as BackgroundWorker) == null)
            {
                throw new Exception("Sender is null");
            }

            // Call asciitize method
            Asciify asciify = new Asciify(sender as BackgroundWorker, actualWidth, multithread, numberOfThreads, colorRange, invert, useKernels, kernelSize);
            e.Result = asciify.Asciitize(bmp);


        }

        #endregion

        #region FORM OBJECT METHODS
        private void EnterKeyPress(object sender, KeyEventArgs e) {

            if (imgMain.Source != null) {
                if (e.Key == Key.Return) {
                    btnGenerate_Click(sender, e);
                }//end if
            }//end if
        }//end event

        private void btnGenerate_Click(object sender, RoutedEventArgs e) {// generate button; makes the ascii image

            double temp = txtAscii.FontSize / kernelSize;
            if (imgMain.Source != null) {
                // change the generate buttons color to red
                btnGenerate.Background = System.Windows.Media.Brushes.IndianRed;

                //Show progress bar and timer
                progressBar.Visibility = Visibility.Visible;
                lblTimer.Visibility = Visibility.Visible;

                // starts the timer
                StartClock();

                // init worker and start asciify
                BackgroundWorker worker = worker_Initialize();
                worker.RunWorkerAsync();
            }//end if
        }//end event

        private void menuOpen_Click(object sender, RoutedEventArgs e) {// handles the open button in the menu box
            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == true) {
                path = ofd.FileName;
                Uri uri = new Uri(path);
                BitmapImage bmi = new BitmapImage(uri);
                imgMain.Source = bmi;

                // empty progress bar and timer
                progressBar.Value = 0;
                ResetClock();
                var window = Window.GetWindow(this);
                window.KeyDown += EnterKeyPress;
            }//end if
        }//end event

        private void menuCopy_Click(object sender, RoutedEventArgs e) {// copy ascii art string to clipboard
            if (txtAscii.Text != "") {
                Clipboard.SetText(txtAscii.Text);
                StartPopup();
            }//end if
        }//end event

        private void menuThread_Click(object sender, RoutedEventArgs e) {// toggle multithreading
            if (multithread) {
                multithread = false;
            } else {
                multithread = true;
            }//end if
        }//end event

        private void menuInvert_Click(object sender, RoutedEventArgs e) {
            if (invert) {
                invert = false;
            } else {
                invert = true;
            }//end if
        }//end event

        private void menuSettings_Click(object sender, RoutedEventArgs e) {
            settingsBox.Visibility = Visibility.Visible;
        }//end event

        private void menuKernel_Click(object sender, RoutedEventArgs e)
        {
            if (useKernels)
            {
                useKernels = false;
            }
            else
            {
                useKernels = true;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            numberOfThreads = int.Parse(txtThreads.Text);
            kernelSize = int.Parse(txtKernels.Text);
            colorRange = cmbRange.SelectedIndex + 1;
            settingsBox.Visibility = Visibility.Collapsed;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            settingsBox.Visibility = Visibility.Collapsed;
        }

        private void txtThreads_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }




        #endregion

        #region HELPER METHODS

        private void StartPopup()
        {
            //Make the label visible, starting the storyboard.
            lblPopup.Visibility = Visibility.Visible;

            DispatcherTimer t = new DispatcherTimer();
            //Set the timer interval to the length of the animation.
            t.Interval = new TimeSpan(0, 0, 3);
            t.Tick += (EventHandler)delegate (object? snd, EventArgs ea)
            {
                // The animation will be over now, collapse the label.
                lblPopup.Visibility = Visibility.Collapsed;
                
                if(snd != null)
                {
                    // Get rid of the timer.
                    ((DispatcherTimer)snd).Stop();
                }

            };
            t.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (sw.IsRunning)
            {
                TimeSpan ts = sw.Elapsed;
                currentTime = String.Format("{0:00}:{1:00}:{2:00}",
                ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                lblTimer.Content = currentTime;
            }
        }

        private void StartClock()
        {
            sw.Start();
            tmr.Start();
        }

        private void StopClock()
        {
            if (sw.IsRunning)
            {
                sw.Stop();
            }
            lblTimer.Content = currentTime;
            sw.Reset();
        }

        private void ResetClock()
        {
            sw.Reset();
            lblTimer.Content = "00:00:00";
        }



        #endregion

        #endregion


    }
}
