using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BilderImport
{
  public class ImageData : BindableBase
  {
    public ImageData(string path)
    {
      //BitmapImage = new BitmapImage();
      //BitmapImage.BeginInit();
      //BitmapImage.UriSource = new Uri(path);
      //BitmapImage.DecodePixelWidth = 200;
      //BitmapImage.EndInit();

      BitmapImage = LoadImageFile(path);

      Name = System.IO.Path.GetFileName(path);
      Path = path;
      IsSelected = true;
    }

    private static string _orientationQuery = "System.Photo.Orientation";
    public BitmapImage LoadImageFile(String path)
    {
      Rotation rotation = Rotation.Rotate0;
      using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
      {
        BitmapFrame bitmapFrame = BitmapFrame.Create(fileStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
        BitmapMetadata bitmapMetadata = bitmapFrame.Metadata as BitmapMetadata;

        if ((bitmapMetadata != null) && (bitmapMetadata.ContainsQuery(_orientationQuery)))
        {
          object o = bitmapMetadata.GetQuery(_orientationQuery);

          if (o != null)
          {
            switch ((ushort)o)
            {
              case 6:
                {
                  rotation = Rotation.Rotate90;
                }
                break;
              case 3:
                {
                  rotation = Rotation.Rotate180;
                }
                break;
              case 8:
                {
                  rotation = Rotation.Rotate270;
                }
                break;
            }
          }
        }
      }

      BitmapImage _image = new BitmapImage();
      _image.BeginInit();
      _image.UriSource = new Uri(path);
      _image.DecodePixelWidth = 200;
      _image.Rotation = rotation;
      _image.EndInit();
      _image.Freeze();

      return _image;
    }


    #region IsSelected

    private bool _isSelected;

    public bool IsSelected
    {
      get
      {
        return _isSelected;
      }
      set
      {
        SetProperty(ref _isSelected, value);
      }
    }

    #endregion


    public string Name { get; set; }

    public BitmapImage BitmapImage { get; set; }

    public string Path { get; set; }
  }
}
