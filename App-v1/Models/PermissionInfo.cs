// Models/PermissionInfo.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App_v1.Models
{
    public class PermissionInfo : INotifyPropertyChanged
    {
        private string name = string.Empty;

        public string Name
        {
            get => name;
            set
            {
                name = value; OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}