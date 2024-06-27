namespace Battlebugs;

public sealed class VoiceComponent : Voice
{
    [Property] bool IsSpectator { get; set; } = false;

    protected override IEnumerable<Connection> ExcludeFilter()
    {
        if ( !IsSpectator ) return Enumerable.Empty<Connection>();

        var connections = Connection.All.ToList();
        var boards = Scene.GetAllComponents<BoardManager>();
        for ( int i = connections.Count - 1; i >= 0; i-- )
        {
            if ( !boards.Any( x => x.Network.OwnerId == connections[i].Id ) )
            {
                connections.RemoveAt( i );
            }
        }
        return connections;
    }
}