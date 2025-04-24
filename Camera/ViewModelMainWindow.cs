using MvCameraControl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Drawing.Configuration;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Data;
using System.Drawing;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;

namespace Camera
{
    public class ViewModelMainWindow : INotifyPropertyChanged
    {
        private CameraManager cameraManager = new CameraManager();
        private MyCamera myCamera = new MyCamera();
        private Thread m_hReceiveThread = null;
        private string feedback;
        private int deviceIndex = 0;
        private int nRet;
        private bool isGrabbing = false;
        private int indexSelectDevice;
        private string nameSelectDevice;
        private bool areAnyDevices = false;
        private BitmapSource inImg1;
        private BitmapSource inImg2;

        private ObservableCollection<PerfomanceCamera> CollectionCamera
        {
            get => cameraManager.CollectionCamera;
            set
            {
                OnPropertyChanged();
            }
        }

        public bool AreAnyDevices
        {
            get => areAnyDevices;

            set
            {
                areAnyDevices = value;
                OnPropertyChanged();
            }
        }

        public int IndexSelectDevice
        {
            get => indexSelectDevice;
            set
            {
                indexSelectDevice = value;
                OnPropertyChanged();
            }
        }

        public string NameSelectDevice
        {
            get => nameSelectDevice;
            set
            {
                nameSelectDevice = value;
                OnPropertyChanged();
            }
        }

        public BitmapSource InImg1
        {
            get => inImg1;
            set
            {
                inImg1 = value;
                OnPropertyChanged();
            }
        }

        public BitmapSource InImg2
        {
            get => inImg1;
            set
            {
                inImg1 = value;
                OnPropertyChanged();
            }
        }

        public int DeviceIndex
        {
            get => deviceIndex;
            set
            {
                deviceIndex = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchDeviceCommand { get; }
        public ICommand OpenDeviceCommand { get; }
        public ICommand CloseDeviceCommand { get; }
        public ICommand StartGrabCommand { get; }
        public ICommand StopGrabCommand { get; }
        public string Feedback
        {
            get => feedback;
            set
            {
                feedback = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public ViewModelMainWindow()
        {
            SearchDeviceCommand = new DelegateCommands(p => SearchDevice());
            OpenDeviceCommand = new DelegateCommands(p => OpenDevice());
            CloseDeviceCommand = new DelegateCommands(p => ClosenDevice());
            StartGrabCommand = new DelegateCommands(p => StartGrab());
            StopGrabCommand = new DelegateCommands(p => StopGrab());
        }

        private void SearchDevice()
        {
            if (!cameraManager.SearchDevice())
            {
                Feedback = $"No device or enumerate devices fail!";
                return;
            }
            Feedback = $"Devices are discovered";
            AreAnyDevices = true;
            indexSelectDevice = 0;
        }


        private void OpenDevice()
        {
            Regex regex = new Regex(@"\(([A-Z0-9]{8,})\)$");
            Match match = regex.Match(nameSelectDevice);

            if (!match.Success)
            {
                Feedback = "Серийный номер не найден!";
            }

            string serialNumber = match.Groups[1].Value;

            Feedback = cameraManager.OpenDevice(serialNumber, indexSelectDevice);
        }

        private void ClosenDevice()
        {
   
        }

        private void StartGrab()
        {
            InImg1 = cameraManager.StartGrab();
        }

        private void StopGrab()
        {
           
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}