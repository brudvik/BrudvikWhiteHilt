namespace BrudvikWhiteHilt.Items;

/// <summary>
/// An item whose gameplay values come from the config and can be applied again when it changes or is synced from the server.
/// </summary>
public interface IWhiteHiltConfigurable
{
    /// <summary>
    /// Applies the configured values. Does nothing before the item is added.
    /// </summary>
    void ApplyConfig();
}
