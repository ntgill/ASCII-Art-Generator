using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;

public class Asciify {
    // FIELDS
    BackgroundWorker? worker;
    int txtBoxWidth;
    int txtBoxHeight;
    bool multithread;
    int numberOfThreads;
    int colorRange;
    bool invert;
    bool useKernels;
    int kernelWidth;
    int kernelHeight;

    // CONSTRUCTOR
    public Asciify(BackgroundWorker? worker, double txtBoxWidth, double txtBoxHeight, bool multithread, int numberOfThreads, int colorRange, bool invert, bool useKernels, int kernelWidth, int kernelHeight) {
        
        if (worker != null) {
            this.worker = worker;
        }//end if

        this.txtBoxWidth = (int)(txtBoxWidth / kernelWidth);
        this.txtBoxHeight = (int)(txtBoxHeight / kernelHeight);
        this.multithread = multithread;
        this.numberOfThreads = numberOfThreads;
        this.colorRange = colorRange;
        this.invert = invert;
        this.useKernels = useKernels;
        this.kernelWidth = kernelWidth;
        this.kernelHeight = kernelHeight;
    }//end constructor


    #region METHODS

    public string Asciitize(Bitmap bmp) {// accepts bitmap image, returns string of ascii text

        if (!useKernels) {
            // scale down the size of the bitmap
            bmp = Resize(bmp, txtBoxWidth);
        } else {
            bmp = Resize(bmp, txtBoxWidth * kernelWidth);
        }//end if

        // returns array of all pixels in bitmap, converted to grey
        double[] greyPixels = GetArrayOfGreyPixels(bmp);

        // get the normalized values for all grey pixels; value == 0 -- 1
        double[] normalizedPixels;
        if (useKernels) {
            return AverageColorNew1(greyPixels, bmp.Height, bmp.Width);
        } else {
            return AverageColorOld(greyPixels, bmp.Height, bmp.Width);
        }//end if

        if (multithread) {
            normalizedPixels = SpeedyNormalize(greyPixels, numberOfThreads);
        } else {
            normalizedPixels = SlowNormalize(greyPixels);
        }//end if

    }//end method

    private Bitmap Resize(Bitmap bmpIn, int txtBoxWidth) {// scales the bitmap to the given width

        // calculate height, bmp.H * txt.W / bmp.W == txt.H
        int txtBoxHeight = (int)Math.Ceiling((double)bmpIn.Height * txtBoxWidth / bmpIn.Width);

        // create bitmap with the new sizes
        Bitmap newMap = new Bitmap(txtBoxWidth, txtBoxHeight);

        // graphic object draws on the given image
        Graphics g = Graphics.FromImage(newMap);

        // INTERPOLATION: In the mathematical field of numerical analysis, interpolation is a type of estimation, a method of constructing new data points based on the range of a discrete set of known data points.

        //The interpolation mode produces high quality images
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        // draws original bmp {bmpIn} on the {newMap} at (0,0), with the given width and height
        g.DrawImage(bmpIn, 0, 0, txtBoxWidth, txtBoxHeight);

        // free memory
        g.Dispose();

        return newMap;
    }//end method

    private double[] GetArrayOfGreyPixels(Bitmap bmp) {// converts pixel to grey and stores it in array

        int count = 0;
        double[] greyPixels = new double[bmp.Width * bmp.Height];

        // loop thru bitmap
        for (int y = 0; y < bmp.Height; y++) {// i == the height of the image
            for (int x = 0; x < bmp.Width; x++) { // j == the width of image

                // get pixel at specified location
                Color pixel = bmp.GetPixel(x, y);

                // Convert pixel to greyscale
                greyPixels[count] = AverageColor(pixel);

                count++;
            }//end for
        }//end for
        return greyPixels;
    }//end method

    private double[] SlowNormalize(double[] arrayIn) {// takes array of every pixel in grey; returns array of values between 0-1

        double[] normalized = new double[arrayIn.Length];
        double range = arrayIn.Max() - arrayIn.Min();

        for (int i = 0; i < arrayIn.Length; i++) {
            normalized[i] = (arrayIn[i] - arrayIn.Min()) / range;

            if (worker != null) worker.ReportProgress((i * 100) / arrayIn.Length);
        }
        if (worker != null) worker.ReportProgress(100);

        return normalized;
    }

    private double[] SlowNormalize(double[] arrayIn, double min, double max) {// takes array of every pixel in grey; returns array of values between 0-1

        double[] normalized = new double[arrayIn.Length];
        double range = max - min;

        for (int i = 0; i < arrayIn.Length; i++) {
            normalized[i] = (arrayIn[i] - min) / range;

            if (worker != null) worker.ReportProgress((i * 100) / arrayIn.Length);
        }//end for

        if (worker != null) worker.ReportProgress(100);

        return normalized;
    }//end method

    private double[] SpeedyNormalize(double[] arrayIn, int numberOfThreads) {// uses multithreading to increase the performance of the normalize method

        // split stores the seperate chunks of arrayIn
        List<double[]> split = new List<double[]>();

        int chunkSize = arrayIn.Length / numberOfThreads;
        int remaining = arrayIn.Length;
        int index = 0;

        // split arrayIn into chunks
        for (int i = 0; i < numberOfThreads; i++) {
            // on last loop, add the rest to the final split
            if (remaining != 0 && i == numberOfThreads - 1) {
                split.Add(arrayIn.Slice(index, remaining));
            }
            else {
                split.Add(arrayIn.Slice(index, chunkSize));
            }
            remaining -= chunkSize;
            index += chunkSize;
        }

        // arrayIn is now split into chunks

        // threads stores a reference to all threads used
        Thread[] threads = new Thread[numberOfThreads];
        // stores all the normalize objects
        Normalize[] nObjects = new Normalize[numberOfThreads];

        // initializes and stores normalize objects and threads
        for (int i = 0; i < numberOfThreads; i++) {
            // initialize and store a new normalize object
            nObjects[i] = worker != null ? new Normalize(split[i], worker) : new Normalize(split[i]);

            // initialize and store the reference to the thread
            threads[i] = new Thread(nObjects[i].NormalizeValues);
            threads[i].Start();
        }

        // all threads have been started

        // wait for each thread to finish
        foreach (Thread thread in threads) {
            thread.Join();
        }

        // Normalizing work is complete

        // Using a list for the addrange method
        List<double> results = new List<double>();

        // loop thru each normalize object and retrieve result
        foreach (Normalize n in nObjects) {
            results.AddRange(n.Result);
        }
        return results.ToArray();
    }

    private double AverageColor(Color colorIn) {// converts pixel to grey
        double grey = (colorIn.R * 0.299) + (colorIn.G * 0.587) + (colorIn.B * 0.114);

        if (invert) {
            grey *= -1;
        }
        return grey;
    }

    private char GrayToString(double normalized) {// returns a character based on the given value

        string shortRange = "@%#*+=-:. ";
        string longRange = "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\u0022 ^`'. ";
        string customRange1 = "@X#%x+*:,.'";
        string customRange2 = "@#$%?*+;:,.";
        string customRange3 = "qwertyuiopasdfghjklzxcvbnm,./;'[]1234567890-=";

        // set selected to a range
        string selected;
        switch (colorRange) {
            case 1: selected = shortRange; break;

            case 2: selected = longRange; break;

            case 3: selected = customRange1; break;

            case 4: selected = customRange2; break;

            case 5: selected = customRange3; break;

            default: selected = shortRange; break;
        }

        int numberOfChars = selected.Count() - 1;

        int index = (int)(numberOfChars * normalized);

        return selected.ElementAt(index);
    }

    private string AverageColorOld(double[] normalizedPixels, int height, int width) {
        // Convert normalized pixels to a corresponding character
        int count = 0;
        bool toggle = false;
        StringBuilder sb = new StringBuilder();

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                // toggle flag skips every other line to minimize height-wise stretch
                if (!toggle) {
                    // append the returned character to the string
                    sb.Append(GrayToString(normalizedPixels[count]));
                }
                count++;
            }
            if (!toggle) {
                sb.Append("\n");
                toggle = true;
            }
            else {
                toggle = false;
            }
        }
        return sb.ToString();
    }
    private string AverageColorNew1(double[] greyPixels, int height, int width) {
        bool toggle = false;
        StringBuilder sb = new StringBuilder();
        int kernelArea = kernelWidth * kernelHeight;
        double[] normalized = new double[greyPixels.Length / kernelArea];
        int current = 0;
        double min = 0.0;
        double max = 0.0;
        int count = 0;
        int newWidth = 0;
        int newHeight = 0;
        double average;
        //kernel width == 4, kernel size = 16

        for (int y = 0; y < height; y += kernelHeight) {

            for (int x = 0; x < width; x += kernelWidth) {

                average = 0.0;

                for (int i = 0; i < kernelHeight; i++) {

                    for (int j = 0; j < kernelWidth; j++) {
                        current = (y + i) * width + (x + j);

                        if (current < greyPixels.Length) {
                            average += greyPixels[current];
                            max = greyPixels[current] > max ? greyPixels[current] : max;
                            min = greyPixels[current] < min ? greyPixels[current] : min;

                        }//end if

                    }//end width loop

                }//end height loop

                average /= kernelArea;

                if (count < normalized.Length) {
                    normalized[count] = average;
                    count++;
                }//end if

            }//end x axis loop

        }//end y axis loop

        normalized = SlowNormalize(normalized, min, max);
        newWidth = width / kernelWidth;
        newHeight = height / kernelHeight;

        for (int i = 0; i < normalized.Length; i++) {

            if (!toggle) {
                sb.Append(GrayToString(normalized[i]));
            }//end if

            if (i != 0 && i % newWidth == 0) {
                if (!toggle) {
                    sb.Append('\n');
                    toggle = true;
                }
                else {
                    toggle = false;
                }//end if
            }//end if
        }//end for
        return sb.ToString();
    }//end method
    #endregion
}//end class


