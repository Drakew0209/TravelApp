using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps.Handlers;

namespace TravelApp.Handlers;

public static class MapPinAppearanceHandler
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;

        MapPinHandler.Mapper.AppendToMapping("TravelAppLuxuryMarkers", (handler, mapPin) =>
        {
            if (mapPin is not Pin pin)
            {
                return;
            }

#if ANDROID
            ApplyAndroidStyle(handler.PlatformView, pin);
#elif IOS || MACCATALYST
            ApplyAppleStyle(handler.PlatformView, pin);
#endif
        });
    }

#if ANDROID
    private static void ApplyAndroidStyle(object? platformView, Pin pin)
    {
        if (platformView is null)
        {
            return;
        }

        var hue = pin.Type == PinType.Place
            ? Android.Gms.Maps.Model.BitmapDescriptorFactory.HueAzure
            : Android.Gms.Maps.Model.BitmapDescriptorFactory.HueRose;

        var icon = Android.Gms.Maps.Model.BitmapDescriptorFactory.DefaultMarker(hue);
        var type = platformView.GetType();

        var iconProperty = type.GetProperty("Icon");
        if (iconProperty?.CanWrite == true)
        {
            iconProperty.SetValue(platformView, icon);
            return;
        }

        var setIcon = type.GetMethod("SetIcon", [typeof(Android.Gms.Maps.Model.BitmapDescriptor)]);
        setIcon?.Invoke(platformView, [icon]);
    }
#endif

#if IOS || MACCATALYST
    private static void ApplyAppleStyle(object? platformView, Pin pin)
    {
        if (platformView is null)
        {
            return;
        }

        var color = pin.Type == PinType.Place
            ? UIKit.UIColor.FromRGB(28, 98, 114)
            : UIKit.UIColor.FromRGB(227, 22, 103);

        var type = platformView.GetType();

        var tintProperty = type.GetProperty("MarkerTintColor");
        if (tintProperty?.CanWrite == true)
        {
            tintProperty.SetValue(platformView, color);
        }

        var glyphTextProperty = type.GetProperty("GlyphText");
        if (glyphTextProperty?.CanWrite == true)
        {
            glyphTextProperty.SetValue(platformView, " ");
        }
    }
#endif
}
