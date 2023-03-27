using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BilderImport
{
  internal class ViewModelBase : INotifyPropertyChanged
  {
        #region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
      var handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
    {
      if (object.Equals(storage, value))
      {
        return false;
      }

      storage = value;
      OnPropertyChanged(propertyName);
      return true;
    }

    #endregion
  }
}