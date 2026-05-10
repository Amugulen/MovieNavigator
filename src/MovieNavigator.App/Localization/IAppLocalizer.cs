namespace MovieNavigator.App.Localization;

public interface IAppLocalizer
{
    string CultureName { get; }
    string Get(string key);
}
