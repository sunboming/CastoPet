using CastoPet.Core.Settings;

namespace CastoPet.Application.Settings;

public interface ISettingsStore
{
    AppSettings Load();

    bool Save(AppSettings settings);
}
