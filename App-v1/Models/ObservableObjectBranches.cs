using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App_v1.Models
{
    public class ObservableObjectBranches : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}