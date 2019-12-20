using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Better_Printing_for_OneNote.AdditionalClasses
{
    public abstract class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, e);
            }
        }

        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                var e = new PropertyChangingEventArgs(propertyName);
                PropertyChanging(this, e);
            }
        }
    }
}
