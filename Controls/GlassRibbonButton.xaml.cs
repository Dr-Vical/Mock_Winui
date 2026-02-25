using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace RswareDesign.Controls;

public sealed partial class GlassRibbonButton : UserControl
{
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(GlassRibbonButton),
            new PropertyMetadata("\uE8A5", OnGlyphChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(GlassRibbonButton),
            new PropertyMetadata("", OnLabelChanged));

    public static readonly DependencyProperty IsAccentProperty =
        DependencyProperty.Register(nameof(IsAccent), typeof(bool), typeof(GlassRibbonButton),
            new PropertyMetadata(true));

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsAccent
    {
        get => (bool)GetValue(IsAccentProperty);
        set => SetValue(IsAccentProperty, value);
    }

    public GlassRibbonButton()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            IconElement.Glyph = Glyph;
            LabelElement.Text = Label;
        };
    }

    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassRibbonButton btn && btn.IconElement != null)
            btn.IconElement.Glyph = (string)e.NewValue;
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassRibbonButton btn && btn.LabelElement != null)
            btn.LabelElement.Text = (string)e.NewValue;
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        RootButton.Background = (SolidColorBrush)Application.Current.Resources["GlassHoverBrush"];
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        RootButton.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }
}
