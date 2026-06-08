#nullable disable
namespace InferiusQoL.Features.InventoryStacking;

internal sealed class StackRestorePlayerPage
{
	public const int GridWidth = 6;

	public const int FirstPageHeight = 4;

	private readonly StackRestoreSlot[,] _cells = new StackRestoreSlot[6, 4];

	public StackRestoreSlot Get(int x, int y)
	{
		if (x < 0 || x >= 6 || y < 0 || y >= 4)
		{
			return StackRestoreSlot.Empty;
		}
		StackRestoreSlot result = _cells[x, y];
		if (!result.IsEmpty)
		{
			return result;
		}
		return StackRestoreSlot.Empty;
	}

	public void Set(int x, int y, TechType tech, int count)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (x >= 0 && x < 6 && y >= 0 && y < 4 && (int)tech != 0 && count >= 1)
		{
			_cells[x, y] = new StackRestoreSlot(tech, count);
		}
	}
}
