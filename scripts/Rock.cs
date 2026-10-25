// Rock: 돌덩이 하나. 남은 돌 개수, 드릴이 붙어 있는지, 폭탄이 설치됐는지를 가진다.
using Godot;

public partial class Rock : Node2D
{
	public const int StonesPerRock = 10;
	public int Stones = StonesPerRock;
	public bool Drilling;
	public bool Bombed;
	public Drill AttachedDrill;
}
