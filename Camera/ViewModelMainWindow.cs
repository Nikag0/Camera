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
        private CameraPerformance selectCamera;
        private Thread m_hReceiveThread = null;
        private string feedback;
        private string nameSelectDevice;
        private static readonly Regex filterRegex = new Regex(@"\(([A-Z]{2}\d{7})\)");
        private BitmapSource inImg;
        private string serialNumber;
        private bool areAnyDevices = false;
        private ObservableCollection <CameraPerformance> creatingCamersCollection { get => cameraManager.CreatingCamersCollection; }
        private bool isCreate = false;
        private bool isGrab = false;


        public ObservableCollection<CameraPerformance> CreatingCamersCollection
        {
            get => creatingCamersCollection;
            set
            {
                OnPropertyChanged();
            }
        }
        public bool IsCreate
        {
            get => isCreate;
            set
            {
                isCreate = value;
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
        public bool AreAnyDevices
        {
            get => areAnyDevices;

            set 
            {
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

                serialNumber = GetSerialNumber(value);
                CameraSwitch();
            }
        }      
        public List<string> NameCamers
        {
            get => cameraManager.NameCamers;
            set
            {
                OnPropertyChanged();
            }
        }

        public BitmapSource InImg
        {
            get => inImg;
            set
            {
                inImg = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchDeviceCommand { get; }
        public ICommand CreateDeviceCommand { get; }
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
            CreateDeviceCommand = new DelegateCommands(p => CreateDevice());
            CloseDeviceCommand = new DelegateCommands(p => ClosenDevice());
            StartGrabCommand = new DelegateCommands(p => StartGrab());
            StopGrabCommand = new DelegateCommands(p => StopGrab());
        }

        private void SearchDevice()
        {
            if (!cameraManager.SearchСamera())
            {
                Feedback = $"No device";
                return;
            }

            AreAnyDevices = true;
            Feedback = $"Devices are discovered";
        }


        private void CreateDevice()
        {
            if (!cameraManager.CameraCreate(serialNumber))
            {
                Feedback = "The error of creating a camera";
                return;
            }

            CameraSwitch();
            Feedback = "The camera is created";
        }

        private void ClosenDevice()
        {
            if (!cameraManager.DestroyCamera(serialNumber))
            {
                Feedback = "The error of destroying a camera";
                return;
            }

            Feedback = "The camera is created";
        }

        private void StartGrab()
        {
            if (selectCamera.IsGrab)
            {
                Feedback = "camera is already grabbing";
                //return;
            }

            selectCamera.StartGrab();
            InImg = selectCamera.InImg;
            Feedback = "camera is grab";
        }

        private void StopGrab()
        {
            if (!selectCamera.IsGrab)
            {
                Feedback = "camera is not grab";
            }

            selectCamera.StopGrab();
            Feedback = "camera is grab";
        }


        private void CameraSwitch()
        {
            if (serialNumber == "")
            {
                return;
            }

            selectCamera = CreatingCamersCollection.FirstOrDefault(camera => camera.SerialNumber == serialNumber);

            if (selectCamera == null)
            {
                return;
            }

            isCreate = selectCamera.IsCreate;
            isGrab = selectCamera.IsGrab;
        }

        private string GetSerialNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var match = filterRegex.Match(input);
            return match.Success ? match.Groups[1].Value : "Не найден";
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}