using Prism.Mvvm;
using System;
using System.IO;
using System.Net.Cache;
using System.Windows.Media.Imaging;

namespace BilderImport
{
  public class ImageData : BindableBase
  {
    #region Public Properties

    public BitmapImage BitmapImage
    {
      get => _bitmapImage;
      set
      {
        SetProperty(ref _bitmapImage, value);
      }
    }

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

    public string Name { get; set; }

    public string Path { get; set; }
    #endregion Public Properties

    #region Public Constructors

    public ImageData(string path)
    {
      BitmapImage = LoadImageFile(path);

      Name = System.IO.Path.GetFileName(path);
      Path = path;
      IsSelected = true;
    }

    #endregion Public Constructors

    #region Public Methods

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
      _image.CacheOption = BitmapCacheOption.None;
      _image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
      _image.CacheOption = BitmapCacheOption.OnLoad;
      _image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
      _image.DecodePixelWidth = 200;
      _image.Rotation = rotation;
      _image.EndInit();
      _image.Freeze();

      return _image;
    }

    #endregion Public Methods

    #region Internal Methods

    internal void ReloadImage()
    {
      BitmapImage = LoadImageFile(Path);
    }

    #endregion Internal Methods

    #region Private Fields
    private static string _orientationQuery = "System.Photo.Orientation";
    private BitmapImage _bitmapImage;
    private bool _isSelected;
    #endregion Private Fields
  }
}