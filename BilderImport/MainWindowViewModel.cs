using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Input;

namespace BilderImport
{
  internal class MainWindowViewModel : ViewModelBase
  {
    #region Public Properties

    public DelegateCommand DeselectAllCommand
    {
      get
      {
        return _DeselectAllCommand ?? (_DeselectAllCommand = new DelegateCommand(
          () =>
          {
            foreach (var imageData in ImagesToCopy)
            {
              imageData.IsSelected = false;
            }
          }
          ));
      }
    }

    public List<string> ExternalDrives
    {
      get
      {
        return _externalDrives;
      }
      set
      {
        SetProperty(ref _externalDrives, value);
      }
    }

    public ObservableCollection<ImageData> ImagesToCopy
    {
      get
      {
        return _imagesToCopy;
      }
      set
      {
        SetProperty(ref _imagesToCopy, value);
      }
    }


    #region FolderName

    private string _folderName;

    public string FolderName
    {
      get
      {
        return _folderName;
      }
      set
      {
        var folderName = string.Join("_", value.Split(Path.GetInvalidFileNameChars()));
          
        SetProperty(ref _folderName, folderName);
      }
    }

    #endregion


    public DelegateCommand ImportImagesCommand
    {
      get
      {
        return _importImagesCommand ?? (_importImagesCommand = new DelegateCommand(
          () =>
          {
            Cursor = Cursors.Wait;
            if (string.IsNullOrEmpty(FolderName))
              FolderName = DateTime.Now.ToShortDateString();
            var imagesToRemove = new List<ImageData>();
            foreach (var imageData in ImagesToCopy.Where(image => image.IsSelected == true))
            {
              var targetFilePath = Path.Combine(Settings.Default.TargetFolder, FolderName, Path.GetFileName(imageData.Path));
              var targetDirectory = Path.GetDirectoryName(targetFilePath);
              if (!Directory.Exists(targetDirectory))
              {
                Directory.CreateDirectory(targetDirectory);
              }
              else
              {
                if (File.Exists(targetFilePath))
                {
                  File.Move(targetFilePath, Path.Combine(Path.GetDirectoryName(targetFilePath),
                    Path.GetFileNameWithoutExtension(targetFilePath) + "_2" + Path.GetExtension(targetFilePath)));
                }
              }
              File.Copy(imageData.Path, targetFilePath);
              imagesToRemove.Add(imageData);
            }
            foreach(var image in imagesToRemove) 
            {
              ImagesToCopy.Remove(image);
            }
            Cursor = Cursors.Arrow;
          }
          ));
      }
    }


    #region Cursor

    private Cursor _cursor;

    public Cursor Cursor
    {
      get
      {
        return _cursor;
      }
      set
      {
        SetProperty(ref _cursor, value);
      }
    }

    #endregion


    public DelegateCommand SelectAllCommand
    {
      get
      {
        return _selectAllCommand ?? (_selectAllCommand = new DelegateCommand(
          () =>
          {
            foreach (var imageData in ImagesToCopy)
            {
              imageData.IsSelected = true;
            }
          }
          ));
      }
    }

    public string SelectedExternalDrive
    {
      get
      {
        return _selectedExternalDrive;
      }
      set
      {
        SetProperty(ref _selectedExternalDrive, value);
      }
    }

    public DelegateCommand SetTargetFolderCommand
    {
      get
      {
        return _setTargetFolderCommand ?? (_setTargetFolderCommand = new DelegateCommand(
          () =>
          {
            // open a wpf select folder dialog and return the path
            var path = SelectFolder("Zielordner aussuchen");
            if (path != null)
            {
              Settings.Default.TargetFolder = path;
              Settings.Default.Save();
            }
          }
          ));
      }
    }

    public DelegateCommand StartImageSearchCommand
    {
      get
      {
        return _startImageSearchCommand ?? (_startImageSearchCommand = new DelegateCommand(
          () =>
          {
            Cursor = Cursors.Wait;
            // get the selected drive
            var drive = SelectedExternalDrive;
            if (drive != null)
            {
              if (!Directory.Exists(Settings.Default.TargetFolder))
                SetTargetFolderCommand.Execute();

              // get the drive letter
              var driveLetter = drive.Substring(drive.Length - 4, 2);

              // get the files
              var sourceFiles = Directory.GetFiles(driveLetter, "*.*", SearchOption.AllDirectories)
                .Where(s => s.ToLower().EndsWith(".png") || s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".tif"));
              var targetFiles = Directory.GetFiles(Settings.Default.TargetFolder, "*.*", SearchOption.AllDirectories)
                .Where(s => s.ToLower().EndsWith(".png") || s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".tif"));

              var filesToCopy = new List<string>();

              // check if the files already exists in the target path
              foreach (var sourceFile in sourceFiles)
              {
                var fileName = Path.GetFileName(sourceFile);
                var possibleTargetFiles = targetFiles.Where(targetFile => Path.GetFileName(targetFile).ToLower() == fileName.ToLower());
                var copyTheFile = true;
                if (possibleTargetFiles.Count() > 0)
                {
                  var fileInfoSource = new FileInfo(sourceFile);
                  foreach (var possibleTargetFile in possibleTargetFiles)
                  {
                    var fileInfoTarget = new FileInfo(possibleTargetFile);
                    if (fileInfoSource.Length == fileInfoTarget.Length
                      && fileInfoSource.LastWriteTimeUtc == fileInfoTarget.LastWriteTimeUtc)
                    {
                      copyTheFile = false;
                      break;
                    }
                  }
                }
                if (copyTheFile)
                {
                  filesToCopy.Add(sourceFile);
                }
              }

              ImagesToCopy.Clear();
              foreach (var file in filesToCopy)
              {
                ImagesToCopy.Add(new ImageData(file));
              }
            }
            Cursor = Cursors.Arrow;
          }
          ));
      }
    }

    #endregion Public Properties

    #region Public Constructors

    public MainWindowViewModel()
    {
      RefreshExternalDrives();

      ManagementEventWatcher watcher = new ManagementEventWatcher();
      WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3");
      watcher.EventArrived += (sender, e) => RefreshExternalDrives();
      watcher.Query = query;
      watcher.Start();
      //watcher.WaitForNextEvent();

      if (string.IsNullOrEmpty(Settings.Default.TargetFolder))
      {
        Settings.Default.TargetFolder = Environment.GetFolderPath(System.Environment.SpecialFolder.CommonPictures);
      }

      FolderName = DateTime.Now.ToShortDateString();

      Cursor = Cursors.Arrow;
    }

    #endregion Public Constructors

    #region Private Methods

    private void RefreshExternalDrives()
    {
      // get a list of external drives (USB ...)
      var drives = DriveInfo.GetDrives().Where(d => d.DriveType == System.IO.DriveType.Removable);

      // add the drives to the list
      ExternalDrives = new List<string>();
      foreach (var drive in drives)
      {
        ExternalDrives.Add(drive.VolumeLabel + $" ({drive.Name})");
      }
      if (ExternalDrives.Count > 0)
      {
        SelectedExternalDrive = ExternalDrives.First();
      }
    }

    // open a wpf select folder dialog and return the path
    private string SelectFolder(string description)
    {
      var dialog = new System.Windows.Forms.FolderBrowserDialog();
      dialog.InitialDirectory = Settings.Default.TargetFolder;
      dialog.Description = description;
      dialog.ShowDialog();
      return dialog.SelectedPath;
    }

    #endregion Private Methods

    #region Private Fields
    private DelegateCommand _DeselectAllCommand;
    private List<string> _externalDrives;
    private ObservableCollection<ImageData> _imagesToCopy = new ObservableCollection<ImageData>();
    private DelegateCommand _importImagesCommand;
    private DelegateCommand _selectAllCommand;
    private string _selectedExternalDrive;
    private DelegateCommand _setTargetFolderCommand;
    private DelegateCommand _startImageSearchCommand;
    #endregion Private Fields
  }
}