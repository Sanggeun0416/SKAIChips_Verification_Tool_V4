using System;
using System.Runtime.InteropServices;
using SKAIChips_Verification_Tool.Core;

namespace SKAIChips_Verification_Tool.Infra
{
    public class Ft4222I2cBus : IBus, IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private bool _isConnected;
        private readonly ushort _slaveAddress;
        private readonly uint _deviceIndex;
        private readonly ushort _speedKbps;

        public bool IsConnected => _isConnected;

        public Ft4222I2cBus(uint deviceIndex, ushort slaveAddress, ushort speedKbps)
        {
            _deviceIndex = deviceIndex;
            _slaveAddress = slaveAddress;
            _speedKbps = speedKbps;
        }

        public bool Connect()
        {
            if (_isConnected)
                return true;

            uint devCount = 0;
            var ftStatus = FT_CreateDeviceInfoList(ref devCount);
            if (ftStatus != FT_STATUS.FT_OK || devCount == 0 || _deviceIndex >= devCount)
                return false;

            ftStatus = FT_Open(_deviceIndex, out _handle);
            if (ftStatus != FT_STATUS.FT_OK || _handle == IntPtr.Zero)
                return false;

            var ft4222Status = FT4222_I2CMaster_Init(_handle, _speedKbps);
            if (ft4222Status != FT4222_STATUS.FT4222_OK)
            {
                FT_Close(_handle);
                _handle = IntPtr.Zero;
                return false;
            }

            _isConnected = true;
            return true;
        }

        public void Disconnect()
        {
            if (_handle != IntPtr.Zero)
            {
                FT_Close(_handle);
                _handle = IntPtr.Zero;
            }

            _isConnected = false;
        }

        public void WriteBytes(byte[] data)
        {
            if (!_isConnected)
                throw new InvalidOperationException("FT4222 not connected.");

            ushort transferred = 0;
            var status = FT4222_I2CMaster_Write(
                _handle,
                _slaveAddress,
                data,
                (ushort)data.Length,
                ref transferred);

            if (status != FT4222_STATUS.FT4222_OK || transferred != data.Length)
                throw new InvalidOperationException($"I2C Write failed: {status}, transferred={transferred}");
        }

        public byte[] ReadBytes(int length)
        {
            if (!_isConnected)
                throw new InvalidOperationException("FT4222 not connected.");

            byte[] buffer = new byte[length];
            ushort transferred = 0;

            var status = FT4222_I2CMaster_Read(
                _handle,
                _slaveAddress,
                buffer,
                (ushort)buffer.Length,
                ref transferred);

            if (status != FT4222_STATUS.FT4222_OK || transferred != buffer.Length)
                throw new InvalidOperationException($"I2C Read failed: {status}, transferred={transferred}");

            return buffer;
        }

        public void Dispose()
        {
            Disconnect();
        }

        private enum FT_STATUS : uint
        {
            FT_OK = 0,
            FT_INVALID_HANDLE,
            FT_DEVICE_NOT_FOUND,
            FT_DEVICE_NOT_OPENED,
            FT_IO_ERROR,
            FT_INSUFFICIENT_RESOURCES,
            FT_INVALID_PARAMETER,
            FT_INVALID_BAUD_RATE,
            FT_DEVICE_NOT_OPENED_FOR_ERASE,
            FT_DEVICE_NOT_OPENED_FOR_WRITE,
            FT_FAILED_TO_WRITE_DEVICE,
            FT_EEPROM_READ_FAILED,
            FT_EEPROM_WRITE_FAILED,
            FT_EEPROM_ERASE_FAILED,
            FT_EEPROM_NOT_PRESENT,
            FT_EEPROM_NOT_PROGRAMMED,
            FT_INVALID_ARGS,
            FT_OTHER_ERROR
        }

        [DllImport("ftd2xx.dll")]
        private static extern FT_STATUS FT_CreateDeviceInfoList(ref uint numDevs);

        [DllImport("ftd2xx.dll")]
        private static extern FT_STATUS FT_Open(uint index, out IntPtr handle);

        [DllImport("ftd2xx.dll")]
        private static extern FT_STATUS FT_Close(IntPtr handle);

        private enum FT4222_STATUS
        {
            FT4222_OK = 0,
        }

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_I2CMaster_Init(
            IntPtr ftHandle,
            ushort kbps);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_I2CMaster_Read(
            IntPtr ftHandle,
            ushort deviceAddress,
            [Out] byte[] buffer,
            ushort sizeToTransfer,
            ref ushort sizeTransferred);

        [DllImport("LibFT4222-64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern FT4222_STATUS FT4222_I2CMaster_Write(
            IntPtr ftHandle,
            ushort deviceAddress,
            byte[] buffer,
            ushort sizeToTransfer,
            ref ushort sizeTransferred);
    }
}
