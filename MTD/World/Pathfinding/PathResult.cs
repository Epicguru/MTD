namespace MTD.World.Pathfinding
{
    public enum PathResult
    {
        SUCCESS,
        ERROR_INTERNAL,
        ERROR_START_IS_END,
        ERROR_END_IS_UNWALKABLE,
        ERROR_PATH_TOO_LONG,
        ERROR_PATH_NOT_FOUND
    }
}
