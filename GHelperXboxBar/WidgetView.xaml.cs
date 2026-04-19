using System;
using System.Threading.Tasks;
using Microsoft.Gaming.XboxGameBar;
using Windows.ApplicationModel;
using Windows.Devices.Power;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace GHelperXboxBar
{
    public sealed partial class WidgetView : Page
    {
        private XboxGameBarWidget? _widget;
        private DispatcherTimer? _batteryTimer;
        private readonly Battery _battery = Battery.AggregateBattery;

        private static readonly SolidColorBrush DischargeBrush =
            new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x8A, 0x80));
        private static readonly SolidColorBrush ChargeBrush =
            new SolidColorBrush(Color.FromArgb(0xFF, 0x81, 0xC7, 0x84));
        private static readonly SolidColorBrush IdleBrush =
            new SolidColorBrush(Color.FromArgb(0xFF, 0xA0, 0xA0, 0xA0));

        public WidgetView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _widget = e.Parameter as XboxGameBarWidget;
            StatusText.Text = "Ready";
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _battery.ReportUpdated += OnBatteryReportUpdated;
            _batteryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _batteryTimer.Tick += (_, __) => UpdateBattery();
            _batteryTimer.Start();
            UpdateBattery();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _battery.ReportUpdated -= OnBatteryReportUpdated;
            if (_batteryTimer != null)
            {
                _batteryTimer.Stop();
                _batteryTimer = null;
            }
        }

        private async void OnBatteryReportUpdated(Battery sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, UpdateBattery);
        }

        private void UpdateBattery()
        {
            try
            {
                var report = _battery.GetReport();
                var rateMw = report.ChargeRateInMilliwatts;
                var remaining = report.RemainingCapacityInMilliwattHours;
                var full = report.FullChargeCapacityInMilliwattHours;

                string pct = (remaining.HasValue && full.HasValue && full.Value > 0)
                    ? $"{(int)Math.Round(remaining.Value * 100.0 / full.Value)}%"
                    : string.Empty;

                if (!rateMw.HasValue || rateMw.Value == 0)
                {
                    BatteryText.Foreground = IdleBrush;
                    BatteryText.Text = string.IsNullOrEmpty(pct) ? "—" : pct;
                    return;
                }

                double watts = rateMw.Value / 1000.0;
                if (watts > 0)
                {
                    BatteryText.Foreground = ChargeBrush;
                    BatteryText.Text = string.IsNullOrEmpty(pct)
                        ? $"▲ {watts:0.0} W"
                        : $"{pct}  ▲ {watts:0.0} W";
                }
                else
                {
                    BatteryText.Foreground = DischargeBrush;
                    BatteryText.Text = string.IsNullOrEmpty(pct)
                        ? $"▼ {Math.Abs(watts):0.0} W"
                        : $"{pct}  ▼ {Math.Abs(watts):0.0} W";
                }
            }
            catch
            {
                BatteryText.Text = string.Empty;
            }
        }

        private async void OnProfileClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is null) return;
            var tag = btn.Tag.ToString() ?? string.Empty;
            await InvokeHotkeyAsync(tag, btn.Content?.ToString() ?? tag);
        }

        private async void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            await InvokeHotkeyAsync("reload", "Reload config");
        }

        private async Task InvokeHotkeyAsync(string action, string label)
        {
            try
            {
                this.IsEnabled = false;
                StatusText.Text = $"{label}…";

                // Pass the requested action to the desktop extension via LocalSettings.
                var settings = ApplicationData.Current.LocalSettings;
                settings.Values["action"] = action;
                settings.Values["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

                StatusText.Text = $"{label} ✓";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error: " + ex.Message;
            }
            finally
            {
                this.IsEnabled = true;
            }
        }
    }
}
