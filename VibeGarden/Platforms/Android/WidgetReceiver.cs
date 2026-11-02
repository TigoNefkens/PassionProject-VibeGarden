using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using System.IO;

namespace VibeGardeb.Platforms.Android;

[BroadcastReceiver(
    Name = "com.example.mauiwidgetdemo.MyWidgetReceiver",
    Exported = true)]
[IntentFilter(new[]
{
    AppWidgetManager.ActionAppwidgetUpdate
})]
[MetaData(
    "android.appwidget.provider",
    Resource = "@xml/my_widget_info")]
public class MyWidgetReceiver : AppWidgetProvider
{
    public override void OnUpdate(
        Context context,
        AppWidgetManager appWidgetManager,
        int[] appWidgetIds)
    {
        foreach (int widgetId in appWidgetIds)
        {
            UpdateWidget(
                context,
                appWidgetManager,
                widgetId);
        }
    }

    public static void UpdateWidget(
        Context context,
        AppWidgetManager appWidgetManager,
        int widgetId)
    {
        var views = new RemoteViews(
            context.PackageName,
            Resource.Layout.garden_widget);

        string path = System.IO.Path.Combine(
            context.FilesDir.AbsolutePath,
            "flowerpot_snapshot.png");

        if (File.Exists(path))
        {
            using FileStream stream = File.OpenRead(path);

            Bitmap bitmap =
                BitmapFactory.DecodeStream(stream);

            if (bitmap != null)
            {
                views.SetImageViewBitmap(
                    Resource.Id.widget_flowerpot,
                    bitmap);
            }
        }

        // Widget klikbaar maken
        var intent = new Intent(
            context,
            typeof(MainActivity));

        var pendingIntent = PendingIntent.GetActivity(
            context,
            widgetId,
            intent,
            PendingIntentFlags.UpdateCurrent |
            PendingIntentFlags.Immutable);

        views.SetOnClickPendingIntent(
            Resource.Id.widget_root,
            pendingIntent);

        appWidgetManager.UpdateAppWidget(
            widgetId,
            views);
    }
}