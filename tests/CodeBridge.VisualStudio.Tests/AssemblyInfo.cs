// WPF XAML loading (Application.LoadComponent) is not safe to run from several test threads at once;
// inside Visual Studio all of this runs on the single UI thread.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
