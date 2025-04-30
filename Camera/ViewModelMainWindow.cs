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
using System.Xml.Linq;
using System.Windows.Media.Media3D;

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
        private string serialNumber;
        private bool areAnyDevices = false;
        private BitmapSource inImg;
        private bool isCreate = false;
        private bool isGrab = false;


        private ObservableCollection<string> nameCreateCamera { get => cameraManager.NameCreateCamers; }

        public ObservableCollection<string> NameCreateCamera
        {
            get { return nameCreateCamera; }
            set 
            {
                OnPropertyChanged();
            }
        }

        public bool IsGrab
        {
            get =>  isGrab;
            set
            {
                isGrab = value;
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
        public bool AreAnyDevices
        {
            get => areAnyDevices;

            set 
            {
                areAnyDevices = value;
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
                cameraManager.NameCamers = value;
                OnPropertyChanged();
            }
        }

        public BitmapSource InImg
        {
            get
            {
                return inImg;
            }
            set
            {
                if (value != null)
                {
                    inImg = value;
                    OnPropertyChanged();
                }    
                else
                {
                    Feedback = "No image";
                }
            }
        }

        public ICommand SearchDeviceCommand { get; }
        public ICommand CreateDeviceCommand { get; }
        public ICommand DestroyDeviceCommand { get; }
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
            DestroyDeviceCommand = new DelegateCommands(p => DestroyDevice());
            StartGrabCommand = new DelegateCommands(p => StartGrab());
            StopGrabCommand = new DelegateCommands(p => StopGrab());
        }

        private void SearchDevice()
        {
            if (!cameraManager.SearchСamera())
            {
                Feedback = $"No device";
                AreAnyDevices = false;
                return;
            }

            AreAnyDevices = true;
            Feedback = $"Devices are discovered";
        }


        private void CreateDevice()
        {
            if (selectCamera != null && IsCreate)
            {
                DestroyDevice();
                return;
            }

            if (!cameraManager.CameraCreate(serialNumber))
            {
                Feedback = "The error of creating a camera";
                return;
            }


            CameraSwitch();
            Feedback = "The camera is created";
        }

        private void DestroyDevice()
        {
            if (!cameraManager.DestroyCamera(serialNumber))
            {
                Feedback = "The error of destroying a camera";
                return;
            }

            IsCreate = selectCamera.IsCreate;
            Feedback = "The camera is destroyed";
        }

        private void StartGrab()
        {
            if (selectCamera.IsGrab)
            {
                StopGrab();
                IsGrab = selectCamera.IsGrab;
                return;
            }

            selectCamera.StartGrab();
            InImg = selectCamera.InImg;
            IsGrab = selectCamera.IsGrab;
            Feedback = "camera is grab";
        }

        private void StopGrab()
        {
            if (!selectCamera.StopGrab())
            {
                Feedback = "Error stop grab";
                return;
            }
            IsGrab = selectCamera.IsGrab;
            Feedback = "camera stop grab";
        }

        private void CameraSwitch()
        {
            if (serialNumber == "")
            {
                return;
            }

            selectCamera = cameraManager.CreatingCamersCollection.FirstOrDefault(camera => camera.SerialNumber == serialNumber);

            if (selectCamera == null)
            {
                return;
            }

            IsCreate = selectCamera.IsCreate;
            IsGrab = selectCamera.IsGrab;
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