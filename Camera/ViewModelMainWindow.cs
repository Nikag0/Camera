﻿using MvCameraControl;
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
        private string feedback;
        private string nameSelectDevice;
        private static readonly Regex filterRegex = new Regex(@"\(([A-Z]{2}\d{7})\)");
        private string serialNumber;

        public CameraManager CameraManager { get => cameraManager; }
        public CameraPerformance SelectCamera
        {
            get { return selectCamera; }
            set
            {
                selectCamera = value;
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
        public ICommand SearchDeviceCommand { get; }
        public ICommand CreateDeviceCommand { get; }
        public ICommand DestroyDeviceCommand { get; }
        public ICommand StartGrabCommand { get; }
        public ICommand StopGrabCommand { get; }
        public ICommand GetParamCommand { get; }
        public ICommand SetParamCommand { get; }
        public ICommand OnAutoExposureCommand { get; }
        public ICommand GetAutoExposureCommand { get; }
        public ICommand SetAutoExposureCommand { get; }
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
            GetParamCommand = new DelegateCommands(propa => GetParam());
            SetParamCommand = new DelegateCommands(propa => SetParam());
            OnAutoExposureCommand = new DelegateCommands(propa => OnAutoExposure());
            GetAutoExposureCommand = new DelegateCommands(propa => GetAutoExposure());
            SetAutoExposureCommand = new DelegateCommands(propa => SetAutoExposure());
        }

        private void SearchDevice()
        {
            if (!cameraManager.SearchСamera())
            {
                Feedback = $"No device";
                return;
            }

            Feedback = $"Devices are discovered";
        }

        private void CreateDevice()
        {
            if (SelectCamera != null && SelectCamera.IsCreate)
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
            SelectCamera.GetParam();
        }

        private void DestroyDevice()
        {
            if (!cameraManager.DestroyCamera(serialNumber))
            {
                Feedback = "The error of destroying a camera";
                return;
            }

            Feedback = "The camera is destroyed";
        }

        private void StartGrab()
        {
            if (SelectCamera.IsGrab)
            {
                StopGrab();
                return;
            }

            if (!SelectCamera.StartGrab())
            {
                Feedback = "Error start grab";
                return;
            }

            Feedback = "camera is grab";
        }

        private void StopGrab()
        {
            if (!SelectCamera.StopGrab())
            {
                Feedback = "Error stop grab";
                return;
            }

            Feedback = "camera stop grab";
        }

        private void SetParam()
        {
            Feedback = SelectCamera.SetParam();
        } 

        private void GetParam()
        {
            SelectCamera.GetParam();
        }

        private void OnAutoExposure()
        {
            if (SelectCamera.OnAutoExposureTime())
            {
                Feedback = "Auto Exposure Time On";
                return;
            }

            Feedback = "Auto Exposure Time Off";
        }

        private void GetAutoExposure()
        {
            SelectCamera.GetAutoExposure();
        }

        private void SetAutoExposure()
        {
            Feedback = SelectCamera.SetAutoExposure();
        }

        private void CameraSwitch()
        {
            if (serialNumber == "")
            {
                return;
            }

            SelectCamera = cameraManager.CreatingCamersCollection.FirstOrDefault(camera => camera.SerialNumber == serialNumber);

            if (SelectCamera == null)
            {
                return;
            }
        }

        private string GetSerialNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

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