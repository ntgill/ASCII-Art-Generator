using System.ComponentModel;
using System.Linq;

class Normalize
{// normalize class used for threading

    // Fields
    double[] array;
    double[] result;
    BackgroundWorker? worker;

    // Properties
    public double[] Result { get { return result; } }

    // Constructor
    public Normalize(double[] arrayIn, BackgroundWorker worker)
    {
        this.worker = worker;
        array = arrayIn;
        result = new double[arrayIn.Length];
    }

    public Normalize(double[] arrayIn)
    {
        array = arrayIn;
        result = new double[arrayIn.Length];
    }

    // Methods
    public void NormalizeValues()
    {// takes array of every pixel in grey; returns array of values between 0-1

        double[] normalized = new double[array.Length];
        double range = array.Max() - array.Min();

        for (int i = 0; i < array.Length; i++)
        {
            normalized[i] = (array[i] - array.Min()) / range;
            if (worker != null) worker.ReportProgress((i * 100) / array.Length);
        }
        if (worker != null) worker.ReportProgress(100);
        result = normalized;
    }
}

