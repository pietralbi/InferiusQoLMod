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
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			if ((int)Tech != 0)
			{
				return Count < 1;
			}
			return true;
		}
	}

	public StackRestoreSlot(TechType tech, int count)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		Tech = tech;
		Count = count;
	}

	public bool SameStackAs(StackRestoreSlot other)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (Tech == other.Tech)
		{
			return Count == other.Count;
		}
		return false;
	}
}
