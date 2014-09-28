using System;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


namespace ch_test
{
    public class CH
    {
        //Field
        private string path;
        public string Path
        {
            get {return path;}
            set {path = value;}
        }
        private double[] histgram;
        public double[] Histgram
        {
            get {return histgram;}
            set {histgram = value;}
        }

        const int DIM = 64;

        private Bitmap data;

        //methods
        public CH(){}

        public CH(string path_)
        {
            path = path_;
            Load();
            CalculateHist();
        }

        public bool Load()
        {
            //1.jpg
            try
            {
                Image image = Image.FromFile(path);
                data = new Bitmap(image);
            }catch (Exception e)
            {
                Console.WriteLine(e.Source);
                return false;
            }
            return true;
        }

        public bool Save(string path = "")
        {
            //ch/1.txt
            return true;
        }

        //compute similirity
        public double GetSimilarity(CH other)
        {
            double dist;

            double h1 = 0.0, h2 = 0.0;

            int N = histgram.Length;
            for (int i = 0; i < N; ++i )
            {
                h1 = h1 + histgram[i];
                h2 = h2 + other.histgram[i];
            }

            double sum = 0.0;
            for (int i = 0; i < N; ++i )
            {
                sum = sum + Math.Sqrt(histgram[i] * other.histgram[i]);
            }
            dist = Math.Sqrt(1 - sum / Math.Sqrt(h1 * h2));
            
            return 1 - dist;
        }

        public void CalculateHist()
        {
            int height = data.Height;
            int width  = data.Width;

            double[] bins = new double[DIM * DIM * DIM];
            int step = 256 / DIM;

            for(int i = 0; i < width; ++i)
            {
                for(int j = 0; j < height; ++j)
                {
                    int r  = data.GetPixel(i, j).R;
                    int g  = data.GetPixel(i, j).G;
                    int b  = data.GetPixel(i, j).B;
                    int y  = (int)(0.299    * r + 0.587   * g + 0.114   * b);
                    int cb = (int)(-0.16874 * r - 0.33126 * g + 0.50000 * b);
                    int cr = (int)(0.50000  * r - 0.41869 * g - 0.08131 * b);

                    int ybin  = y / step;
                    int cbbin = cb / step;
                    int crbin = cr / step;

                    bins[ybin * DIM * DIM + cbbin * DIM + crbin * DIM]++;
                }
            }

            // normalize
            for (int i=0; i<3*DIM; ++i)
            {
                bins[i] = bins[i] / (height * width);
            }

            histgram = bins;
        }

    }
}

namespace vw_test
{
    class VW
    {
        public VW()
        {

        }



    }
}

class Program
{
    static public void init_ch(ref List<ch_test.CH> ch_list)
    {
        string root = @"train\data\";
        string[] dirs = Directory.GetDirectories(root);

        char[] delimiterChars = { '.'};
        foreach (string dir in dirs)
        {
            Console.WriteLine(dir);
            string[] files = Directory.GetFiles(dir);

            foreach (string file in files)
            {
                string[] words = file.Split(delimiterChars);
                string type = words[words.Length - 1].ToLower();
                if ((type == "jpg") || (type == "jpeg") || (type == "png") || (type == "bmp"))
                { 
                    ch_test.CH temp = new ch_test.CH(file);
                    ch_list.Add(temp);
                }
                //double result = source.GetSimilarity(temp);
                //pq.Add(result, temp);
            }
        }
    }

    static public void init_vw()
    {
        //to do change current directory
        string old_directory = Directory.GetCurrentDirectory();

        Console.WriteLine(old_directory);

        string newdir = old_directory + @"\semanticFeature\";
        try
        {
            //Set the current directory.
            Directory.SetCurrentDirectory(newdir);
        }
        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine("The specified directory does not exist. {0}", e);
        }

        // get the demolist
        string root = "train\\data\\";
        string[] dirs = Directory.GetDirectories(root);

        string listPath = @"demolist.txt";
        if (!File.Exists(listPath))
        {
            File.CreateText(listPath).Close();
        }
        else
        {
            File.Delete(listPath);
            File.CreateText(listPath).Close();
        }
        
        using (StreamWriter sw = File.AppendText(listPath))
        {
            foreach (string dir in dirs)
            {
                Console.WriteLine(dir);
                string[] files = Directory.GetFiles(dir);
                foreach (string file in files)
                {
                    //file ext is jpg png bmp jpeg
                    String ext = file.Substring(file.IndexOf('.')+1).ToLower();
                    if (ext=="jpg" || ext == "png" || ext == "bmp" || ext == "jpeg")
                        sw.WriteLine(file);
                }
            }
        }
        
        Console.WriteLine("start invoke");
        //invoke image_classification.exe
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = "image_classification.exe";
        startInfo.Arguments = "demolist.txt";

  /*      using(Process exePro = Process.Start(startInfo))
        {
            Console.WriteLine("start waiting");
            exePro.WaitForExit();
        } */

        // find top k 
        try
        {
            using (StreamReader sr = new StreamReader("demolist.txt"))
            {
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    String pre = line.Substring(0, line.IndexOf('.'));
                    
                    try
                    {
                        using (StreamReader sr2 = new StreamReader(pre+".txt"))
                        {
                            Console.WriteLine("Image: "+pre+".txt");

                            String positiveFile = pre + "Positive.txt";

                            if (File.Exists(positiveFile))
                            {
                                File.CreateText(positiveFile).Close();
                            }
                            else
                            {
                                File.Delete(positiveFile);
                                File.CreateText(positiveFile).Close();
                            }

                            using (StreamWriter sw3 = File.AppendText(positiveFile))
                            {
                                String all = sr2.ReadToEnd();
                                Char[] spliteBy = { ' ' };
                                String[] thousand = all.Split(spliteBy);
                                List<double> thousandDouble = new List<double>();
                                for (int i = 0; i < thousand.Length; i++)
                                {
                                    if (thousand[i] != "")
                                    {

                                        try
                                        {
                                            thousandDouble.Add(Convert.ToDouble(thousand[i]));
                                        }
                                        catch (FormatException)
                                        {
                                            Console.WriteLine("Unable to convert  to a Double.");
                                        }
                                        catch (OverflowException)
                                        {
                                            Console.WriteLine(" is outside the range of a Double.");
                                        }

                                        if (thousandDouble[i] > 0)
                                        {
                                            sw3.WriteLine(i+" "+thousandDouble[i]);
                                        }

                                    }

                                } // 1000
                            }
                        }
                    }
                    catch
                    {

                    }
                }
           
                
            }

        } 
        catch (Exception e)
        {

        }

        // return to old directory
        try
        {
            //Set the current directory.
            Directory.SetCurrentDirectory(old_directory);
            Console.WriteLine("set back");
        }
        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine("The specified directory does not exist. {0}", e);
        }
    }

    static public void init_sift()
    {

    }

    static public void Init(ref List<ch_test.CH> ch_list)
    {
        //init_ch(ref ch_list);
        init_vw();
        init_sift();
        
    }
    static void Main(string[] args)
    {
        // config
        int N = 5;

        String testString = "-2.12286";
        try
        {
            double testDou = Convert.ToDouble(testString);
        }
        catch (Exception e)
        {
            Console.WriteLine("$$ "+e.Message);
        }

        //=================================
        
        //init
        List<ch_test.CH> ch_list = new List<ch_test.CH>();
        
        Init(ref ch_list);
        //=================================
        
        
        //execution perid
        SortedList pq = new SortedList();
        Console.WriteLine("Input Image Name :");
        string line = Console.ReadLine();
        if(File.Exists(line))
        {
            ch_test.CH source = new ch_test.CH(line);
            foreach (var dest in ch_list)
            {
                double result = source.GetSimilarity(dest);
                if (pq.ContainsKey(result))
                {
                    Console.WriteLine("!!!!!!!" + dest.Path);
                    ch_test.CH hahaha = (ch_test.CH)pq[result];
                    Console.WriteLine(hahaha.Path);
                }
                else
                {
                    pq.Add(result, dest);
                }

            }

            for (int i = pq.Count - 1, j = 1; j <= N; j++, i--)
            {
                if (i < 0)
                    break;
                Console.WriteLine("NUM " + j);
                Console.WriteLine(pq.GetKey(i));
                ch_test.CH output = (ch_test.CH)pq.GetByIndex(i);
                Console.WriteLine(output.Path);
            }
            
        }
        // end of execution
    }
}



