using Microsoft.Maui.Controls;

namespace MyPressureRecorder.Pages;

public class BaseContentPage : ContentPage
{
    public BaseContentPage()
    {
        var background = new Image
        {
            Source = "galaxy_bg.png",
            Aspect = Aspect.AspectFill,
            Opacity = 0.25,
            InputTransparent = true
        };

        var mainLayout = new Grid();

        // Sfondo (livello 0)
        mainLayout.Children.Add(background);

        // Contenuto dinamico (livello 1)
        var contentPresenter = new ContentPresenter();
        mainLayout.Children.Add(contentPresenter);

        base.Content = mainLayout;
    }

    public new View Content
    {
        get => (View)((Grid)base.Content).Children[1];
        set => ((Grid)base.Content).Children[1] = value;
    }

}
