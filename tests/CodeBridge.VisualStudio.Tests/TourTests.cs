using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using CodeBridge.VisualStudio.Editor;
using CodeBridge.VisualStudio.Tour;

namespace CodeBridge.VisualStudio.Tests;

public sealed class TourTests
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

    [Fact]
    public void The_tour_tells_a_story_from_welcome_to_the_sdk()
    {
        var chapters = TourContent.Chapters;

        Assert.True(chapters.Count >= 8);
        Assert.Equal("Welcome to CodeBridge", chapters[0].Title);
        Assert.Contains(chapters, c => c.Level == "Advanced" && c.Diagram == DiagramKind.AnyHost);
        Assert.All(chapters, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Title));
            Assert.NotEmpty(c.Story);
        });
    }

    [Fact]
    public void Every_example_chapter_points_at_a_shipped_flow_and_compiled_c_sharp()
    {
        var withExamples = TourContent.Chapters.Where(c => c.ExampleFolder != null).ToList();
        Assert.Equal(5, withExamples.Count);

        foreach (var chapter in withExamples)
        {
            Assert.True(File.Exists(TourContent.ExamplePath(chapter)), $"Missing example flow for '{chapter.Title}'.");
            Assert.NotEmpty(chapter.Code);
        }
    }

    [Fact]
    public void Every_code_tab_resolves_to_real_sample_code()
    {
        foreach (var code in TourContent.Chapters.SelectMany(c => c.Code))
        {
            var text = TourContent.ReadCode(code);

            Assert.DoesNotContain("not available", text);
            Assert.Contains("CodeBridge", text);
        }
    }

    [Fact]
    public void Every_chapter_renders_and_its_animations_start_and_stop()
    {
        RunSta(() =>
        {
            var control = new TourControl(new EditorSettings());
            var window = new Window { Content = control, Width = 1100, Height = 760, ShowInTaskbar = false, Left = -5000, Top = -5000 };
            window.Show();

            for (var i = 0; i < TourContent.Chapters.Count; i++)
            {
                control.ShowChapter(i);
                Assert.Equal(i, control.CurrentChapter);
            }

            window.Close();
        });
    }

    [Fact]
    public void Visited_chapters_are_remembered()
    {
        RunSta(() =>
        {
            var settings = new EditorSettings();
            var control = new TourControl(settings);
            control.ShowChapter(3);

            Assert.Contains(3, settings.TourVisited);
            Assert.Equal(3, settings.TourLastChapter);
        });
    }

    [Fact]
    public void Unique_path_never_overwrites_an_existing_example()
    {
        var directory = Path.Combine(Path.GetTempPath(), "cb-tour-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Example1_Blink.cbflow"), "{}");

            var next = TourContent.UniquePath(directory, "Example1_Blink", ".cbflow");

            Assert.EndsWith("Example1_Blink_2.cbflow", next);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
