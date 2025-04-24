using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Camera
{
    public class PerfomanceCamera : INotifyPropertyChanged
    {
        private string name = "";
        private string serialNumer = "";
        private int nubmerInDeviceLict;
        private bool isOpen = false;
        private bool isGrab = false;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }
        public string SerialNumer
        {
            get => serialNumer;
            set
            {
                serialNumer = value;
                OnPropertyChanged();
            }
        }
        public int NubmerInDeviceLict
        {
            get => nubmerInDeviceLict;
            set
            {
                nubmerInDeviceLict = value;
                OnPropertyChanged();
            }
        }
        public bool IsOpen
        {
            get => isOpen;
            set
            {
                isOpen = value;
                OnPropertyChanged();
            }
        }
        public bool IsGrab
        {
            get => isGrab;
            set
            {
                isGrab = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
