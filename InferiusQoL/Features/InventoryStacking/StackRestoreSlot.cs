#nullable disable
namespace InferiusQoL.Features.InventoryStacking;

internal readonly struct StackRestoreSlot
{
	public static readonly StackRestoreSlot Empty = new StackRestoreSlot((TechType)0, 0);

	public readonly TechType Tech;

	public readonly int Count;

	public bool IsEmpty
	{
		get
		{
			if ((int)Tech != 0)
			{
				return Count < 1;
			}
			return true;
		}
	}

	public StackRestoreSlot(TechType tech, int count)
	{
		Tech = tech;
		Count = count;
	}

	public bool SameStackAs(StackRestoreSlot other)
	{
		if (Tech == other.Tech)
		{
			return Count == other.Count;
		}
		return false;
	}
}
