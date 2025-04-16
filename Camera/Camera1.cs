using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Camera
{
    public class Camera1
    {
        private string model = "MV-CS050-10GM-PRO";
        private string ip = "192.168.10.115";
        private int port;
        private ObservableCollection<bool> inputs = new ObservableCollection<bool>();

        public event PropertyChangedEventHandler PropertyChanged;
        public string Model
        {
            get => model;
            set
            {
                ip = value;
                OnPropertyChanged();
            }
        }
        public string Ip
        {
            get => ip;
            set
            {
                ip = value;
                OnPropertyChanged();
            }
        }

        public int Port
        {
            get => port;
            set
            {
                port = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<bool> Inputs
        {
            get => inputs;
            set
            {
                inputs = value;
                OnPropertyChanged();
            }
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}

