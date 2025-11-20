using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading; // Added for DispatcherTimer
using Wcs.Monitor.Models;
using Wcs.Monitor.Services;

namespace Wcs.Monitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _apiBaseAddress = "http://localhost:5000";
        private bool _isBusy;
        private bool _isConnected;
        private DispatcherTimer _temperatureTimer; // Added

        public ObservableCollection<EquipmentStatusDto> Statuses { get; } =
            new ObservableCollection<EquipmentStatusDto>();

        public ObservableCollection<TemperatureReadingDto> Temperatures { get; } =
            new ObservableCollection<TemperatureReadingDto>();

        public string ApiBaseAddress
        {
            get { return _apiBaseAddress; }
            set
            {
                if (_apiBaseAddress != value)
                {
                    _apiBaseAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    RaiseAllCanExecuteChanged();
                }
            }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    RaiseAllCanExecuteChanged();
                }
            }
        }

        public ICommand ConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(
                async _ => await ConnectAsync(),
                _ => !IsBusy && !IsConnected);

            DisconnectCommand = new RelayCommand(
                _ => Disconnect(),
                _ => !IsBusy && IsConnected);

            RefreshCommand = new RelayCommand(
                async _ => await LoadDataAsync(),
                _ => !IsBusy && IsConnected);

            // Initialize the timer
            _temperatureTimer = new DispatcherTimer();
            _temperatureTimer.Interval = TimeSpan.FromSeconds(3); // Poll every 3 seconds
            _temperatureTimer.Tick += async (s, e) => await LoadTemperatureDataAsync();

            // 자동으로 시작할 때 연결하고 싶으면 주석 해제
            // _ = ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(ApiBaseAddress))
            {
                MessageBox.Show("WCS API 주소를 입력하세요.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsBusy = true;
            try
            {
                using (var client = new WcsApiClient(ApiBaseAddress))
                {
                    var list = await client.GetEquipmentStatusesAsync();

                    Statuses.Clear();
                    foreach (var item in list)
                    {
                        Statuses.Add(item);
                    }
                }

                IsConnected = true;
                _temperatureTimer.Start(); // Start the timer on connect
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "연결 중 오류가 발생했습니다.\n" + ex.Message,
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                IsConnected = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Disconnect()
        {
            IsConnected = false;
            _temperatureTimer.Stop(); // Stop the timer on disconnect
            // 연결 끊을 때 화면도 비우고 싶으면 주석 해제
            // Statuses.Clear();
            // Temperatures.Clear(); // Clear temperatures on disconnect
        }

        private async Task LoadDataAsync()
        {
            if (!IsConnected)
            {
                MessageBox.Show("먼저 연결을 해주세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiBaseAddress))
            {
                MessageBox.Show("WCS API 주소를 입력하세요.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsBusy = true;
            try
            {
                using (var client = new WcsApiClient(ApiBaseAddress))
                {
                    var list = await client.GetEquipmentStatusesAsync();

                    Statuses.Clear();
                    foreach (var item in list)
                    {
                        Statuses.Add(item);
                    }
                    // Also load temperature data when refreshing
                    await LoadTemperatureDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "데이터 조회 중 오류가 발생했습니다.\n" + ex.Message,
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // New method to load temperature data
        private async Task LoadTemperatureDataAsync()
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(ApiBaseAddress))
            {
                return;
            }

            try
            {
                using (var client = new WcsApiClient(ApiBaseAddress))
                {
                    var readings = await client.GetTemperatureReadingsAsync(10); // Get last 10 readings

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Temperatures.Clear();
                        foreach (var item in readings)
                        {
                            Temperatures.Add(item);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Log or display error without interrupting the periodic update
                Console.WriteLine($"Error loading temperature data: {ex.Message}");
            }
        }


        private void RaiseAllCanExecuteChanged()
        {
            var c1 = ConnectCommand as RelayCommand;
            var c2 = DisconnectCommand as RelayCommand;
            var c3 = RefreshCommand as RelayCommand;

            if (c1 != null) c1.RaiseCanExecuteChanged();
            if (c2 != null) c2.RaiseCanExecuteChanged();
            if (c3 != null) c3.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
