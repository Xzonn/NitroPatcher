using CommunityToolkit.Maui;

namespace NitroPatcherMaui
{
  public static class MauiProgram
  {
    public static MauiApp CreateMauiApp()
    {
      var builder = MauiApp.CreateBuilder();
      builder
        .UseMauiCommunityToolkit()
        .UseMauiApp<App>();

#if DEBUG
  		builder.Logging.AddDebug();
#endif

      return builder.Build();
    }
  }
}
