using System.Windows.Controls;

namespace PhotoPilot.App.Views.Controls;

/// <summary>
/// Displays one photo or video from the PhotoPilot media library.
/// The control receives a LibraryCardItem through its DataContext.
/// </summary>
public partial class LibraryCardControl : UserControl
{
    public LibraryCardControl()
    {
        InitializeComponent();
    }
}