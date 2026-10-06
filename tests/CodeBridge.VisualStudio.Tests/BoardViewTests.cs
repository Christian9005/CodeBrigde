using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodeBridge.VisualStudio.Editor;

namespace CodeBridge.VisualStudio.Tests;

public sealed class BoardViewTests
{
    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); } catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null)
            throw new InvalidOperationException(failure.ToString());
    }

    private static BoardViewControl Esp32()
    {
        var view = new BoardViewControl();
        var pins = new[] { 0, 2, 4, 13, 16, 21, 25, 26, 34, 35 }.Select(n => (n, $"GPIO {n}")).ToList();
        view.SetBoard("ESP32 DevKit", "ESP32", pins, ledPin: 2, analogMax: 4095);
        return view;
    }

    [Fact]
    public void Every_pin_of_the_board_is_drawn_and_idle()
    {
        RunSta(() =>
        {
            var view = Esp32();

            Assert.Equal(10, view.PadCount);
            Assert.Equal(string.Empty, view.PadText(2));
            Assert.Equal(0, view.LedBrightness, 3);
        });
    }

    [Fact]
    public void Outputs_light_up_and_the_built_in_LED_follows_its_pin()
    {
        RunSta(() =>
        {
            var view = Esp32();

            view.Apply(2, "mode", 1);
            Assert.Equal("OUT", view.PadText(2));

            view.Apply(2, "digital", 1);
            Assert.Equal("HIGH", view.PadText(2));
            Assert.Equal(1.0, view.PadFill(2), 3);
            Assert.Equal(1.0, view.LedBrightness, 3);

            view.Apply(2, "digital", 0);
            Assert.Equal("LOW", view.PadText(2));
            Assert.Equal(0.0, view.LedBrightness, 3);

            view.Apply(13, "digital", 1); // another pin does not touch the built-in LED
            Assert.Equal(0.0, view.LedBrightness, 3);
        });
    }

    [Fact]
    public void Pwm_servo_analog_and_tone_each_show_their_own_value()
    {
        RunSta(() =>
        {
            var view = Esp32();

            view.Apply(2, "pwm", 128);
            Assert.Equal("128", view.PadText(2));
            Assert.Equal(128 / 255.0, view.PadFill(2), 3);
            Assert.Equal(128 / 255.0, view.LedBrightness, 3); // the LED dims with the duty

            view.Apply(13, "servo", 90);
            Assert.Equal("90°", view.PadText(13));
            Assert.Equal(0.5, view.PadFill(13), 3);

            view.Apply(34, "analog", 2048);
            Assert.Equal("2048", view.PadText(34));
            Assert.Equal(2048 / 4095.0, view.PadFill(34), 3);

            view.Apply(16, "tone", 440);
            Assert.Equal("♪", view.PadText(16));
            view.Apply(16, "tone", 0);
            Assert.Equal("off", view.PadText(16));

            view.Apply(4, "input", 1);
            Assert.Equal("IN 1", view.PadText(4));
        });
    }

    [Fact]
    public void A_pin_the_catalog_hides_still_appears_when_a_flow_touches_it_and_reset_clears_everything()
    {
        RunSta(() =>
        {
            var view = Esp32();

            view.Apply(1, "digital", 1); // GPIO 1 (UART TX) is not in the list
            Assert.Equal(11, view.PadCount);
            Assert.Equal("HIGH", view.PadText(1));

            view.Apply(2, "digital", 1);
            view.Reset();

            Assert.Equal(string.Empty, view.PadText(1));
            Assert.Equal(string.Empty, view.PadText(2));
            Assert.Equal(0.0, view.LedBrightness, 3);
        });
    }

    [Fact]
    public void Activity_is_described_in_plain_language()
    {
        Assert.Equal("GPIO 2 → HIGH", BoardViewControl.Describe(2, "digital", 1));
        Assert.Equal("GPIO 4 PWM 128/255", BoardViewControl.Describe(4, "pwm", 128));
        Assert.Equal("GPIO 13 servo → 90°", BoardViewControl.Describe(13, "servo", 90));
        Assert.Equal("GPIO 2: set as output", BoardViewControl.Describe(2, "mode", 1));
        Assert.Null(BoardViewControl.Describe(2, "unknown", 1));
    }

    [Fact]
    public void The_view_renders_to_an_image()
    {
        RunSta(() =>
        {
            var view = Esp32();
            view.SetSource("Simulator (virtual board)");
            view.Apply(2, "pwm", 180);
            view.Apply(13, "servo", 120);
            view.Apply(34, "analog", 3000);
            view.Apply(4, "input", 1);
            view.Apply(21, "digital", 1);

            var host = new System.Windows.Controls.Border { Width = 280, Height = 360, Background = new SolidColorBrush(Color.FromRgb(37, 37, 38)), Child = view };
            host.Measure(new Size(280, 360));
            host.Arrange(new Rect(0, 0, 280, 360));
            host.UpdateLayout();

            var bitmap = new RenderTargetBitmap(280, 360, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(host);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);

            Assert.True(stream.Length > 2000);
            var path = Environment.GetEnvironmentVariable("CODEBRIDGE_BOARDVIEW_PNG");
            if (!string.IsNullOrEmpty(path))
                File.WriteAllBytes(path, stream.ToArray());
        });
    }
}
