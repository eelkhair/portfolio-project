


namespace JobBoard.Application.Interfaces.Configurations;

public sealed class Unit : IEquatable<Unit>
{
    public static readonly Unit Value = new();


    public bool Equals(Unit? other)
    { 
        return true;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Unit;
    }


    public override int GetHashCode()
    {
        return 0;
    }
    
    public override string ToString()
    {
        return "()";
    }

    public static bool operator ==(Unit? first, Unit? second)
    {
        return first is null ? second is null : second is not null;
    }
    
    public static bool operator !=(Unit? first, Unit? second)
    {
        return !(first == second);
    }
}