namespace JobBoard.AI.Domain.AI;

public sealed record EmbeddingVector(float[] Values)
{
    public int Dimensions => Values.Length;
}