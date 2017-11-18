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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Muho.PSD;
using Muho.PSD.Models;
using System.Diagnostics;
using System.IO;

namespace Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var psdPath = @"D:\OrnekDesenler\Layer\Fish.psd";
            psdPath = @"D:\OrnekDesenler\Layer\B02102308.psd";



            Stopwatch sw = Stopwatch.StartNew();
            Psd psd = new Psd(psdPath, true);

            var thumbnails = psd.ImageResources.Resources.OfType<PsdThumbnail>().Where(w => (w.Id == eResourceId.Thumbnail1) || (w.Id == eResourceId.Thumbnail2)).ToList();
            foreach (var th in thumbnails)
            {
                var immm = th.Image;
                saveAsFile(immm as BitmapSource, th.Name);
            }


            var layersAndMasks = psd.LayersAndMasks;
            foreach (var layer in layersAndMasks.LayersInfo.Layers)
            {
                var bs = layer.Image;
                if (bs != null)
                    saveAsFile(bs,layer.Name);
            }
            var mainImage = psd.ImageData.Image;
            saveAsFile(mainImage,"MainImage");

            sw.Stop();

            var x = string.Format("Kaydetme Hariç Geçen Süre : {0} ms", sw.ElapsedMilliseconds);
            MessageBox.Show(x);

        }

        private static void saveAsFile(BitmapSource source, string name = "")
        {
            var encoder = new PngBitmapEncoder();
            if (string.IsNullOrEmpty(name))
                name = string.Format("ZZ-{0}.png", DateTime.Now.ToFileTime());

            if (!name.ToLower().EndsWith(".png"))
                name += ".png";

            var di = Directory.CreateDirectory("Images");
            var path = System.IO.Path.Combine(di.FullName, name);

            encoder.Frames.Add(BitmapFrame.Create(source));
            var fs = new FileStream(path, FileMode.OpenOrCreate);
            encoder.Save(fs);
            fs.Close();
        }
    }
}
