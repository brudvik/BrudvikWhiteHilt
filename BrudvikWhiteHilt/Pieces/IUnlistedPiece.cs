namespace BrudvikWhiteHilt.Pieces;

/// <summary>
/// A piece the mod places itself and players never build, such as the parts of a trading post. It is not on the
/// hammer and has no [Content], [Recipes] or [Tiers] entries.
/// </summary>
public interface IUnlistedPiece : IWhiteHiltCustomPiece
{
}
