namespace Sandbox.Player;

public class PlayerData
{
	public int WoodcuttingLevel { get; set; } = 1;
	public int MiningLevel { get; set; } = 1;
	public long Wood { get; set; } = 0;
	public long Rocks { get; set; } = 0;
	
	public static void Save( PlayerData data )
	{
		FileSystem.Data.WriteJson( "player_data.json", data );
	}

	public static PlayerData Load()
	{
		return FileSystem.Data.ReadJson<PlayerData>( "player_data.json" );
	}
}
