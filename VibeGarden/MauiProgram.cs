using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Microsoft.Maui.Controls.Platform;

#if ANDROID
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Graphics;

#endif

namespace VibeGardeb
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();



            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Minecraft.ttf", "Minecraft");
                })
                .UseSkiaSharp();
            
#if DEBUG
            builder.Logging.AddDebug();


#endif

            Microsoft.Maui.Handlers.ImageButtonHandler.Mapper.AppendToMapping("NoRipple", (handler, view) =>
            {
#if ANDROID
    if (handler.PlatformView.Background is Android.Graphics.Drawables.RippleDrawable ripple)
    {
        ripple.SetColor(
            Android.Content.Res.ColorStateList.ValueOf(
                Android.Graphics.Color.Transparent));
    }
#endif
            });

            return builder.Build();
        }


    }
}