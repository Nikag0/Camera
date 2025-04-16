using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Camera
{
    public class CameraPerformance
    {
        private string model;

        public string Model
        {
            get { return model; }
            private set { model = value; }
        }

        private string manufacturer;

        public string MoManufacturerdel
        {
            get { return manufacturer; }
            private set { manufacturer = value; }
        }

        private string serialNumber;

        public string SerialNumber
        {
            get { return serialNumber; }
            private set { serialNumber = value; }
        }

        private string macAdress;

        public string MacAdress
        {
            get { return macAdress; }
            private set { macAdress = value; }
        }

        private string ipAdress;

        public string IpAdress
        {
            get { return ipAdress; }
            private set { ipAdress = value; }
        }

        private string firmwareVersion;

        public string FirmwareVersion
        {
            get { return firmwareVersion; }
            private set { firmwareVersion = value; }
        }
    }
}
