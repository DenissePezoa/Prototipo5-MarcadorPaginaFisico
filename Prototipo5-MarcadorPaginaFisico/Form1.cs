using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace Prototipo5_MarcadorPaginaFisico
{
    public partial class Form1 : Form
    {
        VideoCapture _capture;
        private Mat _frame;
        Emgu.CV.Util.VectorOfVectorOfPoint contours;
        Image<Bgr, Byte> final;
        String texto_final;
        private async void ProcessFrame(object sender, EventArgs e)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(_frame, 0);
                pictureBox1.Image = _frame.Bitmap;
                double fps = 15;
                await Task.Delay(1000 / Convert.ToInt32(fps));

            }
        }
        public Form1()
        {
            InitializeComponent();

            _capture = new VideoCapture(1);


            _capture.ImageGrabbed += ProcessFrame;
            _frame = new Mat();
            if (_capture != null)
            {
                try
                {
                    _capture.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_frame.IsEmpty)
            {
                return;
            }

            //try
            //{

            Mat m = new Mat();
            Mat n = new Mat();
            Mat o = new Mat();
            Mat aux = new Mat();
            Mat binaryDiffFrame = new Mat();
            Mat denoisedDiffFrame = new Mat();
            Mat finalFrame = new Mat();




            //
            //OBTENER COLOR
            //
            Image<Bgr, Byte> imge = _frame.ToImage<Bgr, Byte>();
            Image<Bgr, byte> ret = imge.Copy();
            Image<Bgr, byte> auxImge = imge.Copy();
            Image<Bgr, byte> auxImge2 = imge.Copy();
            Image<Bgr, byte> auxImge3 = imge.Copy();
            Image<Bgr, byte> resultadoFinal = imge.Copy();
            //Transformar a espacio de color HSV
            Image<Hsv, Byte> hsvimg = auxImge.Convert<Hsv, Byte>();

            //extract the hue and value channels
            Image<Gray, Byte>[] channels = hsvimg.Split();  //separar en componentes
            Image<Gray, Byte> imghue = channels[0];            //hsv, channels[0] es hue.
            Image<Gray, Byte> imgval = channels[2];            //hsv, channels[2] es value.

            //Filtro AZUL --> 90 a 120
            //Verde --> 40 a 70
            Image<Gray, byte> huefilter = imghue.InRange(new Gray(90), new Gray(120));
            //Filtro colores menos brillantes
            Image<Gray, byte> valfilter = imgval.InRange(new Gray(100), new Gray(255));
            //Filtro de saturación - quitar blancos 
            channels[1]._ThresholdBinary(new Gray(10), new Gray(255)); // Saturacion

            //Unir los filtros para obtener la imagen
            Image<Gray, byte> colordetimg = huefilter.And(valfilter).And(channels[1]);//aqui habia un Not()

            //Colorear imagen

            var mat = auxImge2.Mat;
            mat.SetTo(new MCvScalar(0, 0, 255), colordetimg);
            mat.CopyTo(ret);
            //Image<Bgr, byte> imgout = ret.CopyBlank();//imagen sin fondo negro

            ret._Or(auxImge2);
            //Muestra imagen con los rojos destacados
            pictureBox2.Image = ret.Bitmap;

            Mat SE2 = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(3, 2), new Point(-1, -1));
            CvInvoke.MorphologyEx(colordetimg, colordetimg, MorphOp.Erode, SE2, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(255));
            Mat SE3 = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(3, 2), new Point(-1, -1));
            CvInvoke.MorphologyEx(colordetimg, colordetimg, MorphOp.Dilate, SE3, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(255));


            Mat SE = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
            CvInvoke.MorphologyEx(colordetimg, aux, Emgu.CV.CvEnum.MorphOp.Close, SE, new Point(-1, -1), 2, Emgu.CV.CvEnum.BorderType.Reflect, new MCvScalar(255));

            pictureBox2.Image = aux.Bitmap;
            Image<Bgr, byte> temp = aux.ToImage<Bgr, byte>();

            var temp2 = temp.SmoothGaussian(5).Convert<Gray, byte>().ThresholdBinary(new Gray(230), new Gray(255));
            VectorOfVectorOfPoint contorno = new VectorOfVectorOfPoint();
            Mat matAux = new Mat();
            CvInvoke.FindContours(temp2, contorno, matAux, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
            if (contorno.Size > 0)
            {
                for (int i = 0; i < contorno.Size; i++)
                {

                    VectorOfPoint approxContour = new VectorOfPoint();
                    double perimetro = CvInvoke.ArcLength(contorno[i], true);
                    VectorOfPoint approx = new VectorOfPoint();

                    double area = CvInvoke.ContourArea(contorno[i]);
                    if (area > 1000)
                    {
                        var moments = CvInvoke.Moments(contorno[i]);
                        int x = (int)(moments.M10 / moments.M00);
                        int y = (int)(moments.M01 / moments.M00);

                        CvInvoke.ApproxPolyDP(contorno[i], approx, 0.04 * perimetro, true);
                        CvInvoke.DrawContours(resultadoFinal, contorno, i, new MCvScalar(0, 255, 255), 2);
                        RotatedRect rectangle = CvInvoke.MinAreaRect(approx);
                        //CvInvoke.DrawContours(resultadoFinal, contorno, i, new MCvScalar(255, 255, 255), 2, LineType.AntiAlias);
                        MessageBox.Show("Tamano figura " + rectangle.Size.Width * rectangle.Size.Height);
                        resultadoFinal.Draw(rectangle, new Bgr(Color.Cyan), 1);
                        CvInvoke.PutText(resultadoFinal, "Marcador Pagina", new Point(x, y),
                        Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new MCvScalar(0, 255, 255), 2);
                        pictureBox3.Image = resultadoFinal.ToBitmap();

                    }
                }
            }
        }


        public static Image<Gray, byte> Sharpen(Image<Gray, byte> image, int w, int h, double sigma1, double sigma2, int k)
        {
            w = (w % 2 == 0) ? w - 1 : w;
            h = (h % 2 == 0) ? h - 1 : h;
            //apply gaussian smoothing using w, h and sigma 
            var gaussianSmooth = image.SmoothGaussian(w, h, sigma1, sigma2);
            //obtain the mask by subtracting the gaussian smoothed image from the original one 
            var mask = image - gaussianSmooth;
            //add a weighted value k to the obtained mask 
            mask *= k;
            //sum with the original image 
            image += mask;
            return image;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            string oldFile = "ejemploOK.pdf";
            string newFile = "temporal.pdf";
            PdfReader reader = new PdfReader(oldFile);
            IList<Dictionary<String, Object>> bmProperties = SimpleBookmark.GetBookmark(reader);
            PdfStamper stamp = new PdfStamper(reader, new FileStream(newFile, FileMode.Create));
            if (bmProperties != null)
            {
                MessageBox.Show("Cantidad marcadores " + bmProperties.Count());
                foreach (IDictionary<String, Object> bmProperty in bmProperties)
                {
                    //MessageBox.Show("Key " + bmProperty.Keys + " y values " + bmProperty.Values);
                    foreach (var algo in bmProperty.Keys)
                    {
                        MessageBox.Show("Contiene "+ algo);
                    }
                }
                Dictionary<String, Object> marcador = new Dictionary<String, Object>();
                marcador.Add("Action", "GoTo");
                marcador.Add("Title", "Esto es otro marcador");
                marcador.Add("Page", "2 XYZ 100 100 0"); // use height of 1st page
                bmProperties.Add(marcador);
                stamp.Outlines = bmProperties;
                stamp.Close();
                //System.Diagnostics.Process.Start(newFile);
                //Console.Read();
                reader.Close();
                File.Replace(newFile, oldFile, @"backup.pdf.bac");

                //File.Replace(@"temporal.pdf", @"ejemploOK.pdf", @"backup.pdf.bac");
                MessageBox.Show("Pdf modificado con exito, se ha guardado un backup de la versión anterior ");
            }
            else
            {

                
                //stamp.GetUnderContent(1);
                //var h= stamp.GetImportedPage(reader, 1).Height;
                MessageBox.Show("El PDF no tiene marcadores");
                IList<Dictionary<String, Object>> marcadores= new List<Dictionary<String, Object>>();
                Dictionary<String, Object> marcador=new Dictionary<String, Object>();
                //marcadores.Add(marcador);
                marcador.Add("Action", "GoTo");
                marcador.Add("Title", "Page1 0 H 0");
                marcador.Add("Page", "1 XYZ 100 100 0"); // use height of 1st page
                MessageBox.Show("marcador " + marcador.Count);
               
                //MessageBox.Show("marcador " + marcador.Keys);
                marcadores.Add(marcador);
               
                
                stamp.Outlines = marcadores;
                stamp.Close();
                //System.Diagnostics.Process.Start(newFile);
                //Console.Read();
                reader.Close();
                File.Replace(newFile, oldFile, @"backup.pdf.bac");

                //File.Replace(@"temporal.pdf", @"ejemploOK.pdf", @"backup.pdf.bac");
                MessageBox.Show("Pdf modificado con exito, se ha guardado un backup de la versión anterior ");
            }
            
        }

       
    }
}
