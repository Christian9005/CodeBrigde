using CodeBridge.Hosting;
using Microsoft.Extensions.Options;

namespace CodeBridge.Samples.IntegrationMaui;

public partial class MainPage : ContentPage
{
	private readonly BoardService _board;
	private readonly CodeBridgeOptions _options;
	private readonly IDispatcherTimer _timer;

	public MainPage(BoardService board, IOptions<CodeBridgeOptions> options)
	{
		InitializeComponent();
		_board = board;
		_options = options.Value;

		// Show the connection state whenever it changes (the event can come from any thread).
		_board.Changed += () => Dispatcher.Dispatch(RefreshStatus);

		// The light sensor is polled twice a second while the page is visible.
		_timer = Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromMilliseconds(500);
		_timer.Tick += async (_, _) => await ReadLightAsync();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_timer.Start();
	}

	protected override void OnDisappearing()
	{
		_timer.Stop();
		base.OnDisappearing();
	}

	private async void OnConnectClicked(object? sender, EventArgs e)
	{
		// Async void handlers must never let an exception escape: it would close the app.
		try
		{
			_options.Port = string.IsNullOrWhiteSpace(AddressEntry.Text) ? "simulator" : AddressEntry.Text.Trim();
			_options.AccessToken = string.IsNullOrWhiteSpace(TokenEntry.Text) ? null : TokenEntry.Text.Trim();

			await _board.DisconnectAsync();
			await _board.ConnectAsync();
		}
		catch (Exception)
		{
			// the reason is shown through BoardService.LastError
		}

		RefreshStatus();
	}

	private async void OnLedToggled(object? sender, ToggledEventArgs e)
	{
		await Guard(() => _board.WriteDigitalAsync(2, e.Value));
	}

	private async void OnLampChanged(object? sender, ValueChangedEventArgs e)
	{
		LampLabel.Text = $"Lamp brightness (pin 4): {(int)e.NewValue}";
		await Guard(() => _board.WritePwmAsync(4, (int)e.NewValue));
	}

	private async Task ReadLightAsync()
	{
		if (!_board.IsConnected)
			return;

		try
		{
			var raw = await _board.ReadAnalogAsync(34);
			LightBar.Progress = Math.Clamp(raw / (double)_board.AnalogMax, 0, 1);
			LightLabel.Text = $"Light sensor (pin 34): {raw * 100 / _board.AnalogMax}%";
		}
		catch (Exception)
		{
			// a dropped connection is reported by the board service; the next tick tries again
		}
	}

	private async Task Guard(Func<Task> action)
	{
		try
		{
			await action();
		}
		catch (Exception ex)
		{
			DetailLabel.Text = ex.Message;
		}
	}

	private void RefreshStatus()
	{
		var connected = _board.IsConnected;
		StatusDot.Color = _board.State switch
		{
			BoardConnectionState.Connected => Colors.SeaGreen,
			BoardConnectionState.Connecting => Colors.DodgerBlue,
			BoardConnectionState.Faulted => Colors.IndianRed,
			_ => Colors.Gray
		};
		StatusLabel.Text = _board.State switch
		{
			BoardConnectionState.Connected => (_board.IsSimulated ? "Simulator" : "Connected") + " · " + (_board.Info?.ChipModel ?? "board"),
			BoardConnectionState.Connecting => "Connecting...",
			BoardConnectionState.Faulted => "Connection lost",
			_ => "Not connected"
		};
		DetailLabel.Text = connected ? $"{_board.Target} · firmware {_board.FirmwareVersion}" : _board.LastError ?? string.Empty;
		LedSwitch.IsEnabled = LampSlider.IsEnabled = connected;
	}
}
